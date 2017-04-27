using System;
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
            // todo: this NEEDS to decompress bytes
            // but also not advance the stream too far ahead!
            // say if we have a string ABCCCCC and we read 4 bytes of: ABCC
            // we don't want the stream to be at the ned of ABCCCC*, we want it at ABCC*CC
            return compressedStream.Read(buffer, offset, count);
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
