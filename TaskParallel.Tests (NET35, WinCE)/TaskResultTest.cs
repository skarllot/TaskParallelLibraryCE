using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Threading;

namespace TaskParallel.Tests
{
    /// <summary>
    ///This is a test class for TaskResultTest and is intended
    ///to contain all TaskResultTest Unit Tests
    ///</summary>
    [TestClass()]
    public class TaskResultTest
    {
        /// <summary>
        ///A test for Result
        ///</summary>
        public void ResultTestHelper<TResult>(TResult expected)
        {
            Func<TResult> action = () => expected;
            Task<TResult> target = new Task<TResult>(action);
            target.Start();
            target.Wait();
            Assert.AreEqual<TResult>(expected, target.Result);
        }

        [TestMethod()]
        public void ResultTestInteger()
        {
            ResultTestHelper<int>(20);
        }

        [TestMethod()]
        public void ResultTestString()
        {
            ResultTestHelper<string>("Lorem Ipsum");
        }

        /// <summary>
        ///A test for IsFaulted
        ///</summary>
        [TestMethod()]
        public void IsFaultedTest()
        {
            Func<int> action = () => 1;
            Task<int> target = new Task<int>(action);
            target.Start();

            Func<int> actionEx = () => { throw new Exception(); };
            Task<int> targetEx = new Task<int>(actionEx);
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
            Func<int> action = () => { Thread.Sleep(100); return 1; };
            Task<int> target = new Task<int>(action);
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
            Func<int> action = () => 1;
            Task<int> target = new Task<int>(action);
            target.Start();

            Func<int> actionEx = () => { throw new Exception(); };
            Task<int> targetEx = new Task<int>(actionEx);
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
            Func<int> action = () => { throw new ArgumentNullException("none"); };
            Task<int> target = new Task<int>(action);
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
            Func<int> action = () => Interlocked.Increment(ref counter);
            Task<int> target = new Task<int>(action);
            target.Start();
            target.Wait();
            Assert.IsFalse(target.CompletedSynchronously);
            Assert.AreEqual(1, counter);

            target = new Task<int>(action);
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
            Func<int> action = () => Interlocked.Increment(ref counter);
            Task<int> target = new Task<int>(action);
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
            Func<int> action = () => Interlocked.Increment(ref counter);
            Task<int> target = new Task<int>(action);
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
            Func<int> action = () => { Thread.Sleep(100); return 1; };
            Task<int> target = new Task<int>(action);
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
            Func<int> action = () => { Thread.Sleep(100); return 1; };
            Task<int> target = new Task<int>(action);
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
            Func<int> action = () => Interlocked.Increment(ref counter);
            Task<int> target = new Task<int>(action);
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
            Func<int> action = () => Interlocked.Increment(ref counter);
            Task_Accessor<int> target = new Task_Accessor<int>(action);
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
            Func<int> action = () => Interlocked.Increment(ref counter);
            Task<int> target = new Task<int>(action);
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
            Func<int> action = () => Interlocked.Increment(ref counter);
            Task<int> target = new Task<int>(action);
            var result = target.RunSynchronously();
            Assert.AreEqual(1, result);
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
            Func<int> action = () => 1;
            Task_Accessor<int> target = new Task_Accessor<int>(action);
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
            Func<int> action = () => { throw ex; };
            Task<int> target = new Task<int>(action);
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
            Func<int> action = () => 1;
            Task<int> target = new Task<int>(action);
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
            Func<int> action = () => Interlocked.CompareExchange(ref value, 2, 1);
            Task<int> target = new Task<int>(action);
            Func<int> action2 = () => Interlocked.CompareExchange(ref value, 3, 2);
            Task<int> target2 = target.ContinueWith(action2);
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
            Func<int> action = () => Interlocked.Increment(ref counter);
            Task<int> target = new Task<int>(action);
            target.BeginStart(ar =>
            {
                target.EndStart(ar);
                Assert.AreEqual(1, counter);
                Assert.IsNull(target.Exception);
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
            Func<int> action = () => Interlocked.Increment(ref counter);
            Task<int> target = new Task<int>(action);
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
            Func<object, int> action = obj =>
            {
                int[] v = obj as int[];
                Assert.IsNotNull(v);
                return Interlocked.Increment(ref v[0]);
            };
            Task<int> target = new Task<int>(action, values);
            target.Start();
            target.Wait();
            Assert.AreEqual(1, values[0]);
        }

        [TestMethod()]
        public void TaskDeepNesting()
        {
            int counter = 0;
            Func<int> action = () => Interlocked.Increment(ref counter);
            var task = new Task<int>(action);
            for (int i = 0; i < TaskTest.NESTING_COUNT; i++)
            {
                task = task.ContinueWith(action);
            }

            Assert.IsNotNull(task);
            task.Start();
            task.Wait();
            Assert.AreEqual(TaskTest.NESTING_COUNT + 1, counter);
            Assert.IsNull(task.Exception);
        }
    }
}
