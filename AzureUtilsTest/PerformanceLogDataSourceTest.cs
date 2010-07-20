using AzureUtils;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;
using Microsoft.WindowsAzure.StorageClient;

namespace AzureUtilsTest
{
    
    
    /// <summary>
    ///This is a test class for PerformanceLogDataSourceTest and is intended
    ///to contain all PerformanceLogDataSourceTest Unit Tests
    ///</summary>
    [TestClass()]
    public class PerformanceLogDataSourceTest
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
        ///A test for Insert
        ///</summary>
        [TestMethod()]
        [DeploymentItem("AzureHelper.dll")]
        public void InsertTest()
        {
            PerformanceLogDataSource target = new PerformanceLogDataSource();
            PerformanceLog item = new PerformanceLog("job", Guid.NewGuid().ToString(), DateTime.UtcNow, DateTime.UtcNow.AddSeconds(10));
            target.Insert(item);
            Assert.IsTrue(target
                .PerformanceLogs
                .Where(log => log.PartitionKey == item.PartitionKey && log.RowKey == item.RowKey)
                .Count() >= 1);
        }
    }
}
