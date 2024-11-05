/*using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using AzuExtendedPlayerInventory;
using HarmonyLib;
using UnityEngine;
using Object = UnityEngine.Object;

[HarmonyPatch(typeof(Humanoid), nameof(Humanoid.EquipItem))]
internal static class EquipItem
{
    private static void Equip(Humanoid humanoid, ItemDrop.ItemData item, bool triggerEquipmentEffects)
    {
        if (humanoid is Player player && item?.IsEquipable() == true)
        {
            PlayerVisual? visual = PlayerVisual.PlayerVisuals[player.m_visEquipment];

            // Check if the item is a utility item and handle accordingly
            if (item.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Utility)
            {
                // Unequip the current utility item if it exists
                if (player.m_utilityItem != null)
                {
                    /#1#/ cache the utility item to be unequipped
                    ItemDrop.ItemData utilityItem = player.m_utilityItem;
                    player.UnequipItem(player.m_utilityItem, triggerEquipmentEffects);
                    visual.EquippedItems.Remove(player.m_utilityItem);#1#
                    if (ObjectDB.instance?.GetItemPrefab(item.m_dropPrefab?.name) is { } utilityItemPrefab)
                    {
                        ItemDrop.ItemData.SharedData sharedData = utilityItemPrefab.GetComponent<ItemDrop>().m_itemData.m_shared;
                        sharedData.m_itemType = ItemDrop.ItemData.ItemType.Material;
                    }
                    
                    if (ObjectDB.instance)
                    {
                        if (Player.m_localPlayer is { } player2 && player2 && player2.m_utilityItem.m_shared.m_name == "$item_wishbone")
                        {
                            player2.UnequipItem(player2.m_utilityItem);
                        }
                        Inventory[] inventories = Player.s_players.Select(p => p.GetInventory()).Concat(Object.FindObjectsOfType<Container>().Select(c => c.GetInventory())).Where(c => c is not null).ToArray();
                        foreach (ItemDrop.ItemData itemdata in ObjectDB.instance.m_items.Select(p => p.GetComponent<ItemDrop>()).Where(c => c && c.GetComponent<ZNetView>()).Concat(ItemDrop.s_instances).Select(i => i.m_itemData).Concat(inventories.SelectMany(i => i.GetAllItems())))
                        {
                            if (itemdata.m_shared.m_name == item.m_shared.m_name)
                            {
                                itemdata.m_shared.m_itemType = ItemDrop.ItemData.ItemType.Material;
                            }
                        }
                    }

                    
                    item.m_equipped = true;

                    /#1#/ Set the item back to equipped if it was already equipped
                    player.m_utilityItem = utilityItem;
                    visual.EquippedItems.Add(utilityItem);#1#
                }
            }

            ItemDrop.ItemData? existingItem = visual.EquippedItems.FirstOrDefault(x => x?.m_shared.m_name == item.m_shared.m_name);
            if (existingItem != null)
            {
                player.UnequipItem(existingItem, triggerEquipmentEffects);
                visual.EquippedItems.Remove(existingItem);
            }

            visual.EquippedItems.Add(item);
            AzuExtendedPlayerInventoryPlugin.AzuExtendedPlayerInventoryLogger.LogInfo($"Equipped {item.m_shared.m_name}");

            // Set the item back to equipped if it was already equipped
        }
    }

    private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructionEnumerable)
    {
        MethodInfo itemEquipped = AccessTools.DeclaredMethod(typeof(Humanoid), nameof(Humanoid.IsItemEquiped));
        List<CodeInstruction> instructions = instructionEnumerable.ToList();
        int index = instructions.FindLastIndex(instruction => instruction.Calls(itemEquipped));
        CodeInstruction labelInstruction = instructions[index - 2];
        instructions.InsertRange(index - 2, new[]
        {
            new CodeInstruction(OpCodes.Ldarg_0) { labels = labelInstruction.labels },
            new CodeInstruction(OpCodes.Ldarg_1),
            new CodeInstruction(OpCodes.Ldarg_2),
            new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(typeof(EquipItem), nameof(Equip))),
        });
        labelInstruction.labels = new List<Label>();

        return instructions;
    }
}

[HarmonyPatch(typeof(Humanoid), nameof(Humanoid.UnequipItem))]
internal static class UnequipItem
{
    private static void Unequip(Humanoid humanoid, ItemDrop.ItemData item)
    {
        if (humanoid is Player player)
        {
            PlayerVisual? visual = PlayerVisual.PlayerVisuals[player.m_visEquipment];
            ItemDrop.ItemData? equippedItem = visual.EquippedItems.FirstOrDefault(x => x?.m_shared.m_name == item.m_shared.m_name);
            if (equippedItem != null)
            {
                visual.EquippedItems.Remove(equippedItem);
                AzuExtendedPlayerInventoryPlugin.AzuExtendedPlayerInventoryLogger.LogInfo($"Unequipped {item.m_shared.m_name}");
            }
        }
    }

    private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructionEnumerable)
    {
        MethodInfo setupEquipment = AccessTools.DeclaredMethod(typeof(Humanoid), nameof(Humanoid.SetupEquipment));
        List<CodeInstruction> instructions = instructionEnumerable.ToList();
        int index = instructions.FindIndex(instruction => instruction.Calls(setupEquipment));
        instructions.InsertRange(index - 1, new[]
        {
            new CodeInstruction(OpCodes.Ldarg_0),
            new CodeInstruction(OpCodes.Ldarg_1),
            new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(typeof(UnequipItem), nameof(Unequip))),
        });
        return instructions;
    }
}

[HarmonyPatch(typeof(Humanoid), nameof(Humanoid.UpdateEquipmentStatusEffects))]
internal static class ApplyStatusEffects
{
    [HarmonyAfter("org.bepinex.plugins.jewelcrafting")]
    private static void CollectEffects(Humanoid humanoid, HashSet<StatusEffect?> statusEffects)
    {
        if (humanoid is Player player && PlayerVisual.PlayerVisuals.TryGetValue(player.m_visEquipment, out PlayerVisual visual))
        {
            foreach (ItemDrop.ItemData? item in visual.EquippedItems)
            {
                if (item?.m_shared.m_equipStatusEffect is { } statusEffect)
                {
                    statusEffects.Add(statusEffect);
                }

                if (humanoid.HaveSetEffect(item))
                {
                    if (item!.m_shared.m_equipStatusEffect is { } statusEffect2) // Need to check for null, otherwise NRE
                    {
                        statusEffects.Add(statusEffect2);
                    }
                }
            }
        }
    }

    private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructionsEnumerable)
    {
        List<CodeInstruction> instructions = instructionsEnumerable.ToList();
        instructions.InsertRange(2, new[]
        {
            new CodeInstruction(OpCodes.Ldarg_0),
            new CodeInstruction(OpCodes.Ldloc_0),
            new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(typeof(ApplyStatusEffects), nameof(CollectEffects))),
        });
        return instructions;
    }
}

[HarmonyPatch(typeof(ItemDrop.ItemData), nameof(ItemDrop.ItemData.IsEquipable))]
internal static class IsEquipable
{
    private static void Postfix(ItemDrop.ItemData __instance, ref bool __result)
    {
        if (API.GetSlots().IsValidFuncs.Any(func => func != null && func(__instance)))
        {
            AzuExtendedPlayerInventoryPlugin.AzuExtendedPlayerInventoryLogger.LogInfo($"{__instance.m_shared.m_name} is equipable");
            __result = true;
        }
    }
}

[HarmonyPatch(typeof(Humanoid), nameof(Humanoid.IsItemEquiped))]
internal static class IsItemEquiped
{
    private static void Postfix(Humanoid __instance, ItemDrop.ItemData item, ref bool __result)
    {
        if (__instance is Player player && PlayerVisual.PlayerVisuals.TryGetValue(player.m_visEquipment, out PlayerVisual visual) && visual.EquippedItems.Contains(item))
        {
            AzuExtendedPlayerInventoryPlugin.AzuExtendedPlayerInventoryLogger.LogInfo($"Checking if {item?.m_shared.m_name} is equipped");
            AzuExtendedPlayerInventoryPlugin.AzuExtendedPlayerInventoryLogger.LogInfo($"{item?.m_shared.m_name} is equipped");
            __result = true;
        }
    }
}

[HarmonyPatch(typeof(Player), nameof(Player.Awake))]
internal static class AddVisual
{
    [HarmonyPriority(Priority.First)]
    private static void Prefix(Player __instance)
    {
        PlayerVisual.PlayerVisuals.Add(__instance.GetComponent<VisEquipment>(), new PlayerVisual(__instance.GetComponent<VisEquipment>()));
    }

    private static void Postfix(Player __instance)
    {
        if (__instance.GetField<ItemDrop.ItemData>("m_utilityItem2") == null)
        {
            __instance.SetField<ItemDrop.ItemData>("m_utilityItem2", null);
        }
    }
}

[HarmonyPatch(typeof(VisEquipment), nameof(VisEquipment.OnEnable))]
internal static class AddVisualOnEnable
{
    private static void Postfix(VisEquipment __instance)
    {
        if (!PlayerVisual.PlayerVisuals.ContainsKey(__instance) && __instance.m_isPlayer)
        {
            PlayerVisual.PlayerVisuals[__instance] = new PlayerVisual(__instance);
        }
    }
}

[HarmonyPatch(typeof(VisEquipment), nameof(VisEquipment.OnDisable))]
internal static class RemoveVisualOnDisable
{
    private static void Postfix(VisEquipment __instance)
    {
        PlayerVisual.PlayerVisuals.Remove(__instance);
    }
}

[HarmonyPatch(typeof(Humanoid), nameof(Humanoid.SetupVisEquipment))]
internal static class SetupVisEquipment
{
    private static void Prefix(Humanoid __instance)
    {
        if (__instance is Player player && PlayerVisual.PlayerVisuals.TryGetValue(player.m_visEquipment, out PlayerVisual visual))
        {
            List<string> names = visual.EquippedItems.Select(item => item?.m_dropPrefab?.name ?? "").ToList();
            AzuExtendedPlayerInventoryPlugin.AzuExtendedPlayerInventoryLogger.LogInfo($"Setting items {string.Join(", ", names)}");
            visual.SetItems(names);
        }
    }
}

[HarmonyPatch(typeof(Humanoid), nameof(Humanoid.UnequipAllItems))]
internal static class UnequipAllItems
{
    private static void Prefix(Humanoid __instance)
    {
        if (__instance is Player player)
        {
            foreach (ItemDrop.ItemData? item in PlayerVisual.PlayerVisuals[player.m_visEquipment].EquippedItems)
            {
                player.UnequipItem(item, false);
            }
        }
    }
}

[HarmonyPatch(typeof(VisEquipment), nameof(VisEquipment.UpdateEquipmentVisuals))]
internal static class UpdateEquipmentVisuals
{
    private static void Postfix(VisEquipment __instance)
    {
        if (__instance.m_isPlayer)
        {
            PlayerVisual.PlayerVisuals[__instance].UpdateEquipmentVisuals();
        }
    }
}

public class PlayerVisual
{
    public static readonly Dictionary<VisEquipment, PlayerVisual> PlayerVisuals = new();
    private readonly VisEquipment _visEquipment;
    public readonly HashSet<ItemDrop.ItemData?> EquippedItems = new();
    private List<string> _itemNames = new();
    private readonly List<GameObject> _itemInstances = new();
    private List<int> _currentItemHashes = new();

    internal PlayerVisual(VisEquipment visEquipment)
    {
        this._visEquipment = visEquipment;
    }

    internal void UpdateEquipmentVisuals()
    {
        List<int> hashes = new();
        if (_visEquipment.m_nview.GetZDO() is { } zdo)
        {
            foreach (string? name in _itemNames)
            {
                hashes.Add(zdo.GetInt(name));
            }
        }
        else
        {
            hashes = _itemNames.Select(name => string.IsNullOrEmpty(name) ? 0 : name.GetStableHashCode()).ToList();
        }

        if (SetItemsEquipped(hashes))
        {
            _visEquipment.UpdateLodgroup();
        }
    }

    private bool SetItemsEquipped(List<int> hashes)
    {
        if (_currentItemHashes.SequenceEqual(hashes))
        {
            return false;
        }

        return ForceSetItemsEquipped(hashes);
    }

    public bool ForceSetItemsEquipped(List<int> hashes)
    {
        foreach (GameObject itemInstance in _itemInstances)
        {
            Object.Destroy(itemInstance);
        }

        _itemInstances.Clear();
        _currentItemHashes = hashes;
        if (hashes.Any(hash => hash != 0))
        {
            foreach (int hash in hashes)
            {
                _itemInstances.AddRange(_visEquipment.AttachArmor(hash));
            }
        }

        return true;
    }

    internal void SetItems(List<string> names)
    {
        if (_itemNames.SequenceEqual(names))
        {
            return;
        }

        _itemNames = names;
        if (_visEquipment.m_nview.GetZDO() is { } zdo && _visEquipment.m_nview.IsOwner())
        {
            foreach (string? name in names)
            {
                int hash = string.IsNullOrEmpty(name) ? 0 : name.GetStableHashCode();
                AzuExtendedPlayerInventoryPlugin.AzuExtendedPlayerInventoryLogger.LogInfo($"Setting {name} to {hash}");
                zdo.Set(name, hash);
            }
        }
    }
}

public static class ReflectionExtensions
{
    public static void SetField<T>(this object obj, string fieldName, T value)
    {
        var field = obj.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        if (field != null)
        {
            field.SetValue(obj, value);
        }
    }

    public static T GetField<T>(this object obj, string fieldName)
    {
        var field = obj.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        if (field != null)
        {
            return (T)field.GetValue(obj);
        }

        return default(T);
    }
}*/