#if !NET40
using System.Collections.Generic;
using System.Linq;

namespace System.Threading.Tasks
{
    /// <summary>
    /// Represents an asynchronous operation.
    /// </summary>
    public class Task : IAsyncResult, IDisposable
    {
        private static Task _completedTask; // A task that's already been completed successfully.
        private static readonly TaskFactory _defaultFactory = new TaskFactory();
        private static int _taskIdCounter;  // static counter used to generate unique task IDs

        /// <summary>
        /// The body of the task. Might be <see cref="Action"/>,
        /// <see cref="Action{T}"/>, <see cref="Func{TResult}"/> or
        /// <see cref="Func{T, TResult}"/>.
        /// </summary>
        protected readonly Delegate _action;
        /// <summary>
        /// A state object that can be optionally supplied, passed to action.
        /// </summary>
        protected readonly object _stateObject;


        /// <summary>
        /// A set of exceptions occurred when trying to execute current task.
        /// </summary>
        protected readonly List<System.Exception> _exceptions = new List<System.Exception>();
        /// <summary>
        /// A thread-safe event which notifies that current task is completed its execution.
        /// </summary>
        protected readonly ManualResetEvent _taskCompletedEvent;
        private bool _runSync;
        // this task's unique ID. initialized only if it is ever requested
        private int _taskId;


        /// <summary>
        /// The source which current task continues
        /// </summary>
        protected readonly Task _continueSource;


        private volatile int _stateFlags;

        // State constants for _stateFlags;
        // The bits of _stateFlags are allocated as follows:
        //   0x40000000 - TaskBase state flag
        //   0x3FFF0000 - Task state flags
        //   0x0000FF00 - internal TaskCreationOptions flags
        //   0x000000FF - publicly exposed TaskCreationOptions flags
        //
        // See TaskCreationOptions for bit values associated with TaskCreationOptions
        //
        private const int OptionsMask = 0xFFFF; // signifies the Options portion of _stateFlags  bin: 0000 0000 0000 0000 1111 1111 1111 1111
        internal const int TASK_STATE_STARTED = 0x10000;                                       //bin: 0000 0000 0000 0001 0000 0000 0000 0000
        internal const int TASK_STATE_DELEGATE_INVOKED = 0x20000;                              //bin: 0000 0000 0000 0010 0000 0000 0000 0000
        internal const int TASK_STATE_DISPOSED = 0x40000;                                      //bin: 0000 0000 0000 0100 0000 0000 0000 0000
        internal const int TASK_STATE_FAULTED = 0x200000;                                      //bin: 0000 0000 0010 0000 0000 0000 0000 0000
        internal const int TASK_STATE_CANCELED = 0x400000;                                     //bin: 0000 0000 0100 0000 0000 0000 0000 0000
        internal const int TASK_STATE_RAN_TO_COMPLETION = 0x1000000;                           //bin: 0000 0001 0000 0000 0000 0000 0000 0000
        internal const int TASK_STATE_WAITINGFORACTIVATION = 0x2000000;                        //bin: 0000 0010 0000 0000 0000 0000 0000 0000

        // A mask for all of the final states a task may be in
        private const int TASK_STATE_COMPLETED_MASK = TASK_STATE_CANCELED | TASK_STATE_FAULTED | TASK_STATE_RAN_TO_COMPLETION;


        #region Properties

        /// <summary>
        /// Gets a task that's already been completed successfully.
        /// </summary>
        /// <remarks>
        /// May not always return the same instance.
        /// </remarks>
        public static Task CompletedTask
        {
            get
            {
                var completedTask = _completedTask;
                if (completedTask == null)
                    _completedTask = completedTask = new Task((Exception)null); // lazy initialization
                return completedTask;
            }
        }

        /// <summary>
        /// Gets the <see cref="T:System.AggregateException">Exception</see> that caused the <see
        /// cref="Task">Task</see> to end prematurely. If the <see
        /// cref="Task">Task</see> completed successfully or has not yet thrown any
        /// exceptions, this will return null.
        /// </summary>
        /// <remarks>
        /// Tasks that throw unhandled exceptions store the resulting exception and propagate it wrapped in a
        /// <see cref="System.AggregateException"/> in calls to <see cref="Wait()">Wait</see>
        /// or in accesses to the <see cref="Exception"/> property.  Any exceptions not observed by the time
        /// the Task instance is garbage collected will be propagated on the finalizer thread.
        /// </remarks>
        public AggregateException Exception
        {
            get
            {
                if (_exceptions.Count > 0)
                    return new AggregateException(_exceptions);
                else
                    return null;
            }
        }

        /// <summary>
        /// Gets a unique ID for this <see cref="Task">Task</see> instance.
        /// </summary>
        /// <remarks>
        /// Task IDs are assigned on-demand and do not necessarily represent the order in the which Task
        /// instances were created.
        /// </remarks>
        public int Id
        {
            get
            {
                if (_taskId == 0)
                {
                    int newId = NewId();
                    Interlocked.CompareExchange(ref _taskId, newId, 0);
                }

                return _taskId;
            }
        }

        /// <summary>
        /// Gets whether this <see cref="Task">Task</see> has completed.
        /// </summary>
        /// <remarks>
        /// <see cref="IsCompleted"/> will return true when the Task is in one of the three
        /// final states: <see cref="TaskStatus.RanToCompletion">RanToCompletion</see>,
        /// <see cref="TaskStatus.Faulted">Faulted</see>, or
        /// <see cref="TaskStatus.Canceled">Canceled</see>.
        /// </remarks>
        public bool IsCompleted
        {
            get
            {
                // enable inlining of IsCompletedMethod by "cast"ing away the volatility
                int stateFlags = _stateFlags;
                return IsCompletedMethod(stateFlags);
            }
        }

        /// <summary>
        /// Gets whether the <see cref="Task"/> completed due to an unhandled exception.
        /// </summary>
        /// <remarks>
        /// If <see cref="IsFaulted"/> is true, the Task's <see cref="Status"/> will be equal to
        /// <see cref="TaskStatus.Faulted">TaskStatus.Faulted</see>, and its
        /// <see cref="Exception"/> property will be non-null.
        /// </remarks>
        public bool IsFaulted { get { return _exceptions.Count > 0; } }

        /// <summary>
        /// Provides access to factory methods for creating
        /// <see cref="Task"/> and <see cref="Task{TResult}"/>
        /// instances.
        /// </summary>
        public static TaskFactory Factory { get { return _defaultFactory; } }

        /// <summary>
        /// Gets the <see cref="T:System.Threading.Tasks.TaskStatus">TaskStatus</see> of this Task. 
        /// </summary>
        public TaskStatus Status
        {
            get
            {
                TaskStatus rval;

                // get a cached copy of the state flags.  This should help us
                // to get a consistent view of the flags if they are changing during the
                // execution of this method.
                int sf = _stateFlags;

                if ((sf & TASK_STATE_FAULTED) != 0) rval = TaskStatus.Faulted;
                else if ((sf & TASK_STATE_CANCELED) != 0) rval = TaskStatus.Canceled;
                else if ((sf & TASK_STATE_RAN_TO_COMPLETION) != 0) rval = TaskStatus.RanToCompletion;
                //else if ((sf & TASK_STATE_WAITING_ON_CHILDREN) != 0) rval = TaskStatus.WaitingForChildrenToComplete;
                else if ((sf & TASK_STATE_DELEGATE_INVOKED) != 0) rval = TaskStatus.Running;
                else if ((sf & TASK_STATE_STARTED) != 0) rval = TaskStatus.WaitingToRun;
                else if ((sf & TASK_STATE_WAITINGFORACTIVATION) != 0) rval = TaskStatus.WaitingForActivation;
                else rval = TaskStatus.Created;

                return rval;
            }
        }

        #endregion

        #region Constructors and Destructor

        /// <summary>
        /// Internal constructor to create an empty task.
        /// </summary>
        protected Task()
        {
            _stateFlags = 0;
            _taskCompletedEvent = new ManualResetEvent(false);
        }

        /// <summary>
        /// Internal constructor to create an already-completed task.
        /// </summary>
        protected Task(Exception ex)
        {
            _stateFlags = TASK_STATE_STARTED | TASK_STATE_RAN_TO_COMPLETION;
            _taskCompletedEvent = new ManualResetEvent(true);

            if (ex != null)
                _exceptions.Add(ex);
        }

        /// <summary>
        /// Internal constructor to allow creation of continue tasks.
        /// </summary>
        protected Task(Delegate action, object state, Task continueSource)
        {
            if (action == null)
            {
                throw new ArgumentNullException("action");
            }

            _stateFlags = 0;
            _action = action;
            _stateObject = state;
            _taskCompletedEvent = new ManualResetEvent(false);
            _continueSource = continueSource;
        }

        /// <summary>
        /// Initializes a new <see cref="Task"/> with the specified action.
        /// </summary>
        /// <param name="action">The delegate that represents the code to execute in the Task.</param>
        /// <exception cref="T:System.ArgumentNullException">The <paramref name="action"/> argument is null.</exception>
        public Task(Action action)
            : this(action, null, null)
        {
        }

        /// <summary>
        /// Initializes a new <see cref="Task"/> with the specified action and state.
        /// </summary>
        /// <param name="action">The delegate that represents the code to execute in the task.</param>
        /// <param name="state">An object representing data to be used by the action.</param>
        /// <exception cref="T:System.ArgumentNullException">
        /// The <paramref name="action"/> argument is null.
        /// </exception>
        public Task(Action<object> action, object state)
            : this(action, state, null)
        {
        }

        /// <summary>
        /// Destructor to enforces disposal of unmanaged resources.
        /// </summary>
        ~Task()
        {
            Dispose(false);
        }

        #endregion

        #region Helper methods

        // Atomically OR-in newBits to _stateFlags, while making sure that
        // no illegalBits are set.  Returns true on success, false on failure.
        private bool AtomicStateUpdate(int newBits, int illegalBits)
        {
            SpinWait sw = new SpinWait();
            do
            {
                int oldFlags = _stateFlags;
                if ((oldFlags & illegalBits) != 0) return false;
                if (Interlocked.CompareExchange(ref _stateFlags, oldFlags | newBits, oldFlags) == oldFlags)
                {
                    return true;
                }
                sw.SpinOnce();
            } while (true);
        }

        // Atomically mark a Task as started while making sure that it is not canceled.
        private bool MarkStarted()
        {
            return AtomicStateUpdate(TASK_STATE_STARTED, TASK_STATE_CANCELED | TASK_STATE_STARTED);
        }

        private static int NewId()
        {
            int newId = 0;
            // We need to repeat if Interlocked.Increment wraps around and returns 0.
            // Otherwise next time this task's Id is queried it will get a new value
            do
            {
                newId = Interlocked.Increment(ref _taskIdCounter);
            }
            while (newId == 0);
            return newId;
        }

        /// <summary>
        /// Throws an exception when called more than once.
        /// </summary>
        protected void EnsureStartOnce()
        {
            if (!MarkStarted())
            {
                throw new InvalidOperationException("Trying to start Task more than once");
            }
        }

        // Similar to IsCompleted property, but allows for the use of a cached flags value
        // rather than reading the volatile m_stateFlags field.
        private static bool IsCompletedMethod(int flags)
        {
            return (flags & TASK_STATE_COMPLETED_MASK) != 0;
        }

        // For use in InternalWait -- marginally faster than (Task.Status == TaskStatus.RanToCompletion)
        internal bool IsRanToCompletion
        {
            get { return (_stateFlags & TASK_STATE_COMPLETED_MASK) == TASK_STATE_RAN_TO_COMPLETION; }
        }

        #endregion

        #region Start method

        /// <summary>
        /// Starts the <see cref="Task"/>, scheduling it for execution to the current <see
        /// cref="System.Threading.ThreadPool">ThreadPool</see>.
        /// </summary>
        /// <remarks>
        /// A task may only be started and run only once.  Any attempts to schedule a task a second time
        /// will result in an exception.
        /// </remarks>
        /// <exception cref="InvalidOperationException">
        /// The <see cref="Task"/> is already been started.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// The <see cref="Task"/> could not be enqueued for execution.
        /// </exception>
        public void Start()
        {
            EnsureStartOnce();
            _runSync = false;

            AtomicStateUpdate(TASK_STATE_WAITINGFORACTIVATION, TASK_STATE_COMPLETED_MASK);
            if (_continueSource == null)
            {
                if (!ThreadPool.QueueUserWorkItem(TaskStartAction))
                    throw new NotSupportedException("Could not enqueue task for execution");
            }
            else
            {
                WaitOrTimerCallback callback = (state, timedOut) => TaskStartAction(null);
                Compatibility.ThreadPoolEx.RegisterWaitForSingleObject(_continueSource._taskCompletedEvent, callback, null, -1, true);
            }
        }

        #endregion

        #region Task thread execution

        /// <summary>
        /// Executes the action designed for current task.
        /// </summary>
        /// <param name="stateObject">Ignored.</param>
        protected void TaskStartAction(object stateObject)
        {
            try
            {
                AtomicStateUpdate(TASK_STATE_DELEGATE_INVOKED, TASK_STATE_DELEGATE_INVOKED);

                // Execute provided action
                ExecuteTaskAction();
            }
            catch (AggregateException ex)
            {
                AtomicStateUpdate(TASK_STATE_FAULTED, TASK_STATE_COMPLETED_MASK);
                _exceptions.AddRange(ex.InnerExceptions);
            }
            catch (Exception ex)
            {
                AtomicStateUpdate(TASK_STATE_FAULTED, TASK_STATE_COMPLETED_MASK);
                _exceptions.Add(ex);
            }
            finally
            {
                AtomicStateUpdate(TASK_STATE_RAN_TO_COMPLETION, TASK_STATE_COMPLETED_MASK);
                _taskCompletedEvent.Set();
            }
        }

        /// <summary>
        /// Unbox task action and execute it.
        /// </summary>
        protected virtual void ExecuteTaskAction()
        {
            if (_action is Action)
            {
                Action action = (Action)_action;
                action();
            }
            else if (_action is Action<object>)
            {
                Action<object> action = (Action<object>)_action;
                action(_stateObject);
            }
            else if (_action is Action<Task>)
            {
                Action<Task> action = (Action<Task>)_action;
                action(_continueSource);
            }
            else if (_action is Action<Task, object>)
            {
                Action<Task, object> action = (Action<Task, object>)_action;
                action(_continueSource, _stateObject);
            }
            else
            {
                throw new InvalidOperationException("Unexpected action type");
            }
        }

        #endregion

        #region Synchronous

        /// <summary>
        /// Runs the <see cref="Task"/> synchronously on the current <see cref="Thread"/>.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// The <see cref="Task"/> is already been started.
        /// </exception>
        public void RunSynchronously()
        {
            EnsureStartOnce();

            _runSync = true;
            TaskStartAction(null);
        }

        #endregion

        #region Wait methods

        /// <summary>
        /// Waits for the <see cref="Task"/> to complete execution.
        /// </summary>
        /// <exception cref="AggregateException">
        /// An exception was thrown during the execution of the <see cref="Task"/>.
        /// </exception>
        public void Wait()
        {
            _taskCompletedEvent.WaitOne();

            if (_exceptions.Count > 0)
                throw this.Exception;
        }

        /// <summary>
        /// Waits for the <see cref="Task"/> to complete execution
        /// within a specified time interval.
        /// </summary>
        /// <param name="timeout">
        /// A <see cref="TimeSpan"/> that represents the number of milliseconds to
        /// wait, or a <see cref="TimeSpan"/> that represents -1 milliseconds to wait
        /// indefinitely.
        /// </param>
        /// <returns>
        /// true if the <see cref="Task"/> completed execution within
        /// the allotted time; otherwise, false.
        /// </returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// timeout is a negative number other than -1 milliseconds, which
        /// represents an infinite time-out -or- timeout is greater than
        /// <see cref="int.MaxValue"/>.
        /// </exception>
        /// <exception cref="AggregateException">
        /// An exception was thrown during the execution of the <see cref="Task"/>.
        /// </exception>
        public bool Wait(TimeSpan timeout)
        {
            long totalMilliseconds = (long)timeout.TotalMilliseconds;
            if (totalMilliseconds < -1 || totalMilliseconds > int.MaxValue)
            {
                throw new ArgumentOutOfRangeException("timeout");
            }

            var success = _taskCompletedEvent.WaitOne((int)totalMilliseconds, false);

            if (_exceptions.Count > 0)
                throw this.Exception;

            return success;
        }

        /// <summary>
        /// Waits for the System.Threading.Tasks.Task to complete execution
        /// within a specified number of milliseconds.
        /// </summary>
        /// <param name="millisecondsTimeout">
        /// The number of milliseconds to wait, or
        /// System.Threading.Timeout.Infinite (-1) to wait indefinitely.
        /// </param>
        /// <returns>
        /// true if the System.Threading.Tasks.Task completed execution within
        /// the allotted time; otherwise, false.
        /// </returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// millisecondsTimeout is a negative number other than -1, which
        /// represents an infinite time-out.
        /// </exception>
        /// <exception cref="AggregateException">
        /// An exception was thrown during the execution of the
        /// System.Threading.Tasks.Task.
        /// </exception>
        public bool Wait(int millisecondsTimeout)
        {
            if (millisecondsTimeout < -1)
            {
                throw new ArgumentOutOfRangeException("millisecondsTimeout");
            }

            var success = _taskCompletedEvent.WaitOne(millisecondsTimeout, false);

            if (_exceptions.Count > 0)
                throw this.Exception;

            return success;
        }

        /// <summary>
        /// Waits asynchronously for the <see cref="Task"/> to complete execution.
        /// </summary>
        /// <param name="callback">The <see cref="AsyncCallback"/> delegate.</param>
        /// <param name="stateObject">An object that contains state information for this request.</param>
        /// <returns>An <see cref="IAsyncResult"/> that references the asynchronous wait.</returns>
        public IAsyncResult BeginWait(AsyncCallback callback, object stateObject)
        {
            var ar = new WaitAsyncResult(stateObject);
            WaitOrTimerCallback internalCallback = (state, timedOut) =>
            {
                ar.Result = !timedOut;
                ar.EventHandler.Set();

                if (callback != null)
                    callback(ar);
            };

            Compatibility.ThreadPoolEx.RegisterWaitForSingleObject(_taskCompletedEvent, internalCallback, stateObject, -1, true);
            return ar;
        }

        /// <summary>
        /// Waits asynchronously for the <see cref="Task"/> to complete execution
        /// within a specified time interval.
        /// </summary>
        /// <param name="timeout">
        /// A System.TimeSpan that represents the number of milliseconds to
        /// wait, or a System.TimeSpan that represents -1 milliseconds to wait
        /// indefinitely.
        /// </param>
        /// <param name="callback">The <see cref="AsyncCallback"/> delegate.</param>
        /// <param name="stateObject">An object that contains state information for this request.</param>
        /// <returns>An <see cref="IAsyncResult"/> that references the asynchronous wait.</returns>
        public IAsyncResult BeginWait(TimeSpan timeout, AsyncCallback callback, object stateObject)
        {
            long totalMilliseconds = (long)timeout.TotalMilliseconds;
            if (totalMilliseconds < -1 || totalMilliseconds > int.MaxValue)
            {
                throw new ArgumentOutOfRangeException("timeout");
            }

            var ar = new WaitAsyncResult(stateObject);
            WaitOrTimerCallback internalCallback = (state, timedOut) =>
            {
                ar.Result = !timedOut;
                ar.EventHandler.Set();

                if (callback != null)
                    callback(ar);
            };

            Compatibility.ThreadPoolEx.RegisterWaitForSingleObject(_taskCompletedEvent, internalCallback, stateObject, totalMilliseconds, true);
            return ar;
        }

        /// <summary>
        /// Waits asynchronously for the <see cref="Task"/> to complete execution
        /// within a specified time interval.
        /// </summary>
        /// <param name="millisecondsTimeout">
        /// A System.TimeSpan that represents the number of milliseconds to
        /// wait, or a System.TimeSpan that represents -1 milliseconds to wait
        /// indefinitely.
        /// </param>
        /// <param name="callback">The <see cref="AsyncCallback"/> delegate.</param>
        /// <param name="stateObject">An object that contains state information for this request.</param>
        /// <returns>An <see cref="IAsyncResult"/> that references the asynchronous wait.</returns>
        public IAsyncResult BeginWait(int millisecondsTimeout, AsyncCallback callback, object stateObject)
        {
            if (millisecondsTimeout < -1)
            {
                throw new ArgumentOutOfRangeException("timeout");
            }

            var ar = new WaitAsyncResult(stateObject);
            WaitOrTimerCallback internalCallback = (state, timedOut) =>
            {
                ar.Result = !timedOut;
                ar.EventHandler.Set();

                if (callback != null)
                    callback(ar);
            };

            Compatibility.ThreadPoolEx.RegisterWaitForSingleObject(_taskCompletedEvent, internalCallback, stateObject, millisecondsTimeout, true);
            return ar;
        }

        /// <summary>
        /// Ends a pending asynchronous wait.
        /// </summary>
        /// <param name="asyncResult">
        /// An <see cref="IAsyncResult"/> that stores state information for this asynchronous operation.
        /// </param>
        /// <returns>
        /// true if the <see cref="Task"/> completed execution within
        /// the allotted time; otherwise, false.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="asyncResult"/> is null.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// <paramref name="asyncResult"/> was not returned by a call to the <see cref="BeginWait(AsyncCallback, object)"/> method.
        /// </exception>
        /// <exception cref="InvalidOperationException">Error waiting for <see cref="WaitHandle"/> signal.</exception>
        public bool EndWait(IAsyncResult asyncResult)
        {
            if (asyncResult == null)
                throw new ArgumentNullException("asyncResult");

            var waitHandle = asyncResult as WaitAsyncResult;
            if (waitHandle == null)
                throw new ArgumentException("asyncResult was not returned by a call to the BeginWait method", "asyncResult");

            if (!waitHandle.EventHandler.WaitOne())
                throw new InvalidOperationException("Error waiting for wait handle signal");
            waitHandle.Dispose();

            if (_exceptions.Count > 0)
                throw this.Exception;

            return waitHandle.Result;
        }

        #endregion

        #region Dispose

        /// <summary>
        /// Disposes the <see cref="Task"/>, releasing all of its unmanaged resources.
        /// </summary>
        public virtual void Dispose()
        {
            Dispose(true);
        }

        /// <summary>
        /// Disposes the <see cref="Task"/>, releasing all of its unmanaged resources.
        /// </summary>
        /// <param name="disposing">
        /// A Boolean value that indicates whether this method is being called
        /// due to a call to <see cref="Dispose()"/>.
        /// </param>
        private void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (!IsCompleted)
                {
                    throw new InvalidOperationException(
                        "A task may only be disposed if it has completed its execution");
                }
            }

            if (_taskCompletedEvent != null)
                _taskCompletedEvent.Close();

            AtomicStateUpdate(TASK_STATE_DISPOSED, 0);
        }

        #endregion

        #region IAsyncResult Members

        /// <summary>
        /// Gets the state object supplied when the <see cref="Task">Task</see> was created,
        /// or null if none was supplied.
        /// </summary>
        public object AsyncState
        {
            get { return _stateObject; }
        }

        /// <summary>
        /// Gets a <see cref="T:System.Threading.WaitHandle"/> that can be used to wait for the task to
        /// complete.
        /// </summary>
        /// <remarks>
        /// Using the wait functionality provided by <see cref="Wait()"/>
        /// should be preferred over using <see cref="IAsyncResult.AsyncWaitHandle"/> for similar
        /// functionality.
        /// </remarks>
        /// <exception cref="T:System.ObjectDisposedException">
        /// The <see cref="Task"/> has been disposed.
        /// </exception>
        public WaitHandle AsyncWaitHandle
        {
            get
            {
                bool isDisposed = (_stateFlags & TASK_STATE_DISPOSED) != 0;
                if (isDisposed)
                    throw new ObjectDisposedException("Task");

                return _taskCompletedEvent;
            }
        }

        /// <summary>
        /// Gets an indication of whether the asynchronous operation completed synchronously.
        /// </summary>
        /// <value>true if the asynchronous operation completed synchronously; otherwise, false.</value>
        public bool CompletedSynchronously
        {
            get { return _runSync; }
        }

        #endregion

        #region IAsyncResult Objects

        private class WaitAsyncResult : IAsyncResult, IDisposable
        {
            private readonly ManualResetEvent _doneEvent;
            private readonly object _stateObject;

            public WaitAsyncResult(object stateObject)
            {
                _doneEvent = new ManualResetEvent(false);
                _stateObject = stateObject;
            }

            public object AsyncState
            {
                get { return _stateObject; }
            }

            public WaitHandle AsyncWaitHandle
            {
                get { return _doneEvent; }
            }

            public bool CompletedSynchronously { get; set; }

            public ManualResetEvent EventHandler
            {
                get { return _doneEvent; }
            }

            public bool IsCompleted
            {
                get { return _doneEvent.WaitOne(0, false); }
            }

            public bool Result { get; set; }

            public void Dispose()
            {
                if (_doneEvent != null)
                    _doneEvent.Close();
            }
        }

        #endregion

        #region Continuation Methods

        private Task InternalContinueWith(Delegate continuationAction, object state)
        {
            // Throw on continuation with null action
            if (continuationAction == null)
                throw new ArgumentNullException("continuationAction");

            var continueTask = new Task(continuationAction, state, this);
            continueTask.Start();
            return continueTask;
        }

        /// <summary>
        /// Creates a continuation that executes when the target <see cref="Task"/> completes.
        /// </summary>
        /// <param name="continuationAction">
        /// An action to run when the <see cref="Task"/> completes. When run, the delegate will be
        /// passed the completed task as an argument.
        /// </param>
        /// <returns>A new continuation <see cref="Task"/>.</returns>
        /// <remarks>
        /// The returned <see cref="Task"/> will not be scheduled for execution until the current task has
        /// completed, whether it completes due to running to completion successfully, faulting due to an
        /// unhandled exception, or exiting out early due to being canceled.
        /// </remarks>
        /// <exception cref="T:System.ArgumentNullException">
        /// The <paramref name="continuationAction"/> argument is null.
        /// </exception>
        public Task ContinueWith(Action<Task> continuationAction)
        {
            return InternalContinueWith(continuationAction, null);
        }

        /// <summary>
        /// Creates a continuation that executes when the target <see cref="Task"/> completes.
        /// </summary>
        /// <param name="continuationAction">
        /// An action to run when the <see cref="Task"/> completes. When run, the delegate will be
        /// passed the completed task as and the caller-supplied state object as arguments.
        /// </param>
        /// <param name="state">An object representing data to be used by the continuation action.</param>
        /// <returns>A new continuation <see cref="Task"/>.</returns>
        /// <remarks>
        /// The returned <see cref="Task"/> will not be scheduled for execution until the current task has
        /// completed, whether it completes due to running to completion successfully, faulting due to an
        /// unhandled exception, or exiting out early due to being canceled.
        /// </remarks>
        /// <exception cref="ArgumentNullException">
        /// The <paramref name="continuationAction"/> argument is null.
        /// </exception>
        public Task ContinueWith(Action<Task, object> continuationAction, object state)
        {
            return InternalContinueWith(continuationAction, state);
        }

        private Task<TResult> InternalContinueWith<TResult>(Delegate continuationFunction, object state)
        {
            // Throw on continuation with null action
            if (continuationFunction == null)
                throw new ArgumentNullException("continuationFunction");

            var continueTask = new Task<TResult>(continuationFunction, state, this);
            continueTask.Start();
            return continueTask;
        }

        /// <summary>
        /// Creates a continuation that executes when the target <see cref="Task"/> completes.
        /// </summary>
        /// <typeparam name="TResult">
        /// The type of the result produced by the continuation.
        /// </typeparam>
        /// <param name="continuationFunction">
        /// A function to run when the <see cref="Task"/> completes. When run, the delegate will be
        /// passed the completed task as an argument.
        /// </param>
        /// <returns>A new continuation <see cref="Task{TResult}"/>.</returns>
        /// <remarks>
        /// The returned <see cref="Task{TResult}"/> will not be scheduled for execution until the current task has
        /// completed, whether it completes due to running to completion successfully, faulting due to an
        /// unhandled exception, or exiting out early due to being canceled.
        /// </remarks>
        /// <exception cref="ArgumentNullException">
        /// The <paramref name="continuationFunction"/> argument is null.
        /// </exception>
        public Task<TResult> ContinueWith<TResult>(Func<Task, TResult> continuationFunction)
        {
            return InternalContinueWith<TResult>(continuationFunction, null);
        }

        /// <summary>
        /// Creates a continuation that executes when the target <see cref="Task"/> completes.
        /// </summary>
        /// <typeparam name="TResult">
        /// The type of the result produced by the continuation.
        /// </typeparam>
        /// <param name="continuationFunction">
        /// A function to run when the <see cref="Task"/> completes. When run, the delegate will be
        /// passed the completed task and the caller-supplied state object as arguments.
        /// </param>
        /// <param name="state">An object representing data to be used by the continuation function.</param>
        /// <returns>A new continuation <see cref="Task{TResult}"/>.</returns>
        /// <remarks>
        /// The returned <see cref="Task{TResult}"/> will not be scheduled for execution until the current task has
        /// completed, whether it completes due to running to completion successfully, faulting due to an
        /// unhandled exception, or exiting out early due to being canceled.
        /// </remarks>
        /// <exception cref="ArgumentNullException">
        /// The <paramref name="continuationFunction"/> argument is null.
        /// </exception>
        public Task<TResult> ContinueWith<TResult>(Func<Task, object, TResult> continuationFunction, object state)
        {
            return InternalContinueWith<TResult>(continuationFunction, state);
        }

        #endregion

        #region Wait methods

        /// <summary>
        /// Waits for all of the provided <see cref="Task"/> objects to complete execution.
        /// </summary>
        /// <param name="tasks">
        /// An array of <see cref="Task"/> instances on which to wait.
        /// </param>
        /// <exception cref="T:System.ArgumentNullException">
        /// The <paramref name="tasks"/> argument is null.
        /// </exception>
        /// <exception cref="T:System.ArgumentNullException">
        /// The <paramref name="tasks"/> argument contains a null element.
        /// </exception>
        /// <exception cref="T:System.AggregateException">
        /// At least one of the <see cref="Task"/> instances was canceled -or- an exception was thrown during
        /// the execution of at least one of the <see cref="Task"/> instances.
        /// </exception>
        public static void WaitAll(params Task[] tasks)
        {
            WaitAll(tasks, Timeout.Infinite);
        }

        /// <summary>
        /// Waits for all of the provided <see cref="Task"/> objects to complete execution.
        /// </summary>
        /// <returns>
        /// true if all of the <see cref="Task"/> instances completed execution within the allotted time;
        /// otherwise, false.
        /// </returns>
        /// <param name="tasks">
        /// An array of <see cref="Task"/> instances on which to wait.
        /// </param>
        /// <param name="timeout">
        /// A <see cref="System.TimeSpan"/> that represents the number of milliseconds to wait, or a <see
        /// cref="System.TimeSpan"/> that represents -1 milliseconds to wait indefinitely.
        /// </param>
        /// <exception cref="T:System.ArgumentNullException">
        /// The <paramref name="tasks"/> argument is null.
        /// </exception>
        /// <exception cref="T:System.ArgumentException">
        /// The <paramref name="tasks"/> argument contains a null element.
        /// </exception>
        /// <exception cref="T:System.AggregateException">
        /// At least one of the <see cref="Task"/> instances was canceled -or- an exception was thrown during
        /// the execution of at least one of the <see cref="Task"/> instances.
        /// </exception>
        /// <exception cref="T:System.ArgumentOutOfRangeException">
        /// <paramref name="timeout"/> is a negative number other than -1 milliseconds, which represents an
        /// infinite time-out -or- timeout is greater than
        /// <see cref="int.MaxValue"/>.
        /// </exception>
        public static bool WaitAll(Task[] tasks, TimeSpan timeout)
        {
            long totalMilliseconds = (long)timeout.TotalMilliseconds;
            if (totalMilliseconds < -1 || totalMilliseconds > Int32.MaxValue)
            {
                throw new ArgumentOutOfRangeException("timeout");
            }

            return WaitAll(tasks, (int)totalMilliseconds);
        }

        /// <summary>
        /// Waits for all of the provided <see cref="Task"/> objects to complete execution.
        /// </summary>
        /// <returns>
        /// true if all of the <see cref="Task"/> instances completed execution within the allotted time;
        /// otherwise, false.
        /// </returns>
        /// <param name="millisecondsTimeout">
        /// The number of milliseconds to wait, or <see cref="System.Threading.Timeout.Infinite"/> (-1) to
        /// wait indefinitely.</param>
        /// <param name="tasks">An array of <see cref="Task"/> instances on which to wait.
        /// </param>
        /// <exception cref="T:System.ArgumentNullException">
        /// The <paramref name="tasks"/> argument is null.
        /// </exception>
        /// <exception cref="T:System.ArgumentException">
        /// The <paramref name="tasks"/> argument contains a null element.
        /// </exception>
        /// <exception cref="T:System.AggregateException">
        /// At least one of the <see cref="Task"/> instances was canceled -or- an exception was thrown during
        /// the execution of at least one of the <see cref="Task"/> instances.
        /// </exception>
        /// <exception cref="T:System.ArgumentOutOfRangeException">
        /// <paramref name="millisecondsTimeout"/> is a negative number other than -1, which represents an
        /// infinite time-out.
        /// </exception>
        public static bool WaitAll(Task[] tasks, int millisecondsTimeout)
        {
            if (tasks == null)
                throw new ArgumentNullException("tasks");

            if (millisecondsTimeout < -1)
                throw new ArgumentOutOfRangeException("millisecondsTimeout");

            List<Exception> exceptions = new List<System.Exception>();
            ManualResetEvent waitDone = new ManualResetEvent(false);
            int completedCounter = 0;
            int totalSum = tasks.Length;

            foreach (var task in tasks)
            {
                task.BeginWait(ar =>
                {
                    try { task.EndWait(ar); }
                    catch (Exception ex)
                    {
                        exceptions.Add(ex);
                    }

                    var val = Interlocked.Increment(ref completedCounter);
                    if (val >= totalSum)
                        waitDone.Set();
                }, null);
            }

            var done = waitDone.WaitOne(millisecondsTimeout, false);
            waitDone.Close();

            if (exceptions.Count > 0)
                throw new AggregateException(exceptions).Flatten();

            return done;
        }

        /// <summary>
        /// Waits for any of the provided <see cref="Task"/> objects to complete execution.
        /// </summary>
        /// <param name="tasks">
        /// An array of <see cref="Task"/> instances on which to wait.
        /// </param>
        /// <returns>The index of the completed task in the <paramref name="tasks"/> array argument.</returns>
        /// <exception cref="T:System.ArgumentNullException">
        /// The <paramref name="tasks"/> argument is null.
        /// </exception>
        /// <exception cref="T:System.ArgumentException">
        /// The <paramref name="tasks"/> argument contains a null element.
        /// </exception>
        public static int WaitAny(params Task[] tasks)
        {
            return WaitAny(tasks, Timeout.Infinite);
        }

        /// <summary>
        /// Waits for any of the provided <see cref="Task"/> objects to complete execution.
        /// </summary>
        /// <param name="tasks">
        /// An array of <see cref="Task"/> instances on which to wait.
        /// </param>
        /// <param name="timeout">
        /// A <see cref="System.TimeSpan"/> that represents the number of milliseconds to wait, or a <see
        /// cref="System.TimeSpan"/> that represents -1 milliseconds to wait indefinitely.
        /// </param>
        /// <returns>
        /// The index of the completed task in the <paramref name="tasks"/> array argument, or -1 if the
        /// timeout occurred.
        /// </returns>
        /// <exception cref="T:System.ArgumentNullException">
        /// The <paramref name="tasks"/> argument is null.
        /// </exception>
        /// <exception cref="T:System.ArgumentException">
        /// The <paramref name="tasks"/> argument contains a null element.
        /// </exception>
        /// <exception cref="T:System.ArgumentOutOfRangeException">
        /// <paramref name="timeout"/> is a negative number other than -1 milliseconds, which represents an
        /// infinite time-out -or- timeout is greater than
        /// <see cref="int.MaxValue"/>.
        /// </exception>
        public static int WaitAny(Task[] tasks, TimeSpan timeout)
        {
            long totalMilliseconds = (long)timeout.TotalMilliseconds;
            if (totalMilliseconds < -1 || totalMilliseconds > Int32.MaxValue)
            {
                throw new ArgumentOutOfRangeException("timeout");
            }

            return WaitAny(tasks, (int)totalMilliseconds);
        }

        /// <summary>
        /// Waits for any of the provided <see cref="Task"/> objects to complete execution.
        /// </summary>
        /// <param name="tasks">
        /// An array of <see cref="Task"/> instances on which to wait.
        /// </param>
        /// <param name="millisecondsTimeout">
        /// The number of milliseconds to wait, or <see cref="System.Threading.Timeout.Infinite"/> (-1) to
        /// wait indefinitely.
        /// </param>
        /// <returns>
        /// The index of the completed task in the <paramref name="tasks"/> array argument, or -1 if the
        /// timeout occurred.
        /// </returns>
        /// <exception cref="T:System.ArgumentNullException">
        /// The <paramref name="tasks"/> argument is null.
        /// </exception>
        /// <exception cref="T:System.ArgumentException">
        /// The <paramref name="tasks"/> argument contains a null element.
        /// </exception>
        /// <exception cref="T:System.ArgumentOutOfRangeException">
        /// <paramref name="millisecondsTimeout"/> is a negative number other than -1, which represents an
        /// infinite time-out.
        /// </exception>
        public static int WaitAny(Task[] tasks, int millisecondsTimeout)
        {
            if (tasks == null)
                throw new ArgumentNullException("tasks");

            if (millisecondsTimeout < -1)
                throw new ArgumentOutOfRangeException("millisecondsTimeout");

            List<Exception> exceptions = new List<System.Exception>();
            ManualResetEvent waitDone = new ManualResetEvent(false);
            int totalSum = tasks.Length;
            int completedIndex = -1;

            for (int i = 0; i < tasks.Length; i++)
            {
                tasks[i].BeginWait(ar =>
                {
                    try { tasks[i].EndWait(ar); }
                    catch (Exception ex)
                    {
                        exceptions.Add(ex);
                    }

                    completedIndex = i;
                    waitDone.Set();
                }, null);

                if (waitDone.WaitOne(0, false))
                    break;
            }

            var done = waitDone.WaitOne(millisecondsTimeout, false);
            waitDone.Close();

            if (exceptions.Count > 0)
                throw new AggregateException(exceptions).Flatten();

            if (done)
                return completedIndex;
            else
                return -1;
        }

        #endregion

        #region FromResult / FromException

        /// <summary>
        /// Creates a <see cref="Task{TResult}"/> that's completed
        /// successfully with the specified result.
        /// </summary>
        /// <typeparam name="TResult">The type of the result returned by the task.</typeparam>
        /// <param name="result">The result to store into the completed task.</param>
        /// <returns>The successfully completed task.</returns>
        public static Task<TResult> FromResult<TResult>(TResult result)
        {
            return new Task<TResult>(result, null);
        }

        /// <summary>Creates a <see cref="Task{TResult}"/> that's completed exceptionally with the specified exception.</summary>
        /// <param name="exception">The exception with which to complete the task.</param>
        /// <returns>The faulted task.</returns>
        public static Task FromException(Exception exception)
        {
            if (exception == null)
                throw new ArgumentNullException("exception");

            return new Task(exception);
        }

        /// <summary>Creates a <see cref="Task{TResult}"/> that's completed exceptionally with the specified exception.</summary>
        /// <typeparam name="TResult">The type of the result returned by the task.</typeparam>
        /// <param name="exception">The exception with which to complete the task.</param>
        /// <returns>The faulted task.</returns>
        public static Task<TResult> FromException<TResult>(Exception exception)
        {
            if (exception == null)
                throw new ArgumentNullException("exception");

            return new Task<TResult>(default(TResult), exception);
        }

        #endregion

        #region Run methods

        /// <summary>
        /// Queues the specified work to run on the ThreadPool and returns a Task handle for that work.
        /// </summary>
        /// <param name="action">The work to execute asynchronously</param>
        /// <returns>A Task that represents the work queued to execute in the ThreadPool.</returns>
        /// <exception cref="T:System.ArgumentNullException">
        /// The <paramref name="action"/> parameter was null.
        /// </exception>
        public static Task Run(Action action)
        {
            return Factory.StartNew(action);
        }

        /// <summary>
        /// Queues the specified work to run on the ThreadPool and returns a Task(TResult) handle for that work.
        /// </summary>
        /// <param name="function">The work to execute asynchronously</param>
        /// <returns>A Task(TResult) that represents the work queued to execute in the ThreadPool.</returns>
        /// <exception cref="T:System.ArgumentNullException">
        /// The <paramref name="function"/> parameter was null.
        /// </exception>
        public static Task<TResult> Run<TResult>(Func<TResult> function)
        {
            return Factory.StartNew<TResult>(function);
        }

        /// <summary>
        /// Queues the specified work to run on the ThreadPool and returns a proxy for the
        /// Task returned by <paramref name="function"/>.
        /// </summary>
        /// <param name="function">The work to execute asynchronously</param>
        /// <returns>A Task that represents a proxy for the Task returned by <paramref name="function"/>.</returns>
        /// <exception cref="T:System.ArgumentNullException">
        /// The <paramref name="function"/> parameter was null.
        /// </exception>
        public static Task Run(Func<Task> function)
        {
            return Factory.StartNew(() => { function().Wait(); });
        }

        /// <summary>
        /// Queues the specified work to run on the ThreadPool and returns a proxy for the
        /// Task(TResult) returned by <paramref name="function"/>.
        /// </summary>
        /// <typeparam name="TResult">The type of the result returned by the proxy Task.</typeparam>
        /// <param name="function">The work to execute asynchronously</param>
        /// <returns>A Task(TResult) that represents a proxy for the Task(TResult) returned by <paramref name="function"/>.</returns>
        /// <exception cref="T:System.ArgumentNullException">
        /// The <paramref name="function"/> parameter was null.
        /// </exception>
        public static Task<TResult> Run<TResult>(Func<Task<TResult>> function)
        {
            return Factory.StartNew(() =>
            {
                var task = function();
                task.Wait();
                return task.Result;
            });
        }

        #endregion

        #region Delay methods

        /// <summary>
        /// Creates a Task that will complete after a time delay.
        /// </summary>
        /// <param name="delay">The time span to wait before completing the returned Task</param>
        /// <returns>A Task that represents the time delay</returns>
        /// <exception cref="T:System.ArgumentOutOfRangeException">
        /// The <paramref name="delay"/> is less than -1 or greater than Int32.MaxValue.
        /// </exception>
        /// <remarks>
        /// After the specified time delay, the Task is completed in RanToCompletion state.
        /// </remarks>
        public static Task Delay(TimeSpan delay)
        {
            long totalMilliseconds = (long)delay.TotalMilliseconds;
            if (totalMilliseconds < -1 || totalMilliseconds > int.MaxValue)
            {
                throw new ArgumentOutOfRangeException("delay");
            }

            return Delay((int)totalMilliseconds);
        }

        /// <summary>
        /// Creates a Task that will complete after a time delay.
        /// </summary>
        /// <param name="millisecondsDelay">The number of milliseconds to wait before completing the returned Task</param>
        /// <returns>A Task that represents the time delay</returns>
        /// <exception cref="T:System.ArgumentOutOfRangeException">
        /// The <paramref name="millisecondsDelay"/> is less than -1.
        /// </exception>
        /// <remarks>
        /// After the specified time delay, the Task is completed in RanToCompletion state.
        /// </remarks>
        public static Task Delay(int millisecondsDelay)
        {
            if (millisecondsDelay < -1)
                throw new ArgumentOutOfRangeException("millisecondsDelay");

            if (millisecondsDelay == 0)
                return new Task((Exception)null);

            var task = new Task();
            var timeoutEvent = new ManualResetEvent(false);
            task.MarkStarted();
            WaitOrTimerCallback callback = (state, timedOut) =>
            {
                task.AtomicStateUpdate(TASK_STATE_RAN_TO_COMPLETION, TASK_STATE_COMPLETED_MASK);
                task._taskCompletedEvent.Set();
                timeoutEvent.Close();
                timeoutEvent = null;
            };
            Compatibility.ThreadPoolEx.RegisterWaitForSingleObject(timeoutEvent, callback, null, millisecondsDelay, true);

            return task;
        }

        #endregion

        #region WhenAll

        /// <summary>
        /// Creates a task that will complete when all of the supplied tasks have completed.
        /// </summary>
        /// <param name="tasks">The tasks to wait on for completion.</param>
        /// <returns>A task that represents the completion of all of the supplied tasks.</returns>
        /// <exception cref="T:System.ArgumentNullException">
        /// The <paramref name="tasks"/> argument was null.
        /// </exception>
        /// <exception cref="T:System.ArgumentException">
        /// The <paramref name="tasks"/> collection contained a null task.
        /// </exception>
        public static Task WhenAll(IEnumerable<Task> tasks)
        {
            if (tasks == null)
                throw new ArgumentNullException("tasks");

            // Take a more efficient path if tasks is actually an array
            Task[] taskArray = tasks as Task[];
            return WhenAll(taskArray ?? tasks.ToArray());
        }

        /// <summary>
        /// Creates a task that will complete when all of the supplied tasks have completed.
        /// </summary>
        /// <param name="tasks">The tasks to wait on for completion.</param>
        /// <returns>A task that represents the completion of all of the supplied tasks.</returns>
        /// <exception cref="T:System.ArgumentNullException">
        /// The <paramref name="tasks"/> argument was null.
        /// </exception>
        /// <exception cref="T:System.ArgumentException">
        /// The <paramref name="tasks"/> array contained a null task.
        /// </exception>
        public static Task WhenAll(params Task[] tasks)
        {
            if (tasks == null)
                throw new ArgumentNullException("tasks");

            foreach (var task in tasks)
            {
                if (task == null)
                    throw new ArgumentException("One task from provided task array is null");
            }

            if (tasks.Length == 0)
                return new Task((Exception)null);

            var resultTask = new Task(() =>
            {
                var exceptions = new List<Exception>();
                foreach (var task in tasks)
                {
                    try { task.Wait(); }
                    catch (Exception e)
                    {
                        exceptions.Add(e);
                    }
                }

                if (exceptions.Count() > 0)
                    throw new AggregateException(exceptions.ToArray());
            });
            resultTask.Start();
            return resultTask;
        }

        /// <summary>
        /// Creates a task that will complete when all of the supplied tasks have completed.
        /// </summary>
        /// <param name="tasks">The tasks to wait on for completion.</param>
        /// <returns>A task that represents the completion of all of the supplied tasks.</returns>
        /// <exception cref="T:System.ArgumentNullException">
        /// The <paramref name="tasks"/> argument was null.
        /// </exception>
        /// <exception cref="T:System.ArgumentException">
        /// The <paramref name="tasks"/> collection contained a null task.
        /// </exception>       
        public static Task<TResult[]> WhenAll<TResult>(IEnumerable<Task<TResult>> tasks)
        {
            if (tasks == null)
                throw new ArgumentNullException("tasks");

            // Take a more efficient path if tasks is actually an array
            Task<TResult>[] taskArray = tasks as Task<TResult>[];
            return WhenAll(taskArray ?? tasks.ToArray());
        }

        /// <summary>
        /// Creates a task that will complete when all of the supplied tasks have completed.
        /// </summary>
        /// <param name="tasks">The tasks to wait on for completion.</param>
        /// <returns>A task that represents the completion of all of the supplied tasks.</returns>
        /// <exception cref="T:System.ArgumentNullException">
        /// The <paramref name="tasks"/> argument was null.
        /// </exception>
        /// <exception cref="T:System.ArgumentException">
        /// The <paramref name="tasks"/> array contained a null task.
        /// </exception>
        public static Task<TResult[]> WhenAll<TResult>(params Task<TResult>[] tasks)
        {
            if (tasks == null)
                throw new ArgumentNullException("tasks");

            foreach (var task in tasks)
            {
                if (task == null)
                    throw new ArgumentException("One task from provided task array is null");
            }

            if (tasks.Length == 0)
                return new Task<TResult[]>(new TResult[0], null);

            var resultTask = new Task<TResult[]>(() =>
            {
                var exceptions = new List<Exception>();
                var results = new List<TResult>(tasks.Length);
                foreach (var task in tasks)
                {
                    try
                    {
                        task.Wait();
                        results.Add(task.Result);
                    }
                    catch (Exception e)
                    {
                        exceptions.Add(e);
                        results.Add(default(TResult));
                    }
                }

                if (exceptions.Count() > 0)
                    throw new AggregateException(exceptions.ToArray());

                return results.ToArray();
            });
            resultTask.Start();
            return resultTask;
        }

        #endregion

        #region WhenAny

        /// <summary>
        /// Creates a task that will complete when any of the supplied tasks have completed.
        /// </summary>
        /// <param name="tasks">The tasks to wait on for completion.</param>
        /// <returns>A task that represents the completion of one of the supplied tasks.  The return Task's Result is the task that completed.</returns>
        /// <exception cref="T:System.ArgumentNullException">
        /// The <paramref name="tasks"/> argument was null.
        /// </exception>
        /// <exception cref="T:System.ArgumentException">
        /// The <paramref name="tasks"/> collection contained a null task, or was empty.
        /// </exception>
        public static Task<Task> WhenAny(IEnumerable<Task> tasks)
        {
            if (tasks == null)
                throw new ArgumentNullException("tasks");

            // Take a more efficient path if tasks is actually an array
            Task[] taskArray = tasks as Task[];
            return WhenAny(taskArray ?? tasks.ToArray());
        }

        /// <summary>
        /// Creates a task that will complete when any of the supplied tasks have completed.
        /// </summary>
        /// <param name="tasks">The tasks to wait on for completion.</param>
        /// <returns>A task that represents the completion of one of the supplied tasks.  The return Task's Result is the task that completed.</returns>
        /// <exception cref="T:System.ArgumentNullException">
        /// The <paramref name="tasks"/> argument was null.
        /// </exception>
        /// <exception cref="T:System.ArgumentException">
        /// The <paramref name="tasks"/> array contained a null task, or was empty.
        /// </exception>
        public static Task<Task> WhenAny(params Task[] tasks)
        {
            if (tasks == null)
                throw new ArgumentNullException("tasks");
            if (tasks.Length == 0)
                throw new ArgumentException("At least one task is required to wait for completion");

            foreach (var task in tasks)
            {
                if (task == null)
                    throw new ArgumentException("One task from provided task array is null");
            }

            var resultTask = new Task<Task>(() =>
            {
                while (true)
                {
                    foreach (var task in tasks)
                    {
                        try
                        {
                            if (task.Wait(0))
                                return task;
                        }
                        catch (Exception)
                        {
                            return task;
                        }
                    }

                    Thread.Sleep(1);
                }
            });
            resultTask.Start();
            return resultTask;
        }

        /// <summary>
        /// Creates a task that will complete when any of the supplied tasks have completed.
        /// </summary>
        /// <param name="tasks">The tasks to wait on for completion.</param>
        /// <returns>A task that represents the completion of one of the supplied tasks.  The return Task's Result is the task that completed.</returns>
        /// <exception cref="T:System.ArgumentNullException">
        /// The <paramref name="tasks"/> argument was null.
        /// </exception>
        /// <exception cref="T:System.ArgumentException">
        /// The <paramref name="tasks"/> collection contained a null task, or was empty.
        /// </exception>
        public static Task<Task<TResult>> WhenAny<TResult>(IEnumerable<Task<TResult>> tasks)
        {
            if (tasks == null)
                throw new ArgumentNullException("tasks");

            // Take a more efficient path if tasks is actually an array
            Task<TResult>[] taskArray = tasks as Task<TResult>[];
            return WhenAny(taskArray ?? tasks.ToArray());
        }

        /// <summary>
        /// Creates a task that will complete when any of the supplied tasks have completed.
        /// </summary>
        /// <param name="tasks">The tasks to wait on for completion.</param>
        /// <returns>A task that represents the completion of one of the supplied tasks.  The return Task's Result is the task that completed.</returns>
        /// <exception cref="T:System.ArgumentNullException">
        /// The <paramref name="tasks"/> argument was null.
        /// </exception>
        /// <exception cref="T:System.ArgumentException">
        /// The <paramref name="tasks"/> array contained a null task, or was empty.
        /// </exception>
        public static Task<Task<TResult>> WhenAny<TResult>(params Task<TResult>[] tasks)
        {
            if (tasks == null)
                throw new ArgumentNullException("tasks");
            if (tasks.Length == 0)
                throw new ArgumentException("At least one task is required to wait for completion");

            foreach (var task in tasks)
            {
                if (task == null)
                    throw new ArgumentException("One task from provided task array is null");
            }

            var resultTask = new Task<Task<TResult>>(() =>
            {
                while (true)
                {
                    foreach (var task in tasks)
                    {
                        try
                        {
                            if (task.Wait(0))
                                return task;
                        }
                        catch (Exception)
                        {
                            return task;
                        }
                    }

                    Thread.Sleep(1);
                }
            });
            resultTask.Start();
            return resultTask;
        }

        #endregion
    }
}
#endif
