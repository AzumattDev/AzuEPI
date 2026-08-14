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

        if (!IsSlotMarkedForRemoval("$azuepi_fingerslot"))
        {
            API.AddSlot("$azuepi_fingerslot", new List<string>(EpicLootEquipmentItems).ToArray());
        }
        context._harmony.PatchAll(typeof(EpicLootCompat));
    }

    [HarmonyPatch("EpicLoot.PlayerExtensions, EpicLoot", "GetEquipment"), HarmonyPostfix, HarmonyPriority(Priority.Last)]
    public static void EpicLootGetEquipment(ref List<ItemDrop.ItemData> __result)
    {
        Player? player = Player.m_localPlayer;
        Inventory? inv = player?.GetInventory();
        if (player == null || inv == null) return;
        foreach (SlotSnapshot snap in API.GetEquipmentSlotSnapshots(inv))
        {
            ItemDrop.ItemData? item = inv.GetItemAt(snap.GridPos.x, snap.GridPos.y);
            if (item != null && !__result.Contains(item))
                __result.Add(item);
        }
    }
}