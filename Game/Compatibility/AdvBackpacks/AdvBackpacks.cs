namespace AzuEPI.Game.Compatibility.AdvBackpacks;

public class AdvBackpacksCompat
{
    internal static readonly HashSet<string> Backpacks = new(StringComparer.Ordinal)
    {
        "BackpackMeadows", "BackpackBlackForest", "BackpackSwamp", "BackpackMountains", "BackpackPlains", "BackpackMistlands", "CapeSilverBackpack", "CapeIronBackpack"
    };

    private static Func<ItemDrop.ItemData, bool>? _abApiIsBackpack;
    private static bool _apiResolved;

    public static void Init()
    {
        if (!Chainloader.PluginInfos.TryGetValue("vapok.mods.adventurebackpacks", out PluginInfo advbackpackinfo))
            return;

        if (advbackpackinfo?.Instance == null)
            return;

        ResolveAbApi(advbackpackinfo.Instance.GetType().Assembly);

        if (!IsSlotMarkedForRemoval("$bp_backpack_slot_name"))
        {
            API.AddSlot("$bp_backpack_slot_name", new List<string>(Backpacks).ToArray());
        }
        context._harmony.PatchAll(typeof(AdvBackpacksCompat));
    }

    private static void ResolveAbApi(Assembly abAssembly)
    {
        if (_apiResolved)
            return;

        _apiResolved = true;

        try
        {
            Type? apiType = abAssembly.GetType("AdventureBackpacks.API.ABAPI");
            MethodInfo? mi = AccessTools.Method(apiType, "IsBackpack", [typeof(ItemDrop.ItemData)]);
            if (mi != null)
                _abApiIsBackpack = (Func<ItemDrop.ItemData, bool>)Delegate.CreateDelegate(typeof(Func<ItemDrop.ItemData, bool>), mi);
        }
        catch
        {
            _abApiIsBackpack = null;
        }
    }

    private static bool IsBackpackItem(ItemDrop.ItemData? item)
    {
        if (item == null)
            return false;

        if (_abApiIsBackpack == null) return item.m_dropPrefab != null && Backpacks.Contains(item.m_dropPrefab.name);
        try { return _abApiIsBackpack(item); }
        catch { /* fall back */ }

        return item.m_dropPrefab != null && Backpacks.Contains(item.m_dropPrefab.name);
    }

    private static ItemDrop.ItemData? FindEquippedBackpack(Player player)
    {
        Inventory? inv = player?.GetInventory();

        List<ItemDrop.ItemData>? equipped = inv?.GetEquippedItems();
        if (equipped == null || equipped.Count == 0)
            return null;

        for (int i = 0; i < equipped.Count; ++i)
        {
            ItemDrop.ItemData? it = equipped[i];
            if (it != null && IsBackpackItem(it))
                return it;
        }

        return null;
    }

    [HarmonyPatch("AdventureBackpacks.Extensions.PlayerExtensions, AdventureBackpacks", "IsBackpackEquipped"), HarmonyPostfix]
    public static void AdvBackpackIsBackpackEquipped(ref bool __result)
    {
        if (__result) return;
        Player? player = Player.m_localPlayer;
        if (player == null || player.GetInventory() == null) return;
        __result = FindEquippedBackpack(player) != null;
    }

    [HarmonyPatch("AdventureBackpacks.Extensions.PlayerExtensions, AdventureBackpacks", "IsThisBackpackEquipped"), HarmonyPostfix]
    public static void AdvBackpackIsThisBackpackEquipped(ref bool __result, ref ItemDrop.ItemData itemData)
    {
        if (__result) return;
        Player? player = Player.m_localPlayer;
        if (player == null || player.GetInventory() == null) return;

        if (!IsBackpackItem(itemData))
            return;

        List<ItemDrop.ItemData>? equipped = player.GetInventory().GetEquippedItems();
        if (equipped == null) return;

        __result = equipped.Contains(itemData);
    }

    [HarmonyPatch("AdventureBackpacks.Extensions.PlayerExtensions, AdventureBackpacks", "GetEquippedBackpack"), HarmonyPrefix]
    public static void AdvBackpackGetEquippedBackpackPrefix(Player player, ref ItemDrop.ItemData __state)
    {
        if (player == null || player.GetInventory() == null)
            return;

        __state = player.m_shoulderItem;

        if (IsBackpackItem(player.m_shoulderItem))
            return;

        ItemDrop.ItemData? equippedBackpack = FindEquippedBackpack(player);
        if (equippedBackpack != null)
            player.m_shoulderItem = equippedBackpack;
    }

    [HarmonyPatch("AdventureBackpacks.Extensions.PlayerExtensions, AdventureBackpacks", "GetEquippedBackpack"), HarmonyPostfix]
    public static void AdvBackpackGetEquippedBackpackPostfix(Player player, ItemDrop.ItemData __state)
    {
        if (player == null)
            return;

        player.m_shoulderItem = __state;
    }

    [HarmonyPatch("AdventureBackpacks.Patches.HumanoidPatches+HumanoidUnequipItemPatch, AdventureBackpacks", "Prefix"), HarmonyPrefix]
    public static void AdventureBackpackHumanoidUnequipItemPatchPrefix(ref ItemDrop.ItemData __state)
    {
        Player? player = Player.m_localPlayer;
        if (player == null || player.GetInventory() == null) return;
        __state = player.m_shoulderItem;

        ItemDrop.ItemData? equippedBackpack = FindEquippedBackpack(player);
        if (equippedBackpack != null)
            player.m_shoulderItem = equippedBackpack;
    }

    [HarmonyPatch("AdventureBackpacks.Patches.HumanoidPatches+HumanoidUnequipItemPatch, AdventureBackpacks", "Prefix"), HarmonyPostfix]
    public static void AdventureBackpackHumanoidUnequipItemPatchPostfix(ref ItemDrop.ItemData __state)
    {
        Player? player = Player.m_localPlayer;
        if (player == null || player.GetInventory() == null) return;
        player.m_shoulderItem = __state;
    }
}