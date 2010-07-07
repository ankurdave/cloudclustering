using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AzureUtils
{
    static class AzureMessageFactory
    {
        public static AzureMessage CreateMessage(string queueName, Microsoft.WindowsAzure.StorageClient.CloudQueueMessage queueMessage)
        {
            // Switch on the queue name to figure out what subclass of AzureMessage to instantiate
            // (Ugly, but I don't know any other way apart from reflection, which seems like overkill)
            switch (queueName)
            {
                case "serverrequests":
                    return AzureMessage.FromMessage<KMeansJobData>(queueMessage);
                case "serverresponses":
                    return AzureMessage.FromMessage<KMeansJobResult>(queueMessage);
                default:
                    return null;
            }
        }
    }
}
