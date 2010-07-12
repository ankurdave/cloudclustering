using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.WindowsAzure.StorageClient;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.ServiceRuntime;
using System.Threading;

namespace AzureUtils
{
    public static class AzureHelper
    {
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

        public static void EnqueueMessage(string queueName, AzureMessage message)
        {
            CloudQueue queue = StorageAccount.CreateCloudQueueClient().GetQueueReference(queueName);
            queue.CreateIfNotExist();

            queue.AddMessage(new CloudQueueMessage(message.ToBinary()));
        }

        public static bool PollForMessage(string queueName, Func<AzureMessage, bool> condition, Func<AzureMessage, bool> action)
        {
            CloudQueue queue = StorageAccount.CreateCloudQueueClient().GetQueueReference(queueName);
            queue.CreateIfNotExist();

            CloudQueueMessage queueMessage = queue.GetMessage();
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
                case "serverrequest":
                    return KMeansJobData.FromMessage<KMeansJobData>(queueMessage);
                case "serverresponse":
                    return KMeansJobResult.FromMessage<KMeansJobResult>(queueMessage);
                case "workerrequest":
                    return KMeansTask.FromMessage<KMeansTask>(queueMessage);
                case "workerresponse":
                    return KMeansTaskResult.FromMessage<KMeansTaskResult>(queueMessage);
                default:
                    throw new InvalidOperationException();
            }
        }

        public static void WaitForMessage(string queueName, Func<AzureMessage, bool> condition, Func<AzureMessage, bool> action, int delayMilliseconds = 1000, int iterationLimit = 0)
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
    }
}
