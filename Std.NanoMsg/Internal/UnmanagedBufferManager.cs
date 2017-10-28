using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using Std.NanoMsg.Native;

namespace Std.NanoMsg.Internal
{
    internal abstract unsafe class UnmanagedBufferManager
    {
        public abstract ByteBuffer TakeBuffer(int bufferSize);
        public abstract void ReturnBuffer(ByteBuffer buffer);
        public abstract void Clear();

        public static UnmanagedBufferManager Create(long maxBufferPoolSize, int maxBufferSize)
        {
            if (maxBufferPoolSize == 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maxBufferPoolSize));
            }

            Debug.Assert(maxBufferPoolSize > 0 && maxBufferSize >= 0, "bad params, caller should verify");
            return new PooledBufferManager(maxBufferPoolSize, maxBufferSize);
        }

        internal class PooledBufferManager : UnmanagedBufferManager
        {
            private const int MinBufferSize = 128;
            private const int MaxMissesBeforeTuning = 8;
            private const int InitialBufferCount = 1;
            private bool _areQuotasBeingTuned;
            private readonly BufferPool[] _bufferPools;

            private readonly int[] _bufferSizes;
#if DEBUG && !FEATURE_NETNATIVE
            private readonly ConcurrentDictionary<int, string> _buffersPooled = new ConcurrentDictionary<int, string>();
#endif //DEBUG
            private long _memoryLimit;
            private long _remainingMemory;
            private int _totalMisses;
            private readonly object _tuningLock;

            public PooledBufferManager(long maxMemoryToPool, int maxBufferSize)
            {
                _tuningLock = new object();
                _memoryLimit = maxMemoryToPool;
                _remainingMemory = maxMemoryToPool;
                var bufferPoolList = new List<BufferPool>();

                for (var bufferSize = MinBufferSize;;)
                {
                    var bufferCountLong = _remainingMemory / bufferSize;

                    var bufferCount = bufferCountLong > int.MaxValue
                        ? int.MaxValue
                        : (int) bufferCountLong;

                    if (bufferCount > InitialBufferCount)
                    {
                        bufferCount = InitialBufferCount;
                    }

                    bufferPoolList.Add(BufferPool.CreatePool(bufferSize, bufferCount));

                    _remainingMemory -= (long) bufferCount * bufferSize;

                    if (bufferSize >= maxBufferSize)
                    {
                        break;
                    }

                    var newBufferSizeLong = (long) bufferSize * 2;

                    if (newBufferSizeLong > maxBufferSize)
                    {
                        bufferSize = maxBufferSize;
                    }
                    else
                    {
                        bufferSize = (int) newBufferSizeLong;
                    }
                }

                _bufferPools = bufferPoolList.ToArray();
                _bufferSizes = new int[_bufferPools.Length];
                for (var i = 0; i < _bufferPools.Length; i++)
                {
                    _bufferSizes[i] = _bufferPools[i]
                        .BufferSize;
                }
            }

            public override void Clear()
            {
#if DEBUG && !FEATURE_NETNATIVE
                _buffersPooled.Clear();
#endif //DEBUG

                for (var i = 0; i < _bufferPools.Length; i++)
                {
                    var bufferPool = _bufferPools[i];
                    bufferPool.Clear();
                }
            }

            private void ChangeQuota(ref BufferPool bufferPool, int delta)
            {
                var oldBufferPool = bufferPool;
                var newLimit = oldBufferPool.Limit + delta;
                var newBufferPool = BufferPool.CreatePool(oldBufferPool.BufferSize, newLimit);
                for (var i = 0; i < newLimit; i++)
                {
                    var buffer = oldBufferPool.Take();
                    if (buffer.Data == null)
                    {
                        break;
                    }

                    newBufferPool.Return(buffer);
                    newBufferPool.IncrementCount();
                }

                _remainingMemory -= oldBufferPool.BufferSize * delta;
                bufferPool = newBufferPool;
            }

            private void DecreaseQuota(ref BufferPool bufferPool)
            {
                ChangeQuota(ref bufferPool, -1);
            }

            private int FindMostExcessivePool()
            {
                long maxBytesInExcess = 0;
                var index = -1;

                for (var i = 0; i < _bufferPools.Length; i++)
                {
                    var bufferPool = _bufferPools[i];

                    if (bufferPool.Peak >= bufferPool.Limit)
                    {
                        continue;
                    }

                    var bytesInExcess = (bufferPool.Limit - bufferPool.Peak) * (long) bufferPool.BufferSize;

                    if (bytesInExcess <= maxBytesInExcess)
                    {
                        continue;
                    }

                    index = i;
                    maxBytesInExcess = bytesInExcess;
                }

                return index;
            }

            private int FindMostStarvedPool()
            {
                long maxBytesMissed = 0;
                var index = -1;

                for (var i = 0; i < _bufferPools.Length; i++)
                {
                    var bufferPool = _bufferPools[i];

                    if (bufferPool.Peak != bufferPool.Limit)
                    {
                        continue;
                    }

                    var bytesMissed = bufferPool.Misses * (long) bufferPool.BufferSize;

                    if (bytesMissed <= maxBytesMissed)
                    {
                        continue;
                    }

                    index = i;
                    maxBytesMissed = bytesMissed;
                }

                return index;
            }

            private BufferPool FindPool(int desiredBufferSize)
            {
                for (var i = 0; i < _bufferSizes.Length; i++)
                {
                    if (desiredBufferSize <= _bufferSizes[i])
                    {
                        return _bufferPools[i];
                    }
                }

                return null;
            }

            private void IncreaseQuota(ref BufferPool bufferPool)
            {
                ChangeQuota(ref bufferPool, 1);
            }

            public override void ReturnBuffer(ByteBuffer buffer)
            {
                Debug.Assert(buffer.Data != null, "caller must verify");

                var bufferPool = FindPool(buffer.Length);
                if (bufferPool != null)
                {
                    if (buffer.Length != bufferPool.BufferSize)
                    {
                        throw new ArgumentException("Invalid buffer size", nameof(buffer));
                    }

                    if (bufferPool.Return(buffer))
                    {
                        bufferPool.IncrementCount();
                    }
                }
            }

            public override ByteBuffer TakeBuffer(int bufferSize)
            {
                Debug.Assert(bufferSize >= 0, "caller must ensure a non-negative argument");

                var bufferPool = FindPool(bufferSize);
                ByteBuffer returnValue;
                if (bufferPool != null)
                {
                    var buffer = bufferPool.Take();
                    if (buffer.Data != null)
                    {
                        bufferPool.DecrementCount();
                        returnValue = buffer;
                    }
                    else
                    {
                        if (bufferPool.Peak == bufferPool.Limit)
                        {
                            bufferPool.Misses++;
                            if (++_totalMisses >= MaxMissesBeforeTuning)
                            {
                                TuneQuotas();
                            }
                        }

                        var data = (byte*) Library.nn_allocmsg(bufferPool.BufferSize, 0);
                        returnValue = new ByteBuffer(data, bufferPool.BufferSize);
                    }
                }
                else
                {
                    var data = (byte*) Library.nn_allocmsg(bufferSize, 0);
                    returnValue = new ByteBuffer(data, bufferSize);
                }

#if DEBUG && !FEATURE_NETNATIVE
                _buffersPooled.TryRemove(returnValue.GetHashCode(), out _);
#endif //DEBUG

                return returnValue;
            }

            private void TuneQuotas()
            {
                if (_areQuotasBeingTuned)
                {
                    return;
                }

                var lockHeld = false;
                try
                {
                    Monitor.TryEnter(_tuningLock, ref lockHeld);

                    // Don't bother if another thread already has the lock
                    if (!lockHeld || _areQuotasBeingTuned)
                    {
                        return;
                    }

                    _areQuotasBeingTuned = true;
                }
                finally
                {
                    if (lockHeld)
                    {
                        Monitor.Exit(_tuningLock);
                    }
                }

                // find the "poorest" pool
                var starvedIndex = FindMostStarvedPool();
                if (starvedIndex >= 0)
                {
                    var starvedBufferPool = _bufferPools[starvedIndex];

                    if (_remainingMemory < starvedBufferPool.BufferSize)
                    {
                        // find the "richest" pool
                        var excessiveIndex = FindMostExcessivePool();
                        if (excessiveIndex >= 0)
                        {
                            // steal from the richest
                            DecreaseQuota(ref _bufferPools[excessiveIndex]);
                        }
                    }

                    if (_remainingMemory >= starvedBufferPool.BufferSize)
                    {
                        // give to the poorest
                        IncreaseQuota(ref _bufferPools[starvedIndex]);
                    }
                }

                // reset statistics
                for (var i = 0; i < _bufferPools.Length; i++)
                {
                    var bufferPool = _bufferPools[i];
                    bufferPool.Misses = 0;
                }

                _totalMisses = 0;
                _areQuotasBeingTuned = false;
            }

            internal abstract class BufferPool
            {
                private int _count;

                protected BufferPool(int bufferSize, int limit)
                {
                    BufferSize = bufferSize;
                    Limit = limit;
                }

                public int BufferSize { get; }

                public int Limit { get; }

                public int Misses { get; set; }

                public int Peak { get; private set; }

                public void Clear()
                {
                    OnClear();
                    _count = 0;
                }

                public void DecrementCount()
                {
                    var newValue = _count - 1;
                    if (newValue >= 0)
                    {
                        _count = newValue;
                    }
                }

                public void IncrementCount()
                {
                    var newValue = _count + 1;
                    if (newValue > Limit)
                    {
                        return;
                    }

                    _count = newValue;
                    if (newValue > Peak)
                    {
                        Peak = newValue;
                    }
                }

                internal abstract ByteBuffer Take();
                internal abstract bool Return(ByteBuffer buffer);
                internal abstract void OnClear();

                internal static BufferPool CreatePool(int bufferSize, int limit)
                {
                    // To avoid many buffer drops during training of large objects which
                    // get allocated on the LOH, we use the LargeBufferPool and for 
                    // bufferSize < 85000, the SynchronizedPool. However if bufferSize < 85000
                    // and (bufferSize + array-overhead) > 85000, this would still use 
                    // the SynchronizedPool even though it is allocated on the LOH.
                    if (bufferSize < 85000)
                    {
                        return new SynchronizedBufferPool(bufferSize, limit);
                    }

                    return new LargeBufferPool(bufferSize, limit);
                }

                internal class SynchronizedBufferPool : BufferPool
                {
                    private readonly SynchronizedPool<ByteBuffer> _innerPool;

                    internal SynchronizedBufferPool(int bufferSize, int limit)
                        : base(bufferSize, limit)
                    {
                        _innerPool = new SynchronizedPool<ByteBuffer>(limit);
                    }

                    internal override void OnClear()
                    {
                        _innerPool.Clear(b => Library.nn_freemsg(b.Data));
                    }

                    internal override ByteBuffer Take()
                    {
                        return _innerPool.Take();
                    }

                    internal override bool Return(ByteBuffer buffer)
                    {
                        return _innerPool.Return(buffer);
                    }
                }

                internal class LargeBufferPool : BufferPool
                {
                    private readonly Stack<ByteBuffer> _items;

                    internal LargeBufferPool(int bufferSize, int limit)
                        : base(bufferSize, limit)
                    {
                        _items = new Stack<ByteBuffer>(limit);
                    }

                    private object ThisLock
                    {
                        get => _items;
                    }

                    internal override void OnClear()
                    {
                        lock (ThisLock)
                        {
                            foreach (var item in _items)
                            {
                                if (item.NotNull)
                                {
                                    Library.nn_freemsg(item.Data);
                                }
                            }
                            _items.Clear();
                        }
                    }

                    internal override ByteBuffer Take()
                    {
                        lock (ThisLock)
                        {
                            if (_items.Count > 0)
                            {
                                return _items.Pop();
                            }
                        }

                        return default;
                    }

                    internal override bool Return(ByteBuffer buffer)
                    {
                        lock (ThisLock)
                        {
                            if (_items.Count >= Limit)
                            {
                                return false;
                            }

                            _items.Push(buffer);
                            return true;
                        }
                    }
                }
            }
        }
    }
}