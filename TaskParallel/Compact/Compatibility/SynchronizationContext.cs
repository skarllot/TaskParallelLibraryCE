#if WindowsCE
using System.Collections.Generic;

namespace System.Threading.Compatibility
{
    /// <summary>
    /// Represents a method to be called when a message is to be dispatched to
    /// a synchronization context.
    /// </summary>
    /// <param name="state">The object passed to the delegate.</param>
    public delegate void SendOrPostCallback(object state);

    /// <summary>
    /// Provides the basic functionality for propagating a synchronization
    /// context in various synchronization models.
    /// </summary>
    public class SynchronizationContext
    {
        private static readonly Dictionary<int, SynchronizationContext> _syncContexts = new Dictionary<int, SynchronizationContext>();

        /// <summary>
        /// Initializes a new instance of <see cref="SynchronizationContext"/> class.
        /// </summary>
        public SynchronizationContext() { }

        /// <summary>
        /// Gets the synchronization context for the current thread.
        /// </summary>
        public static SynchronizationContext Current
        {
            get
            {
                int id = Thread.CurrentThread.ManagedThreadId;

                lock (_syncContexts)
                {
                    if (_syncContexts.ContainsKey(id))
                        return _syncContexts[id];
                }

                return null;
            }
        }

        /// <summary>
        /// Sets the current synchronization context.
        /// </summary>
        /// <param name="syncContext">
        /// The <see cref="SynchronizationContext"/> object to be set.
        /// </param>
        public static void SetSynchronizationContext(SynchronizationContext syncContext)
        {
            lock (_syncContexts)
            {
                int id = Thread.CurrentThread.ManagedThreadId;
                if (_syncContexts.ContainsKey(id))
                    _syncContexts[id] = syncContext;
                else
                    _syncContexts.Add(id, syncContext);
            }
        }

        /// <summary>
        /// When overridden in a derived class, dispatches a synchronous
        /// message to a synchronization context.
        /// </summary>
        /// <param name="d">The <see cref="SendOrPostCallback"/> delegate to call.</param>
        /// <param name="state">The object passed to the delegate.</param>
        public virtual void Send(SendOrPostCallback d, object state)
        {
            d(state);
        }

        /// <summary>
        /// When overridden in a derived class, dispatches an asynchronous
        /// message to a synchronization context.
        /// </summary>
        /// <param name="d">The <see cref="SendOrPostCallback"/> delegate to call.</param>
        /// <param name="state">The object passed to the delegate.</param>
        public virtual void Post(SendOrPostCallback d, object state)
        {
            ThreadPool.QueueUserWorkItem(new WaitCallback(d.Invoke), state);
        }

        /// <summary>
        /// When overridden in a derived class, creates a copy of the
        /// synchronization context.
        /// </summary>
        /// <returns>A new <see cref="SynchronizationContext"/> object.</returns>
        public virtual SynchronizationContext CreateCopy()
        {
            return new SynchronizationContext();
        }
    }
}
#endif
