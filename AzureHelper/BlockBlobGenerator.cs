using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.WindowsAzure.StorageClient;
using System.IO;

namespace AzureUtils
{
    /// <summary>
    /// Assists in generating block blobs by automatically paginating data into blocks and then committing the blocks into a blob.
    /// </summary>
    public class BlockBlobGenerator
    {
        private CloudBlockBlob blob;
        private int numDataChunks;
        public Func<int, byte[]> DataGenerator { get; set; }
        
        public BlockBlobGenerator(CloudBlockBlob blob, int numDataChunks)
        {
            this.blob = blob;
            this.numDataChunks = numDataChunks;
        }

        /// <summary>
        /// Repeatedly calls DataGenerator to generate the data, and paginates it into blocks.
        /// Note that if one call to DataGenerator yields more data than the maximum block size, this will not work.
        /// TODO: Look into using BlobStream.Write instead, if it auto-paginates data.
        /// </summary>
        public void Run()
        {
            List<string> blockList = new List<string>();
            MemoryStream block = new MemoryStream();
            
            for (int i = 0; i < numDataChunks; i++)
            {
                // Get the data
                byte[] data = DataGenerator.Invoke(i);

                // If necessary, flush the current block and start a new one
                if (block.Length + data.Length > AzureHelper.BlobBlockSize)
                {
                    Guid blockID = Guid.NewGuid();
                    block.Position = 0;
                    blob.PutBlock(blockID.ToString(), block, null);
                    blockList.Add(blockID.ToString());

                    block = new MemoryStream();
                }

                // Add the data to the block
                block.Write(data, 0, data.Length);
            }

            blob.PutBlockList(blockList);
        }
    }
}
