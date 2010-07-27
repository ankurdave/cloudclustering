using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.WindowsAzure.StorageClient;
using System.IO;

namespace AzureUtils
{
    public class PointStream<T> : IEnumerable<T>, IDisposable where T : Point
    {
        private Stream stream;
        private Func<byte[], T> pointDeserializer;
        private int pointSize;
        private long startByte;
        private long endByte;

        public PointStream(CloudBlob pointBlob, Func<byte[], T> pointDeserializer, int pointSize, int partitionNumber, int totalPartitions)
            : this(pointBlob, pointDeserializer, pointSize)
        {
            long partitionLength = PartitionLength(Length, totalPartitions);
            this.startByte = partitionNumber * partitionLength;
            this.endByte = Math.Min(startByte + partitionLength, stream.Length);
        }

        public PointStream(CloudBlob pointBlob, Func<byte[], T> pointDeserializer, int pointSize, bool read = true)
            : this(read ? pointBlob.OpenRead() : pointBlob.OpenWrite(), pointDeserializer, pointSize)
        {
        }

        public PointStream(Stream stream, Func<byte[], T> pointDeserializer, int pointSize)
        {
            this.stream = stream;
            this.pointDeserializer = pointDeserializer;
            this.pointSize = pointSize;

            this.startByte = 0;
            this.endByte = stream.Length;
        }

        public IEnumerator<T> GetEnumerator()
        {
            if (!stream.CanRead)
            {
                throw new NotSupportedException();
            }

            stream.Position = startByte;
            byte[] bytes = new byte[pointSize];
            while (stream.Position + pointSize <= endByte)
            {
                stream.Read(bytes, 0, bytes.Length);
                yield return pointDeserializer.Invoke(bytes);
            }
        }

        public void Write(T point)
        {
            if (!stream.CanWrite)
            {
                throw new NotSupportedException();
            }

            byte[] bytes = point.ToByteArray();
            stream.Write(bytes, 0, bytes.Length);
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
        
        public void Dispose()
        {
            stream.Close();
        }

        public void CopyPartition(int partitionNumber, int totalPartitions, Stream output)
        {
            // Calculate what portion of points to read
            long numPoints = Length;
            long partitionLength = PartitionLength(numPoints, totalPartitions);
            long startByte = partitionNumber * partitionLength;

            // Read the entire partition from stream
            byte[] partition = new byte[partitionLength];
            stream.Position = startByte;
            int actualLength = stream.Read(partition, 0, partition.Length);

            // Write it to output
            output.Write(partition, 0, actualLength);
        }

        private long PartitionLength(long numPoints, int numPartitions)
        {
            return (long)Math.Ceiling((double)numPoints / numPartitions) * pointSize;
        }

        public long Length
        {
            get
            {
                return stream.Length / pointSize;
            }
        }
    }
}
