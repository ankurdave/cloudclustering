﻿using AzureUtils;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using Microsoft.WindowsAzure.StorageClient;

namespace AzureUtilsTest
{
    
    
    /// <summary>
    ///This is a test class for AzureMessageTest and is intended
    ///to contain all AzureMessageTest Unit Tests
    ///</summary>
    [TestClass()]
    public class AzureMessageTest
    {


        private TestContext testContextInstance;

        /// <summary>
        ///Gets or sets the test context which provides
        ///information about and functionality for the current test run.
        ///</summary>
        public TestContext TestContext
        {
            get
            {
                return testContextInstance;
            }
            set
            {
                testContextInstance = value;
            }
        }

        #region Additional test attributes
        // 
        //You can use the following additional attributes as you write your tests:
        //
        //Use ClassInitialize to run code before running the first test in the class
        //[ClassInitialize()]
        //public static void MyClassInitialize(TestContext testContext)
        //{
        //}
        //
        //Use ClassCleanup to run code after all tests in a class have run
        //[ClassCleanup()]
        //public static void MyClassCleanup()
        //{
        //}
        //
        //Use TestInitialize to run code before running each test
        //[TestInitialize()]
        //public void MyTestInitialize()
        //{
        //}
        //
        //Use TestCleanup to run code after each test has run
        //[TestCleanup()]
        //public void MyTestCleanup()
        //{
        //}
        //
        #endregion


        /// <summary>
        ///A test for ToBinary
        ///</summary>
        [TestMethod()]
        public void ToFromBinaryTest()
        {
            KMeansJobData target = new KMeansJobData(Guid.NewGuid(), 1, null, 2, 10, DateTime.Now);
            byte[] bytes = target.ToBinary();
            KMeansJobData targetNew = KMeansJobData.FromMessage(new CloudQueueMessage(bytes)) as KMeansJobData;

            Assert.AreEqual(target.JobID, targetNew.JobID);
            Assert.AreEqual(target.K, targetNew.K);
            Assert.AreEqual(target.N, targetNew.N);
        }
    }
}
