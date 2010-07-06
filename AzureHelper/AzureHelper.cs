using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AzureUtils
{
    public static class AzureHelper
    {
        public static void EnqueueMessage(string queueName, IAzureMessage message)
        {
            throw new NotImplementedException();
        }

        public static void PollForMessage(string queueName, Func<IAzureMessage, bool> condition, Func<IAzureMessage, bool> action)
        {
            throw new NotImplementedException();
        }
    }
}
