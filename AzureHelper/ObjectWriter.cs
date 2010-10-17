using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace AzureUtils
{
    /// <summary>
    /// Represents a writable stream that writes serializable objects.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class ObjectWriter<T>
    {
        protected Func<T, byte[]> objectSerializer;
        protected int objectSize;

        public ObjectWriter(Func<T, byte[]> objectSerializer, int objectSize)
        {
            this.objectSerializer = objectSerializer;
            this.objectSize = objectSize;
        }

        public abstract void Write(T obj);
    }
}
