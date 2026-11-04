using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Hosting;
using UltraPokemon.Models;

namespace UltraPokemon.Helpers;

public static class ImageHelper
{
    private static readonly string[] Exts = { ".png", ".jpg", ".jpeg", ".webp", ".gif" };

    public static string Slug(string? name) =>
        Regex.Replace(TextHelper.Normalize(name), "[^a-z0-9]+", "-").Trim('-');

    /// <summary>Ưu tiên: ImageUrl trong DB -> file wwwroot/images/{tên-thẻ}.ext -> ảnh mặc định theo hệ.</summary>
    public static string Resolve(IWebHostEnvironment env, Product p)
    {
        if (!string.IsNullOrWhiteSpace(p.ImageUrl)) return p.ImageUrl;

        var slug = Slug(p.Name);
        var dir = Path.Combine(env.WebRootPath ?? "", "images");
        if (slug.Length > 0 && Directory.Exists(dir))
        {
            foreach (var ext in Exts)
                if (File.Exists(Path.Combine(dir, slug + ext))) return $"/images/{slug}{ext}";
        }
        return Placeholder(p);
    }

    /// <summary>Thẻ mặc định có màu theo hệ (không dùng hình nhân vật có bản quyền).</summary>
    public static string Placeholder(Product p)
    {
        var el = TextHelper.Normalize(p.ElementType);
        var (c1, c2, icon) =
            el.Contains("lua")  ? ("#ff8a65", "#d84315", "🔥") :
            el.Contains("nuoc") ? ("#64b5f6", "#1565c0", "💧") :
            el.Contains("dat")  ? ("#a1887f", "#4e342e", "⛰️") :
            el.Contains("set")  ? ("#ffd54f", "#f57f17", "⚡") :
                                  ("#90a4ae", "#37474f", "🃏");
        var name = System.Security.SecurityElement.Escape(p.Name) ?? "";
        var element = System.Security.SecurityElement.Escape(p.ElementType) ?? "";
        var svg =
            "<svg xmlns='http://www.w3.org/2000/svg' width='300' height='400' viewBox='0 0 300 400'>" +
            $"<defs><linearGradient id='g' x1='0' y1='0' x2='1' y2='1'><stop offset='0' stop-color='{c1}'/><stop offset='1' stop-color='{c2}'/></linearGradient></defs>" +
            "<rect width='300' height='400' rx='18' fill='url(#g)'/>" +
            "<rect x='14' y='14' width='272' height='372' rx='12' fill='none' stroke='#fff' stroke-opacity='.65' stroke-width='4'/>" +
            $"<text x='150' y='215' font-size='110' text-anchor='middle'>{icon}</text>" +
            $"<text x='150' y='300' font-size='30' font-weight='bold' fill='#fff' text-anchor='middle' font-family='Arial,sans-serif'>{name}</text>" +
            $"<text x='150' y='340' font-size='20' fill='#fff' fill-opacity='.9' text-anchor='middle' font-family='Arial,sans-serif'>{element}</text>" +
            "</svg>";
        return "data:image/svg+xml;utf8," + Uri.EscapeDataString(svg);
    }
}
