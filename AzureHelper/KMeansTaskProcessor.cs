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
            CloudBlob points = AzureHelper.GetBlob(task.Points);
            using (BlobStream pointsStreamRead = points.OpenRead(), pointsStreamWrite = points.OpenWrite())
            {
                ClusterPoint.MapByteStream(pointsStreamRead, pointsStreamWrite,
                    clusterPoint => AssignClusterPointToNearestCentroid(clusterPoint));
            }
        }

        private void InitializeCentroids()
        {
            CloudBlob centroidsBlob = AzureHelper.GetBlob(task.Centroids);
            using (BlobStream centroidsStream = centroidsBlob.OpenRead())
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
