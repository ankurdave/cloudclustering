using AzureUtils;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;

namespace AzureUtilsTest
{
    
    
    /// <summary>
    ///This is a test class for KMeansTaskProcessorTest and is intended
    ///to contain all KMeansTaskProcessorTest Unit Tests
    ///</summary>
    [TestClass()]
    public class KMeansTaskProcessorTest
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
        ///A test for AssignClusterPointToNearestCentroid
        ///</summary>
        [TestMethod()]
        [DeploymentItem("AzureHelper.dll")]
        public void AssignClusterPointToNearestCentroidTest()
        {
            KMeansTask task = new KMeansTask(Guid.NewGuid(), Guid.NewGuid(), 1, 2, 3, null, null);
            KMeansTaskProcessor_Accessor target = new KMeansTaskProcessor_Accessor(task);
            
            target.centroids = new List<Centroid>();
            target.centroids.Add(new Centroid
            {
                ID = Guid.NewGuid(),
                X = 0.0F,
                Y = -1.0F
            });
            target.centroids.Add(new Centroid
            {
                ID = Guid.NewGuid(),
                X = 10.0F,
                Y = 10.0F
            });

            ClusterPoint clusterPoint = new ClusterPoint
            {
                CentroidID = Guid.Empty,
                X = 1.0F,
                Y = 2.0F
            };
            
            ClusterPoint expected = new ClusterPoint
            {
                CentroidID = target.centroids[0].ID,
                X = 1.0F,
                Y = 2.0F
            };
            ClusterPoint actual;
            actual = target.AssignClusterPointToNearestCentroid(clusterPoint);
            
            Assert.AreEqual(expected.CentroidID, actual.CentroidID);
        }
    }
}
