using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.WindowsAzure.StorageClient;

namespace AzureUtils
{
    public class KMeansJob
    {
        private CloudBlob Points { get; set; }
        private CloudBlob Centroids { get; set; }
        private int TotalNumPointsChanged { get; set; }
        private Dictionary<Guid, PointsProcessedData> totalPointsProcessedDataByCentroid = new Dictionary<Guid,PointsProcessedData>();
        private HashSet<Guid> taskIDs = new HashSet<Guid>();
        private KMeansJobData jobData;

        public KMeansJob(KMeansJobData jobData) {
            this.jobData = jobData;
        }

        public void InitializeStorage()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Enqueues M messages into a queue. Each message is an instruction to a worker to process a partition of the k-means data.
        /// </summary>
        public void EnqueueTasks()
        {
            for (int i = 0; i < jobData.M; i++)
            {
                KMeansTask task = new KMeansTask(jobData);
                task.TaskID = Guid.NewGuid();
                task.Points = CopyPointPartition(i, jobData.M);
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
        /// <param name="partitionNumber">Partition number to copy. Must be in the range [0,totalPartitions)</param>
        /// <param name="totalPartitions">Total number of partitions to split Points into. Must be 1 or more.</param>
        private CloudBlob CopyPointPartition(int partitionNumber, int totalPartitions)
        {
            throw new NotImplementedException();
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
            throw new NotImplementedException();
        }

        private bool NumPointsChangedAboveThreshold()
        {
            throw new NotImplementedException();
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
