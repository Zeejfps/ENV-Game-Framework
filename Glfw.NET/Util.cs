using System;
using System.Runtime.InteropServices;
using System.Text;
using JetBrains.Annotations;

namespace GLFW
{
    internal static class Util
    {
        #region Methods

        /// <summary>
        ///     Reads memory from the pointer until the first null byte is encountered and decodes the bytes from UTF-8 into a
        ///     managed <see cref="string" />.
        /// </summary>
        /// <param name="ptr">Pointer to the start of the string.</param>
        /// <returns>Managed string created from read UTF-8 bytes.</returns>
        [NotNull]
        // ReSharper disable once InconsistentNaming
        public static string PtrToStringUTF8(IntPtr ptr)
        {
            if (ptr != IntPtr.Zero)
            {
                var length = 0;
                while (Marshal.ReadByte(ptr, length) != 0)
                    length++;
                var buffer = new byte[length];
                Marshal.Copy(ptr, buffer, 0, length);
                return Encoding.UTF8.GetString(buffer);
            }

            return "";
        }

        /// <summary>
        ///     Encodes a managed <see cref="string" /> as UTF-8 with the trailing null byte that the C
        ///     <c>const char*</c> parameters these arrays are marshalled to require.
        /// </summary>
        /// <param name="str">The string to encode.</param>
        /// <returns>Null-terminated UTF-8 bytes.</returns>
        // ReSharper disable once InconsistentNaming
        public static byte[] StringToUTF8Z([CanBeNull] string str)
        {
            var count = Encoding.UTF8.GetByteCount(str ?? "");
            var buffer = new byte[count + 1];
            Encoding.UTF8.GetBytes(str ?? "", 0, (str ?? "").Length, buffer, 0);
            return buffer;
        }

        /// <summary>
        ///     Encodes a managed <see cref="string" /> as ASCII with the trailing null byte that the C
        ///     <c>const char*</c> parameters these arrays are marshalled to require.
        /// </summary>
        /// <param name="str">The string to encode.</param>
        /// <returns>Null-terminated ASCII bytes.</returns>
        public static byte[] StringToAsciiZ([CanBeNull] string str)
        {
            var count = Encoding.ASCII.GetByteCount(str ?? "");
            var buffer = new byte[count + 1];
            Encoding.ASCII.GetBytes(str ?? "", 0, (str ?? "").Length, buffer, 0);
            return buffer;
        }

        #endregion
    }
}