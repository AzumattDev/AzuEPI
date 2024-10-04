using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Configuration;
using UnityEngine;

namespace AzuExtendedPlayerInventory.EPI.Utilities;

public static class Utilities
{
    private static readonly string[] WbSuffixes = { "", "k", "m", "b" };

    internal static string FormatNumberSimpleNoDecimal(float number)
    {
        CalculateShortNumberAndSuffix(number, out double shortNumber, out string suffix);
        return $"{shortNumber:N0}{suffix}";
    }

    private static void CalculateShortNumberAndSuffix(float number, out double shortNumber, out string suffix)
    {
        int mag = (int)(Math.Log10(number) / 3);
        mag = Math.Min(WbSuffixes.Length - 1, mag);
        double divisor = Math.Pow(10, mag * 3);

        shortNumber = number / divisor;
        suffix = WbSuffixes[mag];
    }

    public static bool IgnoreKeyPresses(bool includeExtra = false)
    {
        if (!includeExtra)
            return ZNetScene.instance == null || Player.m_localPlayer == null || Minimap.IsOpen() ||
                   Console.IsVisible() || TextInput.IsVisible() || ZNet.instance.InPasswordDialog() ||
                   Chat.instance?.HasFocus() == true;

        return ZNetScene.instance == null || Player.m_localPlayer == null || Minimap.IsOpen() ||
               Console.IsVisible() || TextInput.IsVisible() || ZNet.instance.InPasswordDialog() ||
               Chat.instance?.HasFocus() == true || StoreGui.IsVisible() || InventoryGui.IsVisible() ||
               Menu.IsVisible() || TextViewer.instance?.IsVisible() == true;
    }

    public static void InventoryFix()
    {
        if (Player.m_localPlayer == null)
            return;

        Inventory playerInventory = Player.m_localPlayer.GetInventory();

        if (playerInventory == null || playerInventory.m_inventory == null)
            return;

        List<Vector2i> curPositions = new();
        List<ItemDrop.ItemData> itemsToFix = new();
        for (int index = 0; index < playerInventory.m_inventory.Count; index++)
        {
            ItemDrop.ItemData itemData = playerInventory.m_inventory[index];
            if (itemData == null) 
                continue;

            bool overlappingItem = curPositions.Exists(pos => pos == itemData.m_gridPos);
            if (overlappingItem || (itemData.m_gridPos.x < 0 || itemData.m_gridPos.x >= playerInventory.m_width ||
                                    itemData.m_gridPos.y < 0 || itemData.m_gridPos.y >= playerInventory.m_height) || itemData.m_stack < 1)
            {
                if (itemData.m_stack < 1)
                {
                    playerInventory.RemoveItem(itemData);
                } // Fix anything that has a stack of 0 or less

                AzuExtendedPlayerInventoryPlugin.AzuExtendedPlayerInventoryLogger.LogWarning(
                    overlappingItem
                        ? $"Item {Localization.instance.Localize(itemData.m_shared.m_name)} was overlapping another item in the player inventory grid, moving to first available slot or dropping if no slots are available."
                        : $"Item {Localization.instance.Localize(itemData.m_shared.m_name)} was outside player inventory grid, moving to first available slot or dropping if no slots are available.");
                itemsToFix.Add(itemData);
            }

            curPositions.Add(itemData.m_gridPos);
        }

        foreach (ItemDrop.ItemData brokenItem in itemsToFix)
        {
            TryAddItemToInventory(playerInventory!, brokenItem);
        }
    }

    private static void TryAddItemToInventory(Inventory inventory, ItemDrop.ItemData itemData)
    {
        if (inventory.CanAddItem(itemData))
        {
            Player.m_localPlayer.GetInventory().RemoveItem(itemData);
            inventory.AddItem(itemData);
        }
        else
        {
            AzuExtendedPlayerInventoryPlugin.AzuExtendedPlayerInventoryLogger.LogInfo($"Dropping {Localization.instance.Localize(itemData.m_shared.m_name)} in TryAddItemToInventory");
            Player.m_localPlayer.DropItem(inventory, itemData, itemData.m_stack);
        }
    }
}

public static class KeyboardExtensions
{
    // thank you to 'Margmas' for giving me this snippet from VNEI https://github.com/MSchmoecker/VNEI/blob/master/VNEI/Logic/BepInExExtensions.cs#L21
    // since KeyboardShortcut.IsPressed and KeyboardShortcut.IsDown behave unintuitively
    public static bool IsKeyDown(this KeyboardShortcut shortcut)
    {
        return shortcut.MainKey != KeyCode.None && Input.GetKeyDown(shortcut.MainKey) && shortcut.Modifiers.All(Input.GetKey);
    }

    public static bool IsKeyHeld(this KeyboardShortcut shortcut)
    {
        return shortcut.MainKey != KeyCode.None && Input.GetKey(shortcut.MainKey) && shortcut.Modifiers.All(Input.GetKey);
    }
}