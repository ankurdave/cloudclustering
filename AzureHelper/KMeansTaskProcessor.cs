using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.WindowsAzure.StorageClient;
using Microsoft.WindowsAzure.ServiceRuntime;

namespace AzureUtils
{
    public class KMeansTaskProcessor
    {
        private KMeansTaskData task;
        private List<Centroid> centroids;

        public KMeansTaskResult TaskResult { get; private set; }

        public KMeansTaskProcessor(KMeansTaskData task)
        {
            this.task = task;
            
            this.TaskResult = new KMeansTaskResult(task);
        }

        public void Run()
        {
            InitializeCentroids();
            ProcessPoints();
        }

        /// <summary>
        /// Assigns each ClusterPoint in the "points" blob to the nearest centroid, recording results into TaskResult.
        /// </summary>
        private void ProcessPoints()
        {
            // Initialize the write blob
            CloudBlob writeBlob = AzureHelper.CreateBlob(task.JobID.ToString(), Guid.NewGuid().ToString());

            // Do the mapping and write the new blob
            // Note: Using PLINQ is actually a bit slower than ClusterPoint.MapByteStream, at least on my dual-core laptop (9 seconds vs 8 seconds for 10,000 points and 10 centroids).
            // TODO: Test this on a bigger Azure VM. Still, I suspect that PLINQ will always be slower because IO is the bottleneck anyway.
            using (PointStream<ClusterPoint> stream = new PointStream<ClusterPoint>(AzureHelper.GetBlob(task.Points), ClusterPoint.FromByteArray, ClusterPoint.Size, task.PartitionNumber, task.M))
            {
                var assignedPoints = stream.AsParallel().Select(AssignClusterPointToNearestCentroid);

                TaskResult.NumPointsChanged = 0;
                TaskResult.PointsProcessedDataByCentroid = new Dictionary<Guid, PointsProcessedData>();
                using (PointStream<ClusterPoint> writeStream = new PointStream<ClusterPoint>(writeBlob, ClusterPoint.FromByteArray, ClusterPoint.Size, false))
                {
                    object numPointsChangedLock = new object();

                    foreach (var result in assignedPoints)
                    {
                        // Write the point to the new blob
                        writeStream.Write(result.Point);

                        // Update the number of points changed counter
                        if (result.PointWasChanged)
                        {
                            TaskResult.NumPointsChanged++;
                        }

                        // Add to the appropriate centroid group
                        if (!TaskResult.PointsProcessedDataByCentroid.ContainsKey(result.Point.CentroidID))
                        {
                            TaskResult.PointsProcessedDataByCentroid[result.Point.CentroidID] = new PointsProcessedData();
                        }

                        TaskResult.PointsProcessedDataByCentroid[result.Point.CentroidID].NumPointsProcessed++;
                        TaskResult.PointsProcessedDataByCentroid[result.Point.CentroidID].PartialPointSum += result.Point;
                    }
                }
            }

            // Change TaskResult.Points to refer to the new blob
            TaskResult.Points = writeBlob.Uri;
        }

        private static Dictionary<Guid, PointsProcessedData> MergePointsProcessedDataByCentroid(Dictionary<Guid, PointsProcessedData> runningTotal, Dictionary<Guid, PointsProcessedData> next)
        {
            foreach (var pair in next)
            {
                if (runningTotal.ContainsKey(pair.Key))
                {
                    runningTotal[pair.Key] += pair.Value;
                }
                else
                {
                    runningTotal.Add(pair.Key, pair.Value);
                }
            }
            return runningTotal;
        }

        private ClusterPointProcessingResult AssignClusterPointToNearestCentroid(ClusterPoint clusterPoint)
        {
            // TODO: Try in-place modification to see the performance impact
            ClusterPoint result = new ClusterPoint(clusterPoint);
            result.CentroidID = centroids.MinElement(centroid => Point.Distance(clusterPoint, centroid)).ID;

            return new ClusterPointProcessingResult
            {
                Point = result,
                PointWasChanged = clusterPoint.CentroidID != result.CentroidID
            };
        }

        private void InitializeCentroids()
        {
            using (PointStream<Centroid> stream = new PointStream<Centroid>(AzureHelper.GetBlob(task.Centroids), Centroid.FromByteArray, Centroid.Size))
            {
                centroids = stream.ToList();
            }
        }
    }

    class ClusterPointProcessingResult
    {
        public ClusterPoint Point { get; set; }
        public bool PointWasChanged { get; set; }
    }
}
