#if !API
using AzuEPI.Core.InventoryHandlers;
using AzuEPI.Core.Slots;
using AzuEPI.Game.Patches;
using AzuEPI.Game.PlayerPreview;

# else
using JetBrains.Annotations;
using UnityEngine;
using System;
using System.Collections.Generic;
#endif

namespace AzuEPI.API;

[PublicAPI]
public class API
{
    public delegate void SlotAddedHandler(string slotName);

    public delegate void SlotRemovedHandler(string slotName);

#if !API
    internal static HashSet<Model.EquipmentSlot?> CustomSlots { get; } = new();
#endif

    public static event Action<Hud>? OnHudAwake;
    public static event Action<Hud>? OnHudAwakeComplete;
    public static event Action<Hud>? OnHudUpdate;
    public static event Action<Hud>? OnHudUpdateComplete;

    public static event Action? OnBeforeQuickSlotsAdded;
    public static event Action? OnQuickSlotsAdded;

    public static event SlotAddedHandler? SlotAdded;
    public static event SlotRemovedHandler? SlotRemoved;
    public static event Action<string>? OnRegisterVisualPrefab;

    public static bool IsLoaded()
    {
#if API
        return false;
#else
        return true;
#endif
    }

    public static ItemDrop.ItemData.ItemType GetFakeItemType()
    {
#if !API
        return (ItemDrop.ItemData.ItemType)FakeType;
#else
        return ItemDrop.ItemData.ItemType.None;
#endif
    }

    public static bool AddSlot(string slotName, Func<Player, ItemDrop.ItemData?> getItem, Func<ItemDrop.ItemData, bool> isValid, int index = -1)
    {
#if !API
        if (string.IsNullOrWhiteSpace(slotName) || (getItem == null && isValid == null)) return false;

        int existingIdx = InventoryGuiPatches.UpdateInventory_Patch.slots.FindIndex(s => s.Name == slotName);
        if (existingIdx >= 0 && InventoryGuiPatches.UpdateInventory_Patch.slots[existingIdx] is Model.EquipmentSlot existing)
        {
            ComposeOntoSlot(existing, isValid, getItem);
            AzuExtendedPlayerInventoryLogger.LogDebug($"Extended slot {slotName}");
            return true;
        }

        var slot = new Model.EquipmentSlot
        {
            Name = slotName.StartsWith("$") && Localization.instance != null ? Localization.instance.Localize(slotName) : slotName,
            Get = getItem,
            Valid = isValid,
            IsAPIAdded = true
        };

        if (index < 0 || index > InventoryGuiPatches.UpdateInventory_Patch.slots.Count - Hotkeys.Length)
            index = InventoryGuiPatches.UpdateInventory_Patch.slots.Count - Hotkeys.Length;

        UpdateSlots(index, 1);
        InventoryGuiPatches.UpdateInventory_Patch.slots.Insert(index, slot);
        CustomSlots.Add(slot);
        SlotHelpers.ResizeSlots();

        AzuExtendedPlayerInventoryLogger.LogDebug($"Added slot {slotName}");
        SlotAdded?.Invoke(slotName);

        return true;
#else
        return false;
#endif
    }

    public static bool AddSlot(string slotName, string prefabName, int index = -1)
    {
#if !API
        if (string.IsNullOrWhiteSpace(slotName) || string.IsNullOrWhiteSpace(prefabName)) return false;

        bool IsValid(ItemDrop.ItemData item) => item != null && item.m_dropPrefab && item.m_dropPrefab.name == prefabName;

        ItemDrop.ItemData? Get(Player p) => p?.GetInventory()?.GetEquippedItems()
            ?.FirstOrDefault(i => i != null && i.m_dropPrefab && i.m_dropPrefab.name == prefabName);

        var ok = AddSlot(slotName, Get, IsValid, index);
        if (ok) RegisterVisualsForSlot(slotName, prefabName);
        return ok;
#else
        return false;
#endif
    }

    public static bool AddSlot(string slotName, IEnumerable<string> prefabNames, int index = -1)
    {
#if !API
        if (string.IsNullOrWhiteSpace(slotName) || prefabNames == null) return false;

        var set = new HashSet<string>(prefabNames.Where(n => !string.IsNullOrWhiteSpace(n)), StringComparer.Ordinal);
        if (set.Count == 0) return false;

        bool IsValid(ItemDrop.ItemData item) => item != null && item.m_dropPrefab && set.Contains(item.m_dropPrefab.name);

        ItemDrop.ItemData? Get(Player p) => p?.GetInventory()?.GetEquippedItems()
            ?.FirstOrDefault(i => i != null && i.m_dropPrefab && set.Contains(i.m_dropPrefab.name));

        var ok = AddSlot(slotName, Get, IsValid, index);
        if (ok) RegisterVisualsForSlot(slotName, set.ToArray());
        return ok;
#else
        return false;
#endif
    }

    public static bool AddSlot(string slotName, Func<ItemDrop.ItemData, bool> isValid, int index = -1, IEnumerable<string>? prefabNamesForVisuals = null)
    {
#if !API
        if (string.IsNullOrWhiteSpace(slotName) || isValid == null) return false;

        ItemDrop.ItemData? AutoGet(Player p) => p?.GetInventory()?.GetEquippedItems()
            ?.FirstOrDefault(i => i != null && isValid(i));

        var ok = AddSlot(slotName, AutoGet, isValid, index);
        if (ok && prefabNamesForVisuals != null) RegisterVisualsForSlot(slotName, prefabNamesForVisuals.ToArray());
        return ok;
#else
        return false;
#endif
    }

    public static bool AddQuickSlot(string slotName, bool showName = false, int index = -1)
    {
#if !API
        if (string.IsNullOrWhiteSpace(slotName)) return false;

        int existingIdx = InventoryGuiPatches.UpdateInventory_Patch.slots.FindIndex(s => s?.Name == slotName);
        if (existingIdx >= 0 && InventoryGuiPatches.UpdateInventory_Patch.slots[existingIdx] is Model.EquipmentSlot existing)
        {
            return true;
        }

        var slot = new Model.EquipmentSlot
        {
            Name = showName ? slotName.StartsWith("$") && Localization.instance != null ? Localization.instance.Localize(slotName) : slotName : "",
            Valid = item => true,
            IsAPIAdded = true,
            IsQuickSlot = true
        };

        if (index < 0 || index > InventoryGuiPatches.UpdateInventory_Patch.slots.Count - Hotkeys.Length)
            index = InventoryGuiPatches.UpdateInventory_Patch.slots.Count - Hotkeys.Length;

        UpdateSlots(index, 1);
        InventoryGuiPatches.UpdateInventory_Patch.slots.Insert(index, slot);
        CustomSlots.Add(slot);
        SlotHelpers.ResizeSlots();

        AzuExtendedPlayerInventoryLogger.LogDebug($"Added slot {slotName}");
        SlotAdded?.Invoke(slotName);

        return true;
#else
        return false;
#endif
    }

    public static bool RemoveSlot(string slotName)
    {
#if ! API
        if (InventoryGuiPatches.UpdateInventory_Patch.slots.FindIndex(s => s.Name == slotName) is { } slotIndex and >= 0 && InventoryGuiPatches.UpdateInventory_Patch.slots[slotIndex] is Model.EquipmentSlot slot)
        {
            if (Player.m_localPlayer && slot.Get?.Invoke(Player.m_localPlayer) is { } item) Player.m_localPlayer.UnequipItem(item);

            UpdateSlots(slotIndex, -1);

            InventoryGuiPatches.UpdateInventory_Patch.slots.RemoveAt(slotIndex);

            SlotHelpers.ResizeSlots();
            SlotRemoved?.Invoke(slotName);

            return true;
        }
#endif
        return false;
    }

    public static SlotInfo GetSlots()
    {
#if ! API
        return new SlotInfo
        {
            SlotNames = InventoryGuiPatches.UpdateInventory_Patch.slots.Select(s => s.Name).ToArray(),
            SlotPositions = InventoryGuiPatches.UpdateInventory_Patch.slots.Select(s => s.Position).ToArray(),
            GetItemFuncs = InventoryGuiPatches.UpdateInventory_Patch.slots.Select(s => s.EquipmentSlot?.Get).ToArray(),
            IsValidFuncs = InventoryGuiPatches.UpdateInventory_Patch.slots.Select(s => s.EquipmentSlot?.Valid).ToArray()
        };
#else
        return new SlotInfo();
#endif
    }

    public static SlotInfo GetQuickSlots()
    {
#if !API
        var quickSlots = InventoryGuiPatches.UpdateInventory_Patch.slots
            .Where(s => s is { IsQuickSlot: true })
            .ToArray();

        return new SlotInfo
        {
            SlotNames = quickSlots.Select(s => s!.Name).ToArray(),
            SlotPositions = quickSlots.Select(s => s!.Position).ToArray(),
            GetItemFuncs = quickSlots.Select(s => s!.EquipmentSlot?.Get).ToArray(),
            IsValidFuncs = quickSlots.Select(s => s!.EquipmentSlot?.Valid).ToArray()
        };
#else
        return new SlotInfo();
#endif
    }

    public static List<ItemDrop.ItemData> GetQuickSlotsItems()
    {
#if !API
        List<ItemDrop.ItemData> quickSlotItems = new();
        if (Player.m_localPlayer == null) return quickSlotItems;

        var inv = Player.m_localPlayer.GetInventory();
        int w = inv.GetWidth();
        int baseIndex = Layout.BaseIndex(inv);

        for (int i = 0; i < InventoryGuiPatches.UpdateInventory_Patch.slots.Count; ++i)
        {
            var slot = InventoryGuiPatches.UpdateInventory_Patch.slots[i];
            if (slot is not { IsQuickSlot: true }) continue;

            int idx = baseIndex + i;
            int x = idx % w;
            int y = idx / w;
            var item = inv.GetItemAt(x, y);
            if (item != null) quickSlotItems.Add(item);
        }

        return quickSlotItems;
#else
        return new List<ItemDrop.ItemData>();
#endif
    }

    public static int GetAddedRows(int width)
    {
#if ! API
        int slotsCount = InventoryGuiPatches.UpdateInventory_Patch.slots.Count;
        int requiredRows = Mathf.CeilToInt((float)slotsCount / width);
        return requiredRows;
#else
        return 0;
#endif
    }

    public static void RegisterVisualPrefabs(string slotName, params (string prefabName, string visualName)[] pairs)
    {
#if !API
        foreach (var (prefab, visual) in pairs)
        {
            if (string.IsNullOrWhiteSpace(prefab)) continue;
            CustomEquipVisuals.Register(prefab);
            CustomEquipVisuals.RegisterForSlot(prefab, slotName, visual);
            try
            {
                OnRegisterVisualPrefab?.Invoke(prefab);
            }
            catch
            {
            }
        }
#endif
    }

#if ! API
    private static void RegisterVisualsForSlot(string slotName, params string[] prefabNames)
    {
        if (string.IsNullOrWhiteSpace(slotName) || prefabNames == null) return;

        foreach (var n in prefabNames.Where(s => !string.IsNullOrWhiteSpace(s)))
        {
            CustomEquipVisuals.Register(n);
            CustomEquipVisuals.RegisterForSlot(n, slotName);

            try
            {
                OnRegisterVisualPrefab?.Invoke(n);
            }
            catch
            {
                /* ignore listeners */
            }
        }
    }

    private static void ComposeOntoSlot(Model.EquipmentSlot slot, Func<ItemDrop.ItemData, bool> isValid, Func<Player, ItemDrop.ItemData?> getItem)
    {
        var originalValid = slot.Valid;
        var originalGet = slot.Get;

        slot.Valid = item =>
        {
            try
            {
                if (originalValid?.Invoke(item) == true) return true;
            }
            catch
            {
            }

            try
            {
                if (isValid(item)) return true;
            }
            catch
            {
            }

            return false;
        };

        slot.Get = player =>
        {
            ItemDrop.ItemData? res = null;
            try
            {
                res = originalGet?.Invoke(player);
            }
            catch
            {
            }

            if (res != null) return res;
            try
            {
                return getItem(player);
            }
            catch
            {
                return null;
            }
        };
    }

    internal static bool IsCustomSlot(Model.EquipmentSlot? slot)
    {
        return CustomSlots.Contains(slot);
    }

#endif

#if ! API
    public static void HudAwake(Hud __instance)
    {
        OnHudAwake?.Invoke(__instance);
    }

    public static void HudAwakeComplete(Hud __instance)
    {
        OnHudAwakeComplete?.Invoke(__instance);
    }

    public static void HudUpdate(Hud __instance)
    {
        OnHudUpdate?.Invoke(__instance);
    }

    public static void HudUpdateComplete(Hud __instance)
    {
        OnHudUpdateComplete?.Invoke(__instance);
    }

    public static void BeforeQuickSlotsAdded()
    {
        OnBeforeQuickSlotsAdded?.Invoke();
    }

    public static void QuickSlotsAdded()
    {
        OnQuickSlotsAdded?.Invoke();
    }
#endif

#if ! API
    internal static void UpdateSlots(int index, int shift)
    {
        if (!Player.m_localPlayer) return;
        Inventory inv = Player.m_localPlayer.m_inventory;
        int width = inv.GetWidth();
        int baseRows = Layout.BaseInventoryHeight + ExtraRows.Value;
        foreach (ItemDrop.ItemData item in inv.m_inventory)
            if ((item.m_gridPos.y - baseRows) * width + item.m_gridPos.x >= index)
            {
                item.m_gridPos.x += shift;
                if (item.m_gridPos.x < 0)
                {
                    item.m_gridPos.x = width - 1;
                    --item.m_gridPos.y;
                }

                if (item.m_gridPos.x >= width)
                {
                    item.m_gridPos.x = 0;
                    ++item.m_gridPos.y;
                }
            }

        inv.m_height = baseRows + Mathf.CeilToInt((float)(InventoryGuiPatches.UpdateInventory_Patch.slots.Count + shift) / width);
    }
#endif
}

[PublicAPI]
public class SlotInfo
{
    public string[] SlotNames { get; set; } = { };
    public Vector2[] SlotPositions { get; set; } = { };
    public Func<Player, ItemDrop.ItemData?>?[] GetItemFuncs { get; set; } = { };
    public Func<ItemDrop.ItemData, bool>?[] IsValidFuncs { get; set; } = { };
}