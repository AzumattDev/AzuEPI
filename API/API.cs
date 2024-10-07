﻿using System;
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
				index = AzuExtendedPlayerInventoryPlugin.EquipmentSlotsCount;

            ExtendedPlayerInventory.slots.Insert(index, slot);

            ExtendedPlayerInventory.ShiftExtraInventorySlots(index, 1);

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

			ExtendedPlayerInventory.slots.RemoveAt(slotIndex);
			
            ExtendedPlayerInventory.ShiftExtraInventorySlots(slotIndex, -1);

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
        QuickSlot[] quickSlots = ExtendedPlayerInventory.QuickSlots.GetSlots();

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
		return ExtendedPlayerInventory.QuickSlots.GetItems();
#else
    return new List<ItemDrop.ItemData>();
#endif
	}

	public static int GetAddedRows(int width)
	{
#if !API
		return ExtendedPlayerInventory.GetExtraRowsForItemsToFit(ExtendedPlayerInventory.slots != null ? ExtendedPlayerInventory.slots.Count : 0, width);
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

