using AzuEPI.Game.PlayerPreview;

namespace AzuEPI.Game.Vanity;

internal static class VanityHelper
{
    internal static int GetVanityValue(VisEquipment ve, int key)
    {
        if (ve == AzuEPICharacterPanel.playerPreviewComp?.m_visEquipment)
        {
            VisEquipment? playerVe = Player.m_localPlayer?.m_visEquipment;
            if (playerVe != null)
            {
                int value = VanityAPI.Get(playerVe, key);
                AzuExtendedPlayerInventoryLogger.LogDebug($"[VANITY HELPER] Reading from PLAYER for preview - Key: {key}, Value: {value}");
                return value;
            }
            else
            {
                AzuExtendedPlayerInventoryLogger.LogDebug($"[VANITY HELPER] Preview detected but player is NULL!");
            }
        }

        return VanityAPI.Get(ve, key);
    }

    internal static int GetVanityVariant(VisEquipment ve, int key)
    {
        if (ve == AzuEPICharacterPanel.playerPreviewComp?.m_visEquipment)
        {
            VisEquipment? playerVe = Player.m_localPlayer?.m_visEquipment;
            if (playerVe != null)
                return VanityAPI.GetVariant(playerVe, key);
        }

        return VanityAPI.GetVariant(ve, key);
    }
}

[HarmonyPatch(typeof(VisEquipment), nameof(VisEquipment.SetHelmetEquipped))]
internal static class Vanity_Helmet
{
    static void Prefix(VisEquipment __instance, ref int hash, ref int hairHash)
    {
        int v = VanityHelper.GetVanityValue(__instance, VanityZdoKeys.Helmet);

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
        int v = VanityHelper.GetVanityValue(__instance, VanityZdoKeys.Chest);

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
        int v = VanityHelper.GetVanityValue(__instance, VanityZdoKeys.Legs);
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
        int v = VanityHelper.GetVanityValue(__instance, VanityZdoKeys.Shoulder);
        if (VanityAPI.IsHidden(v))
        {
            hash = 0;
            variant = 0;
            return;
        }

        if (v != 0)
        {
            hash = v;
            variant = VanityHelper.GetVanityVariant(__instance, VanityZdoKeys.ShoulderVariant);
        }
    }
}

[HarmonyPatch(typeof(VisEquipment), nameof(VisEquipment.SetUtilityEquipped))]
internal static class Vanity_Utility
{
    static void Prefix(VisEquipment __instance, ref int hash)
    {
        int v = VanityHelper.GetVanityValue(__instance, VanityZdoKeys.Utility);
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
        int v = VanityHelper.GetVanityValue(__instance, VanityZdoKeys.Trinket);
        if (VanityAPI.IsHidden(v))
        {
            hash = 0;
            return;
        }

        if (v != 0) hash = v;
    }
}