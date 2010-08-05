using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.WindowsAzure.StorageClient;
using Microsoft.WindowsAzure.ServiceRuntime;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

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
            CloudBlockBlob pointsBlob = AzureHelper.GetBlob(task.Points);

            // Do the mapping and write the new blob
            int numThreads = Environment.ProcessorCount;
            PointsProcessedData[,] pointSumsPerCentroidPerThread = new PointsProcessedData[numThreads, task.K];
            int[] pointsChangedPerThread = new int[numThreads];
            string[][] blockIDsPerThread = new string[numThreads][];

            System.Threading.Tasks.Parallel.For(0, numThreads, threadID =>
            {
                using (ObjectCachedStreamReader<ClusterPoint> stream = new ObjectCachedStreamReader<ClusterPoint>(pointsBlob, ClusterPoint.FromByteArray, ClusterPoint.Size, AzureHelper.GetLocalResourceRootPath("cache"), task.JobID.ToString(), task.PartitionNumber, task.M, subPartitionNumber: threadID, subTotalPartitions: numThreads))
                {
                    ObjectBlockWriter<ClusterPoint> writeStream = new ObjectBlockWriter<ClusterPoint>(pointsBlob, point => point.ToByteArray(), ClusterPoint.Size);

                    foreach (var point in stream)
                    {
                        // Assign the point to the nearest centroid
                        Guid oldCentroidID = point.CentroidID;
                        int closestCentroidIndex = centroids.MinIndex(centroid => Point.Distance(point, centroid));
                        Guid newCentroidID = point.CentroidID = centroids[closestCentroidIndex].ID;

                        // Write the updated point to the writeStream
                        writeStream.Write(point);

                        // Update the number of points changed
                        if (oldCentroidID != newCentroidID)
                        {
                            pointsChangedPerThread[threadID]++;
                        }

                        // Update the point sums
                        if (pointSumsPerCentroidPerThread[threadID, closestCentroidIndex] == null)
                        {
                            pointSumsPerCentroidPerThread[threadID, closestCentroidIndex] = new PointsProcessedData();
                        }
                        pointSumsPerCentroidPerThread[threadID, closestCentroidIndex].PartialPointSum += point;
                        pointSumsPerCentroidPerThread[threadID, closestCentroidIndex].NumPointsProcessed++;
                    }

                    // Collect the block IDs from writeStream
                    writeStream.FlushBlock();
                    blockIDsPerThread[threadID] = writeStream.BlockList.ToArray();
                }
            });

            // Combine the per-thread block lists and write the full block list to a blob. Then include that as part of TaskResult
            List<string> blockIDs = new List<string>();
            foreach (string[] blockIDsFromThread in blockIDsPerThread)
            {
                blockIDs.AddRange(blockIDsFromThread);
            }
            CloudBlob blockIDsBlob = AzureHelper.CreateBlob(task.JobID.ToString(), Guid.NewGuid().ToString());
            using (Stream stream = blockIDsBlob.OpenWrite())
            {
                BinaryFormatter bf = new BinaryFormatter();
                bf.Serialize(stream, blockIDs);
            }
            TaskResult.PointsBlockListBlob =  blockIDsBlob.Uri;

            // Total up the per-thread pointSumsPerCentroid
            TaskResult.PointsProcessedDataByCentroid = new Dictionary<Guid, PointsProcessedData>();
            for (int i = 0; i < task.K; ++i)
            {
                Guid centroidID = centroids[i].ID;
                TaskResult.PointsProcessedDataByCentroid[centroidID] = new PointsProcessedData();

                for (int j = 0; j < numThreads; ++j)
                {
                    if (pointSumsPerCentroidPerThread[j, i] != null)
                    {
                        TaskResult.PointsProcessedDataByCentroid[centroidID].PartialPointSum += pointSumsPerCentroidPerThread[j, i].PartialPointSum;
                        TaskResult.PointsProcessedDataByCentroid[centroidID].NumPointsProcessed += pointSumsPerCentroidPerThread[j, i].NumPointsProcessed;
                    }
                }
            }

            // Total up the per-thread numPointsChanged
            TaskResult.NumPointsChanged = 0;
            foreach (int threadPointsChanged in pointsChangedPerThread)
            {
                TaskResult.NumPointsChanged += threadPointsChanged;
            }
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
            using (ObjectStreamReader<Centroid> stream = new ObjectStreamReader<Centroid>(AzureHelper.GetBlob(task.Centroids), Centroid.FromByteArray, Centroid.Size))
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
