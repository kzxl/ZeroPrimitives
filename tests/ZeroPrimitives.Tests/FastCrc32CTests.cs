using System;
using System.Text;
using Xunit;
using ZeroPrimitives.Cryptography;

namespace ZeroPrimitives.Tests
{
    public class FastCrc32CTests
    {
        [Fact]
        public void Crc32C_ComputesKnownTestVectors()
        {
            // Standard Castagnoli test vector: "123456789" -> 0xE3069283
            byte[] bytes = Encoding.ASCII.GetBytes("123456789");
            uint crc = FastCrc.Crc32C(bytes);

            Assert.Equal(0xE3069283u, crc);
        }

        [Fact]
        public void Crc32C_EmptyData_ReturnsZero()
        {
            Assert.Equal(0u, FastCrc.Crc32C(ReadOnlySpan<byte>.Empty));
        }

        [Fact]
        public void Crc32C_HardwareOrSoftware_ProducesConsistentResult()
        {
            byte[] buffer = new byte[1024];
            for (int i = 0; i < buffer.Length; i++) buffer[i] = (byte)(i & 0xFF);

            uint crc1 = FastCrc.Crc32C(buffer);
            uint crc2 = FastCrc.Crc32C(buffer);

            Assert.Equal(crc1, crc2);
            Assert.NotEqual(0u, crc1);
        }
    }
}
