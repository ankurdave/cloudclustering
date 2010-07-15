using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AzureUtils
{
    [Serializable]
    public class BlobPath
    {
        public string ContainerName { get; set; }
        public string BlobName { get; set; }

        public BlobPath(string containerName, string blobName)
        {
            this.ContainerName = containerName;
            this.BlobName = blobName;
        }
    }
}
