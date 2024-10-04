using HarmonyLib;

namespace AzuExtendedPlayerInventory.EPI.Patches;

public class HudPatches
{
    [HarmonyPatch(typeof(Hud), nameof(Hud.Awake))]
    private static class HudAwakePatch
    {
        private static void Postfix(Hud __instance)
        {
            if (AzuExtendedPlayerInventoryPlugin.AddEquipmentRow.Value.IsOff())
                return;
            
            API.HudAwake(__instance);

            ExtendedPlayerInventory.QuickSlots.CreateBar();

            API.HudAwakeComplete(__instance);
        }
    }

    [HarmonyPatch(typeof(Hud), nameof(Hud.Update))]
    private static class HudUpdatePatch
    {
        private static void Postfix(Hud __instance)
        {
            if (AzuExtendedPlayerInventoryPlugin.AddEquipmentRow.Value.IsOff() || Player.m_localPlayer == null)
                return;
            
            API.HudUpdate(__instance);

            ExtendedPlayerInventory.QuickSlots.UpdateHotkeyBars();

            ExtendedPlayerInventory.QuickSlots.UpdatePosition();

            ExtendedPlayerInventory.QuickSlots.UpdateDrag();

            API.HudUpdateComplete(__instance);
        }
    }

    [HarmonyPatch(typeof(Hud), nameof(Hud.OnDestroy))]
    private static class HudOnDestroyPatch
    {
        private static void Postfix() => ExtendedPlayerInventory.QuickSlots.ClearBars();
    }
}