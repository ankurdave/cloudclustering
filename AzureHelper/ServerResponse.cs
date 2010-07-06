using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AzureUtils
{
    public class ServerResponse : IAzureMessage
    {
        public Guid JobID { get; set; }

        public ServerResponse()
        {

        }

        public ServerResponse(string messageString)
        {
            throw new NotImplementedException();
        }
    }
}
