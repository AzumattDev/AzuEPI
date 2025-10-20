namespace AzuEPI.Game.Compatibility;

public class WeightBase
{
    public static void CheckWeightBase()
    {
        if (!Chainloader.PluginInfos.TryGetValue("MadBuffoon.WeightBase", out var WbInfo)) return;
        WbInstalled = true;
    }
}