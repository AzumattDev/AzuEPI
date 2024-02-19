using System.Collections.Generic;

namespace AzuEPI.EPI.Patches.CustomItemTypes;

public class DefaultItemTypes
{
    public static readonly Dictionary<string, ItemDrop.ItemData.ItemType> OriginalItemTypes = new()
    {
        { "None", ItemDrop.ItemData.ItemType.None },
        { "Material", ItemDrop.ItemData.ItemType.Material },
        { "Consumable", ItemDrop.ItemData.ItemType.Consumable },
        { "OneHandedWeapon", ItemDrop.ItemData.ItemType.OneHandedWeapon },
        { "Bow", ItemDrop.ItemData.ItemType.Bow },
        { "Shield", ItemDrop.ItemData.ItemType.Shield },
        { "Helmet", ItemDrop.ItemData.ItemType.Helmet },
        { "Chest", ItemDrop.ItemData.ItemType.Chest },
        { "Ammo", ItemDrop.ItemData.ItemType.Ammo },
        { "Customization", ItemDrop.ItemData.ItemType.Customization },
        { "Legs", ItemDrop.ItemData.ItemType.Legs },
        { "Hands", ItemDrop.ItemData.ItemType.Hands },
        { "Trophy", ItemDrop.ItemData.ItemType.Trophy },
        { "TwoHandedWeapon", ItemDrop.ItemData.ItemType.TwoHandedWeapon },
        { "Torch", ItemDrop.ItemData.ItemType.Torch },
        { "Misc", ItemDrop.ItemData.ItemType.Misc },
        { "Shoulder", ItemDrop.ItemData.ItemType.Shoulder },
        { "Utility", ItemDrop.ItemData.ItemType.Utility },
        { "Tool", ItemDrop.ItemData.ItemType.Tool },
        { "Attach_Atgeir", ItemDrop.ItemData.ItemType.Attach_Atgeir },
        { "Fish", ItemDrop.ItemData.ItemType.Fish },
        { "TwoHandedWeaponLeft", ItemDrop.ItemData.ItemType.TwoHandedWeaponLeft },
        { "AmmoNonEquipable", ItemDrop.ItemData.ItemType.AmmoNonEquipable },
    };
}

/*public class ItemTypeData
{
    public ItemDrop.ItemData.ItemType itemType = ItemDrop.ItemData.ItemType.None;

    public ItemTypeData(Func<string, ItemDrop.ItemData.ItemType> getFaction, Func<HashSet<ItemDrop.ItemData.ItemType>> getFactions)
    {
        Faction = getFaction(yaml.faction);
    }

    private HashSet<ItemDrop.ItemData.ItemType> GetFactions(string data, Func<string, ItemDrop.ItemData.ItemType> getFaction, Func<HashSet<ItemDrop.ItemData.ItemType>> getFactions) =>
        data.ToLowerInvariant() == "all" ? getFactions() : Helper.Split(data).Select(getFaction).ToHashSet();
}*/