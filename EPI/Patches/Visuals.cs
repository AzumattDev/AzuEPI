/*using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using AzuExtendedPlayerInventory;
using HarmonyLib;
using UnityEngine;

[HarmonyPatch(typeof(Humanoid), nameof(Humanoid.EquipItem))]
internal static class EquipItem
{
    private static void Equip(Humanoid humanoid, ItemDrop.ItemData item, bool triggerEquipmentEffects)
    {
        //if (humanoid is Player player && item?.IsEquipable() == true && API.IsCustomSlot(API.CustomSlots.FirstOrDefault(x => x?.EquipmentSlot != null && x.EquipmentSlot.Valid(item))))
        if (humanoid is Player player && item?.IsEquipable() == true)
        {
            player.UnequipItem(PlayerVisual.PlayerVisuals[player.m_visEquipment].EquippedItems.FirstOrDefault(x => x?.m_shared.m_name == item.m_shared.m_name), triggerEquipmentEffects);
            ItemDrop.ItemData? firstOrDefault = PlayerVisual.PlayerVisuals[player.m_visEquipment].EquippedItems.FirstOrDefault(x => x?.m_shared.m_name == item.m_shared.m_name);
            if (firstOrDefault != null)
            {
                PlayerVisual.PlayerVisuals[player.m_visEquipment].EquippedItems.Remove(firstOrDefault);
            }

            PlayerVisual.PlayerVisuals[player.m_visEquipment].EquippedItems.Add(item);
        }
        /*if (humanoid is Player player && item?.IsEquipable() == true && API.IsCustomSlot(API.CustomSlots.FirstOrDefault(x => x?.EquipmentSlot != null && x.EquipmentSlot.Valid(item))))
        {
            // Unequip the item in the slot if already equipped
            if (ExtendedPlayerInventory.IsAtEquipmentSlot(player.m_inventory, item, out int which))
            {
                player.UnequipItem(item, triggerEquipmentEffects);
                PlayerVisual.PlayerVisuals[player.m_visEquipment].equippedItems.Add(item);
            }

            item.m_equipped = true;
        }#1#
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
        //if (humanoid is Player player && item?.IsEquipable() == true && API.IsCustomSlot(API.CustomSlots.FirstOrDefault(x => x?.EquipmentSlot != null && x.EquipmentSlot.Valid(item))))
        if (humanoid is Player player && item?.IsEquipable() == true && PlayerVisual.PlayerVisuals[player.m_visEquipment].EquippedItems.FirstOrDefault(x => x?.m_shared.m_name == item.m_shared.m_name) != null)
        {
            PlayerVisual.PlayerVisuals[player.m_visEquipment].EquippedItems.Remove(item);
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
                    statusEffects.Add(item!.m_shared.m_equipStatusEffect);
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
            visual.SetItems(visual.EquippedItems.Select(item => item?.m_dropPrefab?.name ?? "").ToList());
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
    public readonly List<ItemDrop.ItemData?> EquippedItems = new();
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
                zdo.Set(name, string.IsNullOrEmpty(name) ? 0 : name.GetStableHashCode());
            }
        }
    }
}*/

