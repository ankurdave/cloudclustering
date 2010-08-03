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
        public const string WorkerRequestQueue = "workerrequest";
        public const string ServerResponseQueue = "serverresponse";
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

        public static bool PollForMessage(string queueName, Func<AzureMessage, bool> condition, Func<AzureMessage, bool> action, int visibilityTimeoutSeconds = 30)
        {
            CloudQueue queue = StorageAccount.CreateCloudQueueClient().GetQueueReference(queueName);

            CloudQueueMessage queueMessage;
            queue.CreateIfNotExist();
            queueMessage = queue.GetMessage(new TimeSpan(0, 0, visibilityTimeoutSeconds));

            if (queueMessage == null)
                return false;

            AzureMessage message = AzureMessage.FromMessage(queueMessage);

            if (!condition.Invoke(message))
                return false;

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
                    throw;
                }
            }

            return true;
        }

        public static void ClearQueues()
        {
            CloudQueueClient client = StorageAccount.CreateCloudQueueClient();
            string[] queues = { ServerRequestQueue, ServerResponseQueue, WorkerRequestQueue, WorkerResponseQueue };

            foreach (string queueName in queues)
            {
                CloudQueue queue = client.GetQueueReference(queueName);
                queue.CreateIfNotExist();
                queue.Clear();
            }
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
        /// Slices a sequence into a sub-sequences each containing maxItemsPerSlice, except for the last
        /// which will contain any items left over
        /// </summary>
        public static IEnumerable<IGrouping<int, T>> Slice<T>(this IEnumerable<T> sequence, int maxItemsPerSlice)
        {
            return sequence
                .Select((element, index) => new { Index = index, Element = element })
                .GroupBy(indexedElement => indexedElement.Index / maxItemsPerSlice, indexedElement => indexedElement.Element);
        }

        /// <summary>
        /// Slices a sequence into numSlices slices.
        /// </summary>
        public static IEnumerable<IGrouping<int, T>> SliceInto<T>(this IEnumerable<T> sequence, int numSlices)
        {
            return sequence
                .Select((element, index) => new { Index = index, Element = element })
                .GroupBy(indexedElement => indexedElement.Index % numSlices, indexedElement => indexedElement.Element);
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
    }
}
