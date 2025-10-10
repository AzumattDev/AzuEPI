using AzuExtendedPlayerInventory;

namespace AzuEPI.Compatibility;

public class JudesEquipmentCompat
{
    public HashSet<string> Backpacks = new()
    {
        "BackpackHeavy", "BackpackSimple"
    };

    public static void Init()
    {
        if (!Chainloader.PluginInfos.TryGetValue("GoldenJude_JudesEquipment", out PluginInfo judebackpackInfo)) return;
        if (judebackpackInfo == null || judebackpackInfo.Instance == null) return;
        API.AddSlot(Localization.instance.Localize("$bp_backpack_slot_name"), new JudesEquipmentCompat().Backpacks.ToArray());
        context._harmony.PatchAll(typeof(JudesEquipmentCompat));
    }
}