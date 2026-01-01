namespace AzuEPI.Game.Compatibility;

public class EpicLootCompat
{
    private static readonly HashSet<string> EpicLootEquipmentItems = new(StringComparer.Ordinal)
    {
        "Andvaranaut", "GoldRubyRing", "SilverRing"
    };

    public static void Init()
    {
        if (!Chainloader.PluginInfos.TryGetValue("randyknapp.mods.epicloot", out PluginInfo randyEl))
            return;

        if (randyEl?.Instance == null)
            return;

        API.AddSlot("$azuepi_fingerslot", new List<string>(EpicLootEquipmentItems).ToArray());
        context._harmony.PatchAll(typeof(EpicLootCompat));
    }

    [HarmonyPatch("EpicLoot.PlayerExtensions, EpicLoot", "GetEquipment"), HarmonyPostfix, HarmonyPriority(Priority.Last)]
    public static void EpicLootGetEquipment(ref List<ItemDrop.ItemData> __result)
    {
        Player? player = Player.m_localPlayer;
        if (player == null || player.GetInventory() == null) return;
        foreach (ItemDrop.ItemData equippedItem in player.GetInventory().GetEquippedItems())
        {
            if (!__result.Contains(equippedItem)) __result.Add(equippedItem);
        }
    }
}