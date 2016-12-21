namespace System.Threading.Compatibility
{
    /// <summary>
    /// Provides a portable <see cref="Sleep(int)"/>.
    /// </summary>
    public static class ThreadEx
    {
        /// <summary>
        /// Suspends the current thread for a specified time.
        /// </summary>
        /// <param name="millisecondsTimeout">
        /// The number of milliseconds for which the thread is blocked.
        /// </param>
        /// <exception cref="ArgumentOutOfRangeException">
        /// The time-out value is negative and is not equal to <see cref="Timeout.Infinite"/>.
        /// </exception>
        public static void Sleep(int millisecondsTimeout)
        {
#if NETSTANDARD1_0 || NETSTANDARD1_3 || Profile259
            Tasks.Task.Delay(millisecondsTimeout).Wait();
#elif Profile328
            Tasks.Compatibility.TaskEx.Delay(millisecondsTimeout);
#else
            Thread.Sleep(millisecondsTimeout);
#endif
        }
    }
}
