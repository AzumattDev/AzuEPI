namespace AzuEPI.Game.Compatibility.AdvBackpacks;

public class AdvBackpacksCompat
{
    public HashSet<string> Backpacks = new()
    {
        "BackpackMeadows", "BackpackBlackForest", "BackpackSwamp", "BackpackMountains", "BackpackPlains", "BackpackMistlands", "CapeSilverBackpack", "CapeIronBackpack"
    };

    public static void Init()
    {
        if (!Chainloader.PluginInfos.TryGetValue("vapok.mods.adventurebackpacks", out PluginInfo advbackpackinfo)) return;
        if (advbackpackinfo == null || advbackpackinfo.Instance == null) return;
        API.AddSlot("$bp_backpack_slot_name", new AdvBackpacksCompat().Backpacks.ToArray());
        context._harmony.PatchAll(typeof(AdvBackpacksCompat));
    }

    [HarmonyPatch("AdventureBackpacks.Extensions.PlayerExtensions, AdventureBackpacks", "IsBackpackEquipped"), HarmonyPostfix]
    public static void AdvBackpackIsBackpackEquipped(ref bool __result)
    {
        var player = Player.m_localPlayer;
        if (player == null || player.GetInventory() == null) return;
        if (player.GetInventory().GetEquippedItems().Exists(item => item != null
                                                                    && item.m_dropPrefab != null
                                                                    && new AdvBackpacksCompat().Backpacks.Contains(item.m_dropPrefab.name)))
        {
            __result = true;
        }
    }

    [HarmonyPatch("AdventureBackpacks.Extensions.PlayerExtensions, AdventureBackpacks", "IsThisBackpackEquipped"), HarmonyPostfix]
    public static void AdvBackpackIsThisBackpackEquipped(ref bool __result, ref ItemDrop.ItemData itemData)
    {
        var player = Player.m_localPlayer;
        if (player == null || player.GetInventory() == null) return;
        ItemDrop.ItemData data = itemData;
        if (itemData != null && itemData.m_dropPrefab != null
                             && new AdvBackpacksCompat().Backpacks.Contains(itemData.m_dropPrefab.name)
                             && player.GetInventory().GetEquippedItems().Exists(item => item != null && item.m_dropPrefab != null && item.Equals(data)))
        {
            __result = true;
        }
    }

    [HarmonyPatch("AdventureBackpacks.Patches.HumanoidPatches+HumanoidUnequipItemPatch, AdventureBackpacks", "Prefix"), HarmonyPrefix]
    public static void AdventureBackpackHumanoidUnequipItemPatchPrefix(ref ItemDrop.ItemData __state)
    {
        var player = Player.m_localPlayer;
        if (player == null || player.GetInventory() == null) return;
        __state = player.m_shoulderItem;
        if (__state != null && __state.m_dropPrefab != null)
        {
            // Set the shoulderItem to the equipped backpack if it exists so that EPI can handle it properly.
            player.m_shoulderItem = player.GetInventory().GetEquippedItems().Find(item => item != null && item.m_dropPrefab != null && new AdvBackpacksCompat().Backpacks.Contains(item.m_dropPrefab.name));
        }
    }

    [HarmonyPatch("AdventureBackpacks.Patches.HumanoidPatches+HumanoidUnequipItemPatch, AdventureBackpacks", "Prefix"), HarmonyPostfix]
    public static void AdventureBackpackHumanoidUnequipItemPatchPostfix(ref ItemDrop.ItemData __state)
    {
        var player = Player.m_localPlayer;
        if (player == null || player.GetInventory() == null) return;
        player.m_shoulderItem = __state;
    }
}