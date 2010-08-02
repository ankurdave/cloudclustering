using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Microsoft.WindowsAzure.StorageClient;

namespace AzureUtils
{
    public class ObjectStreamWriter<T> : ObjectWriter<T>, IDisposable
    {
        private Stream stream;

        public ObjectStreamWriter(CloudBlob blob, Func<T, byte[]> objectSerializer, int objectSize)
            : base(objectSerializer, objectSize)
        {
            this.stream = blob.OpenWrite();
        }

        public ObjectStreamWriter(Stream stream, Func<T, byte[]> objectSerializer, int objectSize)
            : base(objectSerializer, objectSize)
        {
            this.stream = stream;
        }

        public override void Write(T obj)
        {
            byte[] bytes = objectSerializer.Invoke(obj);
            stream.Write(bytes, 0, bytes.Length);
        }

        #region IDisposable code
        public void Dispose()
        {
            stream.Close();
        }
        #endregion
    }
}
