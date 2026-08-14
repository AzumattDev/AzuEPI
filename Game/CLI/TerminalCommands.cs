namespace AzuEPI.Game.CLI;

[HarmonyPatch(typeof(Terminal), nameof(Terminal.InitTerminal))]
internal static class TerminalInitTerminalPatch
{
    private static void Postfix(Terminal __instance)
    {
        Terminal.ConsoleCommand RemoveAll = new("azuepi.removeall", "Removes all items from your inventory",
            args =>
            {
                if (Player.m_localPlayer == null)
                {
                    args.Context.AddString("No local player found, please make sure you're in-game");
                    return;
                }

                Player.m_localPlayer.GetInventory().m_inventory.RemoveAll(x => x != null);
            });

        Terminal.ConsoleCommand QuickFixInventory = new("azuepi.quickfix", "Fixes the inventory if something is wrong with it",
            args =>
            {
                if (Player.m_localPlayer == null)
                {
                    args.Context.AddString("No local player found, please make sure you're in-game");
                    return;
                }

                InventoryHealth.InventoryFix();
            });

        Terminal.ConsoleCommand BreakEquipment = new("azuepi.breakall", "Break all the equipment in your inventory",
            args =>
            {
                if (Player.m_localPlayer == null)
                {
                    args.Context.AddString("No local player found, please make sure you're in-game");
                    return;
                }

                List<ItemDrop.ItemData>? inventory = Player.m_localPlayer.GetInventory().m_inventory;
                foreach (ItemDrop.ItemData itemData in inventory.Where(itemData => itemData.m_equipped && itemData.m_shared.m_useDurability)) itemData.m_durability = 0;
            });

        Terminal.ConsoleCommand DropAll = new("azuepi.dropall", "Drop every item in your inventory to the ground",
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

        Terminal.ConsoleCommand InvCheck = new("azuepi.invlistall", "List every item in your inventory (will be printed to the console)",
            args =>
            {
                if (Player.m_localPlayer == null)
                {
                    args.Context.AddString("No local player found, please make sure you're in-game");
                    return;
                }

                Inventory inventory = Player.m_localPlayer.GetInventory();
                AzuExtendedPlayerInventoryLogger.LogWarning($"inv: {inventory.m_name}, ({inventory.m_width}, {inventory.m_height})");
                args.Context.AddString($"inv: {inventory.m_name}, ({inventory.m_width}, {inventory.m_height})");
                foreach (ItemDrop.ItemData? itemData in inventory.m_inventory)
                {
                    string prefabName = itemData.m_dropPrefab != null ? itemData.m_dropPrefab.name : "";
                    args.Context.AddString($"{prefabName} [{itemData.m_shared.m_name}] ({itemData.m_gridPos.x}, {itemData.m_gridPos.y})");

                    AzuExtendedPlayerInventoryLogger.LogWarning($"- {prefabName} [{itemData.m_shared.m_name}] ({itemData.m_gridPos.x}, {itemData.m_gridPos.y})");
                }
            });

        Terminal.ConsoleCommand RepairAll = new("azuepi.repairall", "Repair all items in your inventory",
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

                while (HaveRepairableItems()) RepairItems();

                bool HaveRepairableItems()
                {
                    if (Player.m_localPlayer == null)
                        return false;
                    InventoryGui.instance.m_tempWornItems.Clear();
                    Player.m_localPlayer.GetInventory().GetWornItems(InventoryGui.instance.m_tempWornItems);
                    foreach (ItemDrop.ItemData tempWornItem in InventoryGui.instance.m_tempWornItems)
                        if (CanRepairItems(tempWornItem))
                            return true;

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

        Terminal.ConsoleCommand SlotFree = new("azuepi.slotsfree", "List",
            args =>
            {
                if (Player.m_localPlayer == null)
                {
                    args.Context.AddString("No local player found, please make sure you're in-game");
                    return;
                }

                if (API.GetAddedRows(Player.m_localPlayer.GetInventory().GetWidth()) == 0)
                {
                    args.Context.AddString("You don't have any extra rows added");
                    return;
                }

                if (Player.m_localPlayer.GetInventory().IsEquipmentSlotFree(out int whichEq))
                {
                    args.Context.AddString($"You have a free equipment slot at index {whichEq}");
                }
                else
                {
                    args.Context.AddString("You don't have any free equipment slots");
                }

                {
                }
                if (Player.m_localPlayer.GetInventory().IsQuickSlotFree(out int which))
                {
                    args.Context.AddString($"You have a free quickslot at index {which}");
                }
                else
                {
                    args.Context.AddString("You don't have any free quickslots");
                }
            }, true);

#if DEBUG
        Terminal.ConsoleCommand TestFakeStats = new("azuepi.fakestats", "Simulate receiving stats from a fake player (for testing)",
            args =>
            {
                string? playerName = args.Args.Length > 1 ? string.Join(" ", args.Args.Skip(1)) : null;
                Panels.Stats.FakePlayerStats.SimulateReceiveStats(playerName);
                args.Context.AddString($"Simulated receiving stats from fake player{(playerName != null ? $": {playerName}" : "")}");
            });

        Terminal.ConsoleCommand ResetStats = new("azuepi.resetstats", "Reset stats panel to show local player stats",
            args =>
            {
                Panels.Stats.StatsPanelController.SetVisible(false);
                Panels.Stats.StatsPanelController.SetVisible(true);
                args.Context.AddString("Stats panel reset to local player");
            });

        Terminal.ConsoleCommand TestCompression = new("azuepi.testcompression", "Test the stats compression/decompression",
            args =>
            {
                bool success = Panels.Stats.FakePlayerStats.TestCompression();
                args.Context.AddString(success ? "Compression test PASSED" : "Compression test FAILED - check log for details");
            });

        Terminal.ConsoleCommand TestRpcSelf = new("azuepi.testrpc", "Test full RPC round-trip by sending stats request to yourself",
            args =>
            {
                if (Player.m_localPlayer == null)
                {
                    args.Context.AddString("No local player found, please make sure you're in-game");
                    return;
                }

                if (ZRoutedRpc.instance == null)
                {
                    args.Context.AddString("ZRoutedRpc not available - are you connected to a world?");
                    return;
                }

                long myUid = ZNet.GetUID();
                args.Context.AddString($"Sending RPC stats request to self (UID: {myUid})...");

                Panels.Stats.StatsPanelController.EnableTestMode();

                Panels.Stats.RemoteStatsRPC.RequestStats(myUid);

                args.Context.AddString("RPC sent! Open your inventory and check the stats panel.");
                args.Context.AddString("The dropdown should now be visible and stats should update.");
            });

        Terminal.ConsoleCommand TestRpcPlayer = new("azuepi.testrpcplayer", "Test RPC by requesting stats from another player by name",
            args =>
            {
                if (Player.m_localPlayer == null)
                {
                    args.Context.AddString("No local player found, please make sure you're in-game");
                    return;
                }

                if (ZNet.instance == null || ZRoutedRpc.instance == null)
                {
                    args.Context.AddString("Not connected to a world");
                    return;
                }

                if (args.Args.Length < 2)
                {
                    args.Context.AddString("Usage: azuepi.testrpcplayer <player name>");
                    args.Context.AddString("Available players:");
                    foreach (ZNet.PlayerInfo playerInfo in ZNet.instance.GetPlayerList())
                    {
                        args.Context.AddString($"  - {playerInfo.m_name} (UID: {playerInfo.m_characterID.UserID})");
                    }
                    return;
                }

                string targetName = string.Join(" ", args.Args.Skip(1)).ToLowerInvariant();
                ZNet.PlayerInfo? target = null;

                foreach (ZNet.PlayerInfo playerInfo in ZNet.instance.GetPlayerList())
                {
                    if (playerInfo.m_name.ToLowerInvariant().Contains(targetName))
                    {
                        target = playerInfo;
                        break;
                    }
                }

                if (target == null)
                {
                    args.Context.AddString($"Player '{targetName}' not found");
                    return;
                }

                long targetUid = target.Value.m_characterID.UserID;
                args.Context.AddString($"Sending RPC stats request to {target.Value.m_name} (UID: {targetUid})...");

                Panels.Stats.StatsPanelController.EnableTestMode(targetUid);
                Panels.Stats.RemoteStatsRPC.RequestStats(targetUid);

                args.Context.AddString("RPC sent! Open your inventory and check the stats panel.");
            });

        Terminal.ConsoleCommand AddFakePlayer = new("azuepi.addfakeplayer", "Add a fake player to the stats dropdown for testing",
            args =>
            {
                string playerName = args.Args.Length > 1 ? string.Join(" ", args.Args.Skip(1)) : "Viking Test";
                Panels.Stats.StatsPanelController.AddFakePlayerToDropdown(playerName);
                args.Context.AddString($"Added fake player '{playerName}' to dropdown.");
                args.Context.AddString("Open your inventory and select them from the stats panel dropdown.");
            });

        Terminal.ConsoleCommand ClearFakePlayers = new("azuepi.clearfakeplayers", "Remove all fake players from the stats dropdown",
            args =>
            {
                Panels.Stats.StatsPanelController.ClearFakePlayers();
                args.Context.AddString("Cleared all fake players from dropdown.");
            });
#endif
    }
}