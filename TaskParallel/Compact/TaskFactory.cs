namespace System.Threading.Tasks
{
    /// <summary>
    /// Provides support for creating and scheduling <see cref="Task"/> objects.
    /// </summary>
    public sealed class TaskFactory
    {
        /// <summary>
        /// Initializes a <see cref="TaskFactory"/> instance with the default configuration.
        /// </summary>
        public TaskFactory() { }

        /// <summary>
        /// Creates and starts a task.
        /// </summary>
        /// <param name="action">The action delegate to execute asynchronously.</param>
        /// <returns>The started task.</returns>
        /// <exception cref="ArgumentNullException">The <paramref name="action"/> argument is null.</exception>
        public Task StartNew(Action action)
        {
            if (action == null)
                throw new ArgumentNullException("action");

            var task = new Task(action);
            task.Start();
            return task;
        }

        /// <summary>
        /// Creates and starts a <see cref="Task"/>.
        /// </summary>
        /// <param name="action">The action delegate to execute asynchronously.</param>
        /// <param name="state">An object containing data to be used by the <paramref name="action"/> delegate.</param>
        /// <returns>The started <see cref="Task"/>.</returns>
        /// <exception cref="ArgumentNullException">The <paramref name="action"/> argument is null.</exception>
        public Task StartNew(Action<object> action, object state)
        {
            if (action == null)
                throw new ArgumentNullException("action");

            var task = new Task(action, state);
            task.Start();
            return task;
        }

        /// <summary>
        /// Creates and starts a <see cref="Task{TResult}"/>.
        /// </summary>
        /// <typeparam name="TResult">The type of the result available through the <see cref="Task{TResult}"/>.</typeparam>
        /// <param name="function">
        /// A function delegate that returns the future result to be available
        /// through the <see cref="Task{TResult}"/>.
        /// </param>
        /// <returns>The started <see cref="Task{TResult}"/>.</returns>
        /// <exception cref="ArgumentNullException">The exception that is thrown when the <paramref name="function"/> argument is null.</exception>
        public Task<TResult> StartNew<TResult>(Func<TResult> function)
        {
            if (function == null)
                throw new ArgumentNullException("function");

            var task = new Task<TResult>(function);
            task.Start();
            return task;
        }

        /// <summary>
        /// Creates and starts a <see cref="Task{TResult}"/>.
        /// </summary>
        /// <typeparam name="TResult">
        /// The type of the result available through the <see cref="Task{TResult}"/>.
        /// </typeparam>
        /// <param name="function">
        /// A function delegate that returns the future result to be available
        /// through the <see cref="Task{TResult}"/>.
        /// </param>
        /// <param name="state">
        /// An object containing data to be used by the <paramref name="function"/> delegate.
        /// </param>
        /// <returns>The started <see cref="Task{TResult}"/>.</returns>
        /// <exception cref="ArgumentNullException">The exception that is thrown when the <paramref name="function"/> argument is null.</exception>
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
