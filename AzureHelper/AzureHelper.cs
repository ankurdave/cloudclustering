using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.WindowsAzure.StorageClient;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.ServiceRuntime;
using System.Threading;
using System.IO;

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

            AzureMessage message = CreateAzureMessage(queueName, queueMessage);

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

        private static AzureMessage CreateAzureMessage(string queueName, CloudQueueMessage queueMessage)
        {
            switch (queueName) {
                case ServerRequestQueue:
                    return KMeansJobData.FromMessage<KMeansJobData>(queueMessage);
                case ServerResponseQueue:
                    return KMeansJobResult.FromMessage<KMeansJobResult>(queueMessage);
                case WorkerRequestQueue:
                    return KMeansTaskData.FromMessage<KMeansTaskData>(queueMessage);
                case WorkerResponseQueue:
                    return KMeansTaskResult.FromMessage<KMeansTaskResult>(queueMessage);
                default:
                    throw new InvalidOperationException();
            }
        }

        public static void WaitForMessage(string queueName, Func<AzureMessage, bool> condition, Func<AzureMessage, bool> action, int delayMilliseconds = 500, int iterationLimit = 0, int visibilityTimeoutSeconds = 30)
        {
            for (int i = 0; iterationLimit == 0 || i < iterationLimit; i++)
            {
                if (PollForMessage(queueName, condition, action, visibilityTimeoutSeconds))
                {
                    break;
                }
                else
                {
                    Thread.Sleep(delayMilliseconds);
                }
            }
        }

        public static CloudBlob GetBlob(Uri uri)
        {
            CloudBlob blob = StorageAccount.CreateCloudBlobClient().GetBlobReference(uri.ToString());
            blob.FetchAttributes();
            return blob;
        }

        private static void CopyStreamUpToLimit(Stream input, Stream output, int maxBytesToCopy, byte[] copyBuffer)
        {
            int numBytesToRead, numBytesActuallyRead, numBytesAlreadyRead = 0;
            while (true)
            {
                numBytesToRead = Math.Min(copyBuffer.Length, maxBytesToCopy - numBytesAlreadyRead);
                numBytesActuallyRead = input.Read(copyBuffer, 0, numBytesToRead);
                if (numBytesActuallyRead == 0)
                    break;
                numBytesAlreadyRead += numBytesActuallyRead;

                output.Write(copyBuffer, 0, numBytesActuallyRead);
            }
        }

        public static CloudBlockBlob CreateBlob(string containerName, string blobName)
        {
            CloudBlobContainer container = StorageAccount.CreateCloudBlobClient().GetContainerReference(containerName);
            container.CreateIfNotExist();
            return container.GetBlockBlobReference(blobName);
        }

        public static CloudBlob GetBlob(string containerName, string blobName)
        {
            CloudBlobContainer container = StorageAccount.CreateCloudBlobClient().GetContainerReference(containerName);
            return container.GetBlobReference(blobName);
        }

        public static List<string> CopyBlobToBlocks(CloudBlob input, CloudBlockBlob output)
        {
            List<string> blockIDs = new List<string>();
            using (BlobStream inputStream = input.OpenRead())
            {
                byte[] buffer = new byte[32768];

                // Upload input as one or more blocks
                while (inputStream.Position < inputStream.Length)
                {
                    using (MemoryStream blockStream = new MemoryStream())
                    {
                        AzureHelper.CopyStreamUpToLimit(inputStream, blockStream, AzureHelper.MaxBlockSize, buffer);
                        blockStream.Position = 0; // Reset blockStream's position so that it can be read by PutBlock

                        string blockID = Convert.ToBase64String(Guid.NewGuid().ToByteArray());
                        output.PutBlock(blockID, blockStream, null);

                        blockIDs.Add(blockID);
                    }
                }
            }

            return blockIDs;
        }

        public static void CommitBlockBlob(CloudBlockBlob blob, List<string> blockIDs)
        {
            blob.PutBlockList(blockIDs);
            blob.FetchAttributes(); // Refresh the attributes after PutBlockList has cleared them, so that they can be relied on for later calculations
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

        public static TimeSpan Time(Action action)
        {
            DateTime start = DateTime.Now;
            action.Invoke();
            DateTime end = DateTime.Now;
            return end - start;
        }
    }
}
