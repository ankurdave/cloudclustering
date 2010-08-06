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
        protected Stream stream;
        protected Func<byte[], T> objectDeserializer;
        protected long readStart;
        protected long readEnd;
        protected int objectSize;

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

            Range<long> readRange = CalculateReadBoundaries(stream.Length, objectSize, partitionNumber, totalPartitions, subPartitionNumber, subTotalPartitions);
            this.readStart = readRange.Start;
            this.readEnd = readRange.End;
        }

        public long Length
        {
            get
            {
                return stream.Length / objectSize;
            }
        }

        private static Range<long> CalculateReadBoundaries(long streamLength, int objectSize, int partitionNumber, int totalPartitions, int subPartitionNumber, int subTotalPartitions)
        {
            long streamObjectLength = streamLength / objectSize;
            long streamObjectReadStart = 0;
            long streamObjectReadEnd = streamLength / objectSize;

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

            long start = Math.Min(
                subPartitionObjectReadStart * objectSize,
                streamLength);
            long end = Math.Min(
                subPartitionObjectReadEnd * objectSize,
                streamLength);
            
            var range = new Range<long>(start, end);
            return range;
        }

        #region IEnumerable code
        public virtual IEnumerator<T> GetEnumerator()
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
