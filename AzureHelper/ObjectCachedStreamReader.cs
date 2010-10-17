using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.WindowsAzure.StorageClient;
using System.IO;

namespace AzureUtils
{
    /// <summary>
    /// Wraps around a stream and optionally a cache file, making it possible to read deserialized objects from it using the IEnumerable interface.
    /// </summary>
    public class ObjectCachedStreamReader<T> : ObjectStreamReader<T>
    {
        public string CacheFilePath { get; private set; }
        public bool UsingCache { get; private set; }

        public ObjectCachedStreamReader(CloudBlob blob, Func<byte[], T> objectDeserializer, int objectSize,
            string cacheDirectory, string cachePrefix, int partitionNumber = 0, int totalPartitions = 1, int subPartitionNumber = 0, int subTotalPartitions = 1, int iterationNumber = 0)
            : this(blob.OpenRead(), objectDeserializer, objectSize, cacheDirectory, cachePrefix, partitionNumber, totalPartitions, subPartitionNumber, subTotalPartitions, iterationNumber)
        {
        }

        public ObjectCachedStreamReader(Stream stream, Func<byte[], T> objectDeserializer, int objectSize,
            string cacheDirectory, string cachePrefix, int partitionNumber = 0, int totalPartitions = 1, int subPartitionNumber = 0, int subTotalPartitions = 1, int iterationNumber = 0)
            : base(UseStreamOrCachedFile(stream, AzureHelper.GetCachedFilePath(cacheDirectory, cachePrefix, partitionNumber, totalPartitions, subPartitionNumber, iterationNumber)), objectDeserializer, objectSize, partitionNumber, totalPartitions, subPartitionNumber, subTotalPartitions)
        {
            this.CacheFilePath = AzureHelper.GetCachedFilePath(cacheDirectory, cachePrefix, partitionNumber, totalPartitions, subPartitionNumber, iterationNumber);
            this.UsingCache = File.Exists(this.CacheFilePath);

            // If we're using the cache, we should read the entire cached file, because that is the entirety of the desired partition
            if (UsingCache)
            {
                readStart = 0;
                readEnd = new FileInfo(CacheFilePath).Length;
            }
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

            long cacheFileLength = new FileInfo(CacheFilePath).Length;
            if (cacheFileLength != readEnd - readStart)
                throw new Exception();
        }
    }
}
