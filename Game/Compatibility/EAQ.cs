namespace AzuEPI.Game.Compatibility;

public class EAQ
{
    internal static void CheckRandy()
    {
        if (DisplayEquipmentRowSeparate.Value.isOff() && AddEquipmentRow.Value.isOff()) return;
        if (!Chainloader.PluginInfos.TryGetValue("randyknapp.mods.equipmentandquickslots", out var RandyEAQ)) return;
        DisplayEquipmentRowSeparate.Value = Off;
        AddEquipmentRow.Value = Off;
        context.Config.Save();
        AzuExtendedPlayerInventoryLogger.LogWarning($"{Environment.NewLine}RandyKnapp's Equipment and Quickslots mod has been detected. This mod is not fully compatible with his. As a result, the Display Equipment Row Separate and Add Equipment Row options for this mod have been disabled and the configuration saved.");
    }
}