namespace AzuEPI.Vanity;

[HarmonyPatch(typeof(VisEquipment), nameof(VisEquipment.SetHelmetEquipped))]
internal static class Vanity_Helmet
{
    static void Prefix(VisEquipment __instance, ref int hash, ref int hairHash)
    {
        int v = VanityAPI.Get(__instance, VanityZdoKeys.Helmet);
        if (VanityAPI.IsHidden(v))
        {
            hash = 0;
            return;
        }

        if (v != 0) hash = v;
    }
}

[HarmonyPatch(typeof(VisEquipment), nameof(VisEquipment.SetChestEquipped))]
internal static class Vanity_Chest
{
    static void Prefix(VisEquipment __instance, ref int hash)
    {
        int v = VanityAPI.Get(__instance, VanityZdoKeys.Chest);
        if (VanityAPI.IsHidden(v))
        {
            hash = 0;
            return;
        }

        if (v != 0) hash = v;
    }
}

[HarmonyPatch(typeof(VisEquipment), nameof(VisEquipment.SetLegEquipped))]
internal static class Vanity_Legs
{
    static void Prefix(VisEquipment __instance, ref int hash)
    {
        int v = VanityAPI.Get(__instance, VanityZdoKeys.Legs);
        if (VanityAPI.IsHidden(v))
        {
            hash = 0;
            return;
        }

        if (v != 0) hash = v;
    }
}

[HarmonyPatch(typeof(VisEquipment), nameof(VisEquipment.SetShoulderEquipped))]
internal static class Vanity_Shoulder
{
    static void Prefix(VisEquipment __instance, ref int hash, ref int variant)
    {
        int v = VanityAPI.Get(__instance, VanityZdoKeys.Shoulder);
        if (VanityAPI.IsHidden(v))
        {
            hash = 0;
            variant = 0;
            return;
        }

        if (v != 0)
        {
            hash = v;
            variant = VanityAPI.GetVariant(__instance, VanityZdoKeys.ShoulderVariant);
        }
    }
}

[HarmonyPatch(typeof(VisEquipment), nameof(VisEquipment.SetUtilityEquipped))]
internal static class Vanity_Utility
{
    static void Prefix(VisEquipment __instance, ref int hash)
    {
        int v = VanityAPI.Get(__instance, VanityZdoKeys.Utility);
        if (VanityAPI.IsHidden(v))
        {
            hash = 0;
            return;
        }

        if (v != 0) hash = v;
    }
}

[HarmonyPatch(typeof(VisEquipment), nameof(VisEquipment.SetTrinketEquipped))]
internal static class Vanity_Trinket
{
    static void Prefix(VisEquipment __instance, ref int hash)
    {
        int v = VanityAPI.Get(__instance, VanityZdoKeys.Trinket);
        if (VanityAPI.IsHidden(v))
        {
            hash = 0;
            return;
        }

        if (v != 0) hash = v;
    }
}