namespace AzuEPI.Game.Compatibility.AdvBackpacks;

public class AdvBackpacksCompat
{
    internal static readonly HashSet<string> Backpacks = new(StringComparer.Ordinal)
    {
        "BackpackMeadows", "BackpackBlackForest", "BackpackSwamp", "BackpackMountains", "BackpackPlains", "BackpackMistlands", "CapeSilverBackpack", "CapeIronBackpack",
	};

    internal static bool IsActive;

    internal static bool IsAbBackpack(ItemDrop.ItemData? item) =>
        IsActive && item?.m_dropPrefab != null && Backpacks.Contains(item.m_dropPrefab.name);

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
        IsActive = true;
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

    // Apply AB backpack's stat contributions directly when not already covered by vanilla
    // (vanilla only reads m_shoulderItem; if the backpack is there, IsBackpackItem guard prevents double-apply).
    // This handles three cases: backpack-only, cape+backpack, and reconnect where m_shoulderItem is accidentally set.

    [HarmonyPatch(typeof(Player), nameof(Player.UpdateModifiers)), HarmonyPostfix]
    private static void UpdateModifiers_Postfix(Player __instance)
    {
        if (__instance != Player.m_localPlayer) return;
        if (Player.s_equipmentModifierSourceFields == null) return;
        if (IsBackpackItem(__instance.m_shoulderItem)) return;
        ItemDrop.ItemData? bp = FindEquippedBackpack(__instance);
        if (bp == null) return;
        for (int i = 0; i < __instance.m_equipmentModifierValues.Length; ++i)
            __instance.m_equipmentModifierValues[i] += (float)Player.s_equipmentModifierSourceFields[i].GetValue(bp.m_shared);
    }

    [HarmonyPatch(typeof(Player), nameof(Player.GetBodyArmor)), HarmonyPostfix]
    private static void GetBodyArmor_Postfix(Player __instance, ref float __result)
    {
        if (__instance != Player.m_localPlayer) return;
        if (IsBackpackItem(__instance.m_shoulderItem)) return;
        ItemDrop.ItemData? bp = FindEquippedBackpack(__instance);
        if (bp == null) return;
        __result += bp.GetArmor();
    }

    [HarmonyPatch(typeof(Player), nameof(Player.ApplyArmorDamageMods)), HarmonyPostfix]
    private static void ApplyArmorDamageMods_Postfix(Player __instance, ref HitData.DamageModifiers mods)
    {
        if (__instance != Player.m_localPlayer) return;
        if (IsBackpackItem(__instance.m_shoulderItem)) return;
        ItemDrop.ItemData? bp = FindEquippedBackpack(__instance);
        if (bp == null) return;
        mods.Apply(bp.m_shared.m_damageModifiers);
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