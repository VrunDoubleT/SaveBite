using System.Globalization;
using System.Text;

namespace SaveBite.Backend.Helpers;

public static class VietnameseTextHelper
{
    /// <summary>
    /// Normalizes Unicode to Form C (precomposed) to resolve NFC vs NFD mismatch.
    /// </summary>
    public static string NormalizeNfc(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return string.Empty;
        return text.Trim().Normalize(NormalizationForm.FormC);
    }

    /// <summary>
    /// Removes Vietnamese diacritical marks (accents) and folds 'đ'/'Đ' to 'd'/'D'.
    /// Returns lowercase trimmed unaccented text.
    /// </summary>
    public static string RemoveDiacritics(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return string.Empty;

        var normalizedString = text.Trim().Normalize(NormalizationForm.FormD);
        var stringBuilder = new StringBuilder(normalizedString.Length);

        foreach (var c in normalizedString)
        {
            var unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(c);
            if (unicodeCategory != UnicodeCategory.NonSpacingMark)
            {
                stringBuilder.Append(c);
            }
        }

        return stringBuilder
            .ToString()
            .Normalize(NormalizationForm.FormC)
            .Replace('đ', 'd')
            .Replace('Đ', 'D')
            .Trim()
            .ToLowerInvariant();
    }
}
