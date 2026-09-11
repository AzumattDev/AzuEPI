namespace AzuEPI.Game.Patches;

[HarmonyPatch]
internal class PreventKeys
{
    [HarmonyPatch(typeof(ZInput), nameof(ZInput.TryGetKeyStateLowLevel))]
    [HarmonyPatch(typeof(ZInput), nameof(ZInput.TryGetButtonState))]
    [HarmonyPatch(typeof(ZInput), nameof(ZInput.GetKeyDown))]
    static void Postfix(ref bool __result)
    {
        if (!Player.m_localPlayer || !Player.m_localPlayer.TakeInput()) return;
        foreach (ConfigEntry<KeyboardShortcut> hotkey in Hotkeys)
        {
            if (hotkey.Value.IsKeyDown())
            {
                __result = false;
                return;
            }
        }
    }
}