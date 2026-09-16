using System;
using System.Runtime.CompilerServices;
using System.Text;

namespace ZeroPrimitives.Buffers
{
    /// <summary>
    /// High-performance, zero-allocation sequential binary reader over ReadOnlySpan&lt;byte&gt;.
    /// Eliminates manual offset tracking and bounds bugs during network packet or binary stream parsing.
    /// </summary>
    public ref struct SpanReader
    {
        private readonly ReadOnlySpan<byte> _buffer;
        private int _pos;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public SpanReader(ReadOnlySpan<byte> buffer)
        {
            _buffer = buffer;
            _pos = 0;
        }

        public int Position
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _pos;
        }

        public int Length
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _buffer.Length;
        }

        public int Remaining
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _buffer.Length - _pos;
        }

        public bool HasRemaining
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _pos < _buffer.Length;
        }

        public ReadOnlySpan<byte> RemainingSpan
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _buffer.Slice(_pos);
        }

        public ReadOnlySpan<byte> ConsumedSpan
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _buffer.Slice(0, _pos);
        }

        #region Byte Operations

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public byte ReadByte()
        {
            if (_pos >= _buffer.Length)
                throw new IndexOutOfRangeException("SpanReader has reached end of buffer.");
            return _buffer[_pos++];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryReadByte(out byte value)
        {
            if (_pos < _buffer.Length)
            {
                value = _buffer[_pos++];
                return true;
            }
            value = 0;
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public byte PeekByte()
        {
            if (_pos >= _buffer.Length)
                throw new IndexOutOfRangeException("SpanReader has reached end of buffer.");
            return _buffer[_pos];
        }

        #endregion

        #region Little-Endian Primitives

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public short ReadInt16LittleEndian()
        {
            short val = FastBinary.ReadInt16LittleEndian(_buffer.Slice(_pos));
            _pos += 2;
            return val;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ushort ReadUInt16LittleEndian()
        {
            ushort val = FastBinary.ReadUInt16LittleEndian(_buffer.Slice(_pos));
            _pos += 2;
            return val;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int ReadInt32LittleEndian()
        {
            int val = FastBinary.ReadInt32LittleEndian(_buffer.Slice(_pos));
            _pos += 4;
            return val;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public uint ReadUInt32LittleEndian()
        {
            uint val = FastBinary.ReadUInt32LittleEndian(_buffer.Slice(_pos));
            _pos += 4;
            return val;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public long ReadInt64LittleEndian()
        {
            long val = FastBinary.ReadInt64LittleEndian(_buffer.Slice(_pos));
            _pos += 8;
            return val;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ulong ReadUInt64LittleEndian()
        {
            ulong val = FastBinary.ReadUInt64LittleEndian(_buffer.Slice(_pos));
            _pos += 8;
            return val;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float ReadSingleLittleEndian()
        {
            float val = FastBinary.ReadSingleLittleEndian(_buffer.Slice(_pos));
            _pos += 4;
            return val;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public double ReadDoubleLittleEndian()
        {
            double val = FastBinary.ReadDoubleLittleEndian(_buffer.Slice(_pos));
            _pos += 8;
            return val;
        }

        #endregion

        #region Big-Endian Primitives

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public short ReadInt16BigEndian()
        {
            short val = FastBinary.ReadInt16BigEndian(_buffer.Slice(_pos));
            _pos += 2;
            return val;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ushort ReadUInt16BigEndian()
        {
            ushort val = FastBinary.ReadUInt16BigEndian(_buffer.Slice(_pos));
            _pos += 2;
            return val;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int ReadInt32BigEndian()
        {
            int val = FastBinary.ReadInt32BigEndian(_buffer.Slice(_pos));
            _pos += 4;
            return val;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public uint ReadUInt32BigEndian()
        {
            uint val = FastBinary.ReadUInt32BigEndian(_buffer.Slice(_pos));
            _pos += 4;
            return val;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public long ReadInt64BigEndian()
        {
            long val = FastBinary.ReadInt64BigEndian(_buffer.Slice(_pos));
            _pos += 8;
            return val;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ulong ReadUInt64BigEndian()
        {
            ulong val = FastBinary.ReadUInt64BigEndian(_buffer.Slice(_pos));
            _pos += 8;
            return val;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float ReadSingleBigEndian()
        {
            float val = FastBinary.ReadSingleBigEndian(_buffer.Slice(_pos));
            _pos += 4;
            return val;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public double ReadDoubleBigEndian()
        {
            double val = FastBinary.ReadDoubleBigEndian(_buffer.Slice(_pos));
            _pos += 8;
            return val;
        }

        #endregion

        #region Variable-Length Integers

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public uint ReadVarUInt32()
        {
            if (!VarIntCodec.TryReadVarUInt32(_buffer.Slice(_pos), out uint val, out int bytesRead))
                throw new InvalidOperationException("Invalid VarUInt32 encoding in SpanReader.");
            _pos += bytesRead;
            return val;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int ReadVarInt32()
        {
            if (!VarIntCodec.TryReadVarInt32(_buffer.Slice(_pos), out int val, out int bytesRead))
                throw new InvalidOperationException("Invalid VarInt32 encoding in SpanReader.");
            _pos += bytesRead;
            return val;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ulong ReadVarUInt64()
        {
            if (!VarIntCodec.TryReadVarUInt64(_buffer.Slice(_pos), out ulong val, out int bytesRead))
                throw new InvalidOperationException("Invalid VarUInt64 encoding in SpanReader.");
            _pos += bytesRead;
            return val;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public long ReadVarInt64()
        {
            if (!VarIntCodec.TryReadVarInt64(_buffer.Slice(_pos), out long val, out int bytesRead))
                throw new InvalidOperationException("Invalid VarInt64 encoding in SpanReader.");
            _pos += bytesRead;
            return val;
        }

        #endregion

        #region Slices & Strings

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ReadOnlySpan<byte> ReadBytes(int count)
        {
            if (count < 0 || _pos + count > _buffer.Length)
                throw new ArgumentOutOfRangeException(nameof(count), "Requested byte count exceeds remaining buffer length.");
            var slice = _buffer.Slice(_pos, count);
            _pos += count;
            return slice;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryReadBytes(int count, out ReadOnlySpan<byte> bytes)
        {
            if (count >= 0 && _pos + count <= _buffer.Length)
            {
                bytes = _buffer.Slice(_pos, count);
                _pos += count;
                return true;
            }
            bytes = default;
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public string ReadStringUtf8(int byteLength)
        {
            var bytes = ReadBytes(byteLength);
#if NET8_0_OR_GREATER
            return Encoding.UTF8.GetString(bytes);
#else
            byte[] temp = bytes.ToArray();
            return Encoding.UTF8.GetString(temp, 0, temp.Length);
#endif
        }

        #endregion

        #region Navigation

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Advance(int count)
        {
            if (count < 0 || _pos + count > _buffer.Length)
                throw new ArgumentOutOfRangeException(nameof(count), "Cannot advance beyond buffer bounds.");
            _pos += count;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Rewind(int count)
        {
            if (count < 0 || _pos - count < 0)
                throw new ArgumentOutOfRangeException(nameof(count), "Cannot rewind before buffer start.");
            _pos -= count;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Reset()
        {
            _pos = 0;
        }

        #endregion
    }
}
