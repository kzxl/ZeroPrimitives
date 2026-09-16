using System;
using System.Runtime.CompilerServices;
using System.Threading;

#pragma warning disable CS0169 // Intentionally unused cache-line padding fields

namespace ZeroPrimitives.Concurrency
{
    /// <summary>
    /// High-throughput, lock-free Single-Producer Single-Consumer (SPSC) bounded FIFO queue.
    /// Incorporates CPU cache-line padding to prevent False Sharing (L1/L2 cache-line thrashing) between producer and consumer threads.
    /// Delivers tens of millions of operations per second with zero memory allocations during Enqueue/Dequeue.
    /// </summary>
    /// <typeparam name="T">Element type.</typeparam>
    public sealed class SpscQueue<T>
    {
        private readonly T[] _buffer;
        private readonly int _mask;

        // Cache line padding before head (prevent false sharing with object header and buffer ref)
        private long _pad0, _pad1, _pad2, _pad3, _pad4, _pad5, _pad6;

        // Consumed position - primarily updated by Consumer thread
        private int _head;

        // Cache line padding between head and tail (guarantees head and tail reside on distinct 64-byte cache lines)
        private long _pad7, _pad8, _pad9, _pad10, _pad11, _pad12, _pad13;

        // Produced position - primarily updated by Producer thread
        private int _tail;

        // Cache line padding after tail
        private long _pad14, _pad15, _pad16, _pad17, _pad18, _pad19, _pad20;

        /// <summary>
        /// Initializes a new SPSC queue with a power-of-two capacity.
        /// </summary>
        /// <param name="capacityPowerOfTwo">Capacity (must be power of two: 16, 32, 64, 1024, 65536...).</param>
        public SpscQueue(int capacityPowerOfTwo)
        {
            if (capacityPowerOfTwo < 2) capacityPowerOfTwo = 2;

            // Round up to nearest power of two if not already
            if ((capacityPowerOfTwo & (capacityPowerOfTwo - 1)) != 0)
            {
                int p = 1;
                while (p < capacityPowerOfTwo) p <<= 1;
                capacityPowerOfTwo = p;
            }

            _buffer = new T[capacityPowerOfTwo];
            _mask = capacityPowerOfTwo - 1;
            _head = 0;
            _tail = 0;
        }

        public int Capacity
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _buffer.Length;
        }

        public int Count
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                int count = Volatile.Read(ref _tail) - Volatile.Read(ref _head);
                return count > 0 ? count : 0;
            }
        }

        public bool IsEmpty
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Volatile.Read(ref _head) == Volatile.Read(ref _tail);
        }

        /// <summary>
        /// Attempts to enqueue an item. MUST be called by only ONE producer thread.
        /// Zero allocation.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryEnqueue(T item)
        {
            int tail = _tail;
            int head = Volatile.Read(ref _head);

            if (tail - head < _buffer.Length)
            {
                _buffer[tail & _mask] = item;
                Volatile.Write(ref _tail, tail + 1);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Attempts to dequeue an item. MUST be called by only ONE consumer thread.
        /// Zero allocation.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryDequeue(out T item)
        {
            int head = _head;
            int tail = Volatile.Read(ref _tail);

            if (head != tail)
            {
                int index = head & _mask;
                item = _buffer[index];
                _buffer[index] = default!; // Allow GC collection of reference types
                Volatile.Write(ref _head, head + 1);
                return true;
            }

            item = default!;
            return false;
        }

        /// <summary>
        /// Peeks at the item at the head of the queue without removing it.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryPeek(out T item)
        {
            int head = _head;
            int tail = Volatile.Read(ref _tail);

            if (head != tail)
            {
                item = _buffer[head & _mask];
                return true;
            }

            item = default!;
            return false;
        }
    }
}
