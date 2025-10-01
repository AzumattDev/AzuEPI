namespace AzuEPI.Vanity;

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
    public static bool SetVanity(VisEquipment ve, VisSlot slot, string prefabName, int variant = 0)
    {
        if (!ve || ve.m_nview == null) return false;
        var zdo = ve.m_nview.GetZDO();
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

        ve.UpdateVisuals();
        return true;
    }

    public static bool ClearVanity(VisEquipment ve, VisSlot slot)
    {
        if (!ve || ve.m_nview == null) return false;
        var zdo = ve.m_nview.GetZDO();
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

    internal static int Get(VisEquipment ve, int key)
    {
        var zdo = ve?.m_nview?.GetZDO();
        return zdo?.GetInt(key) ?? 0;
    }

    internal static int GetVariant(VisEquipment ve, int key)
    {
        var zdo = ve?.m_nview?.GetZDO();
        return zdo?.GetInt(key) ?? 0;
    }
}