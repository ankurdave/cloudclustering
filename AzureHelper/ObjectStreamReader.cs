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
            int partitionNumber = 0, int totalPartitions = 1)
            : this(blob.OpenRead(), objectDeserializer, objectSize, partitionNumber, totalPartitions)
        {
        }

        public ObjectStreamReader(Stream stream, Func<byte[], T> objectDeserializer, int objectSize,
            int partitionNumber = 0, int totalPartitions = 1)
        {
            this.stream = stream;
            this.objectDeserializer = objectDeserializer;
            this.objectSize = objectSize;

            long partitionLength = AzureHelper.PartitionLength((int)Length, totalPartitions) * objectSize;
            this.readStart = partitionNumber * partitionLength;
            this.readEnd = Math.Min(readStart + partitionLength, stream.Length);
        }

        public long Length
        {
            get
            {
                return stream.Length / objectSize;
            }
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
