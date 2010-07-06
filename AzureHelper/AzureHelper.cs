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
            //throw new NotImplementedException();
            System.Diagnostics.Debug.WriteLine("Enqueueing message " + message + " to queue " + queueName);
        }

        public static void PollForMessage(string queueName, Func<IAzureMessage, bool> condition, Func<IAzureMessage, bool> action)
        {
            //throw new NotImplementedException();
            System.Diagnostics.Debug.WriteLine("Polling for message on queue " + queueName);

            IAzureMessage message = new ServerResponse { JobID = new Guid() };

            System.Diagnostics.Debug.WriteLine("Message satisfies condition? " + (condition.Invoke(message) ? "yes" : "no"));

            System.Diagnostics.Debug.WriteLine("Running action...");
            action.Invoke(message);
        }
    }
}
