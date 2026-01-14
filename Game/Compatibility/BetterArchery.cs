namespace AzuEPI.Game.Compatibility;

public class BetterArchery
{
    public static void CheckBetterArchery()
    {
        if (!Chainloader.PluginInfos.TryGetValue("ishid4.mods.betterarchery", out PluginInfo? betterArchery)) return;
        // Force disable the configuration for BetterArchery. Turn off the quiver
        bool tryGetEntry = betterArchery.Instance.Config.TryGetEntry("Quiver", "Enable Quiver", out ConfigEntry<bool>? entry);
        if (!tryGetEntry || !entry.Value) return;
        entry.Value = false;
        AzuExtendedPlayerInventoryLogger.LogWarning(
            $"{Environment.NewLine}BetterArchery's quiver feature has been forcibly disabled to prevent potential issues with your inventory. " +
            $"Logging into your world now may cause you to lose all arrows in your quiver. {Environment.NewLine}If you accept this risk, please proceed. " +
            $"{Environment.NewLine}If you prefer to avoid any potential issues, please disable/remove {ModName}, restart the game, empty your quiver, remove " +
            $"the BetterArchery mod, and then reinstall {ModName}.");
        betterArchery.Instance.Config.Save();
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