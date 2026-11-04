using System.Text.Json;

namespace UltraPokemon.Helpers;

/// <summary>Giỏ hàng lưu trong Session: productId -> số lượng.</summary>
public static class CartSession
{
    private const string Key = "CART";

    public static Dictionary<int, int> Get(ISession session)
    {
        var json = session.GetString(Key);
        return string.IsNullOrEmpty(json)
            ? new Dictionary<int, int>()
            : JsonSerializer.Deserialize<Dictionary<int, int>>(json) ?? new Dictionary<int, int>();
    }

    public static void Save(ISession session, Dictionary<int, int> cart) =>
        session.SetString(Key, JsonSerializer.Serialize(cart));

    public static int Count(ISession session) => Get(session).Values.Sum();
}
