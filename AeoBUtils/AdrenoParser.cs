using System.Text;
using System.Xml;
using System.Xml.Serialization;

namespace AeoBUtils
{
    public class AdrenoMethodPackage
    {
        public string? Name { get; set; }

        public string? AeoBsl { get; set; }
    }

    public class AdrenoBasePackage
    {
        public uint headerLength { get; set; }
        public ulong reserved { get; set; }
        public uint methodCount { get; set; }
        public uint binaryNameLength { get; set; }
        public string? binaryName { get; set; }
        public AdrenoMethodPackage[]? adrenoMethodPackages { get; set; }
    }

    internal class AdrenoParser
    {
        public static string ParseAdrenoFile(Stream file)
        {
            using BinaryReader br = new(file, Encoding.Default, false);
            long ogpos = br.BaseStream.Position;
            uint headerlength = br.ReadUInt32();
            ulong reserved = br.ReadUInt64();
            uint methodCount = br.ReadUInt32();
            uint binaryNameLength = br.ReadUInt32();

            byte[] binaryNameBuff = br.ReadBytes((int)binaryNameLength);
            string binaryName = Encoding.ASCII.GetString(binaryNameBuff);

            AdrenoBasePackage adrenoBasePackage = new()
            {
                headerLength = headerlength,
                reserved = reserved,
                methodCount = methodCount,
                binaryNameLength = binaryNameLength,
                binaryName = binaryName
            };

            List<AdrenoMethodPackage> adrenoMethodPackages = new();

            _ = br.BaseStream.Seek(ogpos + headerlength, SeekOrigin.Begin);

            for (int i = 0; i < methodCount; i++)
            {
                byte[] methodNameBuff = br.ReadBytes(4);
                string methodName = Encoding.ASCII.GetString(methodNameBuff);
                uint startOffset = br.ReadUInt32();
                uint length = br.ReadUInt32();
                long retPos = br.BaseStream.Position;

                _ = br.BaseStream.Seek(startOffset, SeekOrigin.Begin);
                byte[] methodBuff = br.ReadBytes((int)length);

                string tmp = Path.GetRandomFileName();
                File.WriteAllBytes(tmp, methodBuff);
                using FileStream filestrm = File.OpenRead(tmp);

                _ = br.BaseStream.Seek(retPos, SeekOrigin.Begin);

                adrenoMethodPackages.Add(new AdrenoMethodPackage()
                {
                    Name = methodName,
                    AeoBsl = AeoBParser.ParseAeoBFile(filestrm)
                });
            }

            adrenoBasePackage.adrenoMethodPackages = adrenoMethodPackages.ToArray();

            using StringWriter writer = new();
            using XmlWriter xmlWriter = XmlWriter.Create(writer, new XmlWriterSettings()
            {
                Indent = true
            });
            XmlSerializer ser = new(adrenoBasePackage.GetType());
            ser.Serialize(xmlWriter, adrenoBasePackage);
            return writer.ToString();
        }
    }
}
