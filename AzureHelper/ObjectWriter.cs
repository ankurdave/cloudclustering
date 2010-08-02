using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace AzureUtils
{
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
