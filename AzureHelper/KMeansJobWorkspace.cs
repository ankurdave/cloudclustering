using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.WindowsAzure.StorageClient;

namespace AzureUtils
{
    public class KMeansJobWorkspace : KMeansJob
    {
        public CloudBlob Points { get; set; }
        public CloudBlob Centroids { get; set; }
        public int TotalNumPointsChanged { get; set; }
        
        private Dictionary<Guid, PointsProcessedData> totalPointsProcessedDataByCentroid = new Dictionary<Guid,PointsProcessedData>();
        private HashSet<Guid> taskIDs = new HashSet<Guid>();

        public KMeansJobWorkspace(KMeansJob job) : base(job) { }

        public void AddTaskID(Guid taskID)
        {
            taskIDs.Add(taskID);
        }

        public bool ContainsTaskID(Guid taskID)
        {
            return taskIDs.Contains(taskID);
        }

        public void RemoveTaskID(Guid taskID)
        {
            taskIDs.Remove(taskID);
        }

        public void InitializeStorage()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Checks whether to move into the next iteration, and performs the appropriate actions to make it happen.
        /// </summary>
        public void NextIteration()
        {
            if (NumPointsChangedAboveThreshold())
            {
                RecalculateCentroids();
                EnqueueTasks();
            }
        }

        /// <summary>
        /// Enqueues M messages into a queue. Each message is an instruction to a worker to process a partition of the k-means data.
        /// </summary>
        public void EnqueueTasks()
        {
            for (int i = 0; i < M; i++)
            {
                KMeansTask task = new KMeansTask(this);
                task.TaskID = Guid.NewGuid();
                task.Points = CopyPointPartition(i, M);
                task.Centroids = Centroids;

                AddTaskID(task.TaskID);

                AzureHelper.EnqueueMessage("workerrequest", task);
            }
        }

        /// <summary>
        /// Copies a partition of the Points blob into a new blob and returns that new blob.
        /// </summary>
        /// <param name="partitionNumber">Partition number to copy. Must be in the range [0,totalPartitions)</param>
        /// <param name="totalPartitions">Total number of partitions to split Points into. Must be 1 or more.</param>
        public CloudBlob CopyPointPartition(int partitionNumber, int totalPartitions)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Sums the given taskResult's points processed data with the totals.
        /// </summary>
        /// <param name="taskResult"></param>
        public void AddDataFromTaskResult(KMeansTaskResult taskResult)
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

        public bool NoMoreTaskIDs()
        {
            throw new NotImplementedException();
        }

        public bool NumPointsChangedAboveThreshold()
        {
            throw new NotImplementedException();
        }

        public void RecalculateCentroids()
        {
            throw new NotImplementedException();
        }
    }
}
