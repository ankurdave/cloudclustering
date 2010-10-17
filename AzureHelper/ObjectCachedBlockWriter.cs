using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.WindowsAzure.StorageClient;
using System.IO;

namespace AzureUtils
{
    /// <summary>
    /// Serializes objects and writes them to two sources: an Azure block blob, and a cache file on disk.
    /// </summary>
    public class ObjectCachedBlockWriter<T> : ObjectWriter<T>, IDisposable
    {
        private CloudBlockBlob blob;
        private List<string> _blockList = new List<string>();
        private MemoryStream blockStream = new MemoryStream();
        private Stream cacheStream;

        public ObjectCachedBlockWriter(CloudBlockBlob blob, Func<T, byte[]> objectSerializer, int objectSize, string cacheFilePath)
            : base(objectSerializer, objectSize)
        {
            this.blob = blob;
            this.cacheStream = File.OpenWrite(cacheFilePath);
        }

        /// <summary>
        /// Serializes the given object and writes it to the block blob and the cache file.
        /// </summary>
        /// <param name="obj"></param>
        public override void Write(T obj)
        {
            if (blockStream.Length + objectSize >= AzureHelper.MaxBlockSize)
            {
                FlushBlock();
            }

            byte[] bytes = objectSerializer.Invoke(obj);
            blockStream.Write(bytes, 0, bytes.Length);
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

        /// <summary>
        /// The list of blocks that have been written to Azure storage.
        /// </summary>
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
            cacheStream.Dispose();
        }
        #endregion
    }
}
