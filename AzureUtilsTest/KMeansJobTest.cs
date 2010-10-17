using AzureUtils;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using Microsoft.WindowsAzure.StorageClient;
using System.Collections.Generic;
using Microsoft.WindowsAzure;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;

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
            KMeansJobData jobData = new KMeansJobData(Guid.NewGuid(), 2, null, 4, 10, DateTime.Now);
            KMeansJob target = new KMeansJob(jobData, "server");
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

            // Verify that unpacking a ClusterPoint actually yields a point with coordinates [-50, 50) and a null centroidID
            byte[] pointBytes;
            using (BlobStream pointsStream = points.OpenRead())
            {
                pointBytes = new byte[ClusterPoint.Size];
                pointsStream.Read(pointBytes, 0, ClusterPoint.Size);
            }
            ClusterPoint p = ClusterPoint.FromByteArray(pointBytes);

            Assert.IsTrue(p.X >= -50 && p.X < 50);
            Assert.IsTrue(p.Y >= -50 && p.Y < 50);
            Assert.AreEqual(p.CentroidID, Guid.Empty);

            // Verify that the blobs are the correct length
            Assert.AreEqual(points.Properties.Length, ClusterPoint.Size * jobData.N);
            Assert.AreEqual(centroids.Properties.Length, Centroid.Size * jobData.K);
        }

        private bool IsInteger(double x)
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
            KMeansJobData jobData = new KMeansJobData(Guid.NewGuid(), 1, null, 1, 10, DateTime.Now);
            KMeansJob_Accessor target = new KMeansJob_Accessor(jobData, "server");
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

        /// <summary>
        /// Tests RecalculateCentroids with an empty centroid (a centroid with no assigned points).
        /// </summary>
        [TestMethod()]
        public void RecalculateCentroidsTest2()
        {
            KMeansJobData jobData = new KMeansJobData(Guid.NewGuid(), 0, null, 1, 10, DateTime.Now);
            KMeansJob_Accessor target = new KMeansJob_Accessor(jobData, "server");
            target.InitializeStorage();

            byte[] cBytes = new byte[Centroid.Size];
            using (BlobStream cStream = target.Centroids.OpenRead())
            {
                cStream.Read(cBytes, 0, cBytes.Length);
            }
            Centroid cOriginal = Centroid.FromByteArray(cBytes);

            target.totalPointsProcessedDataByCentroid[cOriginal.ID] = new PointsProcessedData();

            target.RecalculateCentroids();

            byte[] cBytesNew = new byte[Centroid.Size];
            using (BlobStream cStreamNew = target.Centroids.OpenRead())
            {
                cStreamNew.Read(cBytesNew, 0, cBytesNew.Length);
            }
            Centroid cNew = Centroid.FromByteArray(cBytesNew);

            Assert.AreEqual(cOriginal.ID, cNew.ID);
            Assert.AreEqual(cNew.X, 0);
            Assert.AreEqual(cNew.Y, 0);
        }

        /// <summary>
        ///A test for ProcessWorkerResponse
        ///</summary>
        [TestMethod()]
        public void ProcessWorkerResponseTest()
        {
            KMeansJobData jobData = new KMeansJobData(Guid.NewGuid(), 4, null, 2, 10, DateTime.Now);
            KMeansJob_Accessor target = new KMeansJob_Accessor(jobData, "server");
            target.InitializeStorage();

            // Upload a block with an arbitrary ClusterPoint, so we can verify it gets copied
            ClusterPoint arbitraryPoint = new ClusterPoint(1, 2, Guid.NewGuid());
            List<string> blockList;
            using (ObjectCachedBlockWriter<ClusterPoint> pointPartitionWriteStream = new ObjectCachedBlockWriter<ClusterPoint>(target.Points, point => point.ToByteArray(), ClusterPoint.Size,
                Environment.GetEnvironmentVariable("TEMP") + @"\" + Guid.NewGuid().ToString()))
            {
                pointPartitionWriteStream.Write(arbitraryPoint);
                pointPartitionWriteStream.FlushBlock();
                blockList = pointPartitionWriteStream.BlockList;
            }

            KMeansTaskData taskData = new KMeansTaskData(jobData, Guid.NewGuid(), 0, 1, target.Centroids.Uri, DateTime.Now, 0, null);

            target.tasks.Clear();
            target.tasks.Add(new KMeansTask(taskData));
            
            KMeansTaskResult taskResult = new KMeansTaskResult(taskData);
            CloudBlob pointsBlockListBlob = AzureHelper.CreateBlob(jobData.JobID.ToString(), Guid.NewGuid().ToString());
            using (Stream stream = pointsBlockListBlob.OpenWrite())
            {
                BinaryFormatter bf = new BinaryFormatter();
                bf.Serialize(stream, blockList);
            }
            taskResult.PointsBlockListBlob = pointsBlockListBlob.Uri;
            taskResult.NumPointsChanged = 2;
            Guid centroidID = Guid.NewGuid();
            taskResult.PointsProcessedDataByCentroid = new Dictionary<Guid, PointsProcessedData> {
                { centroidID, new PointsProcessedData() {
                        NumPointsProcessed = 2,
                        PartialPointSum = new Point(1, 2)
                    }
                }
            };
            target.ProcessWorkerResponse(taskResult, new List<Worker>());
            
            // Verify that the first ClusterPoint in Points is indeed equal to arbitraryPoint
            using (ObjectStreamReader<ClusterPoint> pointsStream = new ObjectStreamReader<ClusterPoint>(target.Points, ClusterPoint.FromByteArray, ClusterPoint.Size))
            {
                ClusterPoint point = pointsStream.First();
                Assert.AreEqual(arbitraryPoint.X, point.X);
                Assert.AreEqual(arbitraryPoint.Y, point.Y);
                Assert.AreEqual(arbitraryPoint.CentroidID, point.CentroidID);
            }
        }

        [TestMethod()]
        public void BlockBlobTest()
        {
            // Set up the block blob
            CloudStorageAccount storageAccount = CloudStorageAccount.DevelopmentStorageAccount;
            CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();
            CloudBlobContainer container = blobClient.GetContainerReference("testcontainer");
            container.CreateIfNotExist();
            CloudBlockBlob blob = container.GetBlockBlobReference("testblob");

            // Set up the data to write to a block
            byte[] bytes = new byte[8]; // Just a bunch of zeros
            MemoryStream stream = new MemoryStream(bytes);

            // Put the block
            List<string> blocks = new List<string> {
                Convert.ToBase64String(Guid.NewGuid().ToByteArray())
            };
            
            blob.PutBlock(blocks[0], stream, null);

            // Commit the blob
            blob.PutBlockList(blocks);
        }

        [TestMethod()]
        public void PlainBlobTest()
        {
            // Set up the blob
            CloudStorageAccount storageAccount = CloudStorageAccount.DevelopmentStorageAccount;
            CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();
            CloudBlobContainer container = blobClient.GetContainerReference("testcontainer");
            container.CreateIfNotExist();
            CloudBlob blob = container.GetBlobReference("testblob");

            // Set up the data to write to a block
            byte[] bytes = new byte[8]; // Just a bunch of zeros

            // Put the blob
            using (BlobStream blobStream = blob.OpenWrite()) {
                blobStream.Write(bytes, 0, bytes.Length);
            }
        }

        [TestMethod()]
        public void MultiIterationJobTest() // TODO: make this unit test check things in more detail
        {
            KMeansJobData jobData = new KMeansJobData(Guid.NewGuid(), 4, null, 2, 2, DateTime.Now);
            List<Worker> workers = new List<Worker> {
                new Worker("a", "", 1),
                new Worker("b", "", 1)
            };
            KMeansJob_Accessor job = new KMeansJob_Accessor(jobData, "server");
            
            // First iteration
            job.InitializeStorage();
            job.EnqueueTasks(workers);

            for (int i = 0; i < jobData.MaxIterationCount; i++)
            {
                CheckWorkerRequests(job,
                    (from task in job.tasks
                    where task.Running
                    select task.TaskData),
                    workers.Count, job.Points);

                // Create the worker results and send them to the job
                List<KMeansTaskResult> results = new List<KMeansTaskResult>();
                foreach (var task in job.tasks)
                {
                    var taskResult = new KMeansTaskResult(task.TaskData);
                    taskResult.NumPointsChanged = 1;
                    results.Add(taskResult);
                }

                foreach (var result in results)
                {
                    job.ProcessWorkerResponse(result, workers);
                }
            }
        }

        private void CheckWorkerRequests(KMeansJob_Accessor job, IEnumerable<KMeansTaskData> taskDataList, int expectedNumRequests, CloudBlob pointsBlob)
        {
            // Make sure there are enough taskDatas in the list
            Assert.AreEqual(expectedNumRequests, taskDataList.Count());
            Assert.IsTrue(taskDataList.Where(element => element == null).Count() == 0);
        }
    }
}
