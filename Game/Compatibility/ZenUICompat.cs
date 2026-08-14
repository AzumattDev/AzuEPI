namespace AzuEPI.Game.Compatibility;

public static class ZenUICompat
{
    private const string ZenUIGuid = "ZenDragon.ZenUI";

    public static void DisableEquipmentSlots()
    {
        if (!Chainloader.PluginInfos.TryGetValue(ZenUIGuid, out PluginInfo? ZenUI)) return;
        bool tryGetEntry = ZenUI.Instance.Config.TryGetEntry("Inventory Equip", "Assigned Gear Slots", out ConfigEntry<bool>? entry);
        if (!tryGetEntry || !entry.Value) return;
        entry.Value = false;
        ZenUI.Instance.Config.Save();
        context._harmony.PatchAll(typeof(ZenUICompat));
    }
    
    [HarmonyPatch("ZenUI.Section.InventoryEquip, ZenUI", "Humanoid_EquipItem"), HarmonyPrefix]
    public static bool PreventItemSort()
    {
        return false;
    }
    
    [HarmonyPatch("ZenUI.Section.InventoryEquip, ZenUI", "InventoryGrid_DropItem"), HarmonyPrefix]
    public static bool PreventItemSort2()
    {
        return false;
    }
}