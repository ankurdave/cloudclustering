using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AzureUtils
{
    static class AzureMessageFactory
    {
        private static Dictionary<string, Func<string, IAzureMessage>> queueMessageGenerator = new Dictionary<string, Func<string, IAzureMessage>> {
            { "serverResponses", messageString => new ServerResponse(messageString) }
        };
    }
}
