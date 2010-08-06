using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.WindowsAzure.StorageClient;
using System.IO;

namespace AzureUtils
{
    public class ObjectBlockWriter<T> : ObjectWriter<T>
    {
        private CloudBlockBlob blob;
        private List<string> _blockList = new List<string>();
        private MemoryStream blockStream = new MemoryStream();

        public ObjectBlockWriter(CloudBlockBlob blob, Func<T, byte[]> objectSerializer, int objectSize)
            : base(objectSerializer, objectSize)
        {
            this.blob = blob;
        }

        public override void Write(T obj)
        {
            if (blockStream.Length + objectSize >= AzureHelper.MaxBlockSize)
            {
                FlushBlock();
            }

            byte[] bytes = objectSerializer.Invoke(obj);
            blockStream.Write(bytes, 0, bytes.Length);
        }

        /// <summary>
        /// Flushes the current block to Azure storage and adds the new Block ID to BlockList.
        /// </summary>
        public void FlushBlock()
        {
            if (blockStream.Length == 0)
                return;

            string blockID = AzureHelper.GenerateRandomBlockID();

            blockStream.Position = 0;
            blob.PutBlock(blockID, blockStream, null);
            _blockList.Add(blockID);
            
            blockStream.Close();
            blockStream = new MemoryStream();
        }

        public List<string> BlockList
        {
            get
            {
                return _blockList;
            }
        }
    }
}
