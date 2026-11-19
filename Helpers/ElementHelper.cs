namespace UltraPokemon.Helpers;

public static class ElementHelper
{
    /// <>Trả về màu sắc Bootstrap theo hệ nguyên tố: Lửa=đỏ, Nước=xanh dương, Sét=vàng, Đất=xanh lá.</>
    public static string Color(string? element)
    {
        var el = TextHelper.Normalize(element);
        return el.Contains("lua") || el.Contains("fire") ? "danger" :
               el.Contains("nuoc") || el.Contains("water") ? "primary" :
               el.Contains("set") || el.Contains("electric") ? "warning" :
               el.Contains("dat") || el.Contains("grass") || el.Contains("rock") ? "success" : "secondary";
    }

    public static string Button(string? element, bool active) =>
        active ? $"btn-{Color(element)}" : $"btn-outline-{Color(element)}";
}