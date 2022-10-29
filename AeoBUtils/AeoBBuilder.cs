using System.Text;
using System.Text.RegularExpressions;

namespace AeoBUtils
{
    internal class AeoBBuilder
    {
        public const uint ACPI_EVAL_OUTPUT_BUFFER_SIGNATURE_V1 = 0x426F6541; // 'AeoB'
        public const ushort ACPI_METHOD_ARGUMENT_INTEGER = 0x0;
        public const ushort ACPI_METHOD_ARGUMENT_STRING = 0x1;
        public const ushort ACPI_METHOD_ARGUMENT_BUFFER = 0x2;
        public const ushort ACPI_METHOD_ARGUMENT_PACKAGE = 0x3;
        public const ushort ACPI_METHOD_ARGUMENT_PACKAGE_EX = 0x4;

        public static byte[] StringToByteArrayFastest(string hex)
        {
            if (hex.Length % 2 == 1)
            {
                throw new Exception("The binary key cannot have an odd number of digits");
            }

            byte[] arr = new byte[hex.Length >> 1];

            for (int i = 0; i < hex.Length >> 1; ++i)
            {
                arr[i] = (byte)((GetHexVal(hex[i << 1]) << 4) + GetHexVal(hex[(i << 1) + 1]));
            }

            return arr;
        }

        public static int GetHexVal(char hex)
        {
            int val = hex;
            //For uppercase A-F letters:
            //return val - (val < 58 ? 48 : 55);
            //For lowercase a-f letters:
            //return val - (val < 58 ? 48 : 87);
            //Or the two combined, but a bit slower:
            return val - (val < 58 ? 48 : (val < 97 ? 55 : 87));
        }

        private static string RemoveComments(string input)
        {
            return Regex.Replace(input, "/\"((?:\\\\\"|[^\"])*)\"|'((?:\\\\'|[^'])*)'|(\\/\\/.*|\\/\\*[\\s\\S]*?\\*\\/)/g", m =>
            {
                return string.Join("",
                    m.Groups.OfType<Group>().Select((g, i) =>
                    {
                        return i switch
                        {
                            2 => "",
                            _ => g.Value,
                        };
                    }));
            });
        }

        private static (int start, int end) GetSubBlock(string[] lines, int curpos)
        {
            int depth = 0;
            int start = curpos + 1;
            int end = start;
            for (; end < lines.Length; end++)
            {
                if (lines[end].Contains("{"))
                {
                    depth++;
                }

                if (lines[end].Contains("}"))
                {
                    depth--;
                }

                if (depth == 0)
                {
                    start++;
                    break;
                }
            }

            return (start, end);
        }

        private static void ParseLines(string[] lines, BinaryWriter bw)
        {
            for (int j = 0; j < lines.Length; j++)
            {
                string? unsanitizedline = lines[j];
                string line = unsanitizedline.TrimStart().TrimEnd().TrimEnd(',');

                if (line.StartsWith("Package ("))
                {
                    bw.Write(ACPI_METHOD_ARGUMENT_PACKAGE);

                    ushort packageLengthPos = (ushort)bw.BaseStream.Length;
                    bw.Write((ushort)0); // Placeholder

                    ushort oldlength = (ushort)bw.BaseStream.Length;

                    // Compute where we end
                    (int start, int end) = GetSubBlock(lines, j);

                    ParseLines(lines[start..end], bw);

                    ushort newlength = (ushort)bw.BaseStream.Length;
                    ushort dataLength = (ushort)(newlength - oldlength);

                    // Write correct length
                    _ = bw.BaseStream.Seek(packageLengthPos, SeekOrigin.Begin);
                    bw.Write(dataLength);
                    _ = bw.BaseStream.Seek(newlength, SeekOrigin.Begin);

                    if (dataLength < 4)
                    {
                        for (int i = 0; i < 4 - dataLength; i++)
                        {
                            bw.Write('\0');
                        }
                    }

                    j = end;
                }
                else if (line.StartsWith("\""))
                {
                    bw.Write(ACPI_METHOD_ARGUMENT_STRING);
                    string strval = line.Split("\"")[1];
                    int dataLength = strval.Length + 1;

                    bw.Write((ushort)dataLength);
                    bw.Write(System.Text.Encoding.ASCII.GetBytes(strval));
                    bw.Write('\0');
                    if (dataLength < 4)
                    {
                        for (int i = 0; i < 4 - dataLength; i++)
                        {
                            bw.Write('\0');
                        }
                    }
                }
                else if (line.StartsWith("0x"))
                {
                    bw.Write(ACPI_METHOD_ARGUMENT_INTEGER);
                    string strval = line.Split("0x")[1];
                    byte[] hex = StringToByteArrayFastest(strval).Reverse().ToArray();
                    int dataLength = strval.Length / 2;
                    bw.Write((ushort)dataLength);
                    bw.Write(hex);
                    if (dataLength < 4)
                    {
                        for (int i = 0; i < 4 - dataLength; i++)
                        {
                            bw.Write('\0');
                        }
                    }
                }
                else if (line.StartsWith("Buffer (0x"))
                {
                    bw.Write(ACPI_METHOD_ARGUMENT_BUFFER);

                    string lengthStr = line.Split("(").Last().Split(")").First().Replace("0x", "");
                    byte[] hex = StringToByteArrayFastest(lengthStr).Reverse().ToArray();
                    ushort dataLength = BitConverter.ToUInt16(hex);
                    bw.Write(dataLength);

                    string hexStr = line.Split("{ ").Last().Replace(" }", "").Replace(", 0x", "").Replace("0x", "");
                    byte[] hex2 = StringToByteArrayFastest(hexStr);
                    bw.Write(hex2);
                    if (dataLength < 4)
                    {
                        for (int i = 0; i < 4 - dataLength; i++)
                        {
                            bw.Write('\0');
                        }
                    }
                }
            }
        }

        public static void BuildAeoBFile(string finalOutput, Stream file)
        {
            string input = RemoveComments(finalOutput);

            using BinaryWriter bw = new(file, Encoding.Default, true);
            string[] lines = input.Split("\n").ToArray();
            bw.Write(ACPI_EVAL_OUTPUT_BUFFER_SIGNATURE_V1);
            long lengthPos = bw.BaseStream.Position;
            bw.Write((uint)0);
            int count = lines.Where(x => x.TrimStart() == x && !x.Contains("{") && !x.Contains("}") && x != "").Count();
            bw.Write((uint)count);

            ParseLines(lines, bw);

            _ = bw.BaseStream.Seek(lengthPos, SeekOrigin.Begin);
            bw.Write((uint)bw.BaseStream.Length);
            _ = bw.BaseStream.Seek(0, SeekOrigin.End);
        }
    }
}
