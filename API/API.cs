#if !API
using AzuEPI.Core.InventoryHandlers;
using AzuEPI.Core.Slots;
using AzuEPI.Game.Patches;
using AzuEPI.Game.PlayerPreview;

# else
using BepInEx.Bootstrap;
using JetBrains.Annotations;
using UnityEngine;
using System;
using System.Collections.Generic;
using AzuEPI.Core.Slots;
#endif

namespace AzuEPI;

[PublicAPI]
public class API
{
    public delegate void SlotAddedHandler(string slotName);

    public delegate void SlotRemovedHandler(string slotName);

    internal static HashSet<Model.EquipmentSlot?> CustomSlots { get; } = new();

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
        AzuExtendedPlayerInventoryLogger.LogInfo("API.AddSlot called, asking to add slot " + slotName);
        if (string.IsNullOrWhiteSpace(slotName) || (getItem == null && isValid == null)) return false;

        AzuExtendedPlayerInventoryLogger.LogInfo("API.AddSlot proceeding to add slot " + slotName);

        int existingIdx = InventoryGuiPatches.UpdateInventory_Patch.slots.FindIndex(s => s.Name == slotName || (Localization.instance != null && s.Name == Localization.instance.Localize(slotName)));
        if (existingIdx >= 0 && InventoryGuiPatches.UpdateInventory_Patch.slots[existingIdx] is Model.EquipmentSlot existing)
        {
            ComposeOntoSlot(existing, isValid, getItem);
            AzuExtendedPlayerInventoryLogger.LogInfo($"Extended slot {slotName}");
            return true;
        }

        var slot = new Model.EquipmentSlot
        {
            Name = slotName.StartsWith("$") && Localization.instance != null ? Localization.instance.Localize(slotName) : slotName,
            OriginalName = slotName,
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

        AzuExtendedPlayerInventoryLogger.LogDebug($"Added slot {slotName}, localized as '{slot.Name}'");
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
#if !API
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

    public static SlotInfo GetSlots() => BuildSlotInfo(_ => true);

    public static SlotInfo GetQuickSlots() => BuildSlotInfo(s => s.IsQuickSlot);

    private static SlotInfo BuildSlotInfo(Func<Model.Slot, bool> filter)
    {
        {
#if !API
            var slots = InventoryGuiPatches.UpdateInventory_Patch.slots.Where(s => s != null && filter(s!)).ToArray();

            return new SlotInfo
            {
                SlotNames = slots.Select(s => s!.Name).ToArray(),
                OriginalSlotNames = slots.Select(s => s!.OriginalName).ToArray(),
                SlotPositions = slots.Select(s => s!.Position).ToArray(),
                GetItemFuncs = slots.Select(s => s!.EquipmentSlot?.Get).ToArray(),
                IsValidFuncs = slots.Select(s => s!.EquipmentSlot?.Valid).ToArray()
            };
#else
            return new SlotInfo();
#endif
        }
    }

    public static List<ItemDrop.ItemData> GetQuickSlotsItems()
    {
#if !API
        List<ItemDrop.ItemData> quickSlotItems = new();
        if (Player.m_localPlayer == null) return quickSlotItems;

        var inv = Player.m_localPlayer.GetInventory();
        int w = inv.GetWidth();
        int baseIndex = Layout.GetBaseSlotIndex(inv);

        foreach (var snap in GetQuickSlotSnapshots(inv))
        {
            var itemAt = inv.GetItemAt(snap.GridPos.x, snap.GridPos.y);
            if (itemAt != null) quickSlotItems.Add(itemAt);
        }

        return quickSlotItems;
#else
        return new List<ItemDrop.ItemData>();
#endif
    }

    public static int GetAddedRows(int width)
    {
#if !API
        int slotsCount = InventoryGuiPatches.UpdateInventory_Patch.slots.Count;
        int requiredRows = Mathf.CeilToInt((float)slotsCount / width);
        return requiredRows;
#else
        return 0;
#endif
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int GetFullHeight(int width)
    {
#if !API
        return Layout.BaseInventoryHeight + ExtraRows.Value + (AddEquipmentRow.Value.isOn() ? GetAddedRows(width) : 0);
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

    public static int GetSlotCount()
    {
#if !API
        return InventoryGuiPatches.UpdateInventory_Patch.slots.Count;
#else
        return 0;
#endif
    }

    public static bool TryGetSlotIndexByName(string slotName, out int index, bool allowLocalized = true)
    {
#if !API
        index = -1;
        if (string.IsNullOrWhiteSpace(slotName)) return false;

        var slots = InventoryGuiPatches.UpdateInventory_Patch.slots;

        for (int i = 0; i < slots.Count; ++i)
        {
            var s = slots[i];
            if (s == null) continue;
            if (!string.Equals(s.OriginalName, slotName, StringComparison.Ordinal)
                && (!allowLocalized || !string.Equals(s.Name, slotName, StringComparison.Ordinal))) continue;
            index = i;
            return true;
        }

        return false;
#else
        index = -1;
        return false;
#endif
    }

    public static bool TryGetSlotDescriptor(int index, out SlotDescriptor desc)
    {
#if API
        desc = default;
        return false;
#else
        desc = default;
        var slots = InventoryGuiPatches.UpdateInventory_Patch.slots;
        if ((uint)index >= (uint)slots.Count) return false;

        var s = slots[index];
        if (s == null) return false;

        bool isEquip = s is Model.EquipmentSlot { IsQuickSlot: false };
        bool isQuick = s is { IsQuickSlot: true };
        bool isCustom = IsCustomSlot(s as Model.EquipmentSlot);

        desc = new SlotDescriptor(
            index,
            s.Name ?? string.Empty,
            s.OriginalName ?? string.Empty,
            isQuick,
            isEquip,
            isCustom,
            s.Position
        );
        return true;
#endif
    }

    public static int GetSlotGridLinearIndex(Inventory inv, int slotIndex)
    {
#if API
        return -1;
#else
        if (inv == null) return -1;
        int total = GetSlotCount();
        if ((uint)slotIndex >= (uint)total) return -1;

        int baseIndex = Layout.GetBaseSlotIndex(inv);
        return baseIndex + slotIndex;
#endif
    }

    public static Vector2i GetSlotGridPos(Inventory inv, int slotIndex)
    {
#if API
        return new Vector2i(-1, -1);
#else
        int linear = GetSlotGridLinearIndex(inv, slotIndex);
        if (linear < 0) return new Vector2i(-1, -1);
        int w = inv.GetWidth();
        return new Vector2i(linear % w, linear / w);
#endif
    }

    public static bool TryGetSlotIndexAtGridPos(Inventory inv, Vector2i gridPos, out int slotIndex)
    {
#if API
        slotIndex = -1;
        return false;
#else
        slotIndex = -1;
        if (inv == null) return false;

        int w = inv.GetWidth();
        int normalRows = Layout.NormalRows(inv);
        if (gridPos.y < normalRows) return false;

        int linear = gridPos.y * w + gridPos.x;
        int baseLinear = normalRows * w;
        int epiLinear = linear - baseLinear;

        int total = GetSlotCount();
        if ((uint)epiLinear >= (uint)total) return false;

        slotIndex = epiLinear;
        return true;
#endif
    }

    public static bool IsEquipmentCell(Inventory inv, int x, int y, out int slotIndex)
    {
#if API
        slotIndex = -1;
        return false;
#else
        if (!TryGetSlotIndexAtGridPos(inv, new Vector2i(x, y), out slotIndex)) return false;
        return TryGetSlotDescriptor(slotIndex, out var d) && d.IsEquipmentSlot;
#endif
    }

    public static bool IsQuickCell(Inventory inv, int x, int y, out int slotIndex)
    {
#if API
        slotIndex = -1;
        return false;
#else
        if (!TryGetSlotIndexAtGridPos(inv, new Vector2i(x, y), out slotIndex)) return false;
        return TryGetSlotDescriptor(slotIndex, out var d) && d.IsQuickSlot;
#endif
    }

    public static bool TryGetSlotSnapshot(Inventory inv, int slotIndex, out SlotSnapshot snapshot)
    {
#if API
        snapshot = default;
        return false;
#else
        snapshot = default;
        if (inv == null) return false;
        if (!TryGetSlotDescriptor(slotIndex, out var desc)) return false;

        var slots = InventoryGuiPatches.UpdateInventory_Patch.slots;
        var raw = slots[slotIndex];
        bool occupied = raw?.Occupied ?? false;

        var gp = GetSlotGridPos(inv, slotIndex);
        int linear = GetSlotGridLinearIndex(inv, slotIndex);

        snapshot = new SlotSnapshot(desc, occupied, gp, linear);
        return true;
#endif
    }

    public static IEnumerable<SlotDescriptor> EnumerateSlots()
    {
#if API
        yield break;
#else
        int count = GetSlotCount();
        for (int i = 0; i < count; ++i)
            if (TryGetSlotDescriptor(i, out var d))
                yield return d;
#endif
    }

    public static IEnumerable<SlotSnapshot> GetAllSlotSnapshots(Inventory inv)
    {
#if API
        yield break;
#else
        int count = GetSlotCount();
        for (int i = 0; i < count; ++i)
            if (TryGetSlotSnapshot(inv, i, out var s))
                yield return s;
#endif
    }

    public static IEnumerable<SlotSnapshot> GetQuickSlotSnapshots(Inventory inv)
    {
#if API
        yield break;
#else
        foreach (var s in GetAllSlotSnapshots(inv))
            if (s.Descriptor.IsQuickSlot)
                yield return s;
#endif
    }

    public static IEnumerable<SlotSnapshot> GetEquipmentSlotSnapshots(Inventory inv)
    {
#if API
        yield break;
#else
        foreach (var s in GetAllSlotSnapshots(inv))
            if (s.Descriptor.IsEquipmentSlot)
                yield return s;
#endif
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TryGetSlotIndexByItem(Player player, ItemDrop.ItemData item, out int slotIndex)
    {
#if API
    slotIndex = -1;
    return false;
#else
        slotIndex = -1;
        if (!player || item == null) return false;

        int total = GetSlotCount();
        for (int i = 0; i < total; ++i)
        {
            if (!TryGetSlotDescriptor(i, out var d) || !d.IsEquipmentSlot) continue;
            if (TryGetEquippedItem(i, out var eq) && ReferenceEquals(eq, item))
            {
                slotIndex = i;
                return true;
            }
        }

        var inv = player.GetInventory();
        if (inv == null) return false;

        foreach (var snap in GetQuickSlotSnapshots(inv))
        {
            var at = inv.GetItemAt(snap.GridPos.x, snap.GridPos.y);
            if (ReferenceEquals(at, item))
            {
                slotIndex = snap.Descriptor.Index;
                return true;
            }
        }

        return false;
#endif
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TryGetSlotIndexByItem(ItemDrop.ItemData item, out int slotIndex)
    {
#if API
    slotIndex = -1;
    return false;
#else
        return TryGetSlotIndexByItem(Player.m_localPlayer, item, out slotIndex);
#endif
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TryGetSlotDescriptorByItem(Player player, ItemDrop.ItemData item, out SlotDescriptor desc)
    {
#if API
    desc = default;
    return false;
#else
        desc = default;
        if (!TryGetSlotIndexByItem(player, item, out var idx)) return false;
        return TryGetSlotDescriptor(idx, out desc);
#endif
    }

    public static bool SlotValidates(int slotIndex, ItemDrop.ItemData item)
    {
#if API
        return false;
#else
        var slots = InventoryGuiPatches.UpdateInventory_Patch.slots;
        if ((uint)slotIndex >= (uint)slots.Count) return false;

        if (slots[slotIndex] is Model.EquipmentSlot es)
        {
            if (es.Valid == null) return false;
            try
            {
                return es.Valid(item);
            }
            catch
            {
                return false;
            }
        }

        if (slots[slotIndex] is { IsQuickSlot: true }) return true;
        return false;
#endif
    }

    public static bool TryGetEquippedItem(int slotIndex, out ItemDrop.ItemData? item)
    {
#if API
        item = null;
        return false;
#else
        item = null;
        if (Player.m_localPlayer == null) return false;

        var slots = InventoryGuiPatches.UpdateInventory_Patch.slots;
        if ((uint)slotIndex >= (uint)slots.Count) return false;

        if (slots[slotIndex] is Model.EquipmentSlot es)
        {
            try
            {
                item = es.Get?.Invoke(Player.m_localPlayer);
                return item != null;
            }
            catch
            {
                item = null;
                return false;
            }
        }

        return false;
#endif
    }

    public static bool TryGetSlotDescriptorByName(string slotName, out SlotDescriptor desc, bool allowLocalized = true)
    {
        desc = default;
        if (!TryGetSlotIndexByName(slotName, out var idx, allowLocalized)) return false;
        return TryGetSlotDescriptor(idx, out desc);
    }

#if !API
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
                return isValid(item);
            }
            catch
            {
                return false;
            }
        };

        slot.Get = player =>
        {
            try
            {
                var res = originalGet?.Invoke(player);
                if (res != null) return res;
            }
            catch
            {
            }

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

    public static bool IsCustomSlot(SlotDescriptor d) => d.IsCustom;
    public static bool IsQuickSlot(SlotDescriptor d) => d.IsQuickSlot;
    public static bool IsEquipmentSlot(SlotDescriptor d) => d.IsEquipmentSlot;

#endif

#if !API
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

#if !API

    #region Model.Slot accessors (thin wrappers, no duplication)

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static bool TryGetModelSlot(int slotIndex, out Model.Slot? slot)
    {
#if API
    slot = null;
    return false;
#else
        slot = null;
        var slots = InventoryGuiPatches.UpdateInventory_Patch.slots;
        if ((uint)slotIndex >= (uint)slots.Count) return false;
        slot = slots[slotIndex];
        return slot != null;
#endif
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static bool TryGetEquipmentModelSlot(int slotIndex, out Model.EquipmentSlot? equipmentSlot)
    {
#if API
    equipmentSlot = null;
    return false;
#else
        equipmentSlot = null;
        if (!TryGetModelSlot(slotIndex, out var s)) return false;
        equipmentSlot = s as Model.EquipmentSlot;
        return equipmentSlot != null;
#endif
    }

    internal static bool TryGetModelSlotByName(string slotName, out Model.Slot? slot, bool allowLocalized = true)
    {
#if API
    slot = null;
    return false;
#else
        slot = null;
        if (!TryGetSlotIndexByName(slotName, out var idx, allowLocalized)) return false;
        return TryGetModelSlot(idx, out slot);
#endif
    }

    internal static bool TryGetModelSlotByItem(Player player, ItemDrop.ItemData item, out Model.Slot? slot)
    {
#if API
    slot = null;
    return false;
#else
        slot = null;
        if (!TryGetSlotIndexByItem(player, item, out var idx)) return false;
        return TryGetModelSlot(idx, out slot);
#endif
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static bool TryGetModelSlotByItem(ItemDrop.ItemData item, out Model.Slot? slot)
    {
#if API
    slot = null;
    return false;
#else
        return TryGetModelSlotByItem(Player.m_localPlayer, item, out slot);
#endif
    }

    internal static bool TryGetModelSlotAtGridPos(Inventory inv, Vector2i gridPos, out Model.Slot? slot)
    {
#if API
    slot = null;
    return false;
#else
        slot = null;
        if (!TryGetSlotIndexAtGridPos(inv, gridPos, out var idx)) return false;
        return TryGetModelSlot(idx, out slot);
#endif
    }

    internal static Model.Slot?[] GetModelSlotsSnapshot()
    {
#if API
    return Array.Empty<Model.Slot?>();
#else
        return InventoryGuiPatches
            .UpdateInventory_Patch
            .slots
            .ToArray();
#endif
    }

    internal static IEnumerable<Model.Slot?> EnumerateModelSlots()
    {
#if API
    yield break;
#else
        var slots = InventoryGuiPatches.UpdateInventory_Patch.slots;
        for (int i = 0; i < slots.Count; ++i)
            yield return slots[i];
#endif
    }

    #endregion

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

    internal static void RelocalizeSlots()
    {
        if (Localization.instance == null) return;

        foreach (Model.Slot? slot in InventoryGuiPatches.UpdateInventory_Patch.slots)
        {
            if (slot == null || string.IsNullOrWhiteSpace(slot.OriginalName)) continue;
            if (!slot.OriginalName.StartsWith("$", StringComparison.Ordinal)) continue;
            string localized = Localization.instance.Localize(slot.OriginalName);
            if (slot.Name == localized) continue;
            AzuExtendedPlayerInventoryLogger.LogDebug($"Relocalizing slot '{slot.Name}' to '{localized}' from key '{slot.OriginalName}'");
            slot.Name = localized;
        }
    }
#endif
}

[PublicAPI]
public class SlotInfo
{
    public string[] SlotNames { get; set; } = { };
    public string[] OriginalSlotNames { get; set; } = { };
    public Vector2[] SlotPositions { get; set; } = { };
    public Func<Player, ItemDrop.ItemData?>?[] GetItemFuncs { get; set; } = { };
    public Func<ItemDrop.ItemData, bool>?[] IsValidFuncs { get; set; } = { };
}

[PublicAPI]
public struct SlotDescriptor
{
    public int Index { get; }
    public string Name { get; }
    public string OriginalName { get; }
    public bool IsQuickSlot { get; }
    public bool IsEquipmentSlot { get; }
    public bool IsCustom { get; }
    public Vector2 UiPosition { get; }

    public SlotDescriptor(int index, string name, string originalName, bool isQuickSlot, bool isEquipmentSlot, bool isCustom, Vector2 uiPosition)
    {
        Index = index;
        Name = name ?? string.Empty;
        OriginalName = originalName ?? string.Empty;
        IsQuickSlot = isQuickSlot;
        IsEquipmentSlot = isEquipmentSlot;
        IsCustom = isCustom;
        UiPosition = uiPosition;
    }
}

[PublicAPI]
public struct SlotSnapshot
{
    public SlotDescriptor Descriptor { get; }

    public bool Occupied { get; }
    public Vector2i GridPos { get; }
    public int LinearGridIndex { get; }

    public SlotSnapshot(SlotDescriptor descriptor, bool occupied, Vector2i gridPos, int linearGridIndex)
    {
        Descriptor = descriptor;
        Occupied = occupied;
        GridPos = gridPos;
        LinearGridIndex = linearGridIndex;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Validates(ItemDrop.ItemData item) => API.SlotValidates(Descriptor.Index, item);
}