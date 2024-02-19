using System.Collections.Generic;
using System.Linq;
using AzuExtendedPlayerInventory;

namespace AzuEPI.EPI.Patches.CustomItemTypes;

public static class CustomItemTypeManager
{
    // Create constructor to populate NameToItemType ahead of time
    static CustomItemTypeManager()
    {
        foreach (KeyValuePair<string, ItemDrop.ItemData.ItemType> kvp in DefaultItemTypes.OriginalItemTypes)
            NameToItemType[kvp.Key.ToLowerInvariant()] = kvp.Value;

        // Add Utility2 as a new type
        NameToItemType["utility2"] = (ItemDrop.ItemData.ItemType)101;
        
        // Populate ItemTypeToDisplayName
        foreach (KeyValuePair<string, ItemDrop.ItemData.ItemType> kvp in DefaultItemTypes.OriginalItemTypes)
            ItemTypeToDisplayName[kvp.Value] = kvp.Key;
        
        // Add Utility2 as a new type
        ItemTypeToDisplayName[(ItemDrop.ItemData.ItemType)101] = "Utility2";
    }


    ///<summary>Lower case itemtype names for easier data loading.</summary>
    private static Dictionary<string, ItemDrop.ItemData.ItemType> NameToItemType = DefaultItemTypes.OriginalItemTypes.ToDictionary(kvp => kvp.Key.ToLowerInvariant(), kvp => kvp.Value);

    ///<summary>Original itemtype names because some mods rely on Enum.GetName(s) returning uppercase values.</summary>
    public static Dictionary<ItemDrop.ItemData.ItemType, string> ItemTypeToDisplayName = DefaultItemTypes.OriginalItemTypes.ToDictionary(kvp => kvp.Value, kvp => kvp.Key);
    
    public static bool TryGetItemType(string name, out ItemDrop.ItemData.ItemType itemtype) => NameToItemType.TryGetValue(name.ToLowerInvariant(), out itemtype);

    public static ItemDrop.ItemData.ItemType GetItemType(string name)
    {
        if (TryGetItemType(name, out ItemDrop.ItemData.ItemType itemtype)) return itemtype;
        AzuExtendedPlayerInventoryPlugin.AzuExtendedPlayerInventoryLogger.LogWarning($"Failed to find ItemType {name}.");
        return ItemDrop.ItemData.ItemType.None;
    }

    public static HashSet<ItemDrop.ItemData.ItemType> GetItemTypes() => NameToItemType.Values.Where(f => f != ItemDrop.ItemData.ItemType.None).ToHashSet();
    public static bool TryGetDisplayName(ItemDrop.ItemData.ItemType itemtype, out string name) => ItemTypeToDisplayName.TryGetValue(itemtype, out name);

    public static bool CheckCustomItemType(ItemDrop itemDrop, string customTypeName)
    {
        // Ensure the itemDrop and the name are valid.
        if (itemDrop == null || string.IsNullOrEmpty(customTypeName))
        {
            return false;
        }

        // Attempt to retrieve the custom item type using the item's instance ID or another unique identifier.
        ItemDrop.ItemData.ItemType itemType = itemDrop.m_itemData.m_shared.m_itemType;

        // Check if the itemType is one of the custom types, since direct comparison won't work for enums cast from integers.
        if (NameToItemType.TryGetValue(customTypeName.ToLowerInvariant(), out ItemDrop.ItemData.ItemType customType))
        {
            return itemType == customType;
        }

        return false;
    }
}