using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Microsoft.WindowsAzure.StorageClient;

namespace AzureUtils
{
    public class ObjectStreamReader<T> : IEnumerable<T>, IDisposable
    {
        private Stream stream;
        private Func<byte[], T> objectDeserializer;
        private long readStart;
        private long readEnd;
        private int objectSize;

        public ObjectStreamReader(CloudBlob blob, Func<byte[], T> objectDeserializer, int objectSize,
            int partitionNumber = 0, int totalPartitions = 1, int subPartitionNumber = 0, int subTotalPartitions = 1)
            : this(blob.OpenRead(), objectDeserializer, objectSize, partitionNumber, totalPartitions, subPartitionNumber, subTotalPartitions)
        {
        }

        public ObjectStreamReader(Stream stream, Func<byte[], T> objectDeserializer, int objectSize,
            int partitionNumber = 0, int totalPartitions = 1, int subPartitionNumber = 0, int subTotalPartitions = 1)
        {
            this.stream = stream;
            this.objectDeserializer = objectDeserializer;
            this.objectSize = objectSize;

            CalculateReadBoundaries(objectSize, partitionNumber, totalPartitions, subPartitionNumber, subTotalPartitions);
        }

        public long Length
        {
            get
            {
                return stream.Length / objectSize;
            }
        }

        private void CalculateReadBoundaries(int objectSize, int partitionNumber, int totalPartitions, int subPartitionNumber, int subTotalPartitions)
        {
            long streamObjectLength = Length;
            long streamObjectReadStart = 0;
            long streamObjectReadEnd = Length;

            long partitionObjectLength = AzureHelper.PartitionLength(streamObjectLength, totalPartitions);
            long partitionObjectReadStart = Math.Min(
                streamObjectReadStart + (partitionNumber * partitionObjectLength),
                streamObjectReadEnd);
            long partitionObjectReadEnd = Math.Min(
                partitionObjectReadStart + partitionObjectLength,
                streamObjectReadEnd);

            long subPartitionObjectLength = AzureHelper.PartitionLength(partitionObjectLength, subTotalPartitions);
            long subPartitionObjectReadStart = Math.Min(
                partitionObjectReadStart + (subPartitionNumber * subPartitionObjectLength),
                partitionObjectReadEnd);
            long subPartitionObjectReadEnd = Math.Min(
                subPartitionObjectReadStart + subPartitionObjectLength,
                partitionObjectReadEnd);

            this.readStart = Math.Min(
                subPartitionObjectReadStart * objectSize,
                stream.Length);
            this.readEnd = Math.Min(
                subPartitionObjectReadEnd * objectSize,
                stream.Length);
        }

        #region IEnumerable code
        public IEnumerator<T> GetEnumerator()
        {
            stream.Position = readStart;
            byte[] bytes = new byte[objectSize];
            while (stream.Position + objectSize <= readEnd)
            {
                stream.Read(bytes, 0, bytes.Length);
                yield return objectDeserializer.Invoke(bytes);
            }
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
        #endregion

        #region IDisposable code
        public void Dispose()
        {
            stream.Close();
        }
        #endregion
    }
}
