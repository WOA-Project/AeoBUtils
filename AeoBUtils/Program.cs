using CommandLine;
using System.Reflection;

namespace AeoBUtils
{
    [Verb("aeob2aeobsl", HelpText = "Convert AeoB to AeoBsl")]
    public class AeoB2AeoBslOptions
    {
        [Option('p', "path", HelpText = "Path to AeoB", Required = true)]
        public string? path { get; set; }

        [Option('o', "output", HelpText = "Path to output AeoBsl", Required = true)]
        public string? output { get; set; }
    }

    [Verb("aeobsl2aeob", HelpText = "Convert AeoBslto AeoB")]
    public class AeoBsl2AeoBOptions
    {
        [Option('p', "path", HelpText = "Path to AeoBsl", Required = true)]
        public string? path { get; set; }

        [Option('o', "output", HelpText = "Path to output AeoB", Required = true)]
        public string? output { get; set; }
    }

    internal class Program
    {
        public const uint ACPI_EVAL_OUTPUT_BUFFER_SIGNATURE_V1 = 0x426F6541; // 'AeoB'
        public const ushort ACPI_METHOD_ARGUMENT_INTEGER = 0x0;
        public const ushort ACPI_METHOD_ARGUMENT_STRING = 0x1;
        public const ushort ACPI_METHOD_ARGUMENT_BUFFER = 0x2;
        public const ushort ACPI_METHOD_ARGUMENT_PACKAGE = 0x3;
        public const ushort ACPI_METHOD_ARGUMENT_PACKAGE_EX = 0x4;

        private static int nestedLevel = 0;

        private static string getNestedPadding()
        {
            return new string(' ', nestedLevel * 4);
        }

        private static string ParsePackage(BinaryReader br)
        {
            string output = "";
            ushort type = br.ReadUInt16();
            ushort dataLength = br.ReadUInt16();

            switch (type)
            {
                case ACPI_METHOD_ARGUMENT_INTEGER:
                    {
                        switch (dataLength)
                        {
                            case 2:
                                {
                                    ushort argInt = br.ReadUInt16();
                                    output += getNestedPadding() + $"0x{argInt:X4}," + "\n";
                                    break;
                                }
                            case 4:
                                {
                                    uint argInt = br.ReadUInt32();
                                    output += getNestedPadding() + $"0x{argInt:X8}," + "\n";
                                    break;
                                }
                            case 8:
                                {
                                    ulong argInt = br.ReadUInt64();
                                    output += getNestedPadding() + $"0x{argInt:X16}," + "\n";
                                    break;
                                }
                            default:
                                {
                                    throw new Exception("Unknown integer length! " + dataLength);
                                }
                        }
                        break;
                    }
                case ACPI_METHOD_ARGUMENT_STRING:
                    {
                        byte[] argStringBuff = br.ReadBytes(dataLength - 1);
                        _ = br.ReadByte(); // Terminator
                        string argString = System.Text.Encoding.ASCII.GetString(argStringBuff);
                        output += getNestedPadding() + "\"" + argString + "\"," + "\n";
                        break;
                    }
                case ACPI_METHOD_ARGUMENT_BUFFER:
                    {
                        byte[] argBuffer = br.ReadBytes(dataLength);
                        output += getNestedPadding() + $"Buffer (0x{dataLength:X4})";
                        output += " { ";
                        output += BitConverter.ToString(argBuffer).Replace("-", ", 0x");
                        output += " }" + "\n";
                        break;
                    }
                case ACPI_METHOD_ARGUMENT_PACKAGE:
                    {
                        long maxOffset = br.BaseStream.Position + dataLength;

                        output += getNestedPadding() + $"Package (0x{dataLength:X4})" + "\n";
                        output += getNestedPadding() + "{" + "\n";
                        nestedLevel++;
                        while (br.BaseStream.Position < maxOffset)
                        {
                            output += ParsePackage(br);
                        }
                        if (br.BaseStream.Position != maxOffset)
                        {
                            throw new Exception("Overflow!");
                        }
                        nestedLevel--;
                        output += getNestedPadding() + "}," + "\n";
                        break;
                    }
                case ACPI_METHOD_ARGUMENT_PACKAGE_EX:
                    {
                        throw new Exception("Not implemented!");
                    }
                default:
                    {
                        throw new Exception("Unknown type! " + type);
                    }
            }

            return output;
        }

        private static string ParseAeoBFile(string file)
        {
            string finalOutput = "";
            using BinaryReader br = new(File.OpenRead(file));
            uint sig = br.ReadUInt32();
            if (sig != ACPI_EVAL_OUTPUT_BUFFER_SIGNATURE_V1)
            {
                throw new Exception("Invalid signature! " + sig);
            }

            uint length = br.ReadUInt32();
            uint count = br.ReadUInt32();

            for (int i = 0; i < count; i++)
            {
                finalOutput += ParsePackage(br);
            }

            return finalOutput;
        }

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

        private static void BuildAeoBFile(string finalOutput, string file)
        {
            using BinaryWriter bw = new(File.OpenWrite(file));
            string[] lines = finalOutput.Split("\n").ToArray();
            bw.Write(ACPI_EVAL_OUTPUT_BUFFER_SIGNATURE_V1);
            long lengthPos = bw.BaseStream.Position;
            bw.Write((uint)0);
            int count = lines.Where(x => x.StartsWith("Package (0x")).Count();
            bw.Write((uint)count);

            foreach (string? unsanitizedline in lines)
            {
                string line = unsanitizedline.TrimStart().TrimEnd().TrimEnd(',');

                if (line.Contains("}") || line.Contains("{"))
                {
                    continue;
                }

                if (line.StartsWith("Package (0x"))
                {
                    bw.Write(ACPI_METHOD_ARGUMENT_PACKAGE);

                    string lengthStr = line.Split("x").Last().Replace(")", "");
                    byte[] hex = StringToByteArrayFastest(lengthStr).Reverse().ToArray();
                    ushort dataLength = BitConverter.ToUInt16(hex);
                    bw.Write(dataLength);
                }

                if (line.StartsWith("\""))
                {
                    bw.Write(ACPI_METHOD_ARGUMENT_STRING);
                    string strval = line.Split("\"")[1];
                    int dataLength = strval.Length + 1;

                    bw.Write((ushort)dataLength);
                    bw.Write(System.Text.Encoding.ASCII.GetBytes(strval));
                    bw.Write('\0');
                }

                if (line.TrimStart().StartsWith("0x"))
                {
                    bw.Write(ACPI_METHOD_ARGUMENT_INTEGER);
                    string strval = line.Split("0x")[1];
                    byte[] hex = StringToByteArrayFastest(strval).Reverse().ToArray();
                    int dataLength = strval.Length / 2;
                    bw.Write((ushort)dataLength);
                    bw.Write(hex);
                }

                if (line.TrimStart().StartsWith("Buffer (0x"))
                {
                    bw.Write(ACPI_METHOD_ARGUMENT_BUFFER);

                    string lengthStr = line.Split("(").Last().Split(")").First().Replace("0x", "");
                    byte[] hex = StringToByteArrayFastest(lengthStr).Reverse().ToArray();
                    ushort dataLength = BitConverter.ToUInt16(hex);
                    bw.Write(dataLength);

                    string hexStr = line.Split("{ ").Last().Replace(" }", "").Replace(", 0x", "");
                    byte[] hex2 = StringToByteArrayFastest(lengthStr);
                    bw.Write(hex2);
                }
            }

            _ = bw.BaseStream.Seek(lengthPos, SeekOrigin.Begin);
            bw.Write((uint)bw.BaseStream.Length);
        }

        private static void PrintLogo()
        {
            Console.WriteLine($"AeoB/AeoBsl Converter {Assembly.GetExecutingAssembly().GetName().Version}");
            Console.WriteLine("Copyright (c) Gustave Monce and Contributors");
            Console.WriteLine("https://github.com/WOA-Project/AeoBUtils");
            Console.WriteLine();
            Console.WriteLine("This program comes with ABSOLUTELY NO WARRANTY.");
            Console.WriteLine("This is free software, and you are welcome to redistribute it under certain conditions.");
            Console.WriteLine();
        }

        private static int Main(string[] args)
        {
            return Parser.Default.ParseArguments<AeoB2AeoBslOptions, AeoBsl2AeoBOptions>(args).MapResult(
              (AeoB2AeoBslOptions arg) =>
              {
                  PrintLogo();

                  if (File.Exists(arg.path))
                  {
                      File.WriteAllText(arg.output, ParseAeoBFile(arg.path));
                  }

                  return 0;
              },
              (AeoBsl2AeoBOptions arg) =>
              {
                  PrintLogo();

                  if (File.Exists(arg.path))
                  {
                      BuildAeoBFile(File.ReadAllText(arg.path), arg.output);
                  }

                  return 0;
              },
              errs => 1);
        }
    }
}