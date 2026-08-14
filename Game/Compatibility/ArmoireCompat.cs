namespace AzuEPI.Game.Compatibility;

public static class ArmoireCompat
{
    private const string ArmoireGuid = "advize.Armoire";

    public static void CheckForArmoire()
    {
        if (!IsArmoireInstalled)
        {
            return;
        }

        VanityOption.Value = Off;
        context.Config.Save();
    }

    public static bool IsArmoireInstalled => Chainloader.PluginInfos.ContainsKey(ArmoireGuid);
}