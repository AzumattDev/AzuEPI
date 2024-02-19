using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;

namespace AzuEPI.EPI.Patches.CustomItemTypes;

#pragma warning disable IDE0051
[HarmonyPatch]
public class TryParseItemType
{
    static IEnumerable<MethodBase> TargetMethods()
    {
        return typeof(Enum).GetMethods()
            .Where(method => method.Name == "TryParse")
            .Take(2)
            .Select(method => method.MakeGenericMethod(typeof(ItemDrop.ItemData.ItemType)))
            .Cast<MethodBase>();
    }

    static bool Prefix(string value, ref ItemDrop.ItemData.ItemType result, ref bool __result)
    {
        __result = CustomItemTypeManager.TryGetItemType(value, out result);
        return false;
    }
}
#pragma warning restore  IDE0051
[HarmonyPatch(typeof(Enum), nameof(Enum.GetValues))]
public class GetValues
{
    static bool Prefix(Type enumType, ref Array __result)
    {
        if (enumType != typeof(ItemDrop.ItemData.ItemType)) return true;
        __result = CustomItemTypeManager.ItemTypeToDisplayName.Keys.ToArray();
        return false;
    }
}

[HarmonyPatch(typeof(Enum), nameof(Enum.GetNames))]
public class GetNames
{
    static bool Prefix(Type enumType, ref string[] __result)
    {
        if (enumType != typeof(ItemDrop.ItemData.ItemType)) return true;
        __result = CustomItemTypeManager.ItemTypeToDisplayName.Values.ToArray();
        return false;
    }
}

[HarmonyPatch(typeof(Enum), nameof(Enum.GetName))]
public class GetName
{
    static bool Prefix(Type enumType, object value, ref string __result)
    {
        if (enumType != typeof(ItemDrop.ItemData.ItemType)) return true;
        __result = CustomItemTypeManager.TryGetDisplayName((ItemDrop.ItemData.ItemType)value, out string result) ? result : "None";
        return false;
    }
}

[HarmonyPatch(typeof(Enum), nameof(Enum.Parse), typeof(Type), typeof(string))]
public class EnumParse
{
    static bool Prefix(Type enumType, string value, ref object __result)
    {
        if (enumType != typeof(ItemDrop.ItemData.ItemType)) return true;
        if (!CustomItemTypeManager.TryGetItemType(value, out ItemDrop.ItemData.ItemType itemtype)) return true;
        __result = itemtype;
        return false;
        // Let the original function handle the throwing.
    }
}

[HarmonyPatch(typeof(Enum), nameof(Enum.Parse), typeof(Type), typeof(string), typeof(bool))]
public class ParseIgnoreCase
{
    static bool Prefix(Type enumType, string value, ref object __result)
    {
        if (enumType != typeof(ItemDrop.ItemData.ItemType)) return true;
        if (!CustomItemTypeManager.TryGetItemType(value, out ItemDrop.ItemData.ItemType itemtype)) return true;
        __result = itemtype;
        return false;
        // Let the original function handle the throwing.
    }
}