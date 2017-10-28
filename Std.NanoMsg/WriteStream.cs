using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using Std.NanoMsg.Internal;

namespace Std.NanoMsg
{
    public unsafe class WriteStream : Stream
    {
        private IoBuffer* _head;
        private IoBuffer* _current;
        private int _length;
        private UnmanagedBufferManager _pool;
        private NanoSocket _socket;

        public WriteStream(NanoSocket socket)
        {
            _socket = socket;
            _pool = socket.BufferManager;
        }

        public override bool CanRead
        {
            get => false;
        }

        public override bool CanSeek
        {
            get => false;
        }

        public override bool CanWrite
        {
            get => true;
        }

        public override long Length
        {
            get => _length;
        }

        public override long Position
        {
            get => _length;
            set => throw new NotImplementedException();
        }

        public int PageCount
        {
            get
            {
                var i = 0;
                var header = _head;
                while (header != null)
                {
                    ++i;
                    header = header->Next;
                }

                return i;
            }
        }

        public override void Flush()
        {
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            throw new NotImplementedException();
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotImplementedException();
        }

        public override void SetLength(long value)
        {
            throw new NotImplementedException();
        }

        public override void WriteByte(byte value)
        {
            EnsureCapacity();
            var data = _current->Needle;
            *data = value;
            ++_current->Length;
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            var initialCount = count;
            fixed (byte* src = buffer)
            {
                while (true)
                {
                    var capacity = EnsureCapacity();
                    var toCopy = Math.Min(count, capacity);
                    MemoryUtils.CopyMemory(src + offset, _current->Needle, toCopy);
                    _current->Length += toCopy;
                    count -= toCopy;
                    if (count == 0)
                    {
                        break;
                    }

                    offset += toCopy;
                }
            }
            _length += initialCount;
        }

        public byte[] ToArray()
        {
            var data = new byte[_length];
            var page = _head;
            var offset = 0;

            fixed (byte* ptr = data)
            {
                while (page != null)
                {
                    Unsafe.CopyBlockUnaligned(ptr + offset, page->Data, (uint) page->Length);
                    offset += page->Length;
                    page = page->Next;
                }
            }

            return data;
        }

        internal ByteBuffer FirstPage()
        {
            return new ByteBuffer(_head->Data, _head->Length);
        }

        internal ByteBuffer NextPage(ByteBuffer result)
        {
            var current = (IoBuffer*) result.Data;
            var next = current->Next;
            if (next == null)
            {
                return new ByteBuffer();
            }

            return new ByteBuffer(next->Data, next->Length);
        }

        private const int PageSize = 4096;

        private int EnsureCapacity()
        {
            Internal.ByteBuffer buffer;

            if (_current == null)
            {
                buffer = _pool.TakeBuffer(PageSize);
                _current = (IoBuffer*) buffer.Data;
                _current->Capacity = PageSize;
                _current->Length = 0;
                _head = _current;
                return _current->Capacity;
            }

            var current = *_current;
            if (current.Length != current.Capacity)
            {
                return current.Capacity - current.Length;
            }

            buffer = _pool.TakeBuffer(PageSize);
            var next = (IoBuffer*) buffer.Data;
            next->Capacity = PageSize;
            next->Length = 0;
            _current->Next = next;
            _current = next;

            return buffer.Length;
        }

        protected override void Dispose(bool disposing)
        {
            var pool = Interlocked.Exchange(ref _pool, null);
            var head = _head;
            _head = null;
            _current = null;

            while (head != null)
            {
                var next = head->Next;
                pool.ReturnBuffer(new Internal.ByteBuffer((byte*) head, PageSize));
                head = next;
            }

            base.Dispose(disposing);
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct IoBuffer
        {
            public IoBuffer* Next;
            public int Capacity;
            public int Length;
            public static readonly int SizeOf = sizeof(IoBuffer);

            public byte* Data
            {
                get => (byte*) Unsafe.AsPointer(ref *Next) + SizeOf;
            }

            public byte* Needle
            {
                get => Data + Length;
            }
        }

#if false
        private class BufferPool
        {
            private readonly int _capacity;
            private readonly int _pageSize;
            private readonly Queue<IntPtr> _pool;
            private readonly int _threadId;

            public BufferPool(int pageSize, int maxCached)
            {
                _pageSize = pageSize;
                _capacity = maxCached;
                _pool = new Queue<IntPtr>(maxCached);
                _threadId = Thread.CurrentThread.ManagedThreadId;
            }

            public BufferHeader* Alloc()
            {
                var rightThread = Thread.CurrentThread.ManagedThreadId == _threadId;
                if (rightThread && _pool.Count > 0)
                {
                    return (BufferHeader*) _pool.Dequeue();
                }

                var header = (BufferHeader*) Marshal.AllocHGlobal(_pageSize + BufferHeader.BufferHeaderSize);
                header->Next = null;
                header->Size = _pageSize;
                header->Used = 0;
                return header;
            }

            public BufferHeader* Alloc(int size)
            {
                if (size == _pageSize)
                {
                    return Alloc();
                }

                var header = (BufferHeader*) Marshal.AllocHGlobal(size + BufferHeader.BufferHeaderSize);
                header->Next = null;
                header->Size = size;
                header->Used = 0;
                return header;
            }

            public void Dealloc(BufferHeader* header)
            {
                var rightThread = Thread.CurrentThread.ManagedThreadId == _threadId;
                do
                {
                    var next = header->Next;

                    if (rightThread &&
                        _pool.Count < _capacity &&
                        _pageSize == header->Size)
                    {
                        _pool.Enqueue((IntPtr) header);
                        header->Used = 0;
                        header->Next = null;
                    }
                    else
                    {
                        Marshal.FreeHGlobal((IntPtr) header);
                    }

                    header = next;
                } while (header != null);
            }
        }

        private static class ThreadBufferPool
        {
            [ThreadStatic]
            private static BufferPool _pool;

            public static BufferPool Pool
            {
                get
                {
                    if (_pool != null)
                    {
                        return _pool;
                    }

                    return _pool = new BufferPool(4096 - BufferHeader.BufferHeaderSize, 10);
                }
            }
        }
#endif
    }
}