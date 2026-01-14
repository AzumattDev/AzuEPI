namespace AzuEPI.Game.Compatibility;

public class ZenUICompat
{
    private const string ZenUIGuid = "ZenDragon.ZenUI";

    public static void DisableEquipmentSlots()
    {
        if (!Chainloader.PluginInfos.TryGetValue(ZenUIGuid, out PluginInfo? ZenUI)) return;
        bool tryGetEntry = ZenUI.Instance.Config.TryGetEntry("Inventory Equip", "Assigned gear slots", out ConfigEntry<bool>? entry);
        if (!tryGetEntry || !entry.Value) return;
        entry.Value = false;
        ZenUI.Instance.Config.Save();
    }
}