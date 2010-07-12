using AzureUtils;
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
            KMeansJobData target = new KMeansJobData
            {
                JobID = Guid.NewGuid(),
                K = 1,
                M = 2,
                N = 3
            };
            byte[] bytes = target.ToBinary();
            KMeansJobData targetNew = KMeansJobData.FromMessage<KMeansJobData>(new CloudQueueMessage(bytes));

            Assert.AreEqual(target.JobID, targetNew.JobID);
            Assert.AreEqual(target.K, targetNew.K);
            Assert.AreEqual(target.M, targetNew.M);
            Assert.AreEqual(target.N, targetNew.N);
        }
    }
}
