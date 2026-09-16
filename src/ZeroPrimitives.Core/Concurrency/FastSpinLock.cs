using System;
using System.Runtime.CompilerServices;
using System.Threading;

namespace ZeroPrimitives.Concurrency
{
    /// <summary>
    /// Ultra-lightweight (1-word, 4 bytes) spin-wait lock for sub-microsecond critical sections.
    /// Eliminates OS thread scheduling, kernel transitions, and object header overhead of standard Monitor / lock(obj).
    /// </summary>
    public struct FastSpinLock
    {
        private int _state; // 0 = unlocked, 1 = locked

        public bool IsHeld
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Volatile.Read(ref _state) != 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Enter()
        {
            // Fast path: acquire immediately if uncontended
            if (Interlocked.CompareExchange(ref _state, 1, 0) == 0)
                return;

            EnterContended();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void EnterContended()
        {
            var spinner = new SpinWait();
            while (true)
            {
                // Wait while lock is held to avoid cache line bouncing from continuous CompareExchange
                while (Volatile.Read(ref _state) != 0)
                {
                    spinner.SpinOnce();
                }

                if (Interlocked.CompareExchange(ref _state, 1, 0) == 0)
                    return;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryEnter()
        {
            return Interlocked.CompareExchange(ref _state, 1, 0) == 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Exit()
        {
            Volatile.Write(ref _state, 0);
        }
    }
}
