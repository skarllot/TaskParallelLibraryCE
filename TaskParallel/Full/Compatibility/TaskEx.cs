using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace System.Compatibility
{
    /// <summary>
    /// Provides new <see cref="Task"/> methods introduced in .NET 4.5 and .NET 4.6.
    /// </summary>
    public static class TaskEx
    {
        #region FromResult / FromException

        /// <summary>
        /// Creates a <see cref="Task{TResult}"/> that's completed successfully with the specified result.
        /// </summary>
        /// <typeparam name="TResult">The type of the result returned by the task.</typeparam>
        /// <param name="result">The result to store into the completed task.</param>
        /// <returns>The successfully completed task.</returns>
        public static Task<TResult> FromResult<TResult>(TResult result)
        {
#if !NET45 && NET40
            var tcs = new TaskCompletionSource<TResult>();
            tcs.SetResult(result);
            return tcs.Task;
#else
            return Task.FromResult<TResult>(result);
#endif
        }

        /// <summary>Creates a <see cref="Task{TResult}"/> that's completed exceptionally with the specified exception.</summary>
        /// <typeparam name="TResult">The type of the result returned by the task.</typeparam>
        /// <param name="exception">The exception with which to complete the task.</param>
        /// <returns>The faulted task.</returns>
        public static Task<TResult> FromException<TResult>(Exception exception)
        {
#if !NET46 && NET40
            var tcs = new TaskCompletionSource<TResult>();
            tcs.SetException(exception);
            return tcs.Task;
#else
            return Task.FromException<TResult>(exception);
#endif
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
#if !NET45 && NET40
            return Task.Factory.StartNew(action, CancellationToken.None,
                TaskCreationOptions.None, TaskScheduler.Default);
#else
            return Task.Run(action);
#endif
        }

        /// <summary>
        /// Queues the specified work to run on the ThreadPool and returns a
        /// <see cref="Task{TResult}"/> handle for that work.
        /// </summary>
        /// <typeparam name="TResult">The result type of the task.</typeparam>
        /// <param name="function">The work to execute asynchronously.</param>
        /// <returns>A <see cref="Task{TResult}"/> that represents the work queued to execute in the <see cref="ThreadPoolWaiter"/>.</returns>
        public static Task<TResult> Run<TResult>(Func<TResult> function)
        {
#if !NET45 && NET40
            return Task.Factory.StartNew(function, CancellationToken.None,
                TaskCreationOptions.None, TaskScheduler.Default);
#else
            return Task.Run<TResult>(function);
#endif
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
#if !NET45 && NET40
            Action proxy = () => { function().Wait(); };
            return Task.Factory.StartNew(proxy, CancellationToken.None,
                TaskCreationOptions.None, TaskScheduler.Default);
#else
            return Task.Run(function);
#endif
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
#if !NET45 && NET40
            Func<TResult> proxy = () =>
            {
                var task = function();
                task.Wait();
                return task.Result;
            };
            return Task.Factory.StartNew<TResult>(proxy, CancellationToken.None,
                TaskCreationOptions.None, TaskScheduler.Default);
#else
            return Task.Run(function);
#endif
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
#if !NET45 && NET40
            long totalMilliseconds = (long)delay.TotalMilliseconds;
            if (totalMilliseconds < -1 || totalMilliseconds > int.MaxValue)
            {
                throw new ArgumentOutOfRangeException("delay");
            }

            return Delay((int)totalMilliseconds);
#else
            return Task.Delay(delay);
#endif
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
#if !NET45 && NET40
            if (millisecondsDelay < -1)
                throw new ArgumentOutOfRangeException("millisecondsDelay");

            if (millisecondsDelay == 0)
                return Run(() => { });

            return Run(() =>
            {
                var wait = new ManualResetEvent(false);
                wait.WaitOne(millisecondsDelay);
                wait.Dispose();
                wait = null;
            });
#else
            return Task.Delay(millisecondsDelay);
#endif
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
#if !NET45 && NET40
            if (tasks == null)
                throw new ArgumentNullException("tasks");

            // Take a more efficient path if tasks is actually an array
            Task[] taskArray = tasks as Task[];
            return WhenAll(taskArray ?? tasks.ToArray());
#else
            return Task.WhenAll(tasks);
#endif
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
#if !NET45 && NET40
            if (tasks == null)
                throw new ArgumentNullException("tasks");

            foreach (var task in tasks)
            {
                if (task == null)
                    throw new ArgumentException("One task from provided task array is null");
            }

            if (tasks.Length == 0)
                return Run(() => { });

            return Run(() =>
            {
                Task.WaitAll(tasks);
            });
#else
            return Task.WhenAll(tasks);
#endif
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
#if !NET45 && NET40
            if (tasks == null)
                throw new ArgumentNullException("tasks");

            // Take a more efficient path if tasks is actually an array
            Task<TResult>[] taskArray = tasks as Task<TResult>[];
            return WhenAll(taskArray ?? tasks.ToArray());
#else
            return Task.WhenAll<TResult>(tasks);
#endif
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
#if !NET45 && NET40
            if (tasks == null)
                throw new ArgumentNullException("tasks");

            foreach (var task in tasks)
            {
                if (task == null)
                    throw new ArgumentException("One task from provided task array is null");
            }

            if (tasks.Length == 0)
                return Run<TResult[]>(() => new TResult[0]);

            return Run(() =>
            {
                Task.WaitAll(tasks);
                TResult[] results = new TResult[tasks.Length];
                for (int i = 0; i < tasks.Length; i++)
                    results[i] = tasks[i].Result;

                return results;
            });
#else
            return Task.WhenAll<TResult>(tasks);
#endif
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
#if !NET45 && NET40
            if (tasks == null)
                throw new ArgumentNullException("tasks");

            // Take a more efficient path if tasks is actually an array
            Task[] taskArray = tasks as Task[];
            return WhenAny(taskArray ?? tasks.ToArray());
#else
            return Task.WhenAny(tasks);
#endif
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
#if !NET45 && NET40
            if (tasks == null)
                throw new ArgumentNullException("tasks");
            if (tasks.Length == 0)
                throw new ArgumentException("At least one task is required to wait for completion");

            foreach (var task in tasks)
            {
                if (task == null)
                    throw new ArgumentException("One task from provided task array is null");
            }

            return Run<Task>(() =>
            {
                int idx = Task.WaitAny(tasks);
                return tasks[idx];
            });
#else
            return Task.WhenAny(tasks);
#endif
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
#if !NET45 && NET40
            if (tasks == null)
                throw new ArgumentNullException("tasks");

            // Take a more efficient path if tasks is actually an array
            Task<TResult>[] taskArray = tasks as Task<TResult>[];
            return WhenAny(taskArray ?? tasks.ToArray());
#else
            return Task.WhenAny<TResult>(tasks);
#endif
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
#if !NET45 && NET40
            if (tasks == null)
                throw new ArgumentNullException("tasks");
            if (tasks.Length == 0)
                throw new ArgumentException("At least one task is required to wait for completion");

            foreach (var task in tasks)
            {
                if (task == null)
                    throw new ArgumentException("One task from provided task array is null");
            }

            return Run<Task<TResult>>(() =>
            {
                int idx = Task.WaitAny(tasks);
                return tasks[idx];
            });
#else
            return Task.WhenAny<TResult>(tasks);
#endif
        }

        #endregion
    }
}
