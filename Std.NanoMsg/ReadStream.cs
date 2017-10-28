using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using Std.NanoMsg.Internal;
using Std.NanoMsg.Native;

namespace Std.NanoMsg
{
    public unsafe class ReadStream : Stream
    {
        private byte* _buffer;
        private readonly Action<(ReadStream Stream, Internal.ByteBuffer Buffer)> _disposer;
        private long _length;
        private long _position;

        internal ReadStream(Internal.ByteBuffer buffer, Action<(ReadStream Stream, Internal.ByteBuffer Buffer)> disposer)
        {
            if (buffer.Data == null)
            {
                throw new ArgumentNullException(nameof(buffer));
            }

            if (buffer.Length <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(buffer.Length));
            }

            _buffer = buffer.Data;
            _length = buffer.Length;
            _disposer = disposer;
        }

        public override bool CanRead
        {
            get => _position < _length;
        }

        public override bool CanSeek
        {
            get => true;
        }

        public override bool CanWrite
        {
            get => false;
        }

        public override long Length
        {
            get => _length;
        }

        public override long Position
        {
            get => _position;
            set => _position = value;
        }

        public void Reinitialize(void* buffer, long length)
        {
            if (buffer == null)
            {
                throw new ArgumentNullException(nameof(buffer));
            }

            if (length <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(length));
            }

            _length = length;
            _position = 0;
            _buffer = (byte*) buffer;
            GC.ReRegisterForFinalize(this);
        }

        protected override void Dispose(bool disposing)
        {
            var length = Interlocked.Exchange(ref _length, -1);
            var buffer = _buffer;
            _buffer = null;

            if (buffer != null &&
                length > 0)
            {
                _disposer?.Invoke((this, new ByteBuffer(buffer, (int) length)));
            }

            base.Dispose(disposing);
        }

        public override void Flush() => throw new NotImplementedException();

        public override int Read(byte[] buffer, int offset, int count)
        {
            var remaining = _length - _position;
            if (remaining < count)
            {
                count = (int) remaining;
            }
            if (count <= 0)
            {
                return 0;
            }

            MemoryUtils.PinAndCopyMemory(_buffer, (int) _position, buffer, offset, count);
            _position += count;
            return count;
        }

        public override int ReadByte()
        {
            if (_position >= _length)
            {
                return -1;
            }

            return *(_buffer + _position++);
        }

        public int? ReadInt32()
        {
            if (_position > _length - 4)
            {
                return null;
            }

            var result = *((int*) (_buffer + _position));
            _position += 4;
            return result;
        }

        public long? ReadInt64()
        {
            if (_position > _length - 8)
            {
                return null;
            }

            var result = *((long*) (_buffer + _position));
            _position += 8;
            return result;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            switch (origin)
            {
                case SeekOrigin.Begin:
                    _position = offset;
                    break;
                case SeekOrigin.Current:
                    _position += offset;
                    break;
                case SeekOrigin.End:
                    _position = _length + offset;
                    break;
            }

            return _position;
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