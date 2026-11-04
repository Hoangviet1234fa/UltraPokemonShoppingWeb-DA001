using System.Globalization;
using System.Text;

namespace UltraPokemon.Helpers;

public static class TextHelper
{
    /// <summary>Bỏ dấu tiếng Việt + chuyển chữ thường: "Hệ Lửa" -> "he lua".</summary>
    public static string Normalize(string? s)
    {
        if (string.IsNullOrWhiteSpace(s)) return string.Empty;
        var sb = new StringBuilder();
        foreach (var ch in s.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD))
        {
            if (CharUnicodeInfo.GetUnicodeCategory(ch) == UnicodeCategory.NonSpacingMark) continue;
            sb.Append(ch == 'đ' ? 'd' : ch);
        }
        return sb.ToString().Normalize(NormalizationForm.FormC);
    }

    /// <summary>Mọi từ khóa đều phải xuất hiện trong ít nhất một trường.</summary>
    public static bool Matches(string keyword, params string?[] fields)
    {
        var haystack = Normalize(string.Join(" ", fields));
        return Normalize(keyword)
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .All(w => haystack.Contains(w));
    }
}
