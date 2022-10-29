using System.Text;

namespace AeoBUtils
{
    internal class AeoBParser
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
                        output += " { 0x";
                        output += BitConverter.ToString(argBuffer).Replace("-", ", 0x");
                        output += " }," + "\n";
                        break;
                    }
                case ACPI_METHOD_ARGUMENT_PACKAGE:
                    {
                        long maxOffset = br.BaseStream.Position + dataLength;

                        output += getNestedPadding() + $"Package ()" + "\n";
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

            if (dataLength < 4)
            {
                for (int i = 0; i < 4 - dataLength; i++)
                {
                    _ = br.ReadByte();
                }
            }

            return output;
        }

        public static string ParseAeoBFile(Stream file)
        {
            string finalOutput = "";
            using BinaryReader br = new(file, Encoding.Default, false);
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
    }
}
