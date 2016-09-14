namespace System.Threading.Tasks
{
    public class Task : TaskBase
    {
        public Task(Action userWork)
            : base(userWork, null, null)
        {
        }

        public Task(Action<object> userWork, object state)
            : base(userWork, state, null)
        {
        }

        internal Task(Action userWork, TaskBase continueParent)
            : base(userWork, null, continueParent)
        {
        }

        internal Task(Action<object> userWork, object state, TaskBase continueParent)
            : base(userWork, state, continueParent)
        {
        }

        public void RunSynchronously()
        {
            EnsureStartOnce();

            _runSync = true;
            ExecuteQueue(null);
            _doneEvent.Set();

            if (this.Exception != null)
                throw this.Exception;
        }

        protected override void ExecuteUserWork()
        {
            if (_userWork is Action)
            {
                Action userWork = (Action)_userWork;
                userWork();
            }
            else
            {
                Action<object> userWork = (Action<object>)_userWork;
                userWork(_userState);
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

        /// <summary>
        /// Creates a System.Threading.Tasks.Task`1 that's completed
        /// successfully with the specified result.
        /// </summary>
        /// <typeparam name="TResult">The type of the result returned by the task.</typeparam>
        /// <param name="result">The result to store into the completed task.</param>
        /// <returns>The successfully completed task.</returns>
        public static Task<TResult> FromResult<TResult>(TResult result)
        {
            var task = new Task<TResult>(() => result);
            task.RunSynchronously();
            return task;
        }
    }
}
