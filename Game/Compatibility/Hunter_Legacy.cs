namespace AzuEPI.Game.Compatibility;

public class Hunter_LegacyCompat
{
    private static readonly HashSet<string> Quivers = new(StringComparer.Ordinal)
    {
        "ArmorFrostpeakHarrierCapeDO", "ArmorGoldenStriderCapeDO", "ArmorSapphireFalconCapeDO", "ArmorAshenExileCapeDO", "ArmorDemonHunterCapeDO",
    };

    public static void Init()
    {
        if (!Chainloader.PluginInfos.TryGetValue("Dreanegade.Hunter_Legacy", out PluginInfo hunterLegacyInfo))
            return;

        if (hunterLegacyInfo?.Instance == null)
            return;

        API.AddSlot("$bbh_slot_quiver", [.. Quivers]);
        context._harmony.PatchAll(typeof(Hunter_LegacyCompat));
    }
}
