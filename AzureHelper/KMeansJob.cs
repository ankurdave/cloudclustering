using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Security.Cryptography;

namespace AzureUtils
{
    public class KMeansJob
    {
        private const int PointRange = 50;
        private const int NumPointsChangedThreshold = 0;
        private ICloudBlob Points { get; set; }
        private ICloudBlob Centroids { get; set; }
        private int TotalNumPointsChanged { get; set; }
        private Dictionary<Guid, PointsProcessedData> totalPointsProcessedDataByCentroid = new Dictionary<Guid,PointsProcessedData>();
        private HashSet<Guid> taskIDs = new HashSet<Guid>();
        private KMeansJobData jobData;
        private IAzureHelper azureHelper;

        public KMeansJob(KMeansJobData jobData, IAzureHelper azureHelper) {
            this.jobData = jobData;
            this.azureHelper = azureHelper;
        }

        /// <summary>
        /// Sets up the Azure storage (Points and Centroids) for the first k-means iteration.
        /// </summary>
        public void InitializeStorage()
        {
            // Set up the storage client and the container
            azureHelper.CreateBlobContainer(jobData.JobID.ToString());
            
            // Set up the random point generator
            Random random = new Random();
            Func<int, byte[]> randomPoint = (i => new ClusterPoint(
                    random.Next(-PointRange, PointRange),
                    random.Next(-PointRange, PointRange),
                    Guid.Empty).ToByteArray());

            // Initialize the points blob with N random ClusterPoints
            Points = container.GetBlockBlobReference("points");
            BlockBlobGenerator pointsGen = new BlockBlobGenerator(Points, jobData.N);
            pointsGen.DataGenerator = randomPoint;
            pointsGen.Run();

            // Initialize the centroids blob with K random Centroids
            Centroids = container.GetBlockBlobReference("centroids");
            BlockBlobGenerator centroidsGen = new BlockBlobGenerator(Centroids, jobData.K);
            centroidsGen.DataGenerator = randomPoint;
            centroidsGen.Run();
        }

        /// <summary>
        /// Enqueues M messages into a queue. Each message is an instruction to a worker to process a partition of the k-means data.
        /// </summary>
        public void EnqueueTasks()
        {
            CloudBlobClient client = AzureHelper.StorageAccount.CreateCloudBlobClient();
            CloudBlobContainer container = client.GetContainerReference(jobData.JobID.ToString());
            container.CreateIfNotExist();

            for (int i = 0; i < jobData.M; i++)
            {
                KMeansTask task = new KMeansTask(jobData);
                task.TaskID = Guid.NewGuid();
                task.Points = CopyPointPartition(Points, i, jobData.M, container, task.TaskID.ToString());
                task.Centroids = Centroids;

                taskIDs.Add(task.TaskID);

                AzureHelper.EnqueueMessage("workerrequest", task);
            }
        }

        /// <summary>
        /// Handles a worker's taskResult from a running k-means job. Adds up the partial sums from the taskResult.
        /// </summary>
        /// <param name="message"></param>
        /// <returns>False if the given task result has already been counted, true otherwise.</returns>
        public bool ProcessWorkerResponse(KMeansTaskResult taskResult)
        {
            // Make sure we're actually still waiting for a result for this task
            // If not, this might be a duplicate queue message
            if (!taskIDs.Contains(taskResult.TaskID))
                return false;
            taskIDs.Remove(taskResult.TaskID);

            // Add up the partial sums
            AddDataFromTaskResult(taskResult);

            // If this is the last worker to return, this iteration is done and we should start the next one
            if (NoTaskIDsLeft())
            {
                NextIteration();
            }

            return true;
        }

        /// <summary>
        /// Checks whether to move into the next iteration, and performs the appropriate actions to make it happen.
        /// </summary>
        private void NextIteration()
        {
            if (NumPointsChangedAboveThreshold())
            {
                RecalculateCentroids();
                EnqueueTasks();
            }
            else
            {
                ReturnResults();
            }
        }

        /// <summary>
        /// Copies a partition of the Points blob into a new blob and returns that new blob.
        /// </summary>
        /// <param name="points">The blob from which to read.</param>
        /// <param name="partitionNumber">Partition number to copy. Must be in the range [0,totalPartitions)</param>
        /// <param name="totalPartitions">Total number of partitions to split Points into. Must be 1 or more.</param>
        /// <param name="container">The container in which to place the new blob.</param>
        /// <param name="blobName">What to name the new blob.</param>
        private CloudBlob CopyPointPartition(CloudBlob points, int partitionNumber, int totalPartitions, CloudBlobContainer container, String blobName)
        {
            // Calculate what portion of points to read
            BlobStream pointsStream = points.OpenRead();
            long numPoints = pointsStream.Length / ClusterPoint.Size;
            long partitionLength = (long)Math.Ceiling((float)numPoints / totalPartitions);
            long startByte = partitionNumber * partitionLength;

            // Read it into partition
            byte[] partition = new byte[partitionLength];
            pointsStream.Position = startByte;
            int actualLength = pointsStream.Read(partition, 0, (int)partitionLength);

            // Create the new blob
            CloudBlockBlob newBlob = container.GetBlockBlobReference(blobName);
            BlobStream newBlobStream = newBlob.OpenWrite();
            newBlobStream.BlockSize = AzureHelper.BlobBlockSize;
            newBlobStream.Write(partition, 0, actualLength);

            return newBlob;
        }

        /// <summary>
        /// Sums the given taskResult's points processed data with the totals.
        /// </summary>
        /// <param name="taskResult"></param>
        private void AddDataFromTaskResult(KMeansTaskResult taskResult)
        {
            TotalNumPointsChanged += taskResult.NumPointsChanged;
            foreach (KeyValuePair<Centroid, PointsProcessedData> pointsProcessedDataForCentroid in taskResult.PointsProcessedDataByCentroid)
            {
                AddPointsProcessedDataForCentroid(pointsProcessedDataForCentroid.Key, pointsProcessedDataForCentroid.Value);
            }
        }

        private void AddPointsProcessedDataForCentroid(Centroid centroid, PointsProcessedData data)
        {
            if (!totalPointsProcessedDataByCentroid.ContainsKey(centroid.ID))
            {
                totalPointsProcessedDataByCentroid[centroid.ID] = new PointsProcessedData();
            }

            totalPointsProcessedDataByCentroid[centroid.ID] += data;
        }

        private bool NoTaskIDsLeft()
        {
            return taskIDs.Count == 0;
        }

        private bool NumPointsChangedAboveThreshold()
        {
            return TotalNumPointsChanged > NumPointsChangedThreshold;
        }

        private void RecalculateCentroids()
        {
            throw new NotImplementedException();
        }

        private void ReturnResults()
        {
            throw new NotImplementedException();
        }
    }
}
