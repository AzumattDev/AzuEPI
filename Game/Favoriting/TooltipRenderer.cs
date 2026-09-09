using System.Text;

namespace AzuEPI.Game.Favoriting;

[HarmonyPatch(typeof(ItemDrop.ItemData))]
internal static class TooltipRenderer
{
    private static bool Prepare() => !FavoritingMode.IsExternalFavoritingModLoaded();

    [HarmonyPatch(nameof(ItemDrop.ItemData.GetTooltip), [
        typeof(ItemDrop.ItemData),
        typeof(int),
        typeof(bool),
        typeof(float),
        typeof(int),
        typeof(bool),
    ])]
    [HarmonyPostfix]
    public static void GetTooltip(ItemDrop.ItemData item, bool crafting, ref string __result)
    {
        if (crafting || !DisplayTooltipHint.Value || !Player.m_localPlayer)
        {
            return;
        }
        StringBuilder stringBuilder = new StringBuilder(256);
        stringBuilder.Append(__result);

        UserConfig conf = UserConfig.GetPlayerConfig(Player.m_localPlayer.GetPlayerID());

        bool itemFavorited = conf.IsItemNameFavorited(item.m_shared);
        bool slotFavorited = conf.IsSlotFavorited(item.m_gridPos);

        if (itemFavorited && slotFavorited)
        {
            string? color = ColorUtility.ToHtmlStringRGB(BorderColorFavoritedItemOnFavoritedSlot.Value);

            stringBuilder.Append($"{Environment.NewLine}<color=#{color}>{ItemOnFavoritedSlotTooltip.Value}</color>");
        }
        else if (itemFavorited)
        {
            string? color = ColorUtility.ToHtmlStringRGB(BorderColorFavoritedItem.Value);

            stringBuilder.Append($"{Environment.NewLine}<color=#{color}>{FavoritedItemTooltip.Value}</color>");
        }
        else if (slotFavorited)
        {
            string? color = ColorUtility.ToHtmlStringRGB(BorderColorFavoritedSlot.Value);

            stringBuilder.Append($"{Environment.NewLine}<color=#{color}>{FavoritedSlotTooltip.Value}</color>");
        }
        __result = stringBuilder.ToString();
    }
}
