using EpicLootAPI;

namespace AzuEPI.Game.Compatibility;

public static class EpicLootCompat
{
    private static readonly string[] FingerSlotItems = ["Andvaranaut", "GoldRubyRing", "SilverRing"];

    private static bool _registered;

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
        _registered = true;
    }

    /// <summary>
    /// Our quick access bar replaces <see cref="HotkeyBar.UpdateIcons"/> wholesale, which skips the
    /// transpiler Epic Loot decorates it with, so the rarity background has to be reapplied by hand.
    /// </summary>
    internal static void ApplyItemBackground(GameObject slotRoot, GameObject equippedOverlay, ItemDrop.ItemData? item)
    {
        if (!_registered) return;
        EpicLoot.ApplyMagicItemBackground(slotRoot, equippedOverlay, item, false);
    }

    /// <summary>
    /// Our equipment provider reports items by grid position, but Epic Loot only recomputes on vanilla
    /// equip/unequip. Call whenever slot contents move without one of those.
    /// </summary>
    internal static void NotifySlotsChanged()
    {
        if (!_registered || Player.m_localPlayer == null) return;
        Player.m_localPlayer.InvalidatePlayerEffectCache();
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
            // m_equipped, not just occupancy: unequipping happens before the item is dragged out of the
            // cell, and reporting it until the drag lands would keep its effects and aura alive.
            if (inv.GetItemAt(snap.GridPos.x, snap.GridPos.y) is { m_equipped: true } item && !equipped.Contains(item))
                equipped.Add(item);
        }

        return equipped;
    }
}
