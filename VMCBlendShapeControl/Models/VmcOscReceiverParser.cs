using System;
using System.Collections.Generic;
using System.Text;

namespace VMCBlendShapeControl.Models
{
    internal sealed class VmcOscReceiverParser
    {
        public void ParsePacket(byte[] data, int length, Action<string, List<object>> onMessage)
        {
            if (data == null || length <= 0 || onMessage == null)
            {
                return;
            }

            ParseElement(data, 0, length, onMessage);
        }

        private static void ParseElement(byte[] data, int offset, int count, Action<string, List<object>> onMessage)
        {
            var end = offset + count;
            var cursor = offset;
            var address = ReadPaddedString(data, ref cursor, end);
            if (string.IsNullOrEmpty(address))
            {
                return;
            }

            if (address == "#bundle")
            {
                if (cursor + 8 > end)
                {
                    return;
                }

                cursor += 8; // timetag
                while (cursor + 4 <= end)
                {
                    var elementSize = ReadInt32BE(data, ref cursor, end);
                    if (elementSize <= 0 || cursor + elementSize > end)
                    {
                        break;
                    }

                    ParseElement(data, cursor, elementSize, onMessage);
                    cursor += elementSize;
                }

                return;
            }

            var typeTag = ReadPaddedString(data, ref cursor, end);
            if (string.IsNullOrEmpty(typeTag) || typeTag[0] != ',')
            {
                return;
            }

            var args = new List<object>();
            for (var i = 1; i < typeTag.Length; i++)
            {
                var tag = typeTag[i];
                switch (tag)
                {
                    case 's':
                        args.Add(ReadPaddedString(data, ref cursor, end));
                        break;
                    case 'f':
                        args.Add(ReadFloatBE(data, ref cursor, end));
                        break;
                    case 'i':
                        args.Add(ReadInt32BE(data, ref cursor, end));
                        break;
                    case 'T':
                        args.Add(true);
                        break;
                    case 'F':
                        args.Add(false);
                        break;
                    default:
                        cursor = end;
                        break;
                }

                if (cursor > end)
                {
                    return;
                }
            }

            onMessage(address, args);
        }

        private static string ReadPaddedString(byte[] data, ref int cursor, int end)
        {
            if (cursor >= end)
            {
                return string.Empty;
            }

            var start = cursor;
            while (cursor < end && data[cursor] != 0)
            {
                cursor++;
            }

            if (cursor >= end)
            {
                return string.Empty;
            }

            var text = Encoding.UTF8.GetString(data, start, cursor - start);

            cursor++; // consume null
            while (cursor % 4 != 0 && cursor < end)
            {
                cursor++;
            }

            return text;
        }

        private static int ReadInt32BE(byte[] data, ref int cursor, int end)
        {
            if (cursor + 4 > end)
            {
                cursor = end + 1;
                return 0;
            }

            var bytes = new byte[4];
            Buffer.BlockCopy(data, cursor, bytes, 0, 4);
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(bytes);
            }

            cursor += 4;
            return BitConverter.ToInt32(bytes, 0);
        }

        private static float ReadFloatBE(byte[] data, ref int cursor, int end)
        {
            if (cursor + 4 > end)
            {
                cursor = end + 1;
                return 0f;
            }

            var bytes = new byte[4];
            Buffer.BlockCopy(data, cursor, bytes, 0, 4);
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(bytes);
            }

            cursor += 4;
            return BitConverter.ToSingle(bytes, 0);
        }
    }
}
