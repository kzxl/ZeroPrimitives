using System;
using System.Runtime.CompilerServices;

namespace ZeroPrimitives.Validation
{
    /// <summary>
    /// High-performance, zero-allocation Vietnamese search normalizer and SEO slug generator.
    /// Converts diacritical Vietnamese text into canonical normalized search tokens or URLs directly on stack spans.
    /// </summary>
    public static class VietnameseSearchNormalizer
    {
        /// <summary>
        /// Normalizes Vietnamese text into unaccented, lowercase search tokens directly into destination span.
        /// Replaces 'Đ'/'đ' with 'd', strips tone marks, removes special symbols, and condenses spaces.
        /// Zero heap allocation.
        /// </summary>
        /// <param name="source">Raw input text span.</param>
        /// <param name="destination">Output buffer span.</param>
        /// <returns>Number of characters written to destination.</returns>
        public static int NormalizeForSearch(ReadOnlySpan<char> source, Span<char> destination)
        {
            if (source.IsEmpty || destination.IsEmpty) return 0;

            int written = 0;
            bool lastWasSpace = false;

            for (int i = 0; i < source.Length && written < destination.Length; i++)
            {
                char c = source[i];

                if (c == 'Đ' || c == 'đ')
                {
                    c = 'd';
                }
                else
                {
                    c = StripDiacritic(c);
                }

                // Lowercase
                if (c >= 'A' && c <= 'Z')
                {
                    c = (char)(c + 32);
                }

                // Keep alphanumeric
                if ((c >= 'a' && c <= 'z') || (c >= '0' && c <= '9'))
                {
                    destination[written++] = c;
                    lastWasSpace = false;
                }
                else if ((char.IsWhiteSpace(c) || c == '_' || c == '-') && !lastWasSpace && written > 0)
                {
                    destination[written++] = ' ';
                    lastWasSpace = true;
                }
            }

            // Remove trailing space
            if (written > 0 && destination[written - 1] == ' ')
            {
                written--;
            }

            return written;
        }

        /// <summary>
        /// Converts Vietnamese string into an unaccented, lowercase search string.
        /// </summary>
        public static string ToSearchKeyword(string? source)
        {
            if (string.IsNullOrEmpty(source)) return string.Empty;

            var span = source.AsSpan();
            if (span.Length <= 256)
            {
                Span<char> buffer = stackalloc char[span.Length];
                int written = NormalizeForSearch(span, buffer);
                return buffer.Slice(0, written).ToString();
            }

            char[] rented = System.Buffers.ArrayPool<char>.Shared.Rent(span.Length);
            try
            {
                int written = NormalizeForSearch(span, rented);
                return new string(rented, 0, written);
            }
            finally
            {
                System.Buffers.ArrayPool<char>.Shared.Return(rented);
            }
        }

        /// <summary>
        /// Generates a clean, SEO-friendly slug with hyphen separators (e.g. "don-hang-so-123").
        /// Zero heap allocation.
        /// </summary>
        public static int ToSlug(ReadOnlySpan<char> source, Span<char> destination)
        {
            if (source.IsEmpty || destination.IsEmpty) return 0;

            int written = 0;
            bool lastWasHyphen = false;

            for (int i = 0; i < source.Length && written < destination.Length; i++)
            {
                char c = source[i];

                if (c == 'Đ' || c == 'đ')
                {
                    c = 'd';
                }
                else
                {
                    c = StripDiacritic(c);
                }

                // Lowercase
                if (c >= 'A' && c <= 'Z')
                {
                    c = (char)(c + 32);
                }

                if ((c >= 'a' && c <= 'z') || (c >= '0' && c <= '9'))
                {
                    destination[written++] = c;
                    lastWasHyphen = false;
                }
                else if ((char.IsWhiteSpace(c) || c == '-' || c == '_' || c == '/') && !lastWasHyphen && written > 0)
                {
                    destination[written++] = '-';
                    lastWasHyphen = true;
                }
            }

            if (written > 0 && destination[written - 1] == '-')
            {
                written--;
            }

            return written;
        }

        /// <summary>
        /// Converts string into a clean SEO slug.
        /// </summary>
        public static string ToSlug(string? source)
        {
            if (string.IsNullOrEmpty(source)) return string.Empty;

            var span = source.AsSpan();
            if (span.Length <= 256)
            {
                Span<char> buffer = stackalloc char[span.Length];
                int written = ToSlug(span, buffer);
                return buffer.Slice(0, written).ToString();
            }

            char[] rented = System.Buffers.ArrayPool<char>.Shared.Rent(span.Length);
            try
            {
                int written = ToSlug(span, rented);
                return new string(rented, 0, written);
            }
            finally
            {
                System.Buffers.ArrayPool<char>.Shared.Return(rented);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static char StripDiacritic(char c)
        {
            switch (c)
            {
                case 'á': case 'à': case 'ả': case 'ã': case 'ạ':
                case 'ă': case 'ắ': case 'ằ': case 'ẳ': case 'ẵ': case 'ặ':
                case 'â': case 'ấ': case 'ầ': case 'ẩ': case 'ẫ': case 'ậ':
                case 'Á': case 'À': case 'Ả': case 'Ã': case 'Ạ':
                case 'Ă': case 'Ắ': case 'Ằ': case 'Ẳ': case 'Ẵ': case 'Ặ':
                case 'Â': case 'Ấ': case 'Ầ': case 'Ẩ': case 'Ẫ': case 'Ậ':
                    return 'a';

                case 'é': case 'è': case 'ẻ': case 'ẽ': case 'ẹ':
                case 'ê': case 'ế': case 'ề': case 'ể': case 'ễ': case 'ệ':
                case 'É': case 'È': case 'Ẻ': case 'Ẽ': case 'Ẹ':
                case 'Ê': case 'Ế': case 'Ề': case 'Ể': case 'Ễ': case 'Ệ':
                    return 'e';

                case 'í': case 'ì': case 'ỉ': case 'ĩ': case 'ị':
                case 'Í': case 'Ì': case 'Ỉ': case 'Ĩ': case 'Ị':
                    return 'i';

                case 'ó': case 'ò': case 'ỏ': case 'õ': case 'ọ':
                case 'ô': case 'ố': case 'ồ': case 'ổ': case 'ỗ': case 'ộ':
                case 'ơ': case 'ớ': case 'ờ': case 'ở': case 'ỡ': case 'ợ':
                case 'Ó': case 'Ò': case 'Ỏ': case 'Õ': case 'Ọ':
                case 'Ô': case 'Ố': case 'Ồ': case 'Ổ': case 'Ỗ': case 'Ộ':
                case 'Ơ': case 'Ớ': case 'Ờ': case 'Ở': case 'Ỡ': case 'Ợ':
                    return 'o';

                case 'ú': case 'ù': case 'ủ': case 'ũ': case 'ụ':
                case 'ư': case 'ứ': case 'ừ': case 'ử': case 'ữ': case 'ự':
                case 'Ú': case 'Ù': case 'Ủ': case 'Ũ': case 'Ụ':
                case 'Ư': case 'Ứ': case 'Ừ': case 'Ử': case 'Ữ': case 'Ự':
                    return 'u';

                case 'ý': case 'ỳ': case 'ỷ': case 'ỹ': case 'ỵ':
                case 'Ý': case 'Ỳ': case 'Ỷ': case 'Ỹ': case 'Ỵ':
                    return 'y';

                default:
                    return c;
            }
        }
    }
}
