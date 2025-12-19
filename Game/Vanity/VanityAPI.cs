using HarmonyLib;

namespace AzuEPI.Game.Vanity;

[HarmonyPatch(typeof(Player), nameof(Player.EquipInventoryItems))]
internal static class LoadVanityOnPlayerLoad
{
    [HarmonyPrefix]
    [HarmonyPriority(Priority.First)]
    static void Prefix(Player __instance)
    {
        if (__instance != Player.m_localPlayer) return;
        VanityAPI.LoadFromCustomData(__instance);
    }
}

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

        SaveToCustomData(Player.m_localPlayer);

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

        SaveToCustomData(Player.m_localPlayer);
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

        SaveToCustomData(Player.m_localPlayer);
        ve.UpdateVisuals();
        return true;
    }

    private static void SaveToCustomData(Player player)
    {
        if (!player || player.m_isLoading) return;
        VisEquipment? ve = player.m_visEquipment;
        if (!ve) return;

        ZDO? zdo = ve.m_nview?.GetZDO();
        if (zdo == null) return;

        string data = string.Join(":",
            zdo.GetInt(VanityZdoKeys.Helmet),
            zdo.GetInt(VanityZdoKeys.Chest),
            zdo.GetInt(VanityZdoKeys.Legs),
            zdo.GetInt(VanityZdoKeys.Shoulder),
            zdo.GetInt(VanityZdoKeys.ShoulderVariant),
            zdo.GetInt(VanityZdoKeys.Utility),
            zdo.GetInt(VanityZdoKeys.Trinket)
        );

        player.m_customData["AzuEPI.Vanity"] = data;
    }

    public static void LoadFromCustomData(Player player)
    {
        if (!player) return;
        VisEquipment? ve = player.m_visEquipment;
        if (!ve || !ve.m_nview) return;

        ZDO? zdo = ve.m_nview.GetZDO();
        if (zdo == null) return;

        if (!player.m_customData.TryGetValue("AzuEPI.Vanity", out string data))
            return;

        string[] parts = data.Split(':');
        if (parts.Length >= 6)
        {
            if (int.TryParse(parts[0], out int helmet)) zdo.Set(VanityZdoKeys.Helmet, helmet);
            if (int.TryParse(parts[1], out int chest)) zdo.Set(VanityZdoKeys.Chest, chest);
            if (int.TryParse(parts[2], out int legs)) zdo.Set(VanityZdoKeys.Legs, legs);
            if (int.TryParse(parts[3], out int shoulder)) zdo.Set(VanityZdoKeys.Shoulder, shoulder);
            if (int.TryParse(parts[4], out int shoulderVar)) zdo.Set(VanityZdoKeys.ShoulderVariant, shoulderVar);
            if (int.TryParse(parts[5], out int utility)) zdo.Set(VanityZdoKeys.Utility, utility);
            if (parts.Length >= 7 && int.TryParse(parts[6], out int trinket)) zdo.Set(VanityZdoKeys.Trinket, trinket);
        }
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
        int value = zdo?.GetInt(key) ?? 0;

        return value;
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