using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.WindowsAzure.StorageClient;

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
            using (PointStream<ClusterPoint> stream = new PointStream<ClusterPoint>(AzureHelper.GetBlob(task.Points), ClusterPoint.FromByteArray, ClusterPoint.Size))
            {
                var assignedPoints = stream.AsParallel().Select(AssignClusterPointToNearestCentroid);

                TaskResult.NumPointsChanged = assignedPoints.Select(result => result.NumPointsChanged).Sum();
                TaskResult.PointsProcessedDataByCentroid = assignedPoints.Select(result => result.PointsProcessedDataByCentroid).Aggregate(MergePointsProcessedDataByCentroid);

                using (PointStream<ClusterPoint> writeStream = new PointStream<ClusterPoint>(writeBlob, ClusterPoint.FromByteArray, ClusterPoint.Size, false))
                {
                    foreach (var p in assignedPoints)
                    {
                        writeStream.Write(p.Result);
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
            ClusterPoint result = new ClusterPoint(clusterPoint);
            result.CentroidID = centroids.MinElement(centroid => Point.Distance(clusterPoint, centroid)).ID;

            int numPointsChanged = (clusterPoint.CentroidID != result.CentroidID) ? 1 : 0;

            Dictionary<Guid, PointsProcessedData> pointsProcessedDataByCentroid = new Dictionary<Guid, PointsProcessedData>();
            pointsProcessedDataByCentroid[result.CentroidID] = new PointsProcessedData();
            pointsProcessedDataByCentroid[result.CentroidID].NumPointsProcessed++;
            pointsProcessedDataByCentroid[result.CentroidID].PartialPointSum += result;

            return new ClusterPointProcessingResult
            {
                Result = result,
                NumPointsChanged = numPointsChanged,
                PointsProcessedDataByCentroid = pointsProcessedDataByCentroid
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
        public ClusterPoint Result { get; set; }
        public int NumPointsChanged { get; set; }
        public Dictionary<Guid, PointsProcessedData> PointsProcessedDataByCentroid { get; set; }
    }
}
