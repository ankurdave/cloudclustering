using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace AzureUtils
{
    [Serializable]
    public class ServerResponse : AzureMessage
    {
        public Guid JobID { get; set; }

        /*public ServerResponse(SerializationInfo info, StreamingContext context)
        {
            JobID = (Guid)info.GetValue("JobID", typeof(Guid));
        }

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("JobID", JobID);
        }*/
    }
}
