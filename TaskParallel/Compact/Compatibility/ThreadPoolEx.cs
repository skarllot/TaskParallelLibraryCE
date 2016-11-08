#if WindowsCE
using System.Collections.Generic;
using System.Linq;
#elif PCL
using System.Threading.Tasks;
#endif

namespace System.Threading.Compatibility
{
    /// <summary>
    /// Provides a thread that can be used to wait on behalf of other threads, and process timers.
    /// </summary>
    public static class ThreadPoolEx
    {
#if WindowsCE
        static readonly Thread _thread;
        static readonly AutoResetEvent _addEvent = new AutoResetEvent(false);
        static readonly AutoResetEvent _unregisterEvent = new AutoResetEvent(false);
        static readonly AutoResetEvent _doneInteration = new AutoResetEvent(false);
        static volatile WaitEntry _addQueue = null;
        static volatile int _unregisterQueue = -1;

        static ThreadPoolEx()
        {
            _thread = new Thread(WaitsHandler);
            _thread.IsBackground = true;
            _thread.Priority = ThreadPriority.BelowNormal;
            _thread.Start();
        }

        private static void WaitsHandler()
        {
            List<WaitEntry> registeredWaits = new List<WaitEntry>();

            while (true)
            {
                int closestTimeout = Timeout.Infinite;
                //int closestTimeout = 500;

                if (registeredWaits.Count > 0)
                    closestTimeout = registeredWaits.Min(w => w.RemainingTime);
                if (closestTimeout < -1)
                    closestTimeout = 0;

                WaitHandle[] waitEntries = new WaitHandle[registeredWaits.Count + 2];
                waitEntries[0] = _addEvent;
                waitEntries[1] = _unregisterEvent;

                if (waitEntries.Length > 2)
                {
                    for (int i = 2; i < waitEntries.Length; i++)
                        waitEntries[i] = registeredWaits[i - 2].WaitObject;
                }

                int signalIndex = WaitHandleEx.WaitAny(waitEntries, closestTimeout);
                if (signalIndex > 1 && signalIndex != WaitHandleEx.WaitTimeout)
                {
                    WaitEntry signedEntry = registeredWaits[signalIndex - 2];
                    int remaintingTime = signedEntry.RemainingTime;
                    ThreadPool.QueueUserWorkItem(WaitHandlerCallback,
                        new WaitCallbackArgs(signedEntry.Callback, signedEntry.State, remaintingTime <= 0));

                    if (signedEntry.ExecuteOnlyOnce)
                        registeredWaits.RemoveAt(signalIndex - 2);
                    else
                        signedEntry.Reset();
                }
                else if (signalIndex == 0)
                {
                    lock (_addEvent)
                    {
                        if (_addQueue == null)
                            throw new Exception("Should not try to register null wait entry");

                        registeredWaits.Add(_addQueue);
                        _addQueue = null;
                        _addEvent.Reset();
                    }
                }
                else if (signalIndex == 1)
                {
                    lock (_unregisterEvent)
                    {
                        if (_unregisterQueue == -1)
                            throw new Exception("Should not try to unregister with uninitialized identifier");

                        for (int i = 0; i < registeredWaits.Count; i++)
                        {
                            if (registeredWaits[i].Id == _unregisterQueue)
                            {
                                registeredWaits.RemoveAt(i);
                                break;
                            }
                        }

                        _unregisterQueue = -1;
                        _unregisterEvent.Reset();
                    }
                }

                _doneInteration.Set();

                for (int i = registeredWaits.Count - 1; i >= 0; i--)
                {
                    WaitEntry current = registeredWaits[i];
                    if (current == null)
                        throw new Exception("Registed null wait entry at: " + i.ToString());

                    if (current.RemainingTime <= 0)
                    {
                        ThreadPool.QueueUserWorkItem(WaitHandlerCallback,
                            new WaitCallbackArgs(current.Callback, current.State, true));

                        if (current.ExecuteOnlyOnce)
                            registeredWaits.RemoveAt(i);
                        else
                            current.Reset();
                    }
                }

                Thread.Sleep(1);
            }
        }

        private static void WaitHandlerCallback(object stateObject)
        {
            if (!(stateObject is WaitCallbackArgs))
                throw new InvalidOperationException("Invalid stateObject, should be WaitCallbackArgs");

            WaitCallbackArgs waitEntry = (WaitCallbackArgs)stateObject;
            waitEntry.Callback(waitEntry.State, waitEntry.TimedOut);
        }
#endif

        /// <summary>
        /// Registers a delegate to wait for a WaitHandle, specifying a 32-bit
        /// signed integer for the time-out in milliseconds.
        /// </summary>
        /// <param name="waitObject">The <see cref="WaitHandle"/> to register. Use a <see cref="WaitHandle"/> other than <see cref="Mutex"/>.</param>
        /// <param name="callBack">The <see cref="WaitOrTimerCallback"/> delegate to call when the <paramref name="waitObject"/> parameter is signaled.</param>
        /// <param name="state">The object that is passed to the delegate.</param>
        /// <param name="millisecondsTimeOutInterval">
        /// The time-out in milliseconds. If the <paramref name="millisecondsTimeOutInterval"/> parameter is 0 (zero), the function tests the object's state
        /// and returns immediately. If <paramref name="millisecondsTimeOutInterval"/> is -1, the function's time-out interval never elapses.
        /// </param>
        /// <param name="executeOnlyOnce">
        /// true to indicate that the thread will no longer wait on the <paramref name="waitObject"/> parameter after the delegate has been called;
        /// false to indicate that the timer is reset every time the wait operation completes until the wait is unregistered.
        /// </param>
        public static RegisteredWaitHandle RegisterWaitForSingleObject(
            WaitHandle waitObject, WaitOrTimerCallback callBack, object state,
            long millisecondsTimeOutInterval, bool executeOnlyOnce)
        {
#if WindowsCE || PCL
            if (millisecondsTimeOutInterval < -1 || millisecondsTimeOutInterval > int.MaxValue)
            {
                throw new ArgumentOutOfRangeException("millisecondsTimeOutInterval");
            }

            return RegisterWaitForSingleObject(waitObject, callBack, state, (int)millisecondsTimeOutInterval, executeOnlyOnce);
#else
            return Threading.ThreadPool.RegisterWaitForSingleObject(
                waitObject, callBack, state, millisecondsTimeOutInterval, executeOnlyOnce);
#endif
        }

        /// <summary>
        /// Registers a delegate to wait for a WaitHandle, specifying a 32-bit
        /// signed integer for the time-out in milliseconds.
        /// </summary>
        /// <param name="waitObject">The <see cref="WaitHandle"/> to register. Use a <see cref="WaitHandle"/> other than <see cref="Mutex"/>.</param>
        /// <param name="callBack">The <see cref="WaitOrTimerCallback"/> delegate to call when the <paramref name="waitObject"/> parameter is signaled.</param>
        /// <param name="state">The object that is passed to the delegate.</param>
        /// <param name="millisecondsTimeOutInterval">
        /// The time-out in milliseconds. If the <paramref name="millisecondsTimeOutInterval"/> parameter is 0 (zero), the function tests the object's state
        /// and returns immediately. If <paramref name="millisecondsTimeOutInterval"/> is -1, the function's time-out interval never elapses.
        /// </param>
        /// <param name="executeOnlyOnce">
        /// true to indicate that the thread will no longer wait on the <paramref name="waitObject"/> parameter after the delegate has been called;
        /// false to indicate that the timer is reset every time the wait operation completes until the wait is unregistered.
        /// </param>
        /// <exception cref="ArgumentNullException"><paramref name="waitObject"/> or <paramref name="callBack"/> are null.</exception>
        public static RegisteredWaitHandle RegisterWaitForSingleObject(
            WaitHandle waitObject, WaitOrTimerCallback callBack, object state,
            int millisecondsTimeOutInterval, bool executeOnlyOnce)
        {
#if WindowsCE || PCL
            if (waitObject == null)
                throw new ArgumentNullException("waitObject");
            if (callBack == null)
                throw new ArgumentNullException("callback");
#endif

#if WindowsCE
            var entry = new WaitEntry(waitObject, callBack, state,
                millisecondsTimeOutInterval, executeOnlyOnce);

            lock (_thread)
            {
                lock (_addEvent)
                {
                    if (_addQueue != null)
                        throw new Exception("The previous wait entry on queue should be consumed already");

                    _addQueue = entry;
                }

                _addEvent.Set();
                Thread.Sleep(1);
                _doneInteration.WaitOne();
            }

            return new RegisteredWaitHandle(entry.Id);
#elif PCL
            ManualResetEvent unregisterEvent = new ManualResetEvent(false);
            Action internalCallback = () =>
            {
                int id;
                if (millisecondsTimeOutInterval > -1)
                    id = WaitHandle.WaitAny(
                        new WaitHandle[] { waitObject, unregisterEvent },
                        millisecondsTimeOutInterval);
                else
                    id = WaitHandle.WaitAny(
                        new WaitHandle[] { waitObject, unregisterEvent });

                if (id == 0)
                    callBack(state, false);
                if (id == WaitHandle.WaitTimeout)
                    callBack(state, true);
            };
            Tasks.Compatibility.TaskEx.Run(internalCallback);
            return new RegisteredWaitHandle(unregisterEvent);
#else
            return ThreadPool.RegisterWaitForSingleObject(
                waitObject, callBack, state, millisecondsTimeOutInterval, executeOnlyOnce);
#endif
        }

#if WindowsCE || PCL
        /// <summary>
        /// Represents a handle that has been registered when calling
        /// <see cref="RegisterWaitForSingleObject(WaitHandle, WaitOrTimerCallback, object, int, bool)"/>.
        /// This class cannot be inherited.
        /// </summary>
        public sealed class RegisteredWaitHandle
        {
#if WindowsCE
            private readonly int _waitId;

            internal RegisteredWaitHandle(int waitId)
            {
                _waitId = waitId;
            }
#else
            private readonly ManualResetEvent _unregisterEvent;

            internal RegisteredWaitHandle(ManualResetEvent unregisterEvent)
            {
                _unregisterEvent = unregisterEvent;
            }
#endif

            /// <summary>
            /// Cancels a registered wait operation issued by the
            /// <see cref="RegisterWaitForSingleObject(WaitHandle, WaitOrTimerCallback, object, int, bool)"/> method.
            /// </summary>
            /// <param name="waitObject">The <see cref="WaitHandle"/> to be signaled.</param>
            /// <returns>true if the function succeeds; otherwise, false.</returns>
            public bool Unregister(WaitHandle waitObject)
            {
                if (waitObject != null)
                    throw new NotImplementedException("Unregister with WaitHandle is not supported");

#if WindowsCE
                lock (_thread)
                {
                    lock (_unregisterEvent)
                    {
                        if (_unregisterQueue != -1)
                            throw new Exception("The previous wait identifier on unregister queue should be consumed already");

                        _unregisterQueue = _waitId;
                    }

                    _unregisterEvent.Set();
                    Thread.Sleep(1);
                    _doneInteration.WaitOne();
                }

                return _unregisterQueue != _waitId;
#else
                return _unregisterEvent.Set();
#endif
            }
        }
#endif

#if WindowsCE
        internal sealed class WaitEntry : IDisposable
        {
            private static int _idCounter = 0;
            private readonly Diagnostics.Stopwatch _timeTracker;
            private readonly int _id;

            public WaitHandle WaitObject { get; set; }
            public WaitOrTimerCallback Callback { get; set; }
            public object State { get; set; }
            public int Timeout { get; set; }
            public bool ExecuteOnlyOnce { get; set; }

            public int Id
            {
                get { return _id; }
            }

            public int RemainingTime
            {
                get
                {
                    if (Timeout == -1)
                        return int.MaxValue;
                    else if (Timeout == 0)
                        return 0;

                    return Timeout - (int)_timeTracker.ElapsedMilliseconds;
                }
            }

            public WaitEntry(
                WaitHandle waitObject, WaitOrTimerCallback callback, object state,
                int timeout, bool executeOnlyOnce)
            {
                _timeTracker = new Diagnostics.Stopwatch();
                _timeTracker.Start();
                WaitObject = waitObject;
                Callback = callback;
                State = state;
                Timeout = timeout;
                ExecuteOnlyOnce = executeOnlyOnce;
                _id = Interlocked.Increment(ref _idCounter);
            }

            public void Reset()
            {
                _timeTracker.Reset();
            }

            public void Dispose()
            {
                _timeTracker.Stop();
            }
        }

        private struct WaitCallbackArgs
        {
            public WaitOrTimerCallback Callback { get; set; }
            public object State { get; set; }
            public bool TimedOut { get; set; }

            public WaitCallbackArgs(WaitOrTimerCallback callback, object state, bool timedOut)
                : this()
            {
                Callback = callback;
                State = state;
                TimedOut = timedOut;
            }
        }
#endif
    }
}
