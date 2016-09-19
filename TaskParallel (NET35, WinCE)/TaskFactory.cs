namespace System.Threading.Tasks
{
    /// <summary>
    /// Provides support for creating and scheduling System.Threading.Tasks.Task objects.
    /// </summary>
    public sealed class TaskFactory
    {
        /// <summary>
        /// Initializes a System.Threading.Tasks.TaskFactory instance with the default configuration.
        /// </summary>
        public TaskFactory() { }

        /// <summary>
        /// Creates and starts a task.
        /// </summary>
        /// <param name="action">The action delegate to execute asynchronously.</param>
        /// <returns>The started task.</returns>
        /// <exception cref="ArgumentNullException">The action argument is null.</exception>
        public Task StartNew(Action action)
        {
            if (action == null)
                throw new ArgumentNullException("action");

            var task = new Task(action);
            task.Start();
            return task;
        }

        /// <summary>
        /// Creates and starts a System.Threading.Tasks.Task.
        /// </summary>
        /// <param name="action">The action delegate to execute asynchronously.</param>
        /// <param name="state">An object containing data to be used by the action delegate.</param>
        /// <returns>The started System.Threading.Tasks.Task.</returns>
        /// <exception cref="ArgumentNullException">The action argument is null.</exception>
        public Task StartNew(Action<object> action, object state)
        {
            if (action == null)
                throw new ArgumentNullException("action");

            var task = new Task(action, state);
            task.Start();
            return task;
        }

        /// <summary>
        /// Creates and starts a System.Threading.Tasks.Task`1.
        /// </summary>
        /// <typeparam name="TResult">The type of the result available through the System.Threading.Tasks.Task`1.</typeparam>
        /// <param name="function">
        /// A function delegate that returns the future result to be available
        /// through the System.Threading.Tasks.Task`1.
        /// </param>
        /// <returns>The started System.Threading.Tasks.Task`1.</returns>
        /// <exception cref="ArgumentNullException">The exception that is thrown when the function argument is null.</exception>
        public Task<TResult> StartNew<TResult>(Func<TResult> function)
        {
            if (function == null)
                throw new ArgumentNullException("function");

            var task = new Task<TResult>(function);
            task.Start();
            return task;
        }

        /// <summary>
        /// Creates and starts a System.Threading.Tasks.Task`1.
        /// </summary>
        /// <typeparam name="TResult">
        /// The type of the result available through the System.Threading.Tasks.Task`1.
        /// </typeparam>
        /// <param name="function">
        /// A function delegate that returns the future result to be available
        /// through the System.Threading.Tasks.Task`1.
        /// </param>
        /// <param name="state">
        /// An object containing data to be used by the function delegate.
        /// </param>
        /// <returns>The started System.Threading.Tasks.Task`1.</returns>
        /// <exception cref="ArgumentNullException">The exception that is thrown when the function argument is null.</exception>
        public Task<TResult> StartNew<TResult>(Func<object, TResult> function, object state)
        {
            if (function == null)
                throw new ArgumentNullException("function");

            var task = new Task<TResult>(function, state);
            task.Start();
            return task;
        }
    }
}
