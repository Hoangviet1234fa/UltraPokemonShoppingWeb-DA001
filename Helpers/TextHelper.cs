using System.Globalization;
using System.Text;

namespace UltraPokemon.Helpers;

public static class TextHelper
{
    /// <>Loại bỏ dấu tiếng Việt + chuyển chữ thường: "Hệ Lửa" -> "he lua".</>
    public static string Normalize(string? s)
    {
        if (string.IsNullOrWhiteSpace(s)) return string.Empty;
        var sb = new StringBuilder();
        foreach (var ch in s.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD))
        {
            if (CharUnicodeInfo.GetUnicodeCategory(ch) == UnicodeCategory.NonSpacingMark) continue;
            // [ĐÃ FIX] Khôi phục ký tự 'đ' chuẩn UTF-8 để chuyển thành 'd' khi tìm kiếm không dấu
            sb.Append(ch == 'đ' ? 'd' : ch);
        }
        return sb.ToString().Normalize(NormalizationForm.FormC);
    }

    /// <>Kiểm tra mọi từ khóa đều xuất hiện trong ít nhất một trường dữ liệu.</>
    public static bool Matches(string keyword, params string?[] fields)
    {
        var haystack = Normalize(string.Join(" ", fields));
        return Normalize(keyword)
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .All(w => haystack.Contains(w));
    }
}