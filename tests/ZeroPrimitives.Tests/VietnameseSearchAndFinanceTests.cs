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
        public void VietnameseSearchNormalizer_RemoveDiacritics_PreservesCasingAndPunctuation()
        {
            string input = "Công Ty Cổ Phần Sài Gòn & Đắk Lắk (TP.HCM)!";
            string expected = "Cong Ty Co Phan Sai Gon & Dak Lak (TP.HCM)!";

            string result = VietnameseSearchNormalizer.RemoveDiacritics(input);
            Assert.Equal(expected, result);
        }

        [Fact]
        public void VietnameseSearchNormalizer_UnSignedTransfer_ReplacesSpecialsAndSpaces()
        {
            string input = "Phiếu Nhập Kho #123 (Vật Tư)!";
            // ' ' -> '_', '#' -> '-', '(' -> '-', ')' -> '-', '!' -> '-'
            string result = VietnameseSearchNormalizer.UnSignedTransfer(input);
            Assert.Equal("Phieu_Nhap_Kho_-123_-Vat_Tu--", result);
        }

        [Fact]
        public void VietnameseSearchNormalizer_LongString_UsesArrayPoolCorrectly()
        {
            // String longer than 256 chars to test the ArrayPool fallback branch
            string longPart = "Đơn hàng sản xuất kiểm định chất lượng cao cho nhà máy số 1 ";
            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < 10; i++) sb.Append(longPart);
            string longInput = sb.ToString();

            string result = VietnameseSearchNormalizer.RemoveDiacritics(longInput);
            Assert.DoesNotContain("Đ", result);
            Assert.DoesNotContain("ả", result);
            Assert.DoesNotContain("ế", result);
            Assert.StartsWith("Don hang san xuat", result);
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
