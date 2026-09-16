using System;
using System.Runtime.CompilerServices;
using System.Text;

namespace ZeroPrimitives.Buffers
{
    /// <summary>
    /// High-performance, zero-allocation sequential binary writer over Span&lt;byte&gt;.
    /// Prevents buffer overruns and simplifies sequential serialization for network protocols and file formats.
    /// </summary>
    public ref struct SpanWriter
    {
        private readonly Span<byte> _buffer;
        private int _pos;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public SpanWriter(Span<byte> buffer)
        {
            _buffer = buffer;
            _pos = 0;
        }

        public int Position
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _pos;
        }

        public int BytesWritten
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _pos;
        }

        public int Capacity
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _buffer.Length;
        }

        public int Remaining
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _buffer.Length - _pos;
        }

        public ReadOnlySpan<byte> WrittenSpan
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _buffer.Slice(0, _pos);
        }

        public Span<byte> RemainingSpan
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _buffer.Slice(_pos);
        }

        #region Byte Operations

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteByte(byte value)
        {
            if (_pos >= _buffer.Length)
                throw new IndexOutOfRangeException("SpanWriter destination buffer is full.");
            _buffer[_pos++] = value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryWriteByte(byte value)
        {
            if (_pos < _buffer.Length)
            {
                _buffer[_pos++] = value;
                return true;
            }
            return false;
        }

        #endregion

        #region Little-Endian Primitives

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteInt16LittleEndian(short value)
        {
            FastBinary.WriteInt16LittleEndian(_buffer.Slice(_pos), value);
            _pos += 2;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteUInt16LittleEndian(ushort value)
        {
            FastBinary.WriteUInt16LittleEndian(_buffer.Slice(_pos), value);
            _pos += 2;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteInt32LittleEndian(int value)
        {
            FastBinary.WriteInt32LittleEndian(_buffer.Slice(_pos), value);
            _pos += 4;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteUInt32LittleEndian(uint value)
        {
            FastBinary.WriteUInt32LittleEndian(_buffer.Slice(_pos), value);
            _pos += 4;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteInt64LittleEndian(long value)
        {
            FastBinary.WriteInt64LittleEndian(_buffer.Slice(_pos), value);
            _pos += 8;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteUInt64LittleEndian(ulong value)
        {
            FastBinary.WriteUInt64LittleEndian(_buffer.Slice(_pos), value);
            _pos += 8;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteSingleLittleEndian(float value)
        {
            FastBinary.WriteSingleLittleEndian(_buffer.Slice(_pos), value);
            _pos += 4;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteDoubleLittleEndian(double value)
        {
            FastBinary.WriteDoubleLittleEndian(_buffer.Slice(_pos), value);
            _pos += 8;
        }

        #endregion

        #region Big-Endian Primitives

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteInt16BigEndian(short value)
        {
            FastBinary.WriteInt16BigEndian(_buffer.Slice(_pos), value);
            _pos += 2;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteUInt16BigEndian(ushort value)
        {
            FastBinary.WriteUInt16BigEndian(_buffer.Slice(_pos), value);
            _pos += 2;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteInt32BigEndian(int value)
        {
            FastBinary.WriteInt32BigEndian(_buffer.Slice(_pos), value);
            _pos += 4;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteUInt32BigEndian(uint value)
        {
            FastBinary.WriteUInt32BigEndian(_buffer.Slice(_pos), value);
            _pos += 4;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteInt64BigEndian(long value)
        {
            FastBinary.WriteInt64BigEndian(_buffer.Slice(_pos), value);
            _pos += 8;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteUInt64BigEndian(ulong value)
        {
            FastBinary.WriteUInt64BigEndian(_buffer.Slice(_pos), value);
            _pos += 8;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteSingleBigEndian(float value)
        {
            FastBinary.WriteSingleBigEndian(_buffer.Slice(_pos), value);
            _pos += 4;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteDoubleBigEndian(double value)
        {
            FastBinary.WriteDoubleBigEndian(_buffer.Slice(_pos), value);
            _pos += 8;
        }

        #endregion

        #region Variable-Length Integers

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteVarUInt32(uint value)
        {
            VarIntCodec.WriteVarUInt32(_buffer.Slice(_pos), value, out int written);
            _pos += written;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteVarInt32(int value)
        {
            VarIntCodec.WriteVarInt32(_buffer.Slice(_pos), value, out int written);
            _pos += written;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteVarUInt64(ulong value)
        {
            VarIntCodec.WriteVarUInt64(_buffer.Slice(_pos), value, out int written);
            _pos += written;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteVarInt64(long value)
        {
            VarIntCodec.WriteVarInt64(_buffer.Slice(_pos), value, out int written);
            _pos += written;
        }

        #endregion

        #region Slices & Strings

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteBytes(ReadOnlySpan<byte> bytes)
        {
            if (bytes.Length > Remaining)
                throw new ArgumentOutOfRangeException(nameof(bytes), "Bytes to write exceed remaining buffer capacity.");
            bytes.CopyTo(_buffer.Slice(_pos));
            _pos += bytes.Length;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryWriteBytes(ReadOnlySpan<byte> bytes)
        {
            if (bytes.Length <= Remaining)
            {
                bytes.CopyTo(_buffer.Slice(_pos));
                _pos += bytes.Length;
                return true;
            }
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int WriteStringUtf8(ReadOnlySpan<char> chars)
        {
#if NET8_0_OR_GREATER
            int written = Encoding.UTF8.GetBytes(chars, RemainingSpan);
            _pos += written;
            return written;
#else
            string s = chars.ToString();
            byte[] bytes = Encoding.UTF8.GetBytes(s);
            WriteBytes(bytes);
            return bytes.Length;
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
        public void Reset()
        {
            _pos = 0;
        }

        #endregion
    }
}
