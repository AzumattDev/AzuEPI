namespace AzuEPI.Game.Compatibility;

public class RustyBagsCompat
{
    public HashSet<string> Backpacks = new()
    {
        "LeatherBag_RS", "BarrelBag_RS", "UnbjornBag_RS", "DvergerBag_RS"
    };

    public HashSet<string> Quivers = new()
    {
        "Quiver_RS", "MountainQuiver_RS"
    };

    public static void Init()
    {
        if (!Chainloader.PluginInfos.TryGetValue("RustyMods.RustyBags", out PluginInfo rustyBagInfo)) return;
        if (rustyBagInfo == null || rustyBagInfo.Instance == null) return;
        API.AddSlot("$bp_backpack_slot_name", new RustyBagsCompat().Backpacks.ToArray().Concat(new RustyBagsCompat().Quivers.ToArray()));
        context._harmony.PatchAll(typeof(RustyBagsCompat));
    }

    [HarmonyPatch("RustyBags.BagEquipment, RustyBags", "IsBagEquipped"), HarmonyPostfix]
    public static void AdvBackpackIsBackpackEquipped(ref bool __result)
    {
        var player = Player.m_localPlayer;
        if (player == null || player.GetInventory() == null) return;
        var equippedItems = player.GetInventory().GetEquippedItems();
        if (equippedItems == null) return;
        if (equippedItems.Exists(item => item != null
                                         && item.m_dropPrefab != null
                                         && (new RustyBagsCompat().Backpacks.Contains(item.m_dropPrefab.name)
                                             || new RustyBagsCompat().Quivers.Contains(item.m_dropPrefab.name))))
        {
            __result = true;
        }
    }

    [HarmonyPatch("RustyBags.BagEquipment+Humanoid_UnequipItem_Patch, RustyBags", "Prefix"), HarmonyPrefix]
    public static void RustyBagsHumanoidUnequipItemPatchPrefix(ref ItemDrop.ItemData __state)
    {
        var player = Player.m_localPlayer;
        if (player == null || player.GetInventory() == null) return;
        __state = player.m_trinketItem;
        if (__state != null && __state.m_dropPrefab != null)
        {
            var equippedItems = player.GetInventory().GetEquippedItems();
            if (equippedItems == null) return;
            // Set the shoulderItem to the equipped backpack if it exists so that EPI can handle it properly.
            player.m_trinketItem = equippedItems.Find(item => item != null && item.m_dropPrefab != null && new RustyBagsCompat().Backpacks.Contains(item.m_dropPrefab.name))
                                   ?? equippedItems.Find(item => item != null && item.m_dropPrefab != null && new RustyBagsCompat().Quivers.Contains(item.m_dropPrefab.name));
        }
    }

    [HarmonyPatch("RustyBags.BagEquipment+Humanoid_UnequipItem_Patch, RustyBags", "Prefix"), HarmonyPostfix]
    public static void RustyBagsHumanoidUnequipItemPatchPostfix(ref ItemDrop.ItemData __state)
    {
        var player = Player.m_localPlayer;
        if (player == null || player.GetInventory() == null) return;
        player.m_trinketItem = __state;
    }
}