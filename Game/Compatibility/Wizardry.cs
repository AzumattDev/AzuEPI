namespace AzuEPI.Game.Compatibility;

public class WizardryCompat
{
    private static readonly HashSet<string> Rings = new(StringComparer.Ordinal)
    {
        "RingBlackForest_TW", "RingSwamp_TW", "RingMountain_TW", "RingPlains_TW", "RingMistlands_TW",
    };

    public static void Init()
    {
        if (!Chainloader.PluginInfos.TryGetValue("Therzie.Wizardry", out PluginInfo wizardryInfo))
            return;

        if (wizardryInfo?.Instance == null)
            return;

        API.AddSlot("$azuepi_fingerslot", [.. Rings]);
        context._harmony.PatchAll(typeof(Hunter_LegacyCompat));
    }
}
