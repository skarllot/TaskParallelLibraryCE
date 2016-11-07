using System.Threading;

namespace System.Threading
{
#if (WindowsCE || PCL) && !MOCK
    /// <summary>
    /// Represents a method to be called when a <see cref="WaitHandle"/> is signaled or times out.
    /// </summary>
    /// <param name="state">
    /// An object containing information to be used by the callback method each time it executes.
    /// </param>
    /// <param name="timedOut">
    /// true if the <see cref="WaitHandle"/> timed out; false if it was signaled.
    /// </param>
    public delegate void WaitOrTimerCallback(object state, bool timedOut);
#endif
}