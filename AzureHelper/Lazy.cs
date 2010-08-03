using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AzureUtils
{
    /// <summary>
    /// Represents a lazily-evaluated value that can be passed as an argument to a method.
    /// See http://stackoverflow.com/questions/414176/c-lamda-expressions-and-lazy-evaluation
    /// </summary>
    public class Lazy<T>
    {
        private readonly T value;
        private readonly Func<T> func;

        public Lazy(T value) { this.value = value; }
        public Lazy(Func<T> func) { this.func = func; }

        public static implicit operator Lazy<T>(T value)
        {
            return new Lazy<T>(value);
        }

        public static implicit operator Lazy<T>(Func<T> func)
        {
            return new Lazy<T>(func);
        }

        public bool IsLazy
        {
            get
            {
                return this.func != null;
            }
        }

        public T Eval()
        {
            return this.func != null ? this.func() : this.value;
        }
    }
}
