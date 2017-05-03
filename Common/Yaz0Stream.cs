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
            set {
                if (value > decompressedPosition)
                    Seek(value - decompressedPosition, SeekOrigin.Current);
                else
                    Seek(value, SeekOrigin.Begin);
            }
        }

        public override long Length
        {
            get { return decompressedLength; }
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            if (origin == SeekOrigin.Begin)
            {
                decompressedPosition = 0;
                compressedStream.Position = compressedStartPosition;
                DecompressEnumerator = Decompress(compressedStream).GetEnumerator();

                for (int i = 0; i < offset; i++)
                    DecompressEnumerator.MoveNext();

                return decompressedPosition;
            }

            if (origin == SeekOrigin.Current)
            {
                for (int i = 0; i < offset; i++)
                    DecompressEnumerator.MoveNext();

                return decompressedPosition;
            }

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
        
        // we need to store at minimum the last 4KB of decompressed bytes
        // let's use a circurlar buffer for speed and convinience
        private CircularBuffer<byte> decompressionBuffer;

        private IEnumerator<byte> DecompressEnumerator;

        private IEnumerable<byte> Decompress(Stream input)
        {
            decompressionBuffer = new CircularBuffer<byte>(4096);

            // Decompress the data.
            while (decompressedPosition < decompressedLength)
            {
                // Read the configuration byte of a decompression setting group, and go through each bit of it.
                byte groupConfig = (byte)input.ReadByte();
                for (int i = 7; i >= 0; i--)
                {
                    // Check if bit of the current chunk is set.
                    if ((groupConfig & (1 << i)) == (1 << i))
                    {
                        byte value = (byte)input.ReadByte();
                        decompressionBuffer.PushFront(value);

                        // Bit is set, copy 1 raw byte to the output.
                        yield return value;
                        decompressedPosition++;
                    }
                    else if (decompressedPosition < decompressedLength) // This does not make sense for last byte.
                    {
                        // Bit is not set and data copying configuration follows, either 2 or 3 bytes long.
                        byte[] bytes = new byte[] { (byte)input.ReadByte(), (byte)input.ReadByte() };
                        ushort dataBackSeekOffset = EndianUtils.SwapBytes(BitConverter.ToUInt16(bytes, 0));
                        
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

                        if (dataBackSeekOffset > 4095)
                            throw new Exception("shit!");

                        // Since bytes can be reread right after they were written, write and read bytes one by one.
                        for (int j = 0; j < dataSize; j++)
                        {
                            byte value = decompressionBuffer[dataBackSeekOffset];
                            decompressionBuffer.PushFront(value);

                            yield return value;
                            decompressedPosition++;
                        }
                    }
                }
            }
        }

        public override bool CanRead
        {
            get { return decompressedPosition < decompressedLength; }
        }

        public override bool CanSeek
        {
            get { return true;  }
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
