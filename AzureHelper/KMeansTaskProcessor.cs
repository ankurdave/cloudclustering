using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.WindowsAzure.StorageClient;

namespace AzureUtils
{
    public class KMeansTaskProcessor
    {
        private KMeansTask task;
        private List<Centroid> centroids;

        public KMeansTaskResult TaskResult { get; private set; }

        public KMeansTaskProcessor(KMeansTask task)
        {
            this.task = task;
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
            InitializeTaskResult();
            using (BlobStream pointsStreamRead = task.Points.OpenRead(), pointsStreamWrite = task.Points.OpenWrite())
            {
                ClusterPoint.MapByteStream(pointsStreamRead, pointsStreamWrite,
                    clusterPoint => AssignClusterPointToNearestCentroid(clusterPoint));
            }
        }

        private void InitializeTaskResult()
        {
            TaskResult = new KMeansTaskResult(task);
            TaskResult.NumPointsChanged = 0;
            TaskResult.PointsProcessedDataByCentroid = new Dictionary<Guid, PointsProcessedData>();
        }

        private void InitializeCentroids()
        {
            using (BlobStream centroidsStream = task.Centroids.OpenRead())
            {
                centroids = Centroid.ListFromByteStream(centroidsStream);
            }
        }

        private ClusterPoint AssignClusterPointToNearestCentroid(ClusterPoint clusterPoint)
        {
            ClusterPoint result = new ClusterPoint(clusterPoint);
            result.CentroidID = centroids.MinElement(centroid => Point.Distance(clusterPoint, centroid)).ID;

            // Increment NumPointsChanged if appropriate
            if (clusterPoint.CentroidID != result.CentroidID)
            {
                TaskResult.NumPointsChanged++;
            }

            // Add to the point sum in PointsProcessedDataByCentroid
            if (!TaskResult.PointsProcessedDataByCentroid.ContainsKey(result.CentroidID))
            {
                TaskResult.PointsProcessedDataByCentroid[result.CentroidID] = new PointsProcessedData();
            }
            TaskResult.PointsProcessedDataByCentroid[result.CentroidID].NumPointsProcessed++;
            TaskResult.PointsProcessedDataByCentroid[result.CentroidID].PartialPointSum += result;

            return result;
        }
    }
}
