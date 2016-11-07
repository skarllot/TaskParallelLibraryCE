using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Threading;

namespace System.Compatibility.Tests
{
    [TestClass]
    public class ThreadPoolWaitTests
    {
        const int Timeout = 150;
        const int ListSize = 1000;

        [TestMethod]
        public void RegisterWaitForSingleObjectOnceTest()
        {
            int counter = 0;
            AutoResetEvent eventHandle = new AutoResetEvent(false);
            AutoResetEvent doneEventHandle = new AutoResetEvent(false);
            WaitOrTimerCallback callback = (state, timedOut) =>
            {
                Interlocked.Increment(ref counter);
                doneEventHandle.Set();
            };
            ThreadPoolEx.RegisterWaitForSingleObject(eventHandle, callback, null, Timeout, true);
            Threading.ThreadPool.QueueUserWorkItem(state => { eventHandle.Set(); });
            if (!doneEventHandle.WaitOne(Timeout * 2, false))
                Assert.Fail("Signal was not arrived in a timely manner");

            Assert.AreEqual(1, counter);
        }

        [TestMethod]
        public void RegisterWaitForSingleObjectContinuousTest()
        {
            int counter = 0;
            AutoResetEvent eventHandle = new AutoResetEvent(false);
            AutoResetEvent doneEventHandle = new AutoResetEvent(false);
            WaitOrTimerCallback callback = (state, timedOut) =>
            {
                Interlocked.Increment(ref counter);
                doneEventHandle.Set();
            };
            ThreadPoolEx.RegisterWaitForSingleObject(eventHandle, callback, null, Timeout, false);

            for (int i = 0; i < ListSize; i++)
            {
                ThreadPool.QueueUserWorkItem(state => { eventHandle.Set(); });
                if (!doneEventHandle.WaitOne(Timeout * 2, false))
                    Assert.Fail("Signal was not arrived in a timely manner");

                Assert.AreEqual(1 + i, counter);
            }
        }

        [TestMethod]
        public void RegisterWaitForSingleObjectOnceTimeoutTest()
        {
            int counter = 0;
            AutoResetEvent eventHandle = new AutoResetEvent(false);
            AutoResetEvent doneEventHandle = new AutoResetEvent(false);
            WaitOrTimerCallback callback = (state, timedOut) =>
            {
                if (timedOut)
                    Interlocked.Increment(ref counter);
                else
                    Interlocked.Decrement(ref counter);

                doneEventHandle.Set();
            };
            ThreadPoolEx.RegisterWaitForSingleObject(eventHandle, callback, null, Timeout, true);
            if (!doneEventHandle.WaitOne(Timeout * 4, false))
                Assert.Fail("Signal was not arrived in a timely manner");

            Assert.AreEqual(1, counter, "The callback should be called");
        }

        [TestMethod]
        public void RegisterWaitForSingleObjectOnceMultipleTest()
        {
            // Register wait handles
            int counter = 0;
            List<AutoResetEvent> eventHandles = new List<AutoResetEvent>(ListSize);
            WaitOrTimerCallback callback = (state, timedOut) => Interlocked.Increment(ref counter);
            for (int i = 0; i < ListSize; i++)
            {
                AutoResetEvent current = new AutoResetEvent(false);
                Assert.IsNotNull(current);
                eventHandles.Add(current);
                ThreadPoolEx.RegisterWaitForSingleObject(current, callback, null, Timeout, true);
            }

            // Enqueue to another thread to send signals
            Threading.ThreadPool.QueueUserWorkItem(state =>
            {
                foreach (var item in eventHandles)
                    Assert.IsTrue(item.Set());
            });

            // Setup timeout
            var timeoutTask = Threading.Tasks.Task.Delay(Timeout);

            // Waits for all callbacks until completes or timeout
            while (!timeoutTask.Wait(1))
            {
                if (counter == ListSize)
                    break;
            }

            if (counter != ListSize)
                Assert.Fail("Signal was not arrived in a timely manner");
        }

        [TestMethod]
        public void RegisterWaitForSingleObjectOnceSerializedTest()
        {
            int counter = 0;
            AutoResetEvent eventHandle = new AutoResetEvent(false);
            AutoResetEvent doneEventHandle = new AutoResetEvent(false);
            WaitOrTimerCallback callback = (state, timedOut) =>
            {
                Interlocked.Increment(ref counter);
                doneEventHandle.Set();
            };

            for (int i = 0; i < ListSize; i++)
            {
                ThreadPoolEx.RegisterWaitForSingleObject(eventHandle, callback, null, Timeout, true);
                Threading.ThreadPool.QueueUserWorkItem(state => { eventHandle.Set(); });
                if (!doneEventHandle.WaitOne(Timeout * 2, false))
                    Assert.Fail("{0}: Signal was not arrived in a timely manner", i);

                Assert.AreEqual(1 + i, counter);
                Thread.Sleep(0);
            }
        }
    }
}