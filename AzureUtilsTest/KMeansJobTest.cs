using AzureUtils;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using Microsoft.WindowsAzure.StorageClient;
using System.Collections.Generic;

namespace AzureUtilsTest
{
    
    
    /// <summary>
    ///This is a test class for KMeansJobTest and is intended
    ///to contain all KMeansJobTest Unit Tests
    ///</summary>
    [TestClass()]
    public class KMeansJobTest
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
        ///A test for InitializeStorage
        ///</summary>
        [TestMethod()]
        public void InitializeStorageTest()
        {
            KMeansJobData jobData = new KMeansJobData(Guid.NewGuid(), 2, 4, 6);
            KMeansJob target = new KMeansJob(jobData);
            target.InitializeStorage();
            
            // Verify that the created containers and blobs actually exist
            CloudBlobClient client = AzureHelper.StorageAccount.CreateCloudBlobClient();

            CloudBlobContainer c = null;
            try
            {
                c = client.GetContainerReference(jobData.JobID.ToString());
                c.FetchAttributes();
            }
            catch (StorageClientException e)
            {
                if (e.ErrorCode == StorageErrorCode.ResourceNotFound)
                {
                    Assert.Fail();
                }
                else
                {
                    throw;
                }
            }

            CloudBlob points = null, centroids = null;
            try
            {
                points = c.GetBlobReference("points");
                points.FetchAttributes();
                centroids = c.GetBlobReference("centroids");
                centroids.FetchAttributes();
            }
            catch (StorageClientException e)
            {
                if (e.ErrorCode == StorageErrorCode.ResourceNotFound)
                {
                    Assert.Fail();
                }
                else
                {
                    throw;
                }
            }

            // Verify that unpacking a ClusterPoint actually yields a point with integer coordinates [-50, 50) and a null centroidID
            byte[] pointBytes;
            using (BlobStream pointsStream = points.OpenRead())
            {
                pointBytes = new byte[ClusterPoint.Size];
                pointsStream.Read(pointBytes, 0, ClusterPoint.Size);
            }
            ClusterPoint p = ClusterPoint.FromByteArray(pointBytes);

            Assert.IsTrue(p.X >= -50 && p.X < 50 && IsInteger(p.X));
            Assert.IsTrue(p.Y >= -50 && p.Y < 50 && IsInteger(p.Y));
            Assert.AreEqual(p.CentroidID, Guid.Empty);
        }

        private bool IsInteger(float x)
        {
            return IsCloseToZero(x - Math.Truncate(x));
        }

        private bool IsCloseToZero(double x)
        {
            return (Double.Epsilon >= x) && (x <= Double.Epsilon);
        }


        /// <summary>
        ///A test for RecalculateCentroids
        ///</summary>
        [TestMethod()]
        [DeploymentItem("AzureHelper.dll")]
        public void RecalculateCentroidsTest()
        {
            KMeansJobData jobData = new KMeansJobData(Guid.NewGuid(), 1, 1, 1);
            KMeansJob_Accessor target = new KMeansJob_Accessor(jobData);
            target.InitializeStorage();

            byte[] cBytes = new byte[Centroid.Size];
            using (BlobStream cStream = target.Centroids.OpenRead())
            {
                cStream.Read(cBytes, 0, cBytes.Length);
            }
            Centroid cOriginal = Centroid.FromByteArray(cBytes);

            target.totalPointsProcessedDataByCentroid[cOriginal.ID] = new PointsProcessedData
            {
                NumPointsProcessed = 1,
                PartialPointSum = new Point(1, 2)
            };

            target.RecalculateCentroids();

            byte[] cBytesNew = new byte[Centroid.Size];
            using (BlobStream cStreamNew = target.Centroids.OpenRead())
            {
                cStreamNew.Read(cBytesNew, 0, cBytesNew.Length);
            }
            Centroid cNew = Centroid.FromByteArray(cBytesNew);

            Assert.AreEqual(cNew.ID, cOriginal.ID);
            Assert.AreEqual(cNew.X, 1);
            Assert.AreEqual(cNew.Y, 2);
        }
    }
}
