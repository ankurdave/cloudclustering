using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Security.Cryptography;
using Microsoft.WindowsAzure.StorageClient;

namespace AzureUtils
{
    public class KMeansJob
    {
        private const int PointRange = 50;
        private const int NumPointsChangedThreshold = 0;

        private CloudBlockBlob Points { get; set; }
        private CloudBlob Centroids { get; set; }
        private int TotalNumPointsChanged { get; set; }
        private Dictionary<Guid, PointsProcessedData> totalPointsProcessedDataByCentroid = new Dictionary<Guid,PointsProcessedData>();
        private List<KMeansTask> tasks = new List<KMeansTask>();
        private List<String> pointsBlockIDs = new List<string>();
        private KMeansJobData jobData;
        public int IterationCount { get; private set; }

        public KMeansJob(KMeansJobData jobData) {
            this.jobData = jobData;
            this.IterationCount = 0;
        }

        /// <summary>
        /// Sets up the Azure storage (Points and Centroids) for the first k-means iteration.
        /// </summary>
        public void InitializeStorage()
        {
            DateTime start = DateTime.UtcNow;

            // Set up the storage client and the container
            CloudBlobClient client = AzureHelper.StorageAccount.CreateCloudBlobClient();
            CloudBlobContainer container = client.GetContainerReference(jobData.JobID.ToString());
            container.CreateIfNotExist();
            
            Random random = new Random();

            // Initialize the points blob with N random ClusterPoints
            Points = container.GetBlockBlobReference(AzureHelper.PointsBlob);
            using (Stream pointsStream = Points.OpenWrite())
            {
                for (int i = 0; i < jobData.N; i++)
                {
                    byte[] data = new ClusterPoint(
                        random.NextDouble() * 100 - 50,
                        random.NextDouble() * 100 - 50,
                        Guid.Empty).ToByteArray();
                    pointsStream.Write(data, 0, data.Length);
                }
            }
            
            // Initialize the centroids blob with K random Centroids
            Centroids = container.GetBlobReference(AzureHelper.CentroidsBlob);
            using (Stream centroidsStream = Centroids.OpenWrite())
            {
                for (int i = 0; i < jobData.K; i++)
                {
                    byte[] data = new Centroid(
                        Guid.NewGuid(),
                        random.Next(-PointRange, PointRange),
                        random.Next(-PointRange, PointRange)).ToByteArray();
                    centroidsStream.Write(data, 0, data.Length);
                }
            }

            DateTime end = DateTime.UtcNow;
            PerformanceLog log = new PerformanceLog(jobData.JobID.ToString(), "InitializeStorage", start, end);
            log.IterationCount = IterationCount;
            log.Points = Points.Uri.ToString();
            log.Centroids = Centroids.Uri.ToString();
            AzureHelper.PerformanceLogger.Insert(log);
        }

        /// <summary>
        /// Enqueues M messages into a queue. Each message is an instruction to a worker to process a partition of the k-means data.
        /// </summary>
        public void EnqueueTasks()
        {
            DateTime start = DateTime.UtcNow;

            CloudBlobClient client = AzureHelper.StorageAccount.CreateCloudBlobClient();
            CloudBlobContainer container = client.GetContainerReference(jobData.JobID.ToString());

            for (int i = 0; i < jobData.M; i++)
            {
                Guid taskID = Guid.NewGuid();
                CloudBlob pointPartition = CopyPointPartition(Points, i, jobData.M, container, taskID.ToString());

                KMeansTaskData taskData = new KMeansTaskData(jobData, taskID, pointPartition.Uri, Centroids.Uri, start);

                tasks.Add(new KMeansTask(taskData));

                AzureHelper.EnqueueMessage(AzureHelper.WorkerRequestQueue, taskData);
            }

            DateTime end = DateTime.UtcNow;
            PerformanceLog log = new PerformanceLog(jobData.JobID.ToString(), "EnqueueTasks", start, end);
            log.IterationCount = IterationCount;
            log.Points = Points.Uri.ToString();
            log.Centroids = Centroids.Uri.ToString();
            AzureHelper.PerformanceLogger.Insert(log);
        }

        /// <summary>
        /// Handles a worker's TaskResult from a running k-means job. Adds up the partial sums from the TaskResult.
        /// </summary>
        /// <param name="message"></param>
        /// <returns>False if the given taskData result has already been counted, true otherwise.</returns>
        public bool ProcessWorkerResponse(KMeansTaskResult taskResult)
        {
            DateTime start = DateTime.UtcNow;

            // Make sure we're actually still waiting for a result for this taskData
            // If not, this might be a duplicate queue message
            if (!TaskResultMatchesRunningTask(taskResult))
                return false;

            KMeansTask task = TaskResultWithTaskID(taskResult.TaskID);
            task.Running = false; // The task has returned a response, which means that it has stopped running

            // Copy the worker's point partition into a block
            CloudBlob pointPartitionBlob = AzureHelper.GetBlob(taskResult.Points);
            using (BlobStream pointPartitionStream = pointPartitionBlob.OpenRead())
            {
                byte[] buffer = new byte[32768];

                // Upload pointPartitionStream as one or more blocks
                while (pointPartitionStream.Position < pointPartitionStream.Length)
                {
                    using (MemoryStream blockStream = new MemoryStream())
                    {
                        AzureHelper.CopyStreamUpToLimit(pointPartitionStream, blockStream, AzureHelper.MaxBlockSize, buffer);
                        blockStream.Position = 0; // Reset blockStream's position so that it can be read by PutBlock

                        string blockID = Convert.ToBase64String(Guid.NewGuid().ToByteArray());
                        Points.PutBlock(blockID, blockStream, null);
                        pointsBlockIDs.Add(blockID);
                    }
                }
            }

            // Copy out and integrate the data from the worker response
            AddDataFromTaskResult(taskResult);

            DateTime end = DateTime.UtcNow;
            PerformanceLog log = new PerformanceLog(jobData.JobID.ToString(), "ProcessWorkerResponse", start, end);
            log.IterationCount = IterationCount;
            log.Points = Points.Uri.ToString();
            log.Centroids = Centroids.Uri.ToString();
            AzureHelper.PerformanceLogger.Insert(log);

            // If this is the last worker to return, this iteration is done and we should start the next one
            if (NoMoreRunningTasks())
            {
                NextIteration();
            }

            return true;
        }

        private KMeansTask TaskResultWithTaskID(Guid guid)
        {
            KMeansTask foundTask = null;
            foreach (KMeansTask task in tasks)
            {
                if (task.TaskData.TaskID == guid && task.Running)
                {
                    foundTask = task;
                    break;
                }
            }
            return foundTask;
        }

        private bool TaskResultMatchesRunningTask(KMeansTaskResult taskResult)
        {
            KMeansTask task = TaskResultWithTaskID(taskResult.TaskID);
            return task != null && task.Running;
        }

        /// <summary>
        /// Checks whether to move into the next iteration, and performs the appropriate actions to make it happen.
        /// </summary>
        private void NextIteration()
        {
            System.Diagnostics.Trace.TraceInformation("[ServerRole] NextIteration() JobID={0}", jobData.JobID);

            CommitPointsBlob();

            IterationCount++;

            if (NumPointsChangedAboveThreshold() && !MaxIterationCountExceeded())
            {
                RecalculateCentroids();
                EnqueueTasks();
            }
            else
            {
                ReturnResults();
            }
        }

        private bool MaxIterationCountExceeded()
        {
            return IterationCount >= jobData.MaxIterationCount && jobData.MaxIterationCount != 0;
        }

        private void CommitPointsBlob()
        {
            Points.PutBlockList(pointsBlockIDs);
            Points.FetchAttributes(); // Refresh the attributes after PutBlockList has cleared them, so that they can be relied on for later calculations
            pointsBlockIDs.Clear();
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
            byte[] partition;
            int actualLength;
            using (BlobStream pointsStream = points.OpenRead())
            {
                long numPoints = NumClusterPointsInBlob(points);
                long partitionLength = PartitionLength(numPoints, totalPartitions);
                long startByte = partitionNumber * partitionLength;

                // Read it into partition
                partition = new byte[partitionLength];
                pointsStream.Position = startByte;
                actualLength = pointsStream.Read(partition, 0, (int)partitionLength);
            }

            // Create the new blob
            CloudBlob newBlob = container.GetBlobReference(blobName);
            using (BlobStream newBlobStream = newBlob.OpenWrite())
            {
                newBlobStream.Write(partition, 0, actualLength);
            }

            return newBlob;
        }

        private void CopyBackPointPartition(CloudBlob points, int partitionNumber, int totalPartitions, CloudBlob partition)
        {
            using (BlobStream partitionStream = partition.OpenRead(),
                pointsStream = points.OpenWrite())
            {
                // Calculate where in points to start copying into
                long numPoints = NumClusterPointsInBlob(points);
                long partitionLength = PartitionLength(numPoints, totalPartitions);
                long startByte = partitionNumber * partitionLength;

                // Copy partition into that portion of points
                pointsStream.Position = startByte;
                partitionStream.CopyTo(pointsStream);
            }
        }

        private static long PartitionLength(long numPoints, int numPartitions)
        {
            return (long)Math.Ceiling((double)numPoints / numPartitions) * ClusterPoint.Size;
        }

        private long NumClusterPointsInBlob(CloudBlob points)
        {
            return points.Properties.Length / ClusterPoint.Size;
        }

        /// <summary>
        /// Sums the given TaskResult's points processed data with the totals.
        /// </summary>
        /// <param name="TaskResult"></param>
        private void AddDataFromTaskResult(KMeansTaskResult taskResult)
        {
            TotalNumPointsChanged += taskResult.NumPointsChanged;
            foreach (KeyValuePair<Guid, PointsProcessedData> pointsProcessedDataForCentroid in taskResult.PointsProcessedDataByCentroid)
            {
                AddPointsProcessedDataForCentroid(pointsProcessedDataForCentroid.Key, pointsProcessedDataForCentroid.Value);
            }
        }

        private void AddPointsProcessedDataForCentroid(Guid centroidID, PointsProcessedData data)
        {
            if (!totalPointsProcessedDataByCentroid.ContainsKey(centroidID))
            {
                totalPointsProcessedDataByCentroid[centroidID] = new PointsProcessedData();
            }

            totalPointsProcessedDataByCentroid[centroidID] += data;
        }

        private bool NoMoreRunningTasks()
        {
            return tasks.Count(task => task.Running) == 0;
        }

        private bool NumPointsChangedAboveThreshold()
        {
            return TotalNumPointsChanged > NumPointsChangedThreshold;
        }

        private void RecalculateCentroids()
        {
            DateTime start = DateTime.UtcNow;

            using (BlobStream centroidsRead = Centroids.OpenRead(), centroidsWrite = Centroids.OpenWrite())
            {
                byte[] centroidBytes = new byte[Centroid.Size];
                while (centroidsRead.Position + Centroid.Size <= centroidsRead.Length)
                {
                    centroidsRead.Read(centroidBytes, 0, Centroid.Size);
                    Centroid c = Centroid.FromByteArray(centroidBytes);

                    Point newCentroidPoint;
                    if (totalPointsProcessedDataByCentroid.ContainsKey(c.ID))
                    {
                        newCentroidPoint = totalPointsProcessedDataByCentroid[c.ID].PartialPointSum
                         / (double)totalPointsProcessedDataByCentroid[c.ID].NumPointsProcessed;
                    }
                    else
                    {
                        newCentroidPoint = new Point();
                    }

                    c.X = newCentroidPoint.X;
                    c.Y = newCentroidPoint.Y;

                    centroidBytes = c.ToByteArray();
                    centroidsWrite.Write(centroidBytes, 0, Centroid.Size);
                }
            }

            ResetPointChangedCounts();

            DateTime end = DateTime.UtcNow;
            PerformanceLog log = new PerformanceLog(jobData.JobID.ToString(), "RecalculateCentroids", start, end);
            log.IterationCount = IterationCount;
            log.Points = Points.Uri.ToString();
            log.Centroids = Centroids.Uri.ToString();
            AzureHelper.PerformanceLogger.Insert(log);
        }

        private void ResetPointChangedCounts()
        {
            TotalNumPointsChanged = 0;
            totalPointsProcessedDataByCentroid.Clear();
        }

        private void ReturnResults()
        {
            System.Diagnostics.Trace.TraceInformation("[ServerRole] ReturnResults() JobID={0}", jobData.JobID);

            KMeansJobResult jobResult = new KMeansJobResult(jobData, Points.Uri, Centroids.Uri);
            AzureHelper.EnqueueMessage(AzureHelper.ServerResponseQueue, jobResult);
            // TODO: Delete this KMeansJob from the list of jobs in ServerRole
        }
    }
}
