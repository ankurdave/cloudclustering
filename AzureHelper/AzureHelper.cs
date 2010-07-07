using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.WindowsAzure.StorageClient;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.ServiceRuntime;

namespace AzureUtils
{
    public static class AzureHelper
    {
        private static CloudStorageAccount _storageAccount;
        public static CloudStorageAccount StorageAccount {
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

        public static void EnqueueMessage(string queueName, AzureMessage message)
        {
            CloudQueue queue = StorageAccount.CreateCloudQueueClient().GetQueueReference(queueName);
            queue.CreateIfNotExist();

            queue.AddMessage(new CloudQueueMessage(message.ToBinary()));
        }

        public static void PollForMessage(string queueName, Func<AzureMessage, bool> condition, Func<AzureMessage, bool> action)
        {
            CloudQueue queue = StorageAccount.CreateCloudQueueClient().GetQueueReference(queueName);
            queue.CreateIfNotExist();

            CloudQueueMessage queueMessage = queue.GetMessage();
            AzureMessage message = AzureMessageFactory.CreateMessage(queueName, queueMessage);

            if (!condition.Invoke(message))
                return;

            if (!action.Invoke(message))
                return;

            queue.DeleteMessage(queueMessage);
        }
    }
}
