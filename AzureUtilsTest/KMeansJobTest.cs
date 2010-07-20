using AzureUtils;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using Microsoft.WindowsAzure.StorageClient;
using System.Collections.Generic;
using Microsoft.WindowsAzure;
using System.IO;
using System.Linq;

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
            KMeansJobData jobData = new KMeansJobData(Guid.NewGuid(), 2, 4, 6, 10, DateTime.Now);
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
            KMeansJobData jobData = new KMeansJobData(Guid.NewGuid(), 1, 1, 1, 10, DateTime.Now);
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

        /// <summary>
        ///A test for CopyPointPartition
        ///</summary>
        [TestMethod()]
        [DeploymentItem("AzureHelper.dll")]
        public void CopyPointPartitionTest()
        {
            KMeansJobData jobData = new KMeansJobData(Guid.NewGuid(), 4, 2, 2, 10, DateTime.Now);
            KMeansJob_Accessor target = new KMeansJob_Accessor(jobData);
            target.InitializeStorage();
            int partitionNumber = 0;
            int totalPartitions = 2;
            CloudBlobContainer container = AzureHelper.StorageAccount.CreateCloudBlobClient().GetContainerReference("testcontainer");
            container.CreateIfNotExist();
            string blobName = "testblob";
            CloudBlob partition;
            partition = target.CopyPointPartition(target.Points, partitionNumber, totalPartitions, container, blobName);

            using (BlobStream partitionStream = partition.OpenRead(),
                pointsStream = target.Points.OpenRead())
            {
                Assert.AreEqual(ClusterPoint.Size * 2, partitionStream.Length);
                while (partitionStream.Position < partitionStream.Length)
                {
                    Assert.AreEqual(pointsStream.ReadByte(), partitionStream.ReadByte());
                }
            }
        }

        /// <summary>
        ///A test for ProcessWorkerResponse
        ///</summary>
        [TestMethod()]
        public void ProcessWorkerResponseTest()
        {
            // TODO: Add test case where completed tasks already exist in KMeansJob, and make sure things work in that case. Basically, test multiple iterations.

            KMeansJobData jobData = new KMeansJobData(Guid.NewGuid(), 4, 2, 2, 10, DateTime.Now);
            KMeansJob_Accessor target = new KMeansJob_Accessor(jobData);
            target.InitializeStorage();
            target.EnqueueTasks();

            CloudBlobClient client = AzureHelper.StorageAccount.CreateCloudBlobClient();
            CloudBlobContainer container = client.GetContainerReference(jobData.JobID.ToString());
            CloudBlob pointPartition = target.CopyPointPartition(target.Points, 0, 2, container, "testblob");
            
            // Modify the first few bytes of the pointPartition blob, so we can verify that it got copied
            byte[] arbitraryBytes = new byte[8];
            new Random().NextBytes(arbitraryBytes);
            using (BlobStream pointPartitionWriteStream = pointPartition.OpenWrite())
            {
                pointPartitionWriteStream.Write(arbitraryBytes, 0, arbitraryBytes.Length);
            }

            KMeansTaskData taskData = new KMeansTaskData(jobData, Guid.NewGuid(), pointPartition.Uri, target.Centroids.Uri, DateTime.Now);
            target.tasks.Clear();
            target.tasks.Add(new KMeansTask(taskData));
            KMeansTaskResult taskResult = new KMeansTaskResult(taskData);
            taskResult.NumPointsChanged = 2;
            Guid centroidID = Guid.NewGuid();
            taskResult.PointsProcessedDataByCentroid = new Dictionary<Guid, PointsProcessedData> {
                { centroidID, new PointsProcessedData() {
                        NumPointsProcessed = 2,
                        PartialPointSum = new Point(1, 2)
                    }
                }
            };
            target.ProcessWorkerResponse(taskResult);
            
            // Verify that the first few bytes of Points are indeed full of arbitraryBytes
            using (BlobStream pointsStream = target.Points.OpenRead())
            {
                for (int i = 0; i < 8; i++)
                {
                    Assert.AreEqual(arbitraryBytes[i], pointsStream.ReadByte());
                }
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
        public void MultiIterationJobTest()
        {
            KMeansJobData jobData = new KMeansJobData(Guid.NewGuid(), 4, 2, 2, 2, DateTime.Now);
            KMeansJob_Accessor job = new KMeansJob_Accessor(jobData);
            
            // First iteration
            job.InitializeStorage();
            job.EnqueueTasks();

            for (int i = 0; i < jobData.MaxIterationCount; i++)
            {
                //List<KMeansTaskData> taskDataList = GetKMeansTasksFromWorkerRequestQueue(jobData.M);
                CheckWorkerRequests(job,
                    (from task in job.tasks
                    where task.Running
                    select task.TaskData),
                    jobData.M, job.Points);

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
                    job.ProcessWorkerResponse(result);
                }
            }
        }

        private void CheckWorkerRequests(KMeansJob_Accessor job, IEnumerable<KMeansTaskData> taskDataList, int expectedNumRequests, CloudBlob pointsBlob)
        {
            // Make sure there are enough taskDatas in the list
            Assert.AreEqual(expectedNumRequests, taskDataList.Count());
            Assert.IsTrue(taskDataList.Where(element => element == null).Count() == 0);

            // Make sure the lengths of all the point partition blobs add up to the length of the points blob
            Assert.AreEqual(pointsBlob.Properties.Length,
                taskDataList.Sum(element => AzureHelper.GetBlob(element.Points).Properties.Length));
        }

        private static List<KMeansTaskData> GetKMeansTasksFromWorkerRequestQueue(int numQueueMessages)
        {
            List<KMeansTaskData> taskDataList = new List<KMeansTaskData>();
            for (int i = 0; i < numQueueMessages; i++)
            {
                AzureHelper.WaitForMessage(AzureHelper.WorkerRequestQueue, message => true, message =>
                {
                    taskDataList.Add((KMeansTaskData)message);
                    return true;
                }, 100, 100);
            }
            return taskDataList;
        }
    }
}
