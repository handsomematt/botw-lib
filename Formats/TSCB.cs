using BotWLib.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace BotWLib.Formats
{
    public class TSCB
    {
        public TSCB(Stream inputStream)
        {
            EndianBinaryReader er = new EndianBinaryReader(inputStream, Endian.Big);
            try
            {
                Header = new TSCBHeader(er);
                MaterialLookupHeader = new MaterialLookupTableHeader(er);

                var colorLookupStartPos = er.BaseStream.Position;

                uint[] MaterialLookupOffsets = new uint[Header.MaterialLookupCount];
                for (int i = 0; i < Header.MaterialLookupCount; i++)
                    MaterialLookupOffsets[i] = (uint)er.BaseStream.Position + er.ReadUInt32();

                er.BaseStream.Position = 48 + MaterialLookupHeader.Size;

                uint[] TileTableOffsets = new uint[Header.TileTableCount];
                for (int i = 0; i < Header.TileTableCount; i++)
                    TileTableOffsets[i] = (uint)er.BaseStream.Position + er.ReadUInt32();

                // do this last!
                MaterialLookups = new List<MaterialLookupEntry>((int)Header.MaterialLookupCount);
                for (int i = 0; i < MaterialLookupOffsets.Length; i++)
                {
                    er.BaseStream.Position = MaterialLookupOffsets[i];
                    MaterialLookups.Add(new MaterialLookupEntry(er));
                }

                TileTableList = new List<TileTableEntry>((int)Header.TileTableCount);
                for (int i = 0; i < TileTableOffsets.Length; i++)
                {
                    er.BaseStream.Position = TileTableOffsets[i];
                    TileTableList.Add(new TileTableEntry(er));
                }


            }
            finally
            {
                er.Close();
            }
        }

        public TSCBHeader Header;
        public class TSCBHeader
        {
            public TSCBHeader(EndianBinaryReader er)
            {
                if (new string(er.ReadChars(4)) != "TSCB") throw new InvalidDataException("Invalid magic number!");

                var unk0 = er.ReadUInt32(); // always 0x0A000000
                var unk1 = er.ReadUInt32(); // always 1
                StringTableOffset = er.ReadUInt32();
                var unk2 = er.ReadSingle(); // 500.0
                var unk3 = er.ReadSingle(); // 800.0
                MaterialLookupCount = er.ReadUInt32();
                TileTableCount = er.ReadUInt32();
            }

            public UInt32 StringTableOffset;
            public UInt32 MaterialLookupCount;
            public UInt32 TileTableCount;
        }

        public MaterialLookupTableHeader MaterialLookupHeader;
        public class MaterialLookupTableHeader
        {
            public MaterialLookupTableHeader(EndianBinaryReader er)
            {
                Unknown1 = er.ReadUInt32();
                Unknown2 = er.ReadUInt32();
                Unknown3 = er.ReadSingle();
                Unknown4 = er.ReadUInt32();
                Size = er.ReadUInt32();
            }

            public UInt32 Unknown1;
            public UInt32 Unknown2;
            public Single Unknown3;
            public UInt32 Unknown4;
            public UInt32 Size;
        }
        
        public List<MaterialLookupEntry> MaterialLookups;
        public class MaterialLookupEntry
        {
            public MaterialLookupEntry(EndianBinaryReader er)
            {
                Index = er.ReadUInt32();
                R = er.ReadSingle();
                G = er.ReadSingle();
                B = er.ReadSingle();
                A = er.ReadSingle();
            }

            public uint Index;
            public float R;
            public float G;
            public float B;
            public float A;
        }

        public List<TileTableEntry> TileTableList;
        public class TileTableEntry
        {
            public TileTableEntry(EndianBinaryReader er)
            {
                CenterX = er.ReadSingle();
                CenterY = er.ReadSingle();
                EdgeLength = er.ReadSingle();
                Unknown3 = er.ReadSingle();
                Unknown4 = er.ReadSingle();
                Unknown5 = er.ReadSingle();
                Unknown6 = er.ReadSingle();
                Unknown7 = er.ReadUInt32();

                long currentPosition = er.BaseStream.Position;
                StringOffset = er.PeekReadUInt32();
                er.BaseStream.Seek(StringOffset, SeekOrigin.Current);
                Name = er.ReadStringUntil('\0');
                er.BaseStream.Seek(currentPosition + 4, SeekOrigin.Begin);

                Unknown9 = er.ReadUInt32();
                Unknown10 = er.ReadUInt32();
                Unknown11 = er.ReadUInt32();

                if (Unknown7 == 0)
                    return;

                VariableCount = er.ReadUInt32();
                VariableInts = new uint[VariableCount];
                for (int i = 0; i < VariableCount; i++)
                    VariableInts[i] = er.ReadUInt32();

                
            }

            public float CenterX;
            public float CenterY;
            public float EdgeLength;
            public float Unknown3;
            public float Unknown4;
            public float Unknown5;
            public float Unknown6;
            public uint Unknown7;
            public uint StringOffset;
            public uint Unknown9;
            public uint Unknown10;
            public uint Unknown11;

            public uint VariableCount;
            public uint[] VariableInts;

            public string Name;
        }
    }
}
