using EpicLootAPI;

namespace AzuEPI.Game.Compatibility;

public static class EpicLootCompat
{
    private static readonly string[] FingerSlotItems = ["Andvaranaut", "GoldRubyRing", "SilverRing"];

    public static void Init()
    {
        if (!Chainloader.PluginInfos.TryGetValue("randyknapp.mods.epicloot", out PluginInfo? epicLoot) || epicLoot?.Instance == null)
            return;

        if (!IsSlotMarkedForRemoval("$azuepi_fingerslot"))
            API.AddSlot("$azuepi_fingerslot", FingerSlotItems);

        if (!EpicLoot.IsLoaded())
        {
            AzuExtendedPlayerInventoryLogger.LogWarning("Epic Loot is installed but exposes no API. Items in this mod's slots will not count towards magic effects. Update Epic Loot.");
            return;
        }

        EpicLoot.RegisterEquipmentProvider(ModGUID, GetSlotEquipment);
        EpicLoot.RegisterSacrificeFilter(ModGUID, CanSacrifice);
    }

    private static bool CanSacrifice(ItemDrop.ItemData item)
    {
        Inventory? inv = Player.m_localPlayer == null ? null : Player.m_localPlayer.GetInventory();
        if (item == null || inv == null) return true;

        if (!API.TryGetSlotIndexAtGridPos(inv, item.m_gridPos, out int slotIndex)) return true;
        if (!API.TryGetSlotDescriptor(slotIndex, out SlotDescriptor slot) || !slot.IsQuickSlot) return true;

        // Grid positions repeat across inventories, so confirm this is the instance actually in the slot.
        return !ReferenceEquals(inv.GetItemAt(item.m_gridPos.x, item.m_gridPos.y), item);
    }

    private static List<ItemDrop.ItemData> GetSlotEquipment(Player player)
    {
        List<ItemDrop.ItemData> equipped = [];

        Inventory? inv = player == Player.m_localPlayer ? player?.GetInventory() : null;
        if (inv == null) return equipped;

        foreach (SlotSnapshot snap in API.GetEquipmentSlotSnapshots(inv))
        {
            if (inv.GetItemAt(snap.GridPos.x, snap.GridPos.y) is { } item && !equipped.Contains(item))
                equipped.Add(item);
        }

        return equipped;
    }
}
