using System.Reflection.Emit;

namespace AzuEPI.Game.Favoriting;

[HarmonyPatch(typeof(Inventory), nameof(Inventory.StackAll), typeof(Inventory), typeof(bool))]
internal static class StackAllPatches
{
    private static bool Prepare() => !FavoritingMode.IsExternalFavoritingModLoaded();

    [HarmonyTranspiler]
    private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        MethodInfo getAllItems = AccessTools.DeclaredMethod(typeof(Inventory), nameof(Inventory.GetAllItems), Type.EmptyTypes);
        ConstructorInfo listConstructor = AccessTools.Constructor(typeof(List<ItemDrop.ItemData>), [typeof(IEnumerable<ItemDrop.ItemData>)]);
        MethodInfo filterItems = AccessTools.DeclaredMethod(typeof(StackAllPatches), nameof(FilterItems));

        return new CodeMatcher(instructions)
            .MatchForward(false, new CodeMatch(OpCodes.Callvirt, getAllItems), new CodeMatch(OpCodes.Newobj, listConstructor))
            .Advance(2)
            .Insert(new CodeInstruction(OpCodes.Ldarg_1), new CodeInstruction(OpCodes.Call, filterItems))
            .InstructionEnumeration();
    }

    private static List<ItemDrop.ItemData> FilterItems(List<ItemDrop.ItemData> items, Inventory fromInventory)
    {
        if (!Player.m_localPlayer || fromInventory != Player.m_localPlayer.GetInventory()) return items;

        UserConfig config = UserConfig.GetPlayerConfig(Player.m_localPlayer.GetPlayerID());
        return items.Where(item => !config.IsItemNameOrSlotFavorited(item)).ToList();
    }
}
