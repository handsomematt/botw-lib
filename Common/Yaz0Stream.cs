using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace BotWLib.Common
{
    public class Yaz0Stream : Stream
    {
        private long decompressedPosition;
        private long decompressedLength;
        private long compressedStartPosition;

        private Stream compressedStream;

        public Yaz0Stream(Stream stream)
        {
            compressedStream = stream;

            var reader = new EndianBinaryReader(stream, Encoding.ASCII, false, Endian.Big);
            if (reader.ReadUInt32() != 0x59617A30) // "Yaz0" Magic
                throw new InvalidDataException("Invalid Yaz0 header.");

            decompressedLength = (long)reader.ReadUInt32();
            reader.BaseStream.Position += 8;
            compressedStartPosition = reader.BaseStream.Position;
            
            decompressedPosition = 0;
        }

        public override long Position
        {
            get { return decompressedPosition; }
            set { throw new NotImplementedException(); } // todo
        }

        public override long Length
        {
            get { return decompressedLength; }
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotImplementedException();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (DecompressEnumerator == null)
                DecompressEnumerator = Decompress(compressedStream).GetEnumerator();

            int i;
            for (i = 0; i < count; i++)
            {
                DecompressEnumerator.MoveNext();
                buffer[offset + i] = DecompressEnumerator.Current;
            }

            return i;
        }

        private IEnumerator<byte> DecompressEnumerator;

        private IEnumerable<byte> Decompress(Stream input)
        {
            // Decompress the data.
            int decompressedBytes = 0;
            while (decompressedBytes < decompressedLength)
            {
                // Read the configuration byte of a decompression setting group, and go through each bit of it.
                byte groupConfig = (byte)input.ReadByte();
                for (int i = 7; i >= 0; i--)
                {
                    // Check if bit of the current chunk is set.
                    if ((groupConfig & (1 << i)) == (1 << i))
                    {
                        // Bit is set, copy 1 raw byte to the output.
                        yield return (byte)input.ReadByte();
                        decompressedBytes++;
                    }
                    else if (decompressedBytes < decompressedLength) // This does not make sense for last byte.
                    {
                        // Bit is not set and data copying configuration follows, either 2 or 3 bytes long.
                        byte byte1 = (byte)input.ReadByte();
                        byte byte2 = (byte)input.ReadByte();
                        ushort dataBackSeekOffset = (ushort)(((byte1 & 0xF) << 8) | byte2);
                        int dataSize;
                        // If the nibble of the first back seek offset byte is 0, the config is 3 bytes long.
                        byte nibble = (byte)(dataBackSeekOffset >> 12/*1 byte (8 bits) + 1 nibble (4 bits)*/);
                        if (nibble == 0)
                        {
                            // Nibble is 0, the number of bytes to read is in third byte, which is (size + 0x12).
                            dataSize = input.ReadByte() + 0x12;
                        }
                        else
                        {
                            // Nibble is not 0, and determines (size + 0x02) of bytes to read.
                            dataSize = nibble + 0x02;
                            // Remaining bits are the real back seek offset.
                            dataBackSeekOffset &= 0x0FFF;
                        }
                        // Since bytes can be reread right after they were written, write and read bytes one by one.
                        for (int j = 0; j < dataSize; j++)
                        {
                            // Read one byte from the current back seek position.
                            //writer.BaseStream.Position -= dataBackSeekOffset + 1;
                            //byte readByte = (byte)writer.BaseStream.ReadByte();
                            // Write the byte to the end of the memory stream.
                            //writer.Seek(0, SeekOrigin.End);
                            //writer.Write(readByte);

                            byte readByte = 0x21;

                            yield return readByte;

                            decompressedBytes++;
                        }
                    }
                }
            }
        }

        public override bool CanRead
        {
            get { return true; }
        }

        public override bool CanSeek
        {
            get { return false;  }
        }

        public override bool CanWrite
        {
            get { return false; }
        }

        public override void Flush()
        {
            throw new NotImplementedException();
        }

        public override void SetLength(long value)
        {
            throw new NotImplementedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotImplementedException();
        }
    }
}
