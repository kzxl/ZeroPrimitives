using System;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;

namespace ZeroPrimitives.Memory
{
    /// <summary>
    /// High-throughput off-heap fixed-size slab allocator for zero-LOH, zero-GC streaming buffers.
    /// Manages a thread-safe pool of fixed-size unmanaged memory chunks (e.g., 2MB, 8MB, 32MB) 
    /// tailored for industrial camera frames, LiDAR point clouds, and audio spectra.
    /// </summary>
    public sealed unsafe class SlabAllocator : IDisposable
    {
        private readonly int _slabSize;
        private readonly int _maxSlabs;
        private readonly ConcurrentStack<NativeMemoryBlock> _freeStack;
        private readonly ConcurrentBag<IntPtr> _allAllocations;
        private int _allocatedSlabs;
        private int _disposed;

        /// <summary>
        /// Gets the fixed size in bytes of each slab chunk.
        /// </summary>
        public int SlabSize => _slabSize;

        /// <summary>
        /// Gets the maximum allowed number of slabs in this allocator.
        /// </summary>
        public int MaxSlabs => _maxSlabs;

        /// <summary>
        /// Gets the total number of slabs currently created.
        /// </summary>
        public int AllocatedSlabs => Volatile.Read(ref _allocatedSlabs);

        /// <summary>
        /// Gets the number of slabs currently available in the free pool.
        /// </summary>
        public int AvailableSlabs => _freeStack.Count;

        /// <summary>
        /// Gets whether this allocator has been disposed.
        /// </summary>
        public bool IsDisposed => Volatile.Read(ref _disposed) != 0;

        /// <summary>
        /// Initializes a new instance of <see cref="SlabAllocator"/>.
        /// </summary>
        /// <param name="slabSize">Size in bytes of each slab chunk (e.g., 2 * 1024 * 1024 for 2MB).</param>
        /// <param name="initialSlabs">Number of slabs to pre-allocate immediately off-heap.</param>
        /// <param name="maxSlabs">Maximum number of slabs allowed before blocking or rejecting.</param>
        public SlabAllocator(int slabSize, int initialSlabs = 4, int maxSlabs = 64)
        {
            if (slabSize <= 0)
                throw new ArgumentOutOfRangeException(nameof(slabSize), "Slab size must be positive.");
            if (initialSlabs < 0)
                throw new ArgumentOutOfRangeException(nameof(initialSlabs), "Initial slabs cannot be negative.");
            if (maxSlabs <= 0 || maxSlabs < initialSlabs)
                throw new ArgumentOutOfRangeException(nameof(maxSlabs), "Max slabs must be greater than or equal to initial slabs.");

            _slabSize = slabSize;
            _maxSlabs = maxSlabs;
            _freeStack = new ConcurrentStack<NativeMemoryBlock>();
            _allAllocations = new ConcurrentBag<IntPtr>();

            for (int i = 0; i < initialSlabs; i++)
            {
                CreateAndPushSlab();
            }
        }

        private NativeMemoryBlock CreateAndPushSlab()
        {
            if (Interlocked.Increment(ref _allocatedSlabs) > _maxSlabs)
            {
                Interlocked.Decrement(ref _allocatedSlabs);
                throw new InvalidOperationException($"SlabAllocator capacity limit reached ({_maxSlabs} slabs of {_slabSize} bytes).");
            }

#if NET8_0_OR_GREATER
            byte* ptr = (byte*)NativeMemory.Alloc((nuint)_slabSize);
#else
            byte* ptr = (byte*)Marshal.AllocHGlobal(_slabSize).ToPointer();
#endif
            _allAllocations.Add((IntPtr)ptr);

            var block = new NativeMemoryBlock(ptr, _slabSize, _slabSize, ReturnSlab, ownsMemory: false);
            _freeStack.Push(block);
            return block;
        }

        /// <summary>
        /// Rents an unmanaged slab from the pool with at least <paramref name="minimumBytes"/>.
        /// </summary>
        /// <param name="minimumBytes">Minimum required payload size.</param>
        /// <returns>A recycled or freshly allocated <see cref="NativeMemoryBlock"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public NativeMemoryBlock Rent(int minimumBytes = 0)
        {
            ThrowIfDisposed();

            if (minimumBytes > _slabSize)
            {
                throw new ArgumentOutOfRangeException(nameof(minimumBytes), 
                    $"Requested size {minimumBytes} bytes exceeds fixed slab size {_slabSize} bytes.");
            }

            int activeLength = minimumBytes > 0 ? minimumBytes : _slabSize;

            if (_freeStack.TryPop(out var block))
            {
                block.Reset(activeLength);
                return block;
            }

            // Allocate a new slab if under max limit
            int currentAllocated = Volatile.Read(ref _allocatedSlabs);
            if (currentAllocated < _maxSlabs)
            {
                var newBlock = CreateAndPushSlab();
                if (_freeStack.TryPop(out block))
                {
                    block.Reset(activeLength);
                    return block;
                }
            }

            throw new InvalidOperationException($"All {_maxSlabs} slabs are currently rented out. Consider increasing maxSlabs or disposing rented blocks faster.");
        }

        /// <summary>
        /// Callback invoked by <see cref="NativeMemoryBlock.Dispose"/> to recycle the slab.
        /// </summary>
        private void ReturnSlab(NativeMemoryBlock block)
        {
            if (IsDisposed)
                return;

            _freeStack.Push(block);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ThrowIfDisposed()
        {
            if (IsDisposed)
                throw new ObjectDisposedException(nameof(SlabAllocator), "The slab allocator has already been disposed.");
        }

        /// <summary>
        /// Disposes all slabs and frees the underlying unmanaged memory chunks.
        /// </summary>
        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) != 0)
                return;

            _freeStack.Clear();

            foreach (var ptr in _allAllocations)
            {
                if (ptr != IntPtr.Zero)
                {
#if NET8_0_OR_GREATER
                    NativeMemory.Free((void*)ptr);
#else
                    Marshal.FreeHGlobal(ptr);
#endif
                }
            }
        }
    }
}
