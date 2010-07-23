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
            Random random = new Random();

            // Initialize the points blob with N random ClusterPoints
            Points = AzureHelper.CreateBlob(jobData.JobID.ToString(), AzureHelper.PointsBlob);
            using (PointStream<ClusterPoint> stream = new PointStream<ClusterPoint>(Points, ClusterPoint.FromByteArray, ClusterPoint.Size, false))
            {
                for (int i = 0; i < jobData.N; i++)
                {
                    stream.Write(new ClusterPoint(
                        random.NextDouble() * 100 - 50,
                        random.NextDouble() * 100 - 50,
                        Guid.Empty));
                }
            }
            
            // Initialize the centroids blob with K random Centroids
            Centroids = AzureHelper.CreateBlob(jobData.JobID.ToString(), AzureHelper.CentroidsBlob);
            using (PointStream<Centroid> stream = new PointStream<Centroid>(Centroids, Centroid.FromByteArray, Centroid.Size, false))
            {
                for (int i = 0; i < jobData.K; i++)
                {
                    stream.Write(new Centroid(
                        Guid.NewGuid(),
                        random.Next(-PointRange, PointRange),
                        random.Next(-PointRange, PointRange)));
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

            using (PointStream<ClusterPoint> pointStream = new PointStream<ClusterPoint>(Points, ClusterPoint.FromByteArray, ClusterPoint.Size))
            {
                for (int i = 0; i < jobData.M; i++)
                {
                    KMeansTaskData taskData = new KMeansTaskData(jobData, Guid.NewGuid(), Points.Uri, i, Centroids.Uri, start, IterationCount);

                    tasks.Add(new KMeansTask(taskData));

                    AzureHelper.EnqueueMessage(AzureHelper.WorkerRequestQueue, taskData, true);
                }
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
            List<string> blockIDs = AzureHelper.CopyBlobToBlocks(AzureHelper.GetBlob(taskResult.Points), Points);
            pointsBlockIDs.AddRange(blockIDs);

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
            AzureHelper.CommitBlockBlob(Points, pointsBlockIDs);
            pointsBlockIDs.Clear();
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

            // Initialize the output blob
            CloudBlob writeBlob = AzureHelper.CreateBlob(jobData.JobID.ToString(), Guid.NewGuid().ToString());

            // Do the mapping and write the new blob
            using (PointStream<Centroid> stream = new PointStream<Centroid>(Centroids, Centroid.FromByteArray, Centroid.Size))
            {
                var newCentroids = stream.Select(c =>
                    {
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

                        return c;
                    });

                using (PointStream<Centroid> writeStream = new PointStream<Centroid>(writeBlob, Centroid.FromByteArray, Centroid.Size, false))
                {
                    foreach (Centroid c in newCentroids)
                    {
                        writeStream.Write(c);
                    }
                }
            }

            // Copy the contents of the new blob back into the old blob
            Centroids.CopyFromBlob(writeBlob);

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
