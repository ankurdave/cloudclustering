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

        public static void EnqueueMessage(string queueName, AzureMessage message)
        {
            CloudQueue queue = StorageAccount.CreateCloudQueueClient().GetQueueReference(queueName);
            queue.CreateIfNotExist();

            queue.AddMessage(new CloudQueueMessage(message.ToBinary()));
        }

        public static bool PollForMessage(string queueName, Func<AzureMessage, bool> condition, Func<AzureMessage, bool> action)
        {
            CloudQueue queue = StorageAccount.CreateCloudQueueClient().GetQueueReference(queueName);

            CloudQueueMessage queueMessage;
            try
            {
                queue.CreateIfNotExist();
                queueMessage = queue.GetMessage();
            }
            catch (StorageServerException e)
            {
                return false;
            }

            if (queueMessage == null)
                return false;

            AzureMessage message = CreateAzureMessage(queueName, queueMessage);

            if (!condition.Invoke(message))
                return false;

            if (!action.Invoke(message))
                return false;

            queue.DeleteMessage(queueMessage);

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

        public static void WaitForMessage(string queueName, Func<AzureMessage, bool> condition, Func<AzureMessage, bool> action, int delayMilliseconds = 500, int iterationLimit = 0)
        {
            for (int i = 0; iterationLimit == 0 || i < iterationLimit; i++)
            {
                if (PollForMessage(queueName, condition, action))
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

        public static void CopyStreamUpToLimit(Stream input, Stream output, int maxBytesToCopy, byte[] copyBuffer)
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
    }
}
