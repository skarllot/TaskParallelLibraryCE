using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Threading;

namespace TaskParallel.Tests
{
    /// <summary>
    ///This is a test class for TaskTest and is intended
    ///to contain all TaskTest Unit Tests
    ///</summary>
    [TestClass()]
    public class TaskTest
    {
        public const int NESTING_COUNT = 1000;

        /// <summary>
        ///A test for IsFaulted
        ///</summary>
        [TestMethod()]
        public void IsFaultedTest()
        {
            Action action = () => { };
            Task target = new Task(action);
            target.Start();

            Action actionEx = () => { throw new Exception(); };
            Task targetEx = new Task(actionEx);
            targetEx.Start();

            target.Wait();
            targetEx.Wait();

            Assert.IsFalse(target.IsFaulted);
            Assert.IsTrue(targetEx.IsFaulted);
        }

        /// <summary>
        ///A test for IsCompleted
        ///</summary>
        [TestMethod()]
        public void IsCompletedTest()
        {
            Action action = () => Thread.Sleep(100);
            Task target = new Task(action);
            target.Start();
            Assert.IsFalse(target.IsCompleted);
            target.Wait();
            Assert.IsTrue(target.IsCompleted);
        }

        /// <summary>
        ///A test for ExceptionRecorded
        ///</summary>
        [TestMethod()]
        public void ExceptionRecordedTest()
        {
            Action action = () => { };
            Task target = new Task(action);
            target.Start();

            Action actionEx = () => { throw new Exception(); };
            Task targetEx = new Task(actionEx);
            targetEx.Start();

            target.Wait();
            targetEx.Wait();

            Assert.IsFalse(target.ExceptionRecorded);
            Assert.IsTrue(targetEx.ExceptionRecorded);
        }

        /// <summary>
        ///A test for Exception
        ///</summary>
        [TestMethod()]
        public void ExceptionTest()
        {
            Action action = () => { throw new ArgumentNullException("none"); };
            Task target = new Task(action);
            target.Start();
            target.Wait();

            var ex = target.Exception as ArgumentNullException;
            Assert.IsNotNull(ex);
            Assert.AreEqual("Value can not be null.\r\nParameter name: none", ex.Message);
        }

        /// <summary>
        ///A test for CompletedSynchronously
        ///</summary>
        [TestMethod()]
        public void CompletedSynchronouslyTest()
        {
            int counter = 0;
            Action action = () => { Interlocked.Increment(ref counter); };
            Task target = new Task(action);
            target.Start();
            target.Wait();
            Assert.IsFalse(target.CompletedSynchronously);
            Assert.AreEqual(1, counter);

            target = new Task(action);
            target.RunSynchronously();
            Assert.IsTrue(target.CompletedSynchronously);
            Assert.AreEqual(2, counter);
        }

        /// <summary>
        ///A test for AsyncWaitHandle
        ///</summary>
        [TestMethod()]
        public void AsyncWaitHandleTest()
        {
            int counter = 0;
            Action action = () => { Interlocked.Increment(ref counter); };
            Task target = new Task(action);
            target.Start();
            Assert.IsTrue(target.AsyncWaitHandle.WaitOne());
            Assert.AreEqual(1, counter);
        }

        /// <summary>
        ///A test for AsyncState
        ///</summary>
        [TestMethod()]
        public void AsyncStateTest()
        {
            object refobj = new object();
            int counter = 0;
            Action action = () => { Interlocked.Increment(ref counter); };
            Task target = new Task(action);
            target.BeginStart(ar =>
            {
                Assert.AreEqual(refobj, ar.AsyncState);
                target.EndStart(ar);
            }, refobj);
            Assert.AreEqual(refobj, target.AsyncState);
            target.Wait(100);
        }

        /// <summary>
        ///A test for Wait
        ///</summary>
        [TestMethod()]
        public void WaitTest2()
        {
            Action action = () => Thread.Sleep(100);
            Task target = new Task(action);
            target.Start();
            Assert.IsFalse(target.Wait(0));
            Assert.IsTrue(target.Wait(500));
        }

        /// <summary>
        ///A test for Wait
        ///</summary>
        [TestMethod()]
        public void WaitTest1()
        {
            Action action = () => Thread.Sleep(100);
            Task target = new Task(action);
            target.Start();
            Assert.IsFalse(target.Wait(new TimeSpan(0)));
            Assert.IsTrue(target.Wait(new TimeSpan(0, 0, 1)));
        }

        /// <summary>
        ///A test for Wait
        ///</summary>
        [TestMethod()]
        public void WaitTest()
        {
            int counter = 0;
            Action action = () => Interlocked.Increment(ref counter);
            Task target = new Task(action);
            target.Start();
            target.Wait();
            Assert.AreEqual(1, counter);
        }

        /// <summary>
        ///A test for TaskCallback
        ///</summary>
        [TestMethod()]
        [DeploymentItem("TaskParallel.dll")]
        public void ExecuteUserWorkTest()
        {
            int counter = 0;
            Action action = () => Interlocked.Increment(ref counter);
            Task_Accessor target = new Task_Accessor(action);
            target.ExecuteQueue(null);
            Assert.AreEqual(1, counter);
        }

        /// <summary>
        ///A test for Start
        ///</summary>
        [TestMethod()]
        public void StartTest()
        {
            int counter = 0;
            Action action = () => Interlocked.Increment(ref counter);
            Task target = new Task(action);
            target.Start();
            Thread.Sleep(50);
            Assert.AreEqual(1, counter);
        }

        /// <summary>
        ///A test for RunSynchronously
        ///</summary>
        [TestMethod()]
        public void RunSynchronouslyTest()
        {
            int counter = 0;
            Action action = () => Interlocked.Increment(ref counter);
            Task target = new Task(action);
            target.RunSynchronously();
            Assert.AreEqual(1, counter);
        }

        /// <summary>
        ///A test for EnsureStartOnce
        ///</summary>
        [TestMethod()]
        [DeploymentItem("TaskParallel.dll")]
        [ExpectedException(typeof(InvalidOperationException))]
        public void EnsureStartOnceTest()
        {
            Action action = () => { };
            Task_Accessor target = new Task_Accessor(action);
            target.EnsureStartOnce();
            target.EnsureStartOnce();
        }

        /// <summary>
        ///A test for EndStart
        ///</summary>
        [TestMethod()]
        public void EndStartTest()
        {
            Exception ex = new Exception("none");
            Action action = () => { throw ex; };
            Task target = new Task(action);
            var ar = target.BeginStart(null, null);
            target.EndStart(ar);

            Assert.AreEqual(ex, target.Exception);
        }

        /// <summary>
        ///A test for Dispose
        ///</summary>
        [TestMethod()]
        public void DisposeTest()
        {
            Action action = () => { };
            Task target = new Task(action);
            target.Dispose();
            target.Start();
        }

        /// <summary>
        ///A test for ContinueWith
        ///</summary>
        [TestMethod()]
        public void ContinueWithTest()
        {
            int value = 1;
            Action action = () => Interlocked.CompareExchange(ref value, 2, 1);
            Task target = new Task(action);
            Action action2 = () => Interlocked.CompareExchange(ref value, 3, 2);
            Task target2 = target.ContinueWith(action2);
            target2.Start();
            target2.Wait(100);
            Assert.IsTrue(target.IsCompleted);
            Assert.IsTrue(target2.IsCompleted);
            Assert.AreEqual(3, value);
        }

        /// <summary>
        ///A test for BeginStart
        ///</summary>
        [TestMethod()]
        public void BeginStartTest()
        {
            int counter = 0;
            Action action = () => Interlocked.Increment(ref counter);
            Task target = new Task(action);
            target.BeginStart(ar =>
            {
                target.EndStart(ar);
                Assert.AreEqual(1, counter);
            }, null);
            target.Wait();
        }

        /// <summary>
        ///A test for Task Constructor
        ///</summary>
        [TestMethod()]
        public void TaskConstructorTest1()
        {
            int counter = 0;
            Action action = () => Interlocked.Increment(ref counter);
            Task target = new Task(action);
            target.Start();
            target.Wait();
            Assert.AreEqual(1, counter);
        }

        /// <summary>
        ///A test for Task Constructor
        ///</summary>
        [TestMethod()]
        public void TaskConstructorTest()
        {
            int[] values = new int[] { 0 };
            Action<object> action = obj =>
            {
                int[] v = obj as int[];
                Assert.IsNotNull(v);
                Interlocked.Increment(ref v[0]);
            };
            Task target = new Task(action, values);
            target.Start();
            target.Wait();
            Assert.AreEqual(1, values[0]);
        }

        [TestMethod()]
        public void TaskDeepNesting()
        {
            int counter = 0;
            Action action = () => Interlocked.Increment(ref counter);
            var task = new Task(action);
            for (int i = 0; i < NESTING_COUNT; i++)
            {
                task = task.ContinueWith(action);
            }

            Assert.IsNotNull(task);
            task.Start();
            task.Wait();
            Assert.AreEqual(NESTING_COUNT + 1, counter);
            Assert.IsNull(task.Exception);
        }
    }
}
