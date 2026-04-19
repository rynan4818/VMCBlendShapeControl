using System;
using System.Collections.Generic;
using System.Text;

namespace VMCBlendShapeControl.Models
{
    internal static class VmcOscMessageUtility
    {
        public static byte[] BuildMessage(string address, params object[] args)
        {
            if (string.IsNullOrEmpty(address))
            {
                throw new ArgumentException("OSC address is required.", nameof(address));
            }

            var buffer = new List<byte>(128);
            WriteOscString(buffer, address);

            var typeTag = new StringBuilder(",");
            for (var i = 0; i < args.Length; i++)
            {
                if (args[i] is int)
                {
                    typeTag.Append('i');
                }
                else if (args[i] is float)
                {
                    typeTag.Append('f');
                }
                else if (args[i] is string)
                {
                    typeTag.Append('s');
                }
                else
                {
                    throw new NotSupportedException($@"Unsupported OSC argument type: {args[i]?.GetType().FullName ?? "null"}");
                }
            }

            WriteOscString(buffer, typeTag.ToString());

            for (var i = 0; i < args.Length; i++)
            {
                if (args[i] is int i32)
                {
                    WriteInt32BigEndian(buffer, i32);
                }
                else if (args[i] is float f32)
                {
                    WriteFloatBigEndian(buffer, f32);
                }
                else if (args[i] is string s)
                {
                    WriteOscString(buffer, s);
                }
            }

            return buffer.ToArray();
        }

        private static void WriteOscString(List<byte> buffer, string text)
        {
            var bytes = Encoding.UTF8.GetBytes(text ?? string.Empty);
            buffer.AddRange(bytes);
            buffer.Add(0);
            while (buffer.Count % 4 != 0)
            {
                buffer.Add(0);
            }
        }

        private static void WriteInt32BigEndian(List<byte> buffer, int value)
        {
            var bytes = BitConverter.GetBytes(value);
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(bytes);
            }

            buffer.AddRange(bytes);
        }

        private static void WriteFloatBigEndian(List<byte> buffer, float value)
        {
            var bytes = BitConverter.GetBytes(value);
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(bytes);
            }

            buffer.AddRange(bytes);
        }
    }
}
