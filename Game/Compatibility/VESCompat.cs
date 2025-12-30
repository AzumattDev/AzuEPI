namespace AzuEPI.Game.Compatibility;

public static class VESCompat
{
    private const string VESGuid = "kg.ValheimEnchantmentSystem";

    public static bool IsVesInstalled => Chainloader.PluginInfos.ContainsKey(VESGuid);
}