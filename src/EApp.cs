using QIQI.EProjectFile;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Threading.Tasks;
using static FormRipper.EApp;

namespace FormRipper
{
    internal class EApp
    {

        public struct EDllInfo
        {
            public string DllName;
            public string Symbol;
        }

        public struct EAppInfo
        {
            public int nEAppMajorVersion;
            public int nEAppMinorVersion;
            public int nEAppBuildNumber;
            public int lpfnEcode;
            public int lpEConst;
            public int uEConstSize;
            public int lpEForm;
            public int uEFormSize;
            public int uELibInfoCount;
            public int lpELibInfos;
            public int uEDllImportCount;
            public int lpEDllNames;
            public int lpEDllSymbols;

        }


        public EAppInfo Info;


        PEReader PE;

        BlobReader Reader;

        int SectionAddress;

        ulong BaseAddress;


        public EApp(string FileName)
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            PE = new PEReader(new MemoryStream(File.ReadAllBytes(FileName)));
            Reader = PE.GetSectionData(".rdata").GetReader();
            Info = new EAppInfo();

            BaseAddress = PE.PEHeaders.PEHeader == null ? 0 : PE.PEHeaders.PEHeader.ImageBase;
            SectionAddress = PE.PEHeaders.SectionHeaders.FirstOrDefault(x => x.Name == ".rdata").VirtualAddress;

            int InfoOffset = FindSignture(new byte[] { 0x03, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 });
            if (InfoOffset != -1 && BaseAddress != 0 && SectionAddress != 0)
            {
                Reader.Offset = InfoOffset;

                Info.nEAppMajorVersion = Reader.ReadInt32();
                Info.nEAppMinorVersion = Reader.ReadInt32();
                Info.nEAppBuildNumber = Reader.ReadInt32();
                Info.lpfnEcode = Reader.ReadInt32();
                Info.lpEConst = Reader.ReadInt32();
                Info.uEConstSize = Reader.ReadInt32();
                Info.lpEForm = Reader.ReadInt32();
                Info.uEFormSize = Reader.ReadInt32();
                Info.uELibInfoCount = Reader.ReadInt32();
                Info.lpELibInfos = Reader.ReadInt32();
                Info.uEDllImportCount = Reader.ReadInt32();
                Info.lpEDllNames = Reader.ReadInt32();
                Info.lpEDllSymbols = Reader.ReadInt32();
            }

        }

        public byte[] ReadEConst()
        {
            if (Info.lpEConst != 0 && Info.uEConstSize != 0 && BaseAddress != 0 && SectionAddress != 0)
            {
                Reader.Offset = CalcOffset(Info.lpEConst);
                return Reader.ReadBytes(Info.uEConstSize);
            }
            return Array.Empty<byte>();
        }

        public List<FormInfo> ReadEForm()
        {
            if (Info.lpEForm != 0 && Info.uEFormSize != 0 && BaseAddress != 0 && SectionAddress != 0)
            {
                Reader.Offset = CalcOffset(Info.lpEForm);
                return FormInfo.ReadForms(new BinaryReader(new MemoryStream(Reader.ReadBytes(Info.uEFormSize))), Encoding.GetEncoding("gbk"));
            }
            return new List<FormInfo>();
        }

        public string ReadShortString(int Ptr)
        {
            byte[] Buffer;
            string Result;
            int SLen = 0, Pos = Reader.Offset;

            Reader.Offset = CalcOffset(Ptr);

            Buffer = Reader.ReadBytes(255);
            foreach (byte b in Buffer)
            {
                if (b == 0)
                {
                    break;
                }
                SLen++;
            }
            Buffer = Buffer.Take(SLen).ToArray();
            Result = Encoding.GetEncoding("gbk").GetString(Buffer);

            Reader.Offset = Pos;

            return Result;
        }

        public List<LibraryRefInfo> ReadELib()
        {
            List<LibraryRefInfo> Result = new List<LibraryRefInfo>();

            if (Info.lpELibInfos != 0 && Info.uELibInfoCount != 0 && BaseAddress != 0 && SectionAddress != 0)
            {
                Reader.Offset = CalcOffset(Info.lpELibInfos);
                int LibInfoOffset = FindSignture(new byte[] { 0x65, 0x2d, 0x31, 0x01 });
                if (LibInfoOffset != -1)
                {
                    Reader.Offset = LibInfoOffset;
                    for (int i = 0; i < Info.uELibInfoCount; i++)
                    {
                        if (Reader.ReadUInt32() != 0x1312D65)
                            break;

                        LibraryRefInfo lib = new LibraryRefInfo();

                        lib.GuidString = ReadShortString(Reader.ReadInt32());
                        lib.Version = new Version(Reader.ReadInt32(), Reader.ReadInt32());

                        // skip
                        Reader.Offset += 20;

                        lib.Name = ReadShortString(Reader.ReadInt32());

                        // skip
                        Reader.Offset += 104;

                        Result.Add(lib);
                    }
                }
            }
            return Result;
        }

        public List<EDllInfo> ReadEDll()
        {
            List<EDllInfo> Result = new List<EDllInfo>();

            if (Info.lpEDllNames != 0 && Info.lpEDllSymbols != 0 && Info.uEDllImportCount != 0 && BaseAddress != 0 && SectionAddress != 0)
            {
                for (int i = 0; i < Info.uEDllImportCount; i++)
                {
                    EDllInfo imp = new EDllInfo();

                    Reader.Offset = CalcOffset(Info.lpEDllNames) + i * 4;
                    imp.DllName = ReadShortString(Reader.ReadInt32());

                    Reader.Offset = CalcOffset(Info.lpEDllSymbols) + i * 4;
                    imp.Symbol = ReadShortString(Reader.ReadInt32());

                    Result.Add(imp);
                }
            }

            return Result;
        }

        private int CalcOffset(int Ptr)
        {
            return Ptr - (int)BaseAddress - SectionAddress;
        }

        private int FindSignture(byte[] Signture)
        {
            int Pos, Result = -1;
            byte[] find = new byte[Signture.Length];

            Pos = Reader.Offset;

            while (Reader.Length - Reader.Offset != 0)
            {
                bool flag = true;

                find = find.Concat(Reader.ReadBytes(1)).ToArray();
                if (find.Length == Signture.Length + 1)
                    find = find.Skip(1).ToArray();

                for (int i = 0; i < Signture.Length; i++)
                {
                    if (Signture[i] != find[i])
                        flag = false;
                }

                if (flag)
                {
                    Result = Reader.Offset - Signture.Length;
                    Reader.Offset = Pos;
                    break;
                }

            }

            return Result;
        }

    }
}
