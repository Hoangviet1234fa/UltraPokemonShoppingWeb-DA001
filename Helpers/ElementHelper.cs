namespace UltraPokemon.Helpers;

public static class ElementHelper
{
    /// <summary>Màu nút/badge theo hệ: Lửa=đỏ, Nước=xanh dương, Sét=vàng, Đất=xanh lá.</summary>
    public static string Color(string? element)
    {
        var el = TextHelper.Normalize(element);
        return el.Contains("lua")  ? "danger"  :
               el.Contains("nuoc") ? "primary" :
               el.Contains("set")  ? "warning" :
               el.Contains("dat")  ? "success" : "secondary";
    }

    public static string Button(string? element, bool active) =>
        active ? $"btn-{Color(element)}" : $"btn-outline-{Color(element)}";
}
