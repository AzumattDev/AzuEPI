namespace AzuEPI.Game.Compatibility;

public class BetterArchery
{
    private const string Baguid = "ishid4.mods.betterarchery";

    public static void CheckBetterArchery()
    {
        if (!Chainloader.PluginInfos.TryGetValue(Baguid, out PluginInfo? betterArchery)) return;

        if (betterArchery.Instance.Config.TryGetEntry("Quiver", "Enable Quiver", out ConfigEntry<bool>? entry) && entry.Value)
        {
            entry.Value = false;
            entry.SettingChanged += (_, _) =>
            {
                if (entry.Value) entry.Value = false;
            };
            betterArchery.Instance.Config.Save();
            AzuExtendedPlayerInventoryLogger.LogWarning(
                $"{Environment.NewLine}BetterArchery's quiver feature has been forcibly disabled to prevent potential issues with your inventory. " +
                $"Logging into your world now may cause you to lose all arrows in your quiver. {Environment.NewLine}If you accept this risk, please proceed. " +
                $"{Environment.NewLine}If you prefer to avoid any potential issues, please disable/remove {ModName}, restart the game, empty your quiver, remove " +
                $"the BetterArchery mod, and then reinstall {ModName}.");
        }

        Assembly baAssembly = betterArchery.Instance.GetType().Assembly;
        MethodInfo? onTakeAllSuccess = AccessTools.Method(typeof(TombStone), nameof(TombStone.OnTakeAllSuccess));
        if (onTakeAllSuccess == null)
        {
            AzuExtendedPlayerInventoryLogger.LogWarning("Could not find TombStone.OnTakeAllSuccess — skipping BetterArchery unpatch.");
            return;
        }

        HarmonyLib.Patches? patchInfo = Harmony.GetPatchInfo(onTakeAllSuccess);
        if (patchInfo == null)
        {
            AzuExtendedPlayerInventoryLogger.LogWarning("No Harmony patches found on TombStone.OnTakeAllSuccess — BetterArchery may not have applied its patch yet.");
            return;
        }

        foreach (Patch postfix in patchInfo.Postfixes.ToArray())
        {
            Assembly? patchAssembly = postfix.PatchMethod.DeclaringType?.Assembly;
            if (patchAssembly?.FullName != baAssembly.FullName) continue;
            context._harmony.Unpatch(onTakeAllSuccess, postfix.PatchMethod);
            AzuExtendedPlayerInventoryLogger.LogDebugDebug($"Removed BetterArchery patch on {onTakeAllSuccess.Name} | {postfix.PatchMethod.DeclaringType?.FullName}::{postfix.PatchMethod.Name}");
        }
    }
}

[HarmonyPatch(typeof(FejdStartup), nameof(FejdStartup.Start))]
static class FejdStartupStartPatch
{
    static void Postfix(FejdStartup __instance)
    {
        BetterArchery.CheckBetterArchery();
    }
}