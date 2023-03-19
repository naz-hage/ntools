﻿using Launcher;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace launcherTests
{
    [TestClass()]
    public class ResultHelperExTests
    {
        [TestMethod()]
        public void IsSuccessTest()
        {
            var success = "Success";
            var result = ResultHelperEx.Success();
            Assert.IsTrue(result.IsSuccess());
            Assert.AreEqual(success, result.Output[0]);
            success = "This function passed";
            result = ResultHelperEx.Success(success);
            Assert.IsTrue(result.IsSuccess());
            Assert.AreEqual(success, result.Output[0]);
        }

        [TestMethod()]
        public void IsFailTest()
        {
            var fail = "Fail";
            var result = ResultHelperEx.Fail();
            Assert.IsTrue(result.IsFail());
            Assert.AreEqual(fail, result.Output[0]);
            fail = "This function Failed";
            result = ResultHelperEx.Fail(-1, fail);
            Assert.IsTrue(result.IsFail());
            Assert.AreEqual(fail, result.Output[0]);

        }

        [TestMethod()]
        public void SuccessTest()
        {
            var success = "Success";
            var result = ResultHelperEx.Success();
            Assert.IsNotNull(result);
            Assert.AreEqual(0, result.Code);
            Assert.AreEqual(success, result.Output[0]);
            Assert.AreEqual(1, result.Output.Count);
        }

        [TestMethod()]
        public void FailTest()
        {
            var result = ResultHelperEx.Fail();
            Assert.IsNotNull(result);
            Assert.AreEqual(int.MinValue, result.Code);
            Assert.AreEqual("Fail", result.Output[0]);
            Assert.AreEqual(1, result.Output.Count);
        }

        [TestMethod()]
        public void NewTest()
        {
            var result = ResultHelperEx.New();
            Assert.IsNotNull(result);
            Assert.AreEqual(int.MaxValue, result.Code);
            Assert.AreEqual(0, result.Output.Count);
        }
    }
}