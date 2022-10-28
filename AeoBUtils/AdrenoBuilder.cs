using System.Text;
using System.Xml.Serialization;

namespace AeoBUtils
{
    internal class AdrenoBuilder
    {
        public static void BuildAdrenoFile(string finalOutput, Stream file)
        {
            TextReader reader = new StringReader(finalOutput);
            XmlSerializer ser = new(typeof(AdrenoBasePackage));
            AdrenoBasePackage pkg = (AdrenoBasePackage)ser.Deserialize(reader);

            using BinaryWriter bw = new(file, Encoding.Default, true);
            long ogpos = bw.BaseStream.Position;

            bw.Write(pkg.headerLength);
            bw.Write(pkg.reserved);
            bw.Write(pkg.methodCount);
            bw.Write(pkg.binaryNameLength);
            bw.Write(Encoding.ASCII.GetBytes(pkg.binaryName));
            if (bw.BaseStream.Position - ogpos < pkg.headerLength)
            {
                long missing = ogpos + pkg.headerLength - bw.BaseStream.Position;
                for (int i = 0; i < missing; i++)
                {
                    bw.Write('\0');
                }
            }

            long methodArrayPos = bw.BaseStream.Position;
            foreach (AdrenoMethodPackage method in pkg.adrenoMethodPackages)
            {
                bw.Write(Encoding.ASCII.GetBytes(method.Name));
                bw.Write((uint)0);
                bw.Write((uint)0);
            }

            int j = 0;
            foreach (AdrenoMethodPackage method in pkg.adrenoMethodPackages)
            {
                uint start = (uint)bw.BaseStream.Position - (uint)ogpos;

                string tmp = Path.GetRandomFileName();
                using (FileStream filestrm = File.OpenWrite(tmp))
                {
                    AeoBBuilder.BuildAeoBFile(method.AeoBsl, filestrm);
                }

                byte[] buffer = File.ReadAllBytes(tmp);
                uint length = (uint)buffer.Length;

                bw.Write(buffer);
                long backuppos = start + length;

                _ = bw.BaseStream.Seek(methodArrayPos + (j * 12) + 4, SeekOrigin.Begin);
                bw.Write(BitConverter.GetBytes(start));
                bw.Write(BitConverter.GetBytes(length));
                _ = bw.BaseStream.Seek(backuppos, SeekOrigin.Begin);

                j++;
            }
        }
    }
}