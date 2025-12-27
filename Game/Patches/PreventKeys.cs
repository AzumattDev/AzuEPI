using AzuEPI.Core.Input;

namespace AzuEPI.Game.Patches;

[HarmonyPatch]
internal class PreventKeys
{
    [HarmonyPatch(typeof(ZInput), nameof(ZInput.TryGetKeyStateLowLevel))]
    [HarmonyPatch(typeof(ZInput), nameof(ZInput.TryGetButtonState))]
    [HarmonyPatch(typeof(ZInput), nameof(ZInput.GetKeyDown))]
    static void Postfix(ZInput __instance, ref bool __result)
    {
        foreach (ConfigEntry<KeyboardShortcut> hotkey in Hotkeys)
        {
            if (hotkey.Value.IsKeyDown() && Player.m_localPlayer && !Player.m_localPlayer.TakeInput())
            {
                __result = false;
            }
        }
    }
}