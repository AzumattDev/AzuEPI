using AzuEPI.Core.Slots;

namespace AzuEPI.Game.Vanity;

internal static class VanityZdoKeys
{
    internal static readonly int Chest = "azu.vanity.chest".GetStableHashCode();
    internal static readonly int Legs = "azu.vanity.legs".GetStableHashCode();
    internal static readonly int Helmet = "azu.vanity.helmet".GetStableHashCode();
    internal static readonly int Shoulder = "azu.vanity.shoulder".GetStableHashCode();
    internal static readonly int Utility = "azu.vanity.utility".GetStableHashCode();
    internal static readonly int Trinket = "azu.vanity.trinket".GetStableHashCode();
    internal static readonly int ShoulderVariant = "azu.vanity.shoulder.variant".GetStableHashCode();
}

public static class VanityAPI
{
    public const int HIDE = int.MinValue;

    public static bool IsHidden(int hash) => hash == HIDE;

    public static bool SetVanity(VisEquipment ve, VisSlot slot, string prefabName, int variant = 0)
    {
        if (!ve || ve.m_nview == null) return false;
        ZDO? zdo = ve.m_nview.GetZDO();
        if (zdo == null || !ve.m_nview.IsOwner()) return false;
        int hash = string.IsNullOrEmpty(prefabName) ? 0 : prefabName.GetStableHashCode();
        AzuExtendedPlayerInventoryLogger.LogDebug($"Setting vanity {slot} to {prefabName} (hash {hash})");
        switch (slot)
        {
            case VisSlot.Chest: zdo.Set(VanityZdoKeys.Chest, hash); break;
            case VisSlot.Legs: zdo.Set(VanityZdoKeys.Legs, hash); break;
            case VisSlot.Helmet: zdo.Set(VanityZdoKeys.Helmet, hash); break;
            case VisSlot.Shoulder:
                zdo.Set(VanityZdoKeys.Shoulder, hash);
                zdo.Set(VanityZdoKeys.ShoulderVariant, variant);
                break;
            case VisSlot.Utility: zdo.Set(VanityZdoKeys.Utility, hash); break;
            default: return false;
        }

        ve.UpdateVisuals();
        return true;
    }

    public static bool ClearVanity(VisEquipment ve, VisSlot slot)
    {
        if (!ve || ve.m_nview == null) return false;
        ZDO? zdo = ve.m_nview.GetZDO();
        if (zdo == null || !ve.m_nview.IsOwner()) return false;
        switch (slot)
        {
            case VisSlot.Chest: zdo.Set(VanityZdoKeys.Chest, 0); break;
            case VisSlot.Legs: zdo.Set(VanityZdoKeys.Legs, 0); break;
            case VisSlot.Helmet: zdo.Set(VanityZdoKeys.Helmet, 0); break;
            case VisSlot.Shoulder:
                zdo.Set(VanityZdoKeys.Shoulder, 0);
                zdo.Set(VanityZdoKeys.ShoulderVariant, 0);
                break;
            case VisSlot.Utility: zdo.Set(VanityZdoKeys.Utility, 0); break;
            default: return false;
        }

        ve.UpdateVisuals();
        return true;
    }

    public static bool SetHidden(VisEquipment ve, VisSlot slot, bool hidden)
    {
        if (!ve || ve.m_nview == null) return false;
        ZDO? zdo = ve.m_nview.GetZDO();
        if (zdo == null || !ve.m_nview.IsOwner()) return false;
        int v = hidden ? HIDE : 0;
        switch (slot)
        {
            case VisSlot.Chest: zdo.Set(VanityZdoKeys.Chest, v); break;
            case VisSlot.Legs: zdo.Set(VanityZdoKeys.Legs, v); break;
            case VisSlot.Helmet: zdo.Set(VanityZdoKeys.Helmet, v); break;
            case VisSlot.Shoulder:
                zdo.Set(VanityZdoKeys.Shoulder, v);
                if (!hidden) zdo.Set(VanityZdoKeys.ShoulderVariant, 0);
                break;
            case VisSlot.Utility: zdo.Set(VanityZdoKeys.Utility, v); break;
            default: return false;
        }

        ve.UpdateVisuals();
        return true;
    }

    public static bool IsHidden(VisEquipment ve, VisSlot slot)
    {
        int v = slot switch
        {
            VisSlot.Chest => Get(ve, VanityZdoKeys.Chest),
            VisSlot.Legs => Get(ve, VanityZdoKeys.Legs),
            VisSlot.Helmet => Get(ve, VanityZdoKeys.Helmet),
            VisSlot.Shoulder => Get(ve, VanityZdoKeys.Shoulder),
            VisSlot.Utility => Get(ve, VanityZdoKeys.Utility),
            _ => 0
        };
        return IsHidden(v);
    }

    internal static int Get(VisEquipment ve, int key)
    {
        ZDO? zdo = ve?.m_nview?.GetZDO();
        return zdo?.GetInt(key) ?? 0;
    }

    internal static int GetVariant(VisEquipment ve, int key)
    {
        ZDO? zdo = ve?.m_nview?.GetZDO();
        return zdo?.GetInt(key) ?? 0;
    }

    public static VanityState GetAppliedVanity(Player player, VisSlot slot)
    {
        VisEquipment? ve = player ? player.m_visEquipment : null;
        return VanitySlots.GetState(ve, slot);
    }

    public static VanityState GetAppliedVanityForItem(Player player, ItemDrop.ItemData item)
    {
        if (!player || item?.m_shared == null) return new VanityState(false, false, 0, 0);
        if (!VanitySlots.TryMapItemTypeToVisSlot(item.m_shared.m_itemType, out VisSlot slot))
            return new VanityState(false, false, 0, 0);

        return GetAppliedVanity(player, slot);
    }

    public static bool TryGetEquippedVanityIcon(ItemDrop.ItemData item, out Sprite sprite)
    {
        sprite = null;

        Player? player = Player.m_localPlayer;
        if (!player || item == null || item.m_shared == null) return false;

        if (!item.m_equipped) return false;

        if (!VanitySlots.TryMapItemTypeToVisSlot(item.m_shared.m_itemType, out VisSlot slot))
            return false;

        VanityState vs = GetAppliedVanity(player, slot);
        InventoryGrid.Element? slotObject;
        try
        {
            slotObject = InventoryGui.instance.m_playerGrid.GetElement(item.m_gridPos.x, item.m_gridPos.y, player.GetInventory().GetWidth());
        }
        catch
        {
            return false;
        }

        if (slotObject == null) return false;

        SlotOverlays.ToggleVanityStateOverlay(slotObject.m_go, vs);
        return vs is { HasVanity: true, IsHidden: false } && VanityLookup.TryGetIcon(vs.Hash, vs.Variant, out sprite);
    }
}