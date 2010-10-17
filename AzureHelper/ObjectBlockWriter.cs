using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.WindowsAzure.StorageClient;
using System.IO;

namespace AzureUtils
{
    public class ObjectCachedBlockWriter<T> : ObjectWriter<T>, IDisposable
    {
        private CloudBlockBlob blob;
        private List<string> _blockList = new List<string>();
        private MemoryStream blockStream = new MemoryStream();
        
        private string cacheFilePath;
        private Stream cacheStream;
        private bool usingCache;

        public ObjectCachedBlockWriter(CloudBlockBlob blob, Func<T, byte[]> objectSerializer, int objectSize,
            string cacheDirectory, string cachePrefix, int partitionNumber = 0, int totalPartitions = 1, int subPartitionNumber = 0, int subTotalPartitions = 1)
            : base(objectSerializer, objectSize)
        {
            this.blob = blob;
            
            this.cacheFilePath = AzureHelper.GetCachedFilePath(cacheDirectory, cachePrefix, partitionNumber, totalPartitions, subPartitionNumber);
            this.usingCache = File.Exists(cacheFilePath);
            if (usingCache)
                this.cacheStream = File.OpenWrite(cacheFilePath);
        }

        public override void Write(T obj)
        {
            if (blockStream.Length + objectSize >= AzureHelper.MaxBlockSize)
            {
                FlushBlock();
            }

            byte[] bytes = objectSerializer.Invoke(obj);
            blockStream.Write(bytes, 0, bytes.Length);
            if (usingCache)
                cacheStream.Write(bytes, 0, bytes.Length);
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

        #region IDisposable code
        public void Dispose()
        {
            if (usingCache)
                cacheStream.Dispose();
        }
        #endregion
    }
}
