namespace System.Threading.Tasks
{
    public abstract class TaskBase : IAsyncResult, IDisposable
    {
        protected readonly Delegate _userWork;
        protected readonly object _userState;
        protected readonly ManualResetEvent _doneEvent;
        protected bool _runSync;
        private int _startCount;

        // Async (APM)
        private object _asyncState;
        private AsyncCallback _callback;

        // Serialize Task execution
        readonly TaskBase _continueParent;


        public Exception Exception { get; protected set; }
        public bool IsFaulted { get { return this.Exception != null; } }
        public bool ExceptionRecorded { get { return this.Exception != null; } }


        internal TaskBase(Delegate userWork, object state, TaskBase continueParent)
        {
            if (userWork == null)
            {
                throw new ArgumentNullException("userWork");
            }

            _userWork = userWork;
            _userState = state;
            _doneEvent = new ManualResetEvent(false);
            _startCount = 0;
            _continueParent = continueParent;
        }

        ~TaskBase()
        {
            Dispose();
        }

        protected void EnsureStartOnce()
        {
            int startCount = Interlocked.Increment(ref _startCount);
            if (startCount != 1)
            {
                throw new InvalidOperationException("Trying to start Task more than once");
            }
        }

        public void Start()
        {
            EnsureStartOnce();
            _runSync = false;
            ThreadPool.QueueUserWorkItem(ExecuteQueue);
        }

        public IAsyncResult BeginStart(AsyncCallback callback, object stateObject)
        {
            EnsureStartOnce();

            _runSync = false;
            _callback = callback;
            _asyncState = stateObject;
            ThreadPool.QueueUserWorkItem(ExecuteQueue);
            return this;
        }

        public void EndStart(IAsyncResult asyncResult)
        {
            if (asyncResult != this)
                throw new InvalidOperationException("Not matching asyncResult with current Task instance");
            if (!asyncResult.AsyncWaitHandle.WaitOne())
                throw new InvalidOperationException("Error waiting for wait handle");
            // Release resource
            //asyncResult.AsyncWaitHandle.Close();
        }

        private void RunSynchronously()
        {
            EnsureStartOnce();

            _runSync = true;
            ExecuteQueue(null);
            _doneEvent.Close();
        }

        protected void ExecuteQueue(object threadContext)
        {
            if (_continueParent != null &&
                threadContext == null &&
                !_continueParent.IsCompleted)
            {
                if (_runSync)
                {
                    _continueParent.RunSynchronously();
                }
                else
                {
                    _continueParent.BeginStart(ExecuteQueue, threadContext);
                    return;
                }
            }

            if (_continueParent != null &&
                threadContext == _continueParent)
            {
                var ar = threadContext as IAsyncResult;
                _continueParent.EndStart(ar);
            }

            try
            {
                ExecuteUserWork();
            }
            catch (Exception ex)
            {
                // TODO: Better exception handling
                this.Exception = ex;
            }
            finally
            {
                _doneEvent.Set();

                if (_callback != null)
                {
                    _callback(this);
                }
            }
        }

        protected abstract void ExecuteUserWork();

        public void Wait()
        {
            _doneEvent.WaitOne();
        }

        public bool Wait(TimeSpan timeout)
        {
            long totalMilliseconds = (long)timeout.TotalMilliseconds;
            if (totalMilliseconds < -1 || totalMilliseconds > Int32.MaxValue)
            {
                throw new ArgumentOutOfRangeException("timeout");
            }

            return _doneEvent.WaitOne((int)totalMilliseconds, false);
        }

        public bool Wait(int millisecondsTimeout)
        {
            return _doneEvent.WaitOne(millisecondsTimeout, false);
        }

        #region IAsyncResult Members

        public object AsyncState
        {
            get { return _asyncState; }
        }

        public WaitHandle AsyncWaitHandle
        {
            get { return _doneEvent; }
        }

        public bool CompletedSynchronously
        {
            get { return _runSync; }
        }

        public bool IsCompleted
        {
            get { return _doneEvent.WaitOne(0, false); }
        }

        #endregion

        #region IDisposable Members

        public virtual void Dispose()
        {
            if (_doneEvent != null)
                _doneEvent.Close();
        }

        #endregion
    }
}
