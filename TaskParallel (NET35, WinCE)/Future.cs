namespace System.Threading.Tasks
{
    /// <summary>
    /// Represents an asynchronous operation that produces a result at some time in the future.
    /// </summary>
    /// <typeparam name="TResult">
    /// The type of the result produced by this <see cref="Task{TResult}"/>.
    /// </typeparam>
    public class Task<TResult> : Task
    {
        TResult _result;

        /// <summary>
        /// Gets the result value of this <see cref="Task{TResult}"/>.
        /// </summary>
        /// <remarks>
        /// The get accessor for this property ensures that the asynchronous operation is complete before
        /// returning. Once the result of the computation is available, it is stored and will be returned
        /// immediately on later calls to <see cref="Result"/>.
        /// </remarks>
        /// <exception cref="AggregateException">An exception was thrown during the execution of the <see cref="Task{TResult}"/>.</exception>
        public TResult Result
        {
            get
            {
                _taskCompletedEvent.WaitOne();

                if (_exceptions.Count > 0)
                    throw this.Exception;

                return _result;
            }
        }

        #region Constructors

        /// <summary>
        /// Internal constructor to create an already-completed task.
        /// </summary>
        internal Task(TResult result, Exception ex)
            : base(ex)
        {
            _result = result;
        }

        /// <summary>
        /// Initializes a new <see cref="Task{TResult}"/> with the specified function.
        /// </summary>
        /// <param name="function">
        /// The delegate that represents the code to execute in the task. When the function has completed,
        /// the task's <see cref="Result"/> property will be set to return the result value of the function.
        /// </param>
        /// <exception cref="ArgumentException">
        /// The <paramref name="function"/> argument is null.
        /// </exception>
        public Task(Func<TResult> function)
            : base(function, null, null)
        {
        }

        /// <summary>
        /// Initializes a new <see cref="Task{TResult}"/> with the specified function and state.
        /// </summary>
        /// <param name="function">
        /// The delegate that represents the code to execute in the task. When the function has completed,
        /// the task's <see cref="Result"/> property will be set to return the result value of the function.
        /// </param>
        /// <param name="state">An object representing data to be used by the action.</param>
        /// <exception cref="ArgumentException">
        /// The <paramref name="function"/> argument is null.
        /// </exception>
        public Task(Func<object, TResult> function, object state)
            : base(function, state, null)
        {
        }

        /// <summary>
        /// Internal constructor to allow creation of continue tasks.
        /// </summary>
        internal Task(Delegate function, object state, Task continueSource)
            : base(function, state, continueSource)
        {
        }

        #endregion

        protected override void ExecuteTaskAction()
        {
            if (_action is Func<TResult>)
            {
                var userWork = (Func<TResult>)_action;
                _result = userWork();
            }
            else if (_action is Func<object, TResult>)
            {
                var userWork = (Func<object, TResult>)_action;
                _result = userWork(_stateObject);
            }
            else if (_action is Func<Task, TResult>)
            {
                Func<Task, TResult> userWork = (Func<Task, TResult>)_action;
                _result = userWork(_continueSource);
            }
            else if (_action is Func<Task, object, TResult>)
            {
                Func<Task, object, TResult> userWork = (Func<Task, object, TResult>)_action;
                _result = userWork(_continueSource, _stateObject);
            }
            else
            {
                throw new InvalidOperationException("Unexpected action type");
            }
        }
    }
}
