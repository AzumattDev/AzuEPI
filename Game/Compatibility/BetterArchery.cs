namespace AzuEPI.Game.Compatibility;

public class BetterArchery
{
    private const string Baguid = "ishid4.mods.betterarchery";

    public static void CheckBetterArchery()
    {
        if (!Chainloader.PluginInfos.TryGetValue(Baguid, out PluginInfo? betterArchery)) return;
        // Force disable the configuration for BetterArchery. Turn off the quiver
        bool tryGetEntry = betterArchery.Instance.Config.TryGetEntry("Quiver", "Enable Quiver", out ConfigEntry<bool>? entry);
        if (!tryGetEntry || !entry.Value) return;
        entry.Value = false;
        entry.SettingChanged += (obj, evntArgs) =>
        {
            if (entry is { Value: true })
                entry.Value = false;
        };
        AzuExtendedPlayerInventoryLogger.LogWarning(
            $"{Environment.NewLine}BetterArchery's quiver feature has been forcibly disabled to prevent potential issues with your inventory. " +
            $"Logging into your world now may cause you to lose all arrows in your quiver. {Environment.NewLine}If you accept this risk, please proceed. " +
            $"{Environment.NewLine}If you prefer to avoid any potential issues, please disable/remove {ModName}, restart the game, empty your quiver, remove " +
            $"the BetterArchery mod, and then reinstall {ModName}.");
        betterArchery.Instance.Config.Save();

        Assembly baAssembly = betterArchery.Instance.GetType().Assembly;
        MethodInfo onTakeAllSuccess = AccessTools.Method(typeof(TombStone), nameof(TombStone.OnTakeAllSuccess));
        HarmonyLib.Patches patchInfo = Harmony.GetPatchInfo(onTakeAllSuccess);

        foreach (Patch postfix in patchInfo.Postfixes.ToArray())
        {
            MethodInfo? patchedMethod = postfix.PatchMethod;
            Assembly? patchAssembly = patchedMethod.DeclaringType?.Assembly;
            if (!ReferenceEquals(patchAssembly, baAssembly))
                continue;
            context._harmony.Unpatch(onTakeAllSuccess, postfix.PatchMethod);
            AzuExtendedPlayerInventoryLogger.LogDebugDebug($"Removed patch on {onTakeAllSuccess.Name} | {patchedMethod?.DeclaringType?.FullName}::{patchedMethod?.Name}");
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