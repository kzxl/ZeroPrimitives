using System;
using Xunit;
using ZeroPrimitives.Validation;
using ZeroPrimitives.Finance;

namespace ZeroPrimitives.Tests
{
    public class VietnameseSearchAndFinanceTests
    {
        [Fact]
        public void VietnameseSearchNormalizer_RemovesDiacriticsAndNormalizesSpaces()
        {
            string raw = "  Đơn Hàng / Bán Lẻ - Mã Phiếu: 12345 (Hà Nội)  ";
            string searchKey = VietnameseSearchNormalizer.ToSearchKeyword(raw);

            Assert.Equal("don hang ban le ma phieu 12345 ha noi", searchKey);

            string slug = VietnameseSearchNormalizer.ToSlug(raw);
            Assert.Equal("don-hang-ban-le-ma-phieu-12345-ha-noi", slug);
        }

        [Fact]
        public void VietnameseSearchNormalizer_SpanOverload_ZeroAllocates()
        {
            ReadOnlySpan<char> input = "Phiếu Nhập Kho Thành Phẩm".AsSpan();
            Span<char> buffer = stackalloc char[input.Length];

            int written = VietnameseSearchNormalizer.NormalizeForSearch(input, buffer);
            string result = buffer.Slice(0, written).ToString();

            Assert.Equal("phieu nhap kho thanh pham", result);
        }

        [Fact]
        public void VnFinancialRounding_CalculatesVatAndVndRoundingAccurately()
        {
            decimal rawAmount = 1500250.60m;
            decimal rounded = VnFinancialRounding.RoundVnd(rawAmount);
            Assert.Equal(1500251m, rounded);

            // 10% VAT
            decimal vat10 = VnFinancialRounding.CalculateVat(1000000m, 10m);
            Assert.Equal(100000m, vat10);

            // 8% VAT with fractional decimal
            decimal subTotal = 1234567m;
            decimal vat8 = VnFinancialRounding.CalculateVat(subTotal, 8m);
            Assert.Equal(98765m, vat8); // 1234567 * 0.08 = 98765.36 -> 98765
        }
    }
}
