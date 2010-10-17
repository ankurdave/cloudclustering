using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Security.Cryptography;
using Microsoft.WindowsAzure.StorageClient;
using System.Runtime.Serialization.Formatters.Binary;

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
        public string MachineID { get; set; }

        public KMeansJob(KMeansJobData jobData, string machineID) {
            this.jobData = jobData;
            this.MachineID = machineID;

            this.IterationCount = 0;
        }

        /// <summary>
        /// Sets up the Azure storage (Points and Centroids) for the first k-means iteration.
        /// </summary>
        public void InitializeStorage()
        {
            AzureHelper.LogPerformance(() =>
            {
                Random random = new Random();

                if (jobData.Points == null)
                {
                    // Initialize the points blob with N random ClusterPoints
                    Points = AzureHelper.CreateBlob(jobData.JobID.ToString(), AzureHelper.PointsBlob);
                    using (ObjectStreamWriter<ClusterPoint> stream = new ObjectStreamWriter<ClusterPoint>(Points, point => point.ToByteArray(), ClusterPoint.Size))
                    {
                        for (int i = 0; i < jobData.N; i++)
                        {
                            stream.Write(new ClusterPoint(
                                random.NextDouble() * 100 - 50,
                                random.NextDouble() * 100 - 50,
                                Guid.Empty));
                        }
                    }
                }
                else
                {
                    // Use the given points blob
                    Points = AzureHelper.GetBlob(jobData.Points);

                    // Initialize N based on that
                    using (ObjectStreamReader<ClusterPoint> stream = new ObjectStreamReader<ClusterPoint>(Points, ClusterPoint.FromByteArray, ClusterPoint.Size))
                    {
                        jobData.N = (int)stream.Length;
                    }
                }

                // Initialize the centroids blob with K random Centroids
                Centroids = AzureHelper.CreateBlob(jobData.JobID.ToString(), AzureHelper.CentroidsBlob);
                using (ObjectStreamWriter<Centroid> stream = new ObjectStreamWriter<Centroid>(Centroids, point => point.ToByteArray(), Centroid.Size))
                {
                    for (int i = 0; i < jobData.K; i++)
                    {
                        stream.Write(new Centroid(
                            Guid.NewGuid(),
                            random.Next(-PointRange, PointRange),
                            random.Next(-PointRange, PointRange)));
                    }
                }
            }, jobID: jobData.JobID.ToString(), methodName: "InitializeStorage", iterationCount: IterationCount, points: new Lazy<string>(() => Points.Uri.ToString()), centroids: new Lazy<string>(() => Centroids.Uri.ToString()), machineID: MachineID);
        }

        /// <summary>
        /// Enqueues M messages into a queue. Each message is an instruction to a worker to process a partition of the k-means data.
        /// </summary>
        public void EnqueueTasks(IEnumerable<Worker> workers)
        {
            AzureHelper.LogPerformance(() =>
            {
                int workerNumber = 0;

                // Loop through the known workers and give them each a chunk of the points.
                // Note: This loop must execute in the same order every time, otherwise caching will not work -- the workers will get a different workerNumber each time and therefore a different chunk of the points.
                // We use OrderBy on the PartitionKey to guarantee stable ordering.
                foreach (Worker worker in workers.OrderBy(worker => worker.PartitionKey))
                {
                    KMeansTaskData taskData = new KMeansTaskData(jobData, Guid.NewGuid(), workerNumber++, workers.Count(), Centroids.Uri, DateTime.UtcNow, IterationCount, worker.BuddyGroupID);
                    taskData.Points = Points.Uri;

                    tasks.Add(new KMeansTask(taskData));

                    AzureHelper.EnqueueMessage(AzureHelper.GetWorkerRequestQueue(worker.PartitionKey), taskData, true);
                }
            }, jobData.JobID.ToString(), methodName: "EnqueueTasks", iterationCount: IterationCount, points: Points.Uri.ToString(), centroids: Centroids.Uri.ToString(), machineID: MachineID);
        }

        /// <summary>
        /// Handles a worker's TaskResult from a running k-means job. Adds up the partial sums from the TaskResult.
        /// </summary>
        /// <param name="message"></param>
        /// <returns>False if the given taskData result has already been counted, true otherwise.</returns>
        public bool ProcessWorkerResponse(KMeansTaskResult taskResult, IEnumerable<Worker> workers)
        {
            // Make sure we're actually still waiting for a result for this taskData
            // If not, this might be a duplicate queue message
            if (!TaskResultMatchesRunningTask(taskResult))
                return true;

            AzureHelper.LogPerformance(() =>
            {
                KMeansTask task = TaskResultWithTaskID(taskResult.TaskID);
                task.Running = false; // The task has returned a response, which means that it has stopped running

                // Add the worker's updated points blocks
                if (taskResult.PointsBlockListBlob != null)
                {
                    using (Stream stream = AzureHelper.GetBlob(taskResult.PointsBlockListBlob).OpenRead())
                    {
                        BinaryFormatter bf = new BinaryFormatter();
                        List<string> pointsBlockList = bf.Deserialize(stream) as List<string>;
                        pointsBlockIDs.AddRange(pointsBlockList);
                    }
                }

                // Copy out and integrate the data from the worker response
                AddDataFromTaskResult(taskResult);
            }, jobData.JobID.ToString(), methodName: "ProcessWorkerResponse", iterationCount: IterationCount, points: Points.Uri.ToString(), centroids: Centroids.Uri.ToString(), machineID: MachineID);

            // If this is the last worker to return, this iteration is done and we should start the next one
            if (NoMoreRunningTasks())
            {
                NextIteration(workers);
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
        private void NextIteration(IEnumerable<Worker> workers)
        {
            System.Diagnostics.Trace.TraceInformation("[ServerRole] NextIteration() JobID={0}", jobData.JobID);

            CommitPointsBlob();

            IterationCount++;
            if (!string.IsNullOrEmpty(jobData.ProgressEmail))
            {
                AzureHelper.SendStatusEmail(jobData.ProgressEmail, jobData.JobID, IterationCount);
            }

            if (NumPointsChangedAboveThreshold())
            {
                RecalculateCentroids();

                if (!MaxIterationCountExceeded())
                {
                    EnqueueTasks(workers);
                }
                else
                {
                    ReturnResults();
                }
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
            AzureHelper.LogPerformance(() =>
            {
                // Initialize the output blob
                CloudBlob writeBlob = AzureHelper.CreateBlob(jobData.JobID.ToString(), Guid.NewGuid().ToString());

                // Do the mapping and write the new blob
                using (ObjectStreamReader<Centroid> stream = new ObjectStreamReader<Centroid>(Centroids, Centroid.FromByteArray, Centroid.Size))
                {
                    var newCentroids = stream.Select(c =>
                        {
                            Point newCentroidPoint;
                            if (totalPointsProcessedDataByCentroid.ContainsKey(c.ID) && totalPointsProcessedDataByCentroid[c.ID].NumPointsProcessed != 0)
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

                    using (ObjectStreamWriter<Centroid> writeStream = new ObjectStreamWriter<Centroid>(writeBlob, point => point.ToByteArray(), Centroid.Size))
                    {
                        foreach (Centroid c in newCentroids)
                        {
                            writeStream.Write(c);
                        }
                    }
                }

                // Copy the contents of the new blob back into the old blob
                Centroids.CopyFromBlob(writeBlob);

                System.Diagnostics.Trace.TraceInformation("Finished RecalculateCentroids(). Total points changed: {0}", TotalNumPointsChanged);

                ResetPointChangedCounts();

            }, jobData.JobID.ToString(), methodName: "RecalculateCentroids", iterationCount: IterationCount, points: Points.Uri.ToString(), centroids: Centroids.Uri.ToString(), machineID: MachineID);
        }

        private void ResetPointChangedCounts()
        {
            TotalNumPointsChanged = 0;
            totalPointsProcessedDataByCentroid.Clear();
        }

        private void ReturnResults()
        {
            System.Diagnostics.Trace.TraceInformation("[ServerRole] ReturnResults() JobID={0}", jobData.JobID);

            KMeansJobResult jobResult = new KMeansJobResult(jobData, Centroids.Uri);
            jobResult.Points = Points.Uri;
            AzureHelper.EnqueueMessage(AzureHelper.ServerResponseQueue, jobResult);
            // TODO: Delete this KMeansJob from the list of jobs in ServerRole
        }
    }
}
