using AzureUtils;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;

namespace AzureUtilsTest
{
    
    
    /// <summary>
    ///This is a test class for KMeansTaskResultTest and is intended
    ///to contain all KMeansTaskResultTest Unit Tests
    ///</summary>
    [TestClass()]
    public class KMeansTaskResultTest
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
        ///A test for SavePointsProcessedDataByCentroid
        ///</summary>
        [TestMethod()]
        public void SavePointsProcessedDataByCentroidTest()
        {
            KMeansTaskData task = new KMeansTaskData(Guid.NewGuid(), Guid.NewGuid(), 1, null, 2, 3, 10, 0, null, DateTime.Now, DateTime.Now, 0);
            KMeansTaskResult target = new KMeansTaskResult(task);
            target.PointsProcessedDataByCentroid[Guid.NewGuid()] = new PointsProcessedData()
            {
                NumPointsProcessed = 100,
                PartialPointSum = new Point(10, -10)
            };
            target.PointsProcessedDataByCentroid[Guid.NewGuid()] = new PointsProcessedData()
            {
                NumPointsProcessed = 100,
                PartialPointSum = new Point(10, -10)
            };
            target.SavePointsProcessedDataByCentroid();

            foreach (KeyValuePair<Guid, PointsProcessedData> pair in target.PointsProcessedDataByCentroid)
            {
                Assert.IsTrue(target.PointsProcessedDataByCentroidList.Contains(pair));
            }
        }
    }
}
