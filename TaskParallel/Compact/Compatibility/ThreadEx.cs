#if PCL && NET45
using System.Threading.Tasks;
#endif

namespace System.Threading.Compatibility
{
    /// <summary>
    /// Provides a portable <see cref="Sleep(int)"/>.
    /// </summary>
    public static class ThreadEx
    {
#if (!NET45 && PCL)
        static readonly ManualResetEvent _sleep = new ManualResetEvent(false);
#endif
        
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
#if (!NET45 && PCL)
            // Enforce thread suspend
            if (millisecondsTimeout == 0)
                millisecondsTimeout = 1;

            _sleep.WaitOne(millisecondsTimeout);
#elif PCL
            Task.Delay(millisecondsTimeout).Wait();
#else
            Thread.Sleep(millisecondsTimeout);
#endif
        }
    }
}
