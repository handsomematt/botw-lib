using BotWLib.Common;
using System;
using System.IO;
using System.Text;

namespace BotWLib.Formats
{
    public class SARC
    {
        public SARC(MemoryStream inputStream)
        {
            EndianBinaryReader er = new EndianBinaryReader(inputStream, Endian.Big);
            try
            {
                Header = new SARCHeader(er);
                SfatHeader = new SFATHeader(er);

                SfatNodes = new SFATNode[SfatHeader.NodeCount];
                for (int i = 0; i < SfatHeader.NodeCount; i++) {
                  SfatNodes[i] = new SFATNode(er);
                }

                // Just skip the SFNT_HEADER because it's useless for now ?
                er.ReadBytes(8);

                SfatStringTable = new string[SfatHeader.NodeCount];
                for (int i = 0; i < SfatHeader.NodeCount; i++) {
                  // These are padded strings, so skip the null bytes padding them
                  while (er.PeekReadByte() == 0)
                    er.ReadByte();

                  SfatStringTable[i] = Encoding.UTF8.GetString(er.ReadBytesUntil(0));
                  SfatNodes[i].copied_name = SfatStringTable[i];
                }

                // TODO: Maybe we have to pad here too?
                SfatDataTable = new byte[SfatHeader.NodeCount][];
                for (int i = 0; i < SfatHeader.NodeCount; i++) {
                  uint dataLength = SfatNodes[i].NodeDataEndOffset - SfatNodes[i].NodeDataEndOffset;

                  SfatDataTable[i] = er.ReadBytesAt(SfatNodes[i].NodeDataBeginOffset, (int)dataLength);
                  SfatNodes[i].copied_data = SfatDataTable[i];
                }
            }
            finally
            {
                er.Close();
            }
        }

        public SARCHeader Header;
        public class SARCHeader
        {
            public SARCHeader(EndianBinaryReader er)
            {
                if (new string(er.ReadChars(4)) != "SARC") throw new InvalidDataException("Invalid magic number!");
                Length = er.ReadUInt16();
                ByteOrder = er.ReadUInt16();
                FileSize = er.ReadUInt32();
                DataOffset = er.ReadUInt32();
                Unk0 = er.ReadUInt32();
            }
            public UInt16 Magic;
            public UInt16 Length;
            public UInt16 ByteOrder;
            public UInt32 FileSize;
            public UInt32 DataOffset;
            public UInt32 Unk0;
        }

        public SFATHeader SfatHeader;
        public class SFATHeader
        {
            public SFATHeader(EndianBinaryReader er)
            {
                if (new string(er.ReadChars(4)) != "SFAT") throw new InvalidDataException("Invalid magic number!");
                Length = er.ReadUInt16();
                NodeCount = er.ReadUInt16();
                HashMultiplier = er.ReadUInt32();
            }
            public UInt16 Magic;
            public UInt16 Length;
            public UInt16 NodeCount;
            public UInt32 HashMultiplier;
        }

        public SFATNode[] SfatNodes;
        public class SFATNode
        {
            public SFATNode(EndianBinaryReader er) {
                FileNameHash = er.ReadUInt32();

                // We need the lower 8 bits and upper 24 bits separately
                UInt32 data = er.ReadUInt32();
                NodeType = (byte)(data & 0xFF000000);
                FileNameTableOffset = data & 0x00FF0000;

                NodeDataBeginOffset = er.ReadUInt32();
                NodeDataEndOffset = er.ReadUInt32();
            }

            public UInt32 FileNameHash;
            public byte NodeType;
            public UInt32 FileNameTableOffset;
            public UInt32 NodeDataBeginOffset;
            public UInt32 NodeDataEndOffset;

            public string copied_name;
            public byte[] copied_data;
        }

        public string[] SfatStringTable;
        public byte[][] SfatDataTable;

        public object Endianness { get; private set; }
    }
}
