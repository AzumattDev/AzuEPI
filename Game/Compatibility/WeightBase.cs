namespace AzuEPI.Game.Compatibility;

public class WeightBase
{
    public static void CheckWeightBase()
    {
        if (!Chainloader.PluginInfos.TryGetValue("MadBuffoon.WeightBase", out PluginInfo? WbInfo)) return;
        WbInstalled = true;
    }
}