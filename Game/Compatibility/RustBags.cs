namespace AzuEPI.Game.Compatibility;

public class RustyBagsCompat
{
    public static HashSet<string> Backpacks = ["LeatherBag_RS", "BarrelBag_RS", "UnbjornBag_RS", "DvergerBag_RS"];

    public static HashSet<string> Quivers = ["Quiver_RS", "MountainQuiver_RS"];

    public static void Init()
    {
        if (!Chainloader.PluginInfos.TryGetValue("RustyMods.RustyBags", out PluginInfo rustyBagInfo)) return;
        if (rustyBagInfo == null || rustyBagInfo.Instance == null) return;
        API.AddSlot("$bp_backpack_slot_name", Backpacks.ToArray().Concat([.. Quivers]));
        context._harmony.PatchAll(typeof(RustyBagsCompat));
    }

    [HarmonyPatch("RustyBags.BagEquipment, RustyBags", "IsBagEquipped"), HarmonyPostfix]
    public static void AdvBackpackIsBackpackEquipped(ref bool __result)
    {
        Player? player = Player.m_localPlayer;
        if (player == null || player.GetInventory() == null) return;
        List<ItemDrop.ItemData>? equippedItems = player.GetInventory().GetEquippedItems();
        if (equippedItems == null) return;
        if (equippedItems.Exists(item => item != null
                                         && item.m_dropPrefab != null
                                         && (Backpacks.Contains(item.m_dropPrefab.name) || Quivers.Contains(item.m_dropPrefab.name))))
        {
            __result = true;
        }
    }

    [HarmonyPatch("RustyBags.BagEquipment+Humanoid_UnequipItem_Patch, RustyBags", "Prefix"), HarmonyPrefix]
    public static void RustyBagsHumanoidUnequipItemPatchPrefix(ref ItemDrop.ItemData __state)
    {
        Player? player = Player.m_localPlayer;
        if (player == null || player.GetInventory() == null) return;
        __state = player.m_trinketItem;
        if (__state != null && __state.m_dropPrefab != null)
        {
            List<ItemDrop.ItemData>? equippedItems = player.GetInventory().GetEquippedItems();
            if (equippedItems == null) return;
            // Set the shoulderItem to the equipped backpack if it exists so that EPI can handle it properly.
            player.m_trinketItem = equippedItems.Find(item => item != null && item.m_dropPrefab != null && Backpacks.Contains(item.m_dropPrefab.name))
                                   ?? equippedItems.Find(item => item != null && item.m_dropPrefab != null && Quivers.Contains(item.m_dropPrefab.name));
        }
    }

    [HarmonyPatch("RustyBags.BagEquipment+Humanoid_UnequipItem_Patch, RustyBags", "Prefix"), HarmonyPostfix]
    public static void RustyBagsHumanoidUnequipItemPatchPostfix(ref ItemDrop.ItemData __state)
    {
        Player? player = Player.m_localPlayer;
        if (player == null || player.GetInventory() == null) return;
        player.m_trinketItem = __state;
    }
}
