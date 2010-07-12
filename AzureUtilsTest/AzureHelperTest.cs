using AzureUtils;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace AzureUtilsTest
{
    
    
    /// <summary>
    ///This is a test class for AzureHelperTest and is intended
    ///to contain all AzureHelperTest Unit Tests
    ///</summary>
    [TestClass()]
    public class AzureHelperTest
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
        ///A test for EnqueueMessage
        ///</summary>
        [TestMethod()]
        public void EnqueueWaitForMessageTest()
        {
            string queueName = "serverrequest";
            KMeansJobData message = new KMeansJobData
            {
                JobID = Guid.Empty,
                K = 1,
                M = 2,
                N = 3
            };
            AzureHelper.EnqueueMessage(queueName, message);

            KMeansJobData responseMessage = null;
            AzureHelper.WaitForMessage(queueName, msg => ((KMeansJobData)msg).N == 3, msg =>
            {
                responseMessage = (KMeansJobData)msg;
                return true;
            });

            Assert.AreEqual(message.JobID, responseMessage.JobID);
            Assert.AreEqual(message.K, responseMessage.K);
        }
    }
}
