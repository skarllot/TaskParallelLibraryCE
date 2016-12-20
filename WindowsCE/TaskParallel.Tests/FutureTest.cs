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
    public class FutureTest
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
            try { targetEx.Wait(); }
            catch (Exception) { }

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
        ///A test for Exception
        ///</summary>
        [TestMethod()]
        public void ExceptionTest()
        {
            Func<int> action = () => { throw new ArgumentNullException("none"); };
            Task<int> target = new Task<int>(action);
            target.Start();
            try { target.Wait(); }
            catch (AggregateException) { }

            Assert.AreEqual(1, target.Exception.InnerExceptions.Count);
            var ex = target.Exception.InnerExceptions[0] as ArgumentNullException;
            Assert.IsNotNull(ex);
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
            Assert.IsFalse(((IAsyncResult)target).CompletedSynchronously);
            Assert.AreEqual(1, counter);

            target = new Task<int>(action);
            target.RunSynchronously();
#if !NET35
            Assert.IsTrue(((IAsyncResult)target).CompletedSynchronously);
            Assert.AreEqual(2, counter);
#endif
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
            Assert.IsTrue(((IAsyncResult)target).AsyncWaitHandle.WaitOne());
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
            Func<object, int> action = state =>
            {
                Assert.AreEqual(refobj, state);
                return Interlocked.Increment(ref counter);
            };
            Task<int> target = new Task<int>(action, refobj);
            target.Start();
            target.Wait();
            Assert.AreEqual(refobj, target.AsyncState);
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

#if WindowsCE
        /// <summary>
        ///A test for TaskCallback
        ///</summary>
        [TestMethod()]
        [DeploymentItem("TaskParallel.dll")]
        public void TaskStartActionTest()
        {
            int counter = 0;
            Func<int> action = () => Interlocked.Increment(ref counter);
            Task_Accessor<int> target = new Task_Accessor<int>(action);
            target.TaskStartAction(null);
            Assert.AreEqual(1, counter);
        }
#endif

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
            target.RunSynchronously();
            var result = target.Result;
            Assert.AreEqual(1, result);
            Assert.AreEqual(1, counter);
        }

#if WindowsCE

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
        ///A test for EndWait
        ///</summary>
        [TestMethod()]
        public void EndWaitTest()
        {
            Exception ex = new ArgumentNullException("none");
            Func<int> action = () => { throw ex; };
            Task<int> target = new Task<int>(action);
            target.Start();
            var ar = target.BeginWait(null, null);

            bool throwException = false;
            try { target.EndWait(ar); }
            catch (AggregateException)
            {
                throwException = true;
            }

            Assert.IsTrue(throwException);
            Assert.AreEqual(1, target.Exception.InnerExceptions.Count);
            Assert.IsTrue(target.Exception.InnerExceptions[0] is ArgumentNullException);
        }

#endif

        /// <summary>
        ///A test for Dispose
        ///</summary>
        [TestMethod()]
        public void DisposeTest()
        {
            Func<int> action = () => 1;
            Task<int> target = new Task<int>(action);

            bool throwException = false;
            try { target.Dispose(); }
            catch (InvalidOperationException)
            {
                throwException = true;
            }
            Assert.IsTrue(throwException, "Should not dispose a task that is not completed");

            target.Start();
            target.Wait();
            target.Dispose();
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
            target.Start();
            Func<Task, int> continueFunction = t =>
            {
                Assert.IsNotNull(t);
                Assert.IsFalse(t.IsFaulted);
                return Interlocked.CompareExchange(ref value, 3, 2);
            };
            Task<int> target2 = target.ContinueWith(continueFunction);
            if (!target2.Wait(100))
                Assert.Fail("Timeout waiting for continuation task signal");

            Assert.IsTrue(target.IsCompleted);
            Assert.IsTrue(target2.IsCompleted);
            Assert.AreEqual(3, value);
        }

#if WindowsCE

        /// <summary>
        ///A test for BeginWait
        ///</summary>
        [TestMethod()]
        public void BeginWaitTest()
        {
            int counter = 0;
            Func<int> action = () => Interlocked.Increment(ref counter);
            Task<int> target = new Task<int>(action);
            target.Start();
            target.BeginWait(ar =>
            {
                target.EndWait(ar);
                Assert.AreEqual(1, counter);
                Assert.IsNull(target.Exception);
            }, null);
            target.Wait();
        }

#endif

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
            task.Start();

            Func<Task, int> continueFunction = t => Interlocked.Increment(ref counter);
            for (int i = 0; i < TaskTest.NestingCount; i++)
            {
                task = task.ContinueWith(continueFunction);
            }

            Assert.IsNotNull(task);
            task.Wait();
            Assert.AreEqual(TaskTest.NestingCount + 1, counter);
            Assert.IsNull(task.Exception);
        }
    }
}
