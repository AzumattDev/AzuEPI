using System.Collections.Generic;
using System.Linq;
using AzuExtendedPlayerInventory;
using BepInEx.Bootstrap;
using HarmonyLib;
using UnityEngine;
using Object = UnityEngine.Object;

namespace AzuEPI.EPI.Utilities;

// Patch the Terminal.Init to add commands to the terminal
[HarmonyPatch(typeof(Terminal), nameof(Terminal.InitTerminal))]
static class TerminalInitTerminalPatch
{
    static void Postfix(Terminal __instance)
    {
        Terminal.ConsoleCommand RemoveAll = new("azuepi.removeall",
            "Removes all items from your inventory",
            args =>
            {
                if (Player.m_localPlayer == null)
                {
                    args.Context.AddString("No local player found, please make sure you're in-game");
                    return;
                }

                Player.m_localPlayer.GetInventory().m_inventory.RemoveAll(x => x != null);
            });

        Terminal.ConsoleCommand QuickFixInventory = new("azuepi.quickfix",
            "Fixes the inventory if something is wrong with it",
            args =>
            {
                if (Player.m_localPlayer == null)
                {
                    args.Context.AddString("No local player found, please make sure you're in-game");
                    return;
                }

                AzuExtendedPlayerInventory.EPI.Utilities.Utilities.InventoryFix();
            });

        Terminal.ConsoleCommand BreakEquipment = new("azuepi.breakall",
            "Break all the equipment in your inventory",
            args =>
            {
                if (Player.m_localPlayer == null)
                {
                    args.Context.AddString("No local player found, please make sure you're in-game");
                    return;
                }

                List<ItemDrop.ItemData>? inventory = Player.m_localPlayer.GetInventory().m_inventory;
                foreach (ItemDrop.ItemData itemData in inventory.Where(itemData => itemData.m_equipped && itemData.m_shared.m_useDurability))
                {
                    itemData.m_durability = 0;
                }
            });

        Terminal.ConsoleCommand DropAll = new Terminal.ConsoleCommand("azuepi.dropall",
            "Drop every item in your inventory to the ground",
            args =>
            {
                if (Player.m_localPlayer == null)
                {
                    args.Context.AddString("No local player found, please make sure you're in-game");
                    return;
                }

                Inventory inventory = Player.m_localPlayer.GetInventory();

                for (int i = inventory.m_inventory.Count - 1; i >= 0; --i)
                {
                    ItemDrop.ItemData itemData = inventory.m_inventory[i];
                    Player.m_localPlayer.DropItem(inventory, itemData, itemData.m_stack);
                }
            });

        Terminal.ConsoleCommand InvCheck = new Terminal.ConsoleCommand("azuepi.invlistall",
            "List every item in your inventory (will be printed to the console)",
            args =>
            {
                if (Player.m_localPlayer == null)
                {
                    args.Context.AddString("No local player found, please make sure you're in-game");
                    return;
                }

                Inventory inventory = Player.m_localPlayer.GetInventory();
                AzuExtendedPlayerInventoryPlugin.AzuExtendedPlayerInventoryLogger.LogWarning($"inv: {inventory.m_name}, ({inventory.m_width}, {inventory.m_height})");
                args.Context.AddString($"inv: {inventory.m_name}, ({inventory.m_width}, {inventory.m_height})");
                foreach (ItemDrop.ItemData? itemData in inventory.m_inventory)
                {
                    string prefabName = itemData.m_dropPrefab != null ? itemData.m_dropPrefab.name : "";
                    args.Context.AddString($"{prefabName} [{itemData.m_shared.m_name}] ({itemData.m_gridPos.x}, {itemData.m_gridPos.y})");


                    AzuExtendedPlayerInventoryPlugin.AzuExtendedPlayerInventoryLogger.LogWarning($"- {prefabName} [{itemData.m_shared.m_name}] ({itemData.m_gridPos.x}, {itemData.m_gridPos.y})");
                }
            });

        Terminal.ConsoleCommand RepairAll = new Terminal.ConsoleCommand("azuepi.repairall",
            "Repair all items in your inventory",
            args =>
            {
                if (Player.m_localPlayer == null)
                {
                    args.Context.AddString("No local player found, please make sure you're in-game");
                    return;
                }

                if (!HaveRepairableItems())
                {
                    args.Context.AddString("No items to repair");
                    Player.m_localPlayer.Message(MessageHud.MessageType.Center, "No items to repair");
                    return;
                }

                while (HaveRepairableItems())
                {
                    RepairItems();
                }


                bool HaveRepairableItems()
                {
                    if (Player.m_localPlayer == null)
                        return false;
                    InventoryGui.instance.m_tempWornItems.Clear();
                    Player.m_localPlayer.GetInventory().GetWornItems(InventoryGui.instance.m_tempWornItems);
                    foreach (ItemDrop.ItemData tempWornItem in InventoryGui.instance.m_tempWornItems)
                    {
                        if (CanRepairItems(tempWornItem))
                            return true;
                    }

                    return false;
                }

                bool CanRepairItems(ItemDrop.ItemData item)
                {
                    if (Player.m_localPlayer == null || !item.m_shared.m_canBeReparied)
                        return false;
                    return Player.m_localPlayer.NoCostCheat() || true;
                }

                void RepairItems()
                {
                    InventoryGui.instance.m_tempWornItems.Clear();
                    Player.m_localPlayer.GetInventory().GetWornItems(InventoryGui.instance.m_tempWornItems);

                    for (int i = 0; i < InventoryGui.instance.m_tempWornItems.Count; ++i)
                    {
                        ItemDrop.ItemData tempWornItem = InventoryGui.instance.m_tempWornItems[i];
                        if (CanRepairItems(tempWornItem))
                        {
                            tempWornItem.m_durability = tempWornItem.GetMaxDurability();
                            Player.m_localPlayer.Message(MessageHud.MessageType.Center, Localization.instance.Localize("$msg_repaired", tempWornItem.m_shared.m_name));
                            return;
                        }
                    }

                    Player.m_localPlayer.Message(MessageHud.MessageType.Center, "No more items to repair");
                }
            }, true);
    }
}