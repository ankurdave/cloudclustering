using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AzureUtils
{
    public interface IAzureHelper
    {
        void EnqueueMessage(string queueName, AzureMessage message);
        bool PollForMessage(string queueName, Func<AzureMessage, bool> condition, Func<AzureMessage, bool> action);
        void WaitForMessage(string queueName, Func<AzureMessage, bool> condition, Func<AzureMessage, bool> action);
        void CreateBlobContainer(string containerName);
    }
}
