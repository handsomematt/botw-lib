using BotWLib.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace BotWLib.Formats
{
    public class BFRES
    {
        public BFRES(Stream inputStream)
        {
            EndianBinaryReader er = new EndianBinaryReader(inputStream, Endian.Big);
            try
            {
                Header = new FRESHeader(er);
            }
            finally
            {
                er.Close();
            }
        }

        public FRESHeader Header;
        public class FRESHeader
        {
            public FRESHeader(EndianBinaryReader er)
            {
                if (new string(er.ReadChars(4)) != "FRES") throw new InvalidDataException("Invalid magic number!");
                Unknown1 = er.ReadUInt32();
                BOM = er.ReadUInt16();
                Unknown2 = er.ReadUInt16();
                FileLength = er.ReadUInt32();
                FileAlignment = er.ReadUInt32();
                FileNameOffset = er.ReadUInt32();
                StringTableLength = er.ReadUInt32();
                StringTableOffset = er.ReadUInt32();

                IndexGroupOffsets = new UInt32[12];
                for (int i = 0; i < 12; i++)
                    IndexGroupOffsets[i] = er.ReadUInt32();

                IndexFileCounts = new UInt16[12];
                for (int i = 0; i < 12; i++)
                    IndexFileCounts[i] = er.ReadUInt16();
            }

            public UInt32 Unknown1; // Probably version number.
            public UInt16 BOM;
            public UInt16 Unknown2; // Always 10.
            public UInt32 FileLength;
            public UInt32 FileAlignment; // power of two
            public UInt32 FileNameOffset;
            public UInt32 StringTableLength;
            public UInt32 StringTableOffset;
            public UInt32[] IndexGroupOffsets; // [12]
            public UInt16[] IndexFileCounts; // [12]
            public UInt32 Unknown3; // Always 0?

            public List<string> StringTable;
        }
    }
}
