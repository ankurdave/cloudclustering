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
    public static class AzureHelper : IAzureHelper
    {
        private CloudStorageAccount _storageAccount;
        private CloudStorageAccount StorageAccount {
            get {
                if (_storageAccount == null)
                {
                    CloudStorageAccount.SetConfigurationSettingPublisher((configName, configSetter) =>
                        configSetter(RoleEnvironment.GetConfigurationSettingValue(configName)));
                    _storageAccount = CloudStorageAccount.FromConfigurationSetting("DataConnectionString");
                }

                return _storageAccount;
            }
        }
        
        /// <summary>
        /// The maximum size of a blob block. It's actually 4MB, but this leaves some margin.
        /// </summary>
        private const int BlobBlockSize = 4000000;

        public void EnqueueMessage(string queueName, AzureMessage message)
        {
            CloudQueue queue = StorageAccount.CreateCloudQueueClient().GetQueueReference(queueName);
            queue.CreateIfNotExist();

            queue.AddMessage(new CloudQueueMessage(message.ToBinary()));
        }

        public bool PollForMessage(string queueName, Func<AzureMessage, bool> condition, Func<AzureMessage, bool> action)
        {
            CloudQueue queue = StorageAccount.CreateCloudQueueClient().GetQueueReference(queueName);
            queue.CreateIfNotExist();

            CloudQueueMessage queueMessage = queue.GetMessage();
            AzureMessage message = AzureMessageFactory.CreateMessage(queueName, queueMessage);  

            if (!condition.Invoke(message))
                return false;

            if (!action.Invoke(message))
                return false;

            queue.DeleteMessage(queueMessage);

            return true;
        }

        public void WaitForMessage(string queueName, Func<AzureMessage, bool> condition, Func<AzureMessage, bool> action, int delayMilliseconds = 1000, int iterationLimit = 0)
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

        public void CreateBlobContainer(string containerName)
        {
            CloudBlobClient client = StorageAccount.CreateCloudBlobClient();
            CloudBlobContainer container = client.GetContainerReference(containerName);
            container.CreateIfNotExist();
        }
    }
}
