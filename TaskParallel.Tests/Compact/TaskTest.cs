﻿using System.Threading.Tasks;
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
        public const int NestingCount = 100;

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
            try { targetEx.Wait(); }
            catch (AggregateException) { }

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
        ///A test for Exception
        ///</summary>
        [TestMethod()]
        public void ExceptionTest()
        {
            Action action = () => { throw new ArgumentNullException("none"); };
            Task target = new Task(action);
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
            Action<object> action = state =>
            {
                Interlocked.Increment(ref counter);
                Assert.AreEqual(refobj, state);
            };
            Task target = new Task(action, refobj);
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
        public void TaskStartActionTest()
        {
            int counter = 0;
            Action action = () => Interlocked.Increment(ref counter);
#if NETFX_CE
            Task_Accessor target = new Task_Accessor(action);
            target.TaskStartAction(null);
#else
            var target = new Task(action);
            var privateTarget = new PrivateObject(target);
            privateTarget.Invoke("TaskStartAction", new object[] { null });
#endif
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
#if NETFX_CE
            Task_Accessor target = new Task_Accessor(action);
            target.EnsureStartOnce();
            target.EnsureStartOnce();
#else
            var target = new Task(action);
            var privateTarget = new PrivateObject(target);
            privateTarget.Invoke("EnsureStartOnce");
            privateTarget.Invoke("EnsureStartOnce");
#endif
        }

        /// <summary>
        ///A test for EndWait
        ///</summary>
        [TestMethod()]
        public void EndWaitTest()
        {
            Exception ex = new ArgumentNullException("none");
            Action action = () => { throw ex; };
            Task target = new Task(action);
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

        /// <summary>
        ///A test for Dispose
        ///</summary>
        [TestMethod()]
        public void DisposeTest()
        {
            Action action = () => { };
            Task target = new Task(action);

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
            Action action = () => Interlocked.CompareExchange(ref value, 2, 1);
            Task target = new Task(action);
            Action<Task> continueAction = t =>
            {
                Assert.IsNotNull(t);
                Assert.IsFalse(t.IsFaulted);
                Interlocked.CompareExchange(ref value, 3, 2);
            };
            Task target2 = target.ContinueWith(continueAction);
            target.Start();
            if (!target2.Wait(100))
                Assert.Fail("Timeout waiting for continuation task signal");

            Assert.IsTrue(target.IsCompleted);
            Assert.IsTrue(target2.IsCompleted);
            Assert.AreEqual(3, value);
        }

        /// <summary>
        ///A test for BeginWait
        ///</summary>
        [TestMethod()]
        public void BeginWaitTest()
        {
            int counter = 0;
            Action action = () => Interlocked.Increment(ref counter);
            Task target = new Task(action);
            target.Start();
            target.BeginWait(ar =>
            {
                target.EndWait(ar);
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
            task.Start();

            Action<Task> continueAction = t => Interlocked.Increment(ref counter);
            for (int i = 0; i < NestingCount; i++)
            {
                task = task.ContinueWith(continueAction);
            }

            Assert.IsNotNull(task);
            task.Wait();
            Assert.AreEqual(NestingCount + 1, counter);
            Assert.IsNull(task.Exception);
        }
    }
}
