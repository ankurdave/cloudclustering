using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.WindowsAzure.StorageClient;
using System.IO;

namespace AzureUtils
{
    public class ObjectCachedStreamReader<T> : ObjectStreamReader<T>
    {
        public string CacheFilePath { get; private set; }
        public bool UsingCache { get; private set; }

        public ObjectCachedStreamReader(CloudBlob blob, Func<byte[], T> objectDeserializer, int objectSize,
            string cacheDirectory, string cachePrefix, int partitionNumber = 0, int totalPartitions = 1, int subPartitionNumber = 0, int subTotalPartitions = 1)
            : this(blob.OpenRead(), objectDeserializer, objectSize, cacheDirectory, cachePrefix, partitionNumber, totalPartitions, subPartitionNumber, subTotalPartitions)
        {
        }

        public ObjectCachedStreamReader(Stream stream, Func<byte[], T> objectDeserializer, int objectSize,
            string cacheDirectory, string cachePrefix, int partitionNumber = 0, int totalPartitions = 1, int subPartitionNumber = 0, int subTotalPartitions = 1)
            : base(UseStreamOrCachedFile(stream, GetCachedFilePath(cacheDirectory, cachePrefix, partitionNumber, totalPartitions, subPartitionNumber)), objectDeserializer, objectSize, partitionNumber, totalPartitions, subPartitionNumber, subTotalPartitions)
        {
            this.CacheFilePath = GetCachedFilePath(cacheDirectory, cachePrefix, partitionNumber, totalPartitions, subPartitionNumber);
            this.UsingCache = File.Exists(this.CacheFilePath);
        }

        private static string GetCachedFilePath(string cacheDirectory, string cachePrefix, int partitionNumber, int totalPartitions, int subPartitionNumber)
        {
            return string.Format(@"{4}\{0}-{1}-{2}-{3}", cachePrefix, totalPartitions, partitionNumber, subPartitionNumber, cacheDirectory);
        }

        private static Stream UseStreamOrCachedFile(Stream stream, string path)
        {
            if (File.Exists(path))
            {
                return File.OpenRead(path);
            }
            else
            {
                return stream;
            }
        }

        public override IEnumerator<T> GetEnumerator()
        {
            stream.Position = readStart;
            byte[] bytes = new byte[objectSize];

            FileStream cacheWriteStream = null;
            if (!UsingCache)
            {
                cacheWriteStream = File.OpenWrite(CacheFilePath);
            }

            while (stream.Position + objectSize <= readEnd)
            {
                stream.Read(bytes, 0, bytes.Length);

                if (!UsingCache)
                {
                    cacheWriteStream.Write(bytes, 0, bytes.Length);
                }

                yield return objectDeserializer.Invoke(bytes);
            }

            if (!UsingCache)
            {
                cacheWriteStream.Dispose();
            }
        }
    }
}
