using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using Microsoft.WindowsAzure.StorageClient;

namespace AzureUtils
{
    [Serializable]
    public abstract class AzureMessage
    {
        // See Figure 2 in http://msdn.microsoft.com/en-us/magazine/ee335721.aspx
        public byte[] ToBinary()
        {
            BinaryFormatter bf = new BinaryFormatter();
            MemoryStream ms = new MemoryStream();
            ms.Position = 0;
            bf.Serialize(ms, this);
            byte[] messageSerialized = ms.GetBuffer();
            ms.Close();
            return messageSerialized;
        }

        // See Figure 2 in http://msdn.microsoft.com/en-us/magazine/ee335721.aspx
        public static AzureMessage FromMessage(CloudQueueMessage message)
        {
            byte[] buffer = message.AsBytes;
            MemoryStream ms = new MemoryStream(buffer);
            ms.Position = 0;
            BinaryFormatter bf = new BinaryFormatter();
            return (AzureMessage)bf.Deserialize(ms);
        }
    }
}
