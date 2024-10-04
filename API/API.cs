using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;
#if ! API
using System.Linq;
using AzuExtendedPlayerInventory.EPI;
#endif

namespace AzuExtendedPlayerInventory;
#nullable enable

[PublicAPI]
public class API
{
	// Using events to allow other code to register for updates.
	public static event Action<Hud>? OnHudAwake;
	public static event Action<Hud>? OnHudAwakeComplete;
	public static event Action<Hud>? OnHudUpdate;
	public static event Action<Hud>? OnHudUpdateComplete;

	// Delegate types for the event handlers
	public delegate void SlotAddedHandler(string slotName);

	public delegate void SlotRemovedHandler(string slotName);

	// Events fired when a slot is added or removed
	public static event SlotAddedHandler? SlotAdded;
	public static event SlotRemovedHandler? SlotRemoved;

	public static bool IsLoaded()
	{
#if API
		return false;
#else
		return true;
#endif
	}

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
#endif

	// Add a new slot
	public static bool AddSlot(string slotName, Func<Player, ItemDrop.ItemData?> getItem, Func<ItemDrop.ItemData, bool> isValid, int index = -1)
	{
#if ! API
		if (ExtendedPlayerInventory.slots.FindIndex(s => s.Name == slotName) < 0)
		{
			EquipmentSlot slot = new() { Name = slotName, Get = getItem, Valid = isValid };
			if (index < 0 || index > AzuExtendedPlayerInventoryPlugin.EquipmentSlotsCount)
			{
				index = AzuExtendedPlayerInventoryPlugin.EquipmentSlotsCount;
			}

			UpdateSlots(index, 1);

            ExtendedPlayerInventory.slots.Insert(index, slot);

            ExtendedPlayerInventory.EquipmentPanel.SetSlotsPositions();

			AzuExtendedPlayerInventoryPlugin.AzuExtendedPlayerInventoryLogger.LogDebug($"Added slot {slotName}");

			SlotAdded?.Invoke(slotName);

			return true;
		}
#endif
		return false;
	}

	public static bool RemoveSlot(string slotName)
	{
#if ! API
		if (ExtendedPlayerInventory.slots.FindIndex(s => s.Name == slotName) is { } slotIndex and >= 0 && ExtendedPlayerInventory.slots[slotIndex] is EquipmentSlot slot)
		{
			if (Player.m_localPlayer && slot.Get(Player.m_localPlayer) is { } item)
				Player.m_localPlayer.UnequipItem(item);

			UpdateSlots(slotIndex, -1);

			ExtendedPlayerInventory.slots.RemoveAt(slotIndex);
			
			ExtendedPlayerInventory.EquipmentPanel.SetSlotsPositions();

			SlotRemoved?.Invoke(slotName);

			return true;
		}
#endif
		return false;
	}
	
#if ! API
	private static void UpdateSlots(int index, int shift)
	{
		if (Player.m_localPlayer)
		{
			Inventory inv = Player.m_localPlayer.m_inventory;
			int width = inv.GetWidth();
			int baseRows = 4 + AzuExtendedPlayerInventoryPlugin.ExtraRows.Value;
			foreach (ItemDrop.ItemData item in inv.m_inventory)
			{
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
			}

			inv.m_height = baseRows + Mathf.CeilToInt((float)(ExtendedPlayerInventory.slots.Count + shift) / width);
		}
	}
#endif

	public static SlotInfo GetSlots()
	{
#if ! API
		return new SlotInfo
		{
			SlotNames = ExtendedPlayerInventory.slots.Select(s => s.Name).ToArray(),
			SlotPositions = ExtendedPlayerInventory.slots.Select(s => s.Position).ToArray(),
			GetItemFuncs = ExtendedPlayerInventory.slots.Select(s => s.EquipmentSlot?.Get).ToArray(),
			IsValidFuncs = ExtendedPlayerInventory.slots.Select(s => s.EquipmentSlot?.Valid).ToArray(),
		};
#else
    return new SlotInfo();
#endif
	}

	public static SlotInfo GetQuickSlots()
	{
#if !API
        Slot[] quickSlots = ExtendedPlayerInventory.GetQuickSlots();

		return new SlotInfo
		{
			SlotNames = quickSlots.Select(s => s.Name).ToArray(),
			SlotPositions = quickSlots.Select(s => s.Position).ToArray(),
			GetItemFuncs = quickSlots.Select(s => s.EquipmentSlot?.Get).ToArray(),
			IsValidFuncs = quickSlots.Select(s => s.EquipmentSlot?.Valid).ToArray(),
		};
#else
    return new SlotInfo();
#endif
	}
	
	public static List<ItemDrop.ItemData> GetQuickSlotsItems()
	{
#if ! API
		List<ItemDrop.ItemData> quickSlotItems = new();
		if (Player.m_localPlayer == null) return quickSlotItems;
		Inventory inventory = Player.m_localPlayer.GetInventory();
		int width = inventory.GetWidth();
		int adjustedHeight = inventory.GetHeight() - GetAddedRows(width);
		int firstHotkeyIndex = adjustedHeight * width + AzuExtendedPlayerInventoryPlugin.EquipmentSlotsCount;

		for (int i = 0; i < AzuExtendedPlayerInventoryPlugin.QuickSlotsCount; ++i)
		{
			int index = firstHotkeyIndex + i;
			if (inventory.GetItemAt(index % width, index / width) is { } item)
			{
				quickSlotItems.Add(item);
			}
		}

		return quickSlotItems;
#else
    return new List<ItemDrop.ItemData>();
#endif
	}

	public static int GetAddedRows(int width)
	{
#if ! API
		int slotsCount = ExtendedPlayerInventory.slots.Count;
		int requiredRows = Mathf.CeilToInt((float)slotsCount / width);
		return requiredRows;
#else
		return 0;
#endif
	}
}

[PublicAPI]
public class SlotInfo
{
	public string[] SlotNames { get; set; } = { };
	public Vector2[] SlotPositions { get; set; } = { };
	public Func<Player, ItemDrop.ItemData?>?[] GetItemFuncs { get; set; } = { };
	public Func<ItemDrop.ItemData, bool>?[] IsValidFuncs { get; set; } = { };
}

