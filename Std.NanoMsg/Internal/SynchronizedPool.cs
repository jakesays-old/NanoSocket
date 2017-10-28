//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Security;
using System.Threading;

namespace Std.NanoMsg.Internal
{
    // A simple synchronized pool would simply lock a stack and push/pop on return/take.
    //
    // This implementation tries to reduce locking by exploiting the case where an item
    // is taken and returned by the same thread, which turns out to be common in our 
    // scenarios.  
    //
    // Initially, all the quota is allocated to a global (non-thread-specific) pool, 
    // which takes locks.  As different threads take and return values, we record their IDs, 
    // and if we detect that a thread is taking and returning "enough" on the same thread, 
    // then we decide to "promote" the thread.  When a thread is promoted, we decrease the 
    // quota of the global pool by one, and allocate a thread-specific entry for the thread 
    // to store it's value.  Once this entry is allocated, the thread can take and return 
    // it's value from that entry without taking any locks.  Not only does this avoid 
    // locks, but it affinitizes pooled items to a particular thread.
    //
    // There are a couple of additional things worth noting:
    // 
    // It is possible for a thread that we have reserved an entry for to exit.  This means
    // we will still have a entry allocated for it, but the pooled item stored there 
    // will never be used.  After a while, we could end up with a number of these, and 
    // as a result we would begin to exhaust the quota of the overall pool.  To mitigate this
    // case, we throw away the entire per-thread pool, and return all the quota back to 
    // the global pool if we are unable to promote a thread (due to lack of space).  Then 
    // the set of active threads will be re-promoted as they take and return items.
    // 
    // You may notice that the code does not immediately promote a thread, and does not
    // immediately throw away the entire per-thread pool when it is unable to promote a 
    // thread.  Instead, it uses counters (based on the number of calls to the pool) 
    // and a threshold to figure out when to do these operations.  In the case where the
    // pool to misconfigured to have too few items for the workload, this avoids constant 
    // promoting and rebuilding of the per thread entries.
    //
    // You may also notice that we do not use interlocked methods when adjusting statistics.
    // Since the statistics are a heuristic as to how often something is happening, they 
    // do not need to be perfect.
    // 
    internal class SynchronizedPool<TObject>
//        where TObject : class
    {
        private const int MaxPendingEntries = 128;
        private const int MaxPromotionFailures = 64;
        private const int MaxReturnsBeforePromotion = 64;
        private const int MaxThreadItemsPerProcessor = 16;
        private Entry[] _entries;
        private readonly GlobalPool _globalPool;
        private readonly int _maxCount;
        private PendingEntry[] _pending;
        private int _promotionFailures;

        public SynchronizedPool(int maxCount)
        {
            var threadCount = maxCount;
            var maxThreadCount = MaxThreadItemsPerProcessor + SynchronizedPoolHelper._processorCount;
            if (threadCount > maxThreadCount)
            {
                threadCount = maxThreadCount;
            }
            _maxCount = maxCount;
            _entries = new Entry[threadCount];
            _pending = new PendingEntry[4];
            _globalPool = new GlobalPool(maxCount);
        }

        private object ThisLock
        {
            get => this;
        }

        public void Clear(Action<TObject> dealloc = null)
        {
            var entries = _entries;

            for (var i = 0; i < entries.Length; i++)
            {
                if (entries[i]
                    .HasValue)
                {
                    dealloc?.Invoke(entries[i].Value);
                }
                entries[i].Clear();
            }

            _globalPool.Clear();
        }

        private void HandlePromotionFailure(int thisThreadId)
        {
            var newPromotionFailures = _promotionFailures + 1;

            if (newPromotionFailures >= MaxPromotionFailures)
            {
                lock (ThisLock)
                {
                    _entries = new Entry[_entries.Length];

                    _globalPool.MaxCount = _maxCount;
                }

                PromoteThread(thisThreadId);
            }
            else
            {
                _promotionFailures = newPromotionFailures;
            }
        }

        private bool PromoteThread(int thisThreadId)
        {
            lock (ThisLock)
            {
                for (var i = 0; i < _entries.Length; i++)
                {
                    var threadId = _entries[i]
                        .ThreadId;

                    if (threadId == thisThreadId)
                    {
                        return true;
                    }

                    if (threadId == 0)
                    {
                        _globalPool.DecrementMaxCount();
                        _entries[i]
                            .ThreadId = thisThreadId;
                        return true;
                    }
                }
            }

            return false;
        }

        private void RecordReturnToGlobalPool(int thisThreadId)
        {
            var localPending = _pending;

            for (var i = 0; i < localPending.Length; i++)
            {
                var threadId = localPending[i]
                    .ThreadId;

                if (threadId == thisThreadId)
                {
                    var newReturnCount = localPending[i]
                            .ReturnCount +
                        1;

                    if (newReturnCount >= MaxReturnsBeforePromotion)
                    {
                        localPending[i]
                            .ReturnCount = 0;

                        if (!PromoteThread(thisThreadId))
                        {
                            HandlePromotionFailure(thisThreadId);
                        }
                    }
                    else
                    {
                        localPending[i]
                            .ReturnCount = newReturnCount;
                    }
                    break;
                }

                if (threadId == 0)
                {
                    break;
                }
            }
        }

        private void RecordTakeFromGlobalPool(int thisThreadId)
        {
            var localPending = _pending;

            for (var i = 0; i < localPending.Length; i++)
            {
                var threadId = localPending[i]
                    .ThreadId;

                if (threadId == thisThreadId)
                {
                    return;
                }

                if (threadId == 0)
                {
                    lock (localPending)
                    {
                        if (localPending[i]
                                .ThreadId ==
                            0)
                        {
                            localPending[i]
                                .ThreadId = thisThreadId;
                            return;
                        }
                    }
                }
            }

            if (localPending.Length >= MaxPendingEntries)
            {
                _pending = new PendingEntry[localPending.Length];
            }
            else
            {
                var newPending = new PendingEntry[localPending.Length * 2];
                Array.Copy(localPending, newPending, localPending.Length);
                _pending = newPending;
            }
        }

        public bool Return(TObject value)
        {
            var thisThreadId = Thread.CurrentThread.ManagedThreadId;

            if (thisThreadId == 0)
            {
                return false;
            }

            if (ReturnToPerThreadPool(thisThreadId, value))
            {
                return true;
            }

            return ReturnToGlobalPool(thisThreadId, value);
        }

        private bool ReturnToPerThreadPool(int thisThreadId, TObject value)
        {
            var entries = _entries;

            for (var i = 0; i < entries.Length; i++)
            {
                var threadId = entries[i]
                    .ThreadId;

                if (threadId == thisThreadId)
                {
                    if (entries[i]
                        .HasValue)
                    {
                        return false;
                    }

                    entries[i].Set(value);
                    return true;
                }

                if (threadId == 0)
                {
                    break;
                }
            }

            return false;
        }

        private bool ReturnToGlobalPool(int thisThreadId, TObject value)
        {
            RecordReturnToGlobalPool(thisThreadId);

            return _globalPool.Return(value);
        }

        public TObject Take()
        {
            var thisThreadId = Thread.CurrentThread.ManagedThreadId;

            if (thisThreadId == 0)
            {
                return default;
            }

            var value = TakeFromPerThreadPool(thisThreadId);

            if (value != null)
            {
                return value;
            }

            return TakeFromGlobalPool(thisThreadId);
        }

        private TObject TakeFromPerThreadPool(int thisThreadId)
        {
            var entries = _entries;

            for (var i = 0; i < entries.Length; i++)
            {
                var threadId = entries[i]
                    .ThreadId;

                if (threadId == thisThreadId)
                {
                    if (!entries[i]
                        .HasValue)
                    {
                        return default;
                    }

                    var value = entries[i]
                        .Value;
                    entries[i].Clear();
                    return value;
                }

                if (threadId == 0)
                {
                    break;
                }
            }

            return default;
        }

        private TObject TakeFromGlobalPool(int thisThreadId)
        {
            RecordTakeFromGlobalPool(thisThreadId);

            return _globalPool.Take();
        }

        private struct Entry
        {
            public bool HasValue;
            public int ThreadId;
            public TObject Value;

            public void Set(TObject value)
            {
                Value = value;
                HasValue = true;
            }

            public void Clear()
            {
                Value = default;
                HasValue = false;
            }
        }

        private struct PendingEntry
        {
            public int ReturnCount;
            public int ThreadId;
        }

        private static class SynchronizedPoolHelper
        {
            public static readonly int _processorCount = GetProcessorCount();

            [SecuritySafeCritical]
            private static int GetProcessorCount()
            {
                return Environment.ProcessorCount;
            }
        }

        private class GlobalPool
        {
            private readonly Stack<TObject> _items;

            private int _maxCount;

            public GlobalPool(int maxCount)
            {
                _items = new Stack<TObject>();
                _maxCount = maxCount;
            }

            public int MaxCount
            {
                get => _maxCount;
                set
                {
                    lock (ThisLock)
                    {
                        while (_items.Count > value)
                        {
                            _items.Pop();
                        }

                        _maxCount = value;
                    }
                }
            }

            private object ThisLock
            {
                get => this;
            }

            public void DecrementMaxCount()
            {
                lock (ThisLock)
                {
                    if (_items.Count == _maxCount)
                    {
                        _items.Pop();
                    }
                    _maxCount--;
                }
            }

            public TObject Take()
            {
                if (_items.Count <= 0)
                {
                    return default;
                }

                lock (ThisLock)
                {
                    if (_items.Count > 0)
                    {
                        return _items.Pop();
                    }
                }

                return default;
            }

            public bool Return(TObject value)
            {
                if (_items.Count >= MaxCount)
                {
                    return false;
                }

                lock (ThisLock)
                {
                    if (_items.Count >= MaxCount)
                    {
                        return false;
                    }

                    _items.Push(value);
                    return true;
                }
            }

            public void Clear()
            {
                lock (ThisLock)
                {
                    _items.Clear();
                }
            }
        }
    }
}