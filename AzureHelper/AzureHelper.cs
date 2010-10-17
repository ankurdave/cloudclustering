using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.WindowsAzure.StorageClient;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.ServiceRuntime;
using System.Threading;
using System.IO;
using System.Net.Mail;
using System.Net;

namespace AzureUtils
{
    public static class AzureHelper
    {
        public const string ServerRequestQueue = "serverrequest";
        public const string WorkerResponseQueue = "workerresponse";
        public const string ServerResponseQueue = "serverresponse";
        public const string ServerControlQueue = "servercontrol";
        public const string PointsBlob = "points";
        public const string CentroidsBlob = "centroids";

        /// <summary>
        /// The maximum number of bytes that can be stored in a block. It's actually 4 MiB, but this leaves some headroom.
        /// </summary>
        public const int MaxBlockSize = 4000000;

        private static CloudStorageAccount _storageAccount;
        public static CloudStorageAccount StorageAccount
        {
            get
            {
                if (_storageAccount == null)
                {
                    if (RoleEnvironment.IsAvailable)
                    {
                        CloudStorageAccount.SetConfigurationSettingPublisher((configName, configSetter) =>
                            configSetter(RoleEnvironment.GetConfigurationSettingValue(configName)));
                        _storageAccount = CloudStorageAccount.FromConfigurationSetting("DataConnectionString");
                    }
                    else
                    {
                        _storageAccount = CloudStorageAccount.DevelopmentStorageAccount;
                    }
                }

                return _storageAccount;
            }
        }

        private static PerformanceLogDataSource _performanceLogger;
        public static PerformanceLogDataSource PerformanceLogger
        {
            get
            {
                if (_performanceLogger == null)
                {
                    _performanceLogger = new PerformanceLogDataSource();
                }

                return _performanceLogger;
            }
        }

        private static WorkerStatsContext _workerStatsReporter;
        public static WorkerStatsContext WorkerStatsReporter
        {
            get
            {
                if (_workerStatsReporter == null)
                {
                    _workerStatsReporter = new WorkerStatsContext();
                }
                return _workerStatsReporter;
            }
        }

        #region Queue-related methods
        public static void EnqueueMessage(string queueName, AzureMessage message, bool async = false)
        {
            CloudQueue queue = StorageAccount.CreateCloudQueueClient().GetQueueReference(queueName);
            queue.CreateIfNotExist();

            CloudQueueMessage queueMessage = new CloudQueueMessage(message.ToBinary());

            if (async)
            {
                queue.BeginAddMessage(queueMessage, ar => { }, null);
            }
            else
            {
                queue.AddMessage(queueMessage);
            }
        }

        public static bool PollForMessageRawCondition<T>(string queueName, Func<T, bool> action, int visibilityTimeoutSeconds = 30, Func<CloudQueueMessage, bool> condition = null) where T : AzureMessage
        {
            CloudQueue queue = StorageAccount.CreateCloudQueueClient().GetQueueReference(queueName);

            CloudQueueMessage queueMessage;
            queue.CreateIfNotExist();
            queueMessage = queue.GetMessage(new TimeSpan(0, 0, visibilityTimeoutSeconds));

            if (queueMessage == null)
                return false;

            T message = AzureMessage.FromMessage(queueMessage) as T;

            if (condition != null && !condition.Invoke(queueMessage))
            {
                // Force the message to become visible on the queue, because we don't want to process it. See AzureHelperTest.AddThenDeleteMessageTest for a demonstration that this works.
                queue.AddMessage(queueMessage);
                queue.DeleteMessage(queueMessage);

                return false;
            }

            if (!action.Invoke(message))
                return false;

            try
            {
                queue.DeleteMessage(queueMessage);
            }
            catch (StorageClientException e)
            {
                // It took too long to process the message and the visibility timeout expired
                // See http://blog.smarx.com/posts/deleting-windows-azure-queue-messages-handling-exceptions
                if (e.ExtendedErrorInformation.ErrorCode == "MessageNotFound")
                {
                    System.Diagnostics.Trace.WriteLine("Visibility timeout expired: " + e);
                    return false;
                }
                else
                {
                    throw e;
                }
            }

            return true;
        }

        public static bool PollForMessage<T>(string queueName, Func<T, bool> action, int visibilityTimeoutSeconds = 30, Func<T, bool> condition = null) where T : AzureMessage
        {
            // Wrap condition in a wrapper function that converts the raw message into the type that condition wants
            Func<CloudQueueMessage, bool> rawCondition = null;
            if (condition != null)
            {
                rawCondition = rawMessage => condition.Invoke(AzureMessage.FromMessage(rawMessage) as T);
            }

            return PollForMessageRawCondition<T>(queueName, action, visibilityTimeoutSeconds, rawCondition);
        }

        public static void ClearQueues()
        {
            CloudQueueClient client = StorageAccount.CreateCloudQueueClient();
            string[] queues = { ServerRequestQueue, ServerResponseQueue, WorkerResponseQueue };

            foreach (string queueName in queues)
            {
                CloudQueue queue = client.GetQueueReference(queueName);
                queue.CreateIfNotExist();
                queue.Clear();
            }
        }

        public static string GetWorkerRequestQueue(string machineID)
        {
            return "workerrequest" + machineID;
        }
        #endregion

        #region Blob-related methods
        public static CloudBlockBlob GetBlob(Uri uri)
        {
            CloudBlockBlob blob = StorageAccount.CreateCloudBlobClient().GetBlockBlobReference(uri.ToString());
            blob.FetchAttributes();
            return blob;
        }

        public static CloudBlob GetBlob(string containerName, string blobName)
        {
            CloudBlobContainer container = StorageAccount.CreateCloudBlobClient().GetContainerReference(containerName);
            return container.GetBlobReference(blobName);
        }

        public static CloudBlockBlob CreateBlob(string containerName, string blobName)
        {
            CloudBlobContainer container = StorageAccount.CreateCloudBlobClient().GetContainerReference(containerName);
            container.CreateIfNotExist();
            return container.GetBlockBlobReference(blobName);
        }

        public static string GenerateRandomBlockID()
        {
            return Convert.ToBase64String(Guid.NewGuid().ToByteArray());
        }

        public static void CommitBlockBlob(CloudBlockBlob blob, List<string> blockIDs)
        {
            blob.PutBlockList(blockIDs);
            blob.FetchAttributes(); // Refresh the attributes after PutBlockList has cleared them, so that they can be relied on for later calculations
        }

        /// <summary>
        /// Given the number of total elements in a sequence and the number of partitions to divide it into, calculates the maximum number of elements per partition.
        /// </summary>
        public static long PartitionLength(long numElements, int numPartitions)
        {
            return (long)Math.Ceiling((double)numElements / numPartitions);
        }
        #endregion

        #region General utility functions
        public static TimeSpan Time(Action action)
        {
            DateTime start = DateTime.Now;
            action.Invoke();
            DateTime end = DateTime.Now;
            return end - start;
        }

        public static void ExponentialBackoff(Func<bool> action, int firstDelayMilliseconds = 100, int backoffFactor = 2, int maxDelay = 10000, int retryLimit = int.MaxValue)
        {
            int delayMilliseconds = firstDelayMilliseconds;
            for (int i = 0; i < retryLimit; i++)
            {
                if (action.Invoke())
                    break;

                Thread.Sleep(delayMilliseconds);

                delayMilliseconds *= backoffFactor;
                if (delayMilliseconds > maxDelay)
                {
                    delayMilliseconds = maxDelay;
                }
            }

        }
        #endregion

        #region LINQ extension methods
        /// <summary>
        /// Slices a sequence into sub-sequences each containing maxItemsPerSlice, except for the last,
        /// which will contain any items left over.
        /// </summary>
        public static IEnumerable<IEnumerable<T>> Slice<T>(this IEnumerable<T> sequence, int maxItemsPerSlice)
        {
            return sequence
                .Select((element, index) => new { Index = index, Element = element })
                .GroupBy(indexedElement => indexedElement.Index / maxItemsPerSlice, indexedElement => indexedElement.Element);
        }

        /// <summary>
        /// Slices a sequences into sub-sequences each containing minItemsPerSlice, except for the last,
        /// which will contain minItemsPerSlice plus any items left over.
        /// 
        /// Note: This iterates over the sequence at least once, so it needs to be reusable.
        /// </summary>
        public static IEnumerable<IEnumerable<T>> SliceMin<T>(this IEnumerable<T> sequence, int minItemsPerSlice)
        {
            var sliced = sequence.Slice(minItemsPerSlice);
            if (sliced.Last().Count() < minItemsPerSlice)
            {
                return sliced.AppendLastToSecondLast();
            }
            else
            {
                return sliced;
            }
        }

        /// <summary>
        /// Takes a list of lists and merges the last two elements.
        /// 
        /// For example, given { { 1, 2 }, { 3, 4 }, { 5 } }, returns { { 1, 2 }, { 3, 4, 5} }.
        /// 
        /// Note: This iterates over the sequence at least once, so it needs to be reusable.
        /// </summary>
        public static IEnumerable<IEnumerable<T>> AppendLastToSecondLast<T>(this IEnumerable<IEnumerable<T>> sequence)
        {
            int length = sequence.Count();

            if (length < 2)
            {
                return sequence;
            }

            var butLast2 = sequence.Take(length - 2);
            var last2 = sequence.Skip(length - 2);

            return butLast2.Concat(new List<IEnumerable<T>> { last2.Flatten1() });
        }

        /// <summary>
        /// Slices a sequence into numSlices slices.
        /// </summary>
        public static IEnumerable<IEnumerable<T>> SliceInto<T>(this IEnumerable<T> sequence, int numSlices)
        {
            int maxItemsPerSlice = (int)Math.Ceiling((double)sequence.Count() / numSlices);
            return sequence.Slice(maxItemsPerSlice);
        }

        /// <summary>
        /// Flattens a sequence of sequences by one level.
        /// </summary>
        public static IEnumerable<T> Flatten1<T>(this IEnumerable<IEnumerable<T>> sequence)
        {
            return sequence.Aggregate((list1, list2) => list1.Concat(list2));
        }

        /// <summary>
        /// Merges an arbitrary number of sequences by using the specified predicate function. This is a generalized form of the standard Enumerable.Zip method, except that all the sequences have to be of the same type.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of all the input sequences.</typeparam>
        /// <typeparam name="TResult">The type of the elements of the result sequence.</typeparam>
        /// <param name="source">A list of sequences to merge.</param>
        /// <param name="resultSelector">A function that specifies how to merge the elements from all the sequences.</param>
        /// <remarks>Like Enumerable.Zip, this method merges sequences until it reaches the end of one of them.</remarks>
        public static IEnumerable<TResult> ZipN<TSource, TResult>(this IEnumerable<IEnumerable<TSource>> source, Func<IEnumerable<TSource>, TResult> resultSelector)
        {
            var iterators = source.Select(list => list.GetEnumerator()).ToList();
            // Note: ToList forces the iterators to be generated now. Lazy evaluation causes weird out-of-order behavior.

            // Step through the iterators until one of them runs out, and keep calling resultSelector on the current list of values
            while (iterators.Select(iter => iter.MoveNext()).Aggregate((a, b) => a && b))
            {
                yield return resultSelector.Invoke(iterators.Select(iter => iter.Current).ToList());
                // Same as above -- ToList is needed because if resultSelector does not use part of its input, the iterator ordering gets messed up.
            }
        }
        #endregion

        public static void SendStatusEmail(string emailAddress, Guid jobID, int iteration)
        {
            SmtpClient client = new SmtpClient(RoleEnvironment.GetConfigurationSettingValue("mailSmtpHost"), int.Parse(RoleEnvironment.GetConfigurationSettingValue("mailSmtpPort")))
            {
                Credentials = new NetworkCredential(RoleEnvironment.GetConfigurationSettingValue("mailSendingAddress"), RoleEnvironment.GetConfigurationSettingValue("mailSendingPassword")),
                EnableSsl = bool.Parse(RoleEnvironment.GetConfigurationSettingValue("mailSmtpSsl"))
            };

            try
            {
                client.Send(RoleEnvironment.GetConfigurationSettingValue("mailSendingAddress"), emailAddress, string.Format("CloudClustering job {0}", jobID), string.Format("CloudClustering job {0} has begun iteration {1}.", jobID, iteration));
            }
            catch (Exception e)
            {
                System.Diagnostics.Trace.WriteLine("Failed to send status email: " + e.ToString());
            }
        }

        /// <summary>
        /// Logs a PerformanceLog entry having the given parameters based on the evaluation of the given action. Supports lazy arguments in certain cases. If an argument is passed lazily, it will only be evaluated after the action is invoked.
        /// </summary>
        public static void LogPerformance(Action action, string jobID, string methodName, int iterationCount, Lazy<string> points, Lazy<string> centroids, string machineID)
        {
            DateTime start = DateTime.UtcNow;
            PerformanceLog log = new PerformanceLog(jobID, methodName, start, start);
            log.IterationCount = iterationCount;
            if (!points.IsLazy)
                log.Points = points.Eval();
            if (!centroids.IsLazy)
                log.Centroids = centroids.Eval();
            log.MachineID = machineID;
            AzureHelper.PerformanceLogger.Insert(log);

            action.Invoke();

            DateTime end = DateTime.UtcNow;
            log.EndTime = end;
            if (points.IsLazy)
                log.Points = points.Eval();
            if (centroids.IsLazy)
                log.Centroids = centroids.Eval();
            AzureHelper.PerformanceLogger.Update(log);
        }

        public static string GetLocalResourceRootPath(string localResourceName)
        {
            if (RoleEnvironment.IsAvailable)
            {
                return RoleEnvironment.GetLocalResource(localResourceName).RootPath;
            }
            else
            {
                return Environment.GetEnvironmentVariable("TEMP");
            }
        }

        public static string GetCachedFilePath(string cacheDirectory, string cachePrefix, int partitionNumber, int totalPartitions, int subPartitionNumber, int iterationNumber)
        {
            return string.Format(@"{4}\{0}-{1}-{2}-{3}-{5}", cachePrefix, totalPartitions, partitionNumber, subPartitionNumber, cacheDirectory, iterationNumber);
        }
    }
}
