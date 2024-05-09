/* 
*   Function
*   Copyright © 2024 NatML Inc. All rights reserved.
*/

#nullable enable

namespace Function.Internal {

    using System;
    using System.IO;
    using System.Runtime.CompilerServices;
    using System.Text;
    using Status = Internal.Function.Status;

    /// <summary>
    /// Helpful extension methods.
    /// </summary>
    internal static class FunctionUtils {

        public static Status Throw (this Status status) {
            switch (status) {
                case Status.Ok:                 return status;
                case Status.InvalidArgument:    throw new ArgumentException();
                case Status.InvalidOperation:   throw new InvalidOperationException();
                case Status.NotImplemented:     throw new NotImplementedException();
                default:                        throw new InvalidOperationException();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Stream ToStream (this string data) {
            var buffer = Encoding.UTF8.GetBytes(data);
            return new MemoryStream(buffer);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe Stream ToStream<T> (this T[] data) where T : unmanaged {
            if (data is byte[] raw)
                return new MemoryStream(raw);
            var size = data.Length * sizeof(T);
            var array = new byte[size];
            fixed (void* src = data, dst = array)
                Buffer.MemoryCopy(src, dst, size, size);
            return new MemoryStream(array);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte[] ToArray (this Stream stream) {
            if (stream is MemoryStream memoryStream)
                return memoryStream.ToArray();
            using (var dstStream = new MemoryStream()) {
                stream.CopyTo(dstStream);
                return dstStream.ToArray();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe T[] ToArray<T> (this MemoryStream stream) where T : unmanaged {
            var rawData = stream.ToArray();
            var data = new T[rawData.Length / sizeof(T)];
            Buffer.BlockCopy(rawData, 0, data, 0, rawData.Length);
            return data;
        }
    }

    /// <summary>
    /// Prevent code stripping.
    /// </summary>
    [AttributeUsage(AttributeTargets.All, Inherited = true, AllowMultiple = false)]
    internal sealed class PreserveAttribute : Attribute { }
}