using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.WindowsAzure.StorageClient;
using System.IO;

namespace AzureUtils
{
    public class PointStream<T> : IEnumerable<T>, IDisposable
    {
        private Stream stream;
        private Func<byte[], T> pointDeserializer;
        private int pointSize;

        public PointStream(CloudBlob pointBlob, Func<byte[], T> pointDeserializer, int pointSize)
        {
            this.stream = pointBlob.OpenRead();
            this.pointDeserializer = pointDeserializer;
            this.pointSize = pointSize;
        }

        public PointStream(Stream stream, Func<byte[], T> pointDeserializer, int pointSize)
        {
            this.stream = stream;
            this.pointDeserializer = pointDeserializer;
            this.pointSize = pointSize;
        }

        public IEnumerator<T> GetEnumerator()
        {
            stream.Position = 0;
            byte[] bytes = new byte[pointSize];
            while (stream.Position + pointSize <= stream.Length)
            {
                int bytesRead = stream.Read(bytes, 0, bytes.Length);
                yield return pointDeserializer.Invoke(bytes);
            }
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
        
        public void Dispose()
        {
            stream.Close();
        }
    }
}
