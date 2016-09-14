namespace System.Threading.Tasks
{
    public class Task<TResult> : TaskBase
    {
        public TResult Result { get; private set; }

        public Task(Func<TResult> userWork)
            : base(userWork, null, null)
        {
        }

        public Task(Func<object, TResult> userWork, object state)
            : base(userWork, state, null)
        {
        }

        internal Task(Func<TResult> userWork, TaskBase continueParent)
            : base(userWork, null, continueParent)
        {
        }

        internal Task(Func<object, TResult> userWork, object state, TaskBase continueParent)
            : base(userWork, state, continueParent)
        {
        }

        public TResult RunSynchronously()
        {
            EnsureStartOnce();

            _runSync = true;
            ExecuteQueue(null);
            _doneEvent.Set();

            if (this.Exception != null)
                throw this.Exception;

            return Result;
        }

        protected override void ExecuteUserWork()
        {
            if (_userWork is Func<TResult>)
            {
                var userWork = (Func<TResult>)_userWork;
                Result = userWork();
            }
            else
            {
                var userWork = (Func<object, TResult>)_userWork;
                Result = userWork(_userState);
            }
        }

        public Task ContinueWith(Action action)
        {
            return new Task(action, this);
        }

        public Task ContinueWith(Action<object> action, object state)
        {
            return new Task(action, state, this);
        }

        public Task<TNewResult> ContinueWith<TNewResult>(Func<TNewResult> action)
        {
            return new Task<TNewResult>(action, this);
        }

        public Task<TNewResult> ContinueWith<TNewResult>(Func<object, TNewResult> action, object state)
        {
            return new Task<TNewResult>(action, state, this);
        }
    }
}
