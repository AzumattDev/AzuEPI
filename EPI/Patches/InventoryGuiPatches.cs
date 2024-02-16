using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace AzuExtendedPlayerInventory.EPI.Patches;

public class InventoryGuiPatches
{
    [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.Show))]
    static class InventoryGuiShowPatch
    {
        static void Postfix(InventoryGui __instance)
        {
            if (Player.m_localPlayer == null)
                return;
            Utilities.Utilities.InventoryFix();
        }
    }


    [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.OnSelectedItem))]
    static class InventoryGuiOnSelectedItemPatch
    {
        static void Prefix(InventoryGui __instance, InventoryGrid grid, ItemDrop.ItemData item, Vector2i pos, InventoryGrid.Modifier mod)
        {
            Player localPlayer = Player.m_localPlayer;
            if (localPlayer.IsTeleporting())
                return;
            if (__instance.m_dragGo && localPlayer.IsItemEquiped(__instance.m_dragItem))
            {
                // If the item is one of the equipped armor pieces, unequip it
                if (ExtendedPlayerInventory.IsAtEquipmentSlot(grid.m_inventory, __instance.m_dragItem, out _))
                {
                    localPlayer.UnequipItem(__instance.m_dragItem, false);
                }
            }
        }
    }

    [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.Update))]
    private static class InventoryGuiUpdatePatch
    {
        private static void Postfix(InventoryGui __instance, InventoryGrid ___m_playerGrid, Animator ___m_animator)
        {
            if (!Player.m_localPlayer)
                return;
            if (AzuExtendedPlayerInventoryPlugin.AddEquipmentRow.Value == AzuExtendedPlayerInventoryPlugin.Toggle.On)
            {
                var player = Player.m_localPlayer;
                Inventory inventory = player.GetInventory();
                List<ItemDrop.ItemData> allItems = inventory.GetAllItems();

                int width = inventory.GetWidth(); // cache the width
                int height = inventory.GetHeight(); // cache the height
                int requiredRows = API.GetAddedRows(width);

                int num = width * (height - requiredRows);
                ItemDrop.ItemData?[] equippedItems = new ItemDrop.ItemData[UpdateInventory_Patch.slots.Count];
                for (int i = 0; i < UpdateInventory_Patch.slots.Count; ++i)
                {
                    Slot slot = UpdateInventory_Patch.slots[i];
                    if (slot is EquipmentSlot equipmentSlot)
                    {
                        if (equipmentSlot.Get(player) is { } item)
                        {
                            item.m_gridPos = new Vector2i(num % width, num / width);
                            equippedItems[i] = item;
                        }

                        ++num;
                    }
                }

                for (int index = 0; index < allItems.Count; ++index)
                {
                    ItemDrop.ItemData t = allItems[index];
                    try
                    {
                        if (ExtendedPlayerInventory.IsAtEquipmentSlot(inventory, t, out int which) &&
                            (which <= -1 || t != equippedItems[which]) &&
                            (which <= -1 || UpdateInventory_Patch.slots[which] is not EquipmentSlot slot || !slot.Valid(t) || ExtendedPlayerInventory.equipItems[which] == t || !player.EquipItem(t, false)))
                        {
                            Vector2i vector2I = inventory.FindEmptySlot(true);
                            if (vector2I.x < 0 || vector2I.y < 0 || vector2I.y >= height - requiredRows)
                            {
                                // Technically, the code will handle when it cannot be added before this, but in the case of low durability items
                                // it will drop them simply because it cannot be added to the inventory and it's "outside" the normal inventory when it breaks.
                                // Check if it's a valid item to drop based on manually checking inventory and durability here as well.
                                if (t.m_durability > 0 && !inventory.CanAddItem(t))
                                    player.DropItem(inventory, t, t.m_stack);
                            }
                            else
                            {
                                t.m_gridPos = vector2I;
                                ___m_playerGrid.UpdateInventory(inventory, player, null);
                            }
                        }
                    }
                    catch
                    {
                        // I'm not proud of this one, but it prevents the occasional NRE spam when spawning in for the first time. (and you have weapons in the hidden left/right slots)
                    }
                }

                ExtendedPlayerInventory.equipItems = equippedItems;
            }

            if (!___m_animator.GetBool(ExtendedPlayerInventory.Visible))
                return;

            RectTransform bkgRect = __instance.m_player.Find("Bkg").GetComponent<RectTransform>();
            bkgRect.anchorMin = new Vector2(0.0f,
                (AzuExtendedPlayerInventoryPlugin.ExtraRows.Value +
                 (AzuExtendedPlayerInventoryPlugin.AddEquipmentRow.Value == AzuExtendedPlayerInventoryPlugin.Toggle.Off || AzuExtendedPlayerInventoryPlugin.DisplayEquipmentRowSeparate.Value == AzuExtendedPlayerInventoryPlugin.Toggle.On ? 0 : API.GetAddedRows(Player.m_localPlayer.m_inventory.GetWidth()))) *
                -0.25f);

            if (AzuExtendedPlayerInventoryPlugin.AddEquipmentRow.Value == AzuExtendedPlayerInventoryPlugin.Toggle.Off)
                return;

            var equipmentBkgTransform = __instance.m_player.Find(ExtendedPlayerInventory.AzuBkgName);
            var dropallButton = __instance.m_player.Find(ExtendedPlayerInventory.DropAllButtonName);

            switch (AzuExtendedPlayerInventoryPlugin.DisplayEquipmentRowSeparate.Value)
            {
                case AzuExtendedPlayerInventoryPlugin.Toggle.On when equipmentBkgTransform == null:
                {
                    Transform transform = Object.Instantiate(bkgRect.transform, __instance.m_player);
                    transform.SetAsFirstSibling();
                    transform.name = ExtendedPlayerInventory.AzuBkgName;
                    RectTransform rectTransform = transform.GetComponent<RectTransform>();
                    rectTransform.anchorMin = new Vector2(1f, 0.0f);
                    Vector2 maxAnchor = new(1.13f + Math.Max(AzuExtendedPlayerInventoryPlugin.Hotkeys.Length, (UpdateInventory_Patch.slots.Count - 1) / 3) * UpdateInventory_Patch.tileSize / 570, 1f);
                    if (BepInEx.Bootstrap.Chainloader.PluginInfos.TryGetValue(ExtendedPlayerInventory.MinimalUiguid, out var pluginInfo) && pluginInfo is not null)
                    {
                        maxAnchor.x += 0.03f;
                    }

                    rectTransform.anchorMax = maxAnchor;
                    InventoryGui.instance.m_playerGrid.m_gridRoot.GetComponent<RectTransform>().anchorMax = maxAnchor;
                    InventoryGui.instance.m_playerGrid.m_gridRoot.GetComponent<Image>().raycastTarget = false;

                    break;
                }
                case AzuExtendedPlayerInventoryPlugin.Toggle.Off when equipmentBkgTransform:
                    Object.DestroyImmediate(equipmentBkgTransform.gameObject);
                    break;
            }

            if (AzuExtendedPlayerInventoryPlugin.MakeDropAllButton.Value == AzuExtendedPlayerInventoryPlugin.Toggle.On)
            {
                RectTransform dropAllButtonTransform = null;

                // If the drop all button doesn't exist, create it
                if (dropallButton == null)
                {
                    Transform dropAllButtonPrefab = __instance.m_takeAllButton.transform; // Assuming cloning the take all button
                    dropAllButtonTransform = Object.Instantiate(dropAllButtonPrefab, __instance.m_player).GetComponent<RectTransform>();
                    dropAllButtonTransform.name = ExtendedPlayerInventory.DropAllButtonName;
                    // Set the button text
                    dropAllButtonTransform.GetComponentInChildren<TMPro.TMP_Text>().text = "Drop All";
                    // Dropall button
                    var buttonComp = dropAllButtonTransform.GetComponent<Button>();
                    // Remove all listeners from the take all button
                    buttonComp.onClick.RemoveAllListeners();
                    // Add the new listener to the drop all button
                    buttonComp.onClick.AddListener(() => Console.instance.TryRunCommand("azuepi.dropall"));
                }
                else
                {
                    // If it already exists, get the RectTransform
                    dropAllButtonTransform = dropallButton.GetComponent<RectTransform>();
                }

                // Position the drop all button in the top left
                dropAllButtonTransform.SetAsFirstSibling();
                dropAllButtonTransform.anchorMin = new Vector2(0.0f, 1.0f);
                dropAllButtonTransform.anchorMax = new Vector2(0.0f, 1.0f);
                dropAllButtonTransform.pivot = new Vector2(0.0f, 1.0f);
                dropAllButtonTransform.anchoredPosition = AzuExtendedPlayerInventoryPlugin.DropAllButtonPosition.Value;
                dropAllButtonTransform.sizeDelta = new Vector2(100, 30);
            }
            else
            {
                // If the configuration is set to Off, check if the drop all button exists and destroy it
                if (dropallButton != null)
                {
                    Object.DestroyImmediate(dropallButton.gameObject);
                }
            }
        }
    }

    internal class Slot
    {
        public string Name = null!;
        public Vector2 Position;
        public EquipmentSlot? EquipmentSlot => this as EquipmentSlot;
    }

    internal class EquipmentSlot : Slot
    {
        public Func<Player, ItemDrop.ItemData?> Get = null!;
        public Func<ItemDrop.ItemData, bool> Valid = null!;
    }

    [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.UpdateInventory))]
    internal static class UpdateInventory_Patch
    {
        internal static float leftOffset = 643f;
        internal const float tileSize = 70f;

        /*internal static readonly List<Slot> slots = new()
        {
            new EquipmentSlot { Name = AzuExtendedPlayerInventoryPlugin.HelmetText.Value, Get = player => player.m_helmetItem, Valid = item => item.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Helmet, },
            new EquipmentSlot { Name = AzuExtendedPlayerInventoryPlugin.LegsText.Value, Get = player => player.m_legItem, Valid = item => item.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Legs, },
            new EquipmentSlot { Name = AzuExtendedPlayerInventoryPlugin.RightHandText.Value, Get = player => player.RightItem ?? player.m_hiddenRightItem, Valid = item => (item.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Tool || item.m_shared.m_itemType == ItemDrop.ItemData.ItemType.OneHandedWeapon || item.m_shared.m_itemType == ItemDrop.ItemData.ItemType.TwoHandedWeapon || item.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Torch || item.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Attach_Atgeir), },
            new EquipmentSlot { Name = AzuExtendedPlayerInventoryPlugin.BackText.Value, Get = player => player.m_shoulderItem, Valid = item => item.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Shoulder, },
            new EquipmentSlot { Name = AzuExtendedPlayerInventoryPlugin.ChestText.Value, Get = player => player.m_chestItem, Valid = item => item.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Chest, },
            new EquipmentSlot { Name = AzuExtendedPlayerInventoryPlugin.LeftHandText.Value, Get = player => player.LeftItem ?? player.m_hiddenLeftItem, Valid = item => (item.m_shared.m_itemType == ItemDrop.ItemData.ItemType.TwoHandedWeaponLeft || item.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Shield || item.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Torch || item.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Bow), },
            new EquipmentSlot { Name = AzuExtendedPlayerInventoryPlugin.UtilityText.Value, Get = player => player.m_utilityItem, Valid = item => item.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Utility, },
        };*/
        internal static readonly List<Slot> slots = new()
        {
            new EquipmentSlot { Name = AzuExtendedPlayerInventoryPlugin.HelmetText.Value, Get = player => player.m_helmetItem, Valid = item => item.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Helmet, },
            new EquipmentSlot { Name = AzuExtendedPlayerInventoryPlugin.LegsText.Value, Get = player => player.m_legItem, Valid = item => item.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Legs, },
            new EquipmentSlot { Name = AzuExtendedPlayerInventoryPlugin.UtilityText.Value, Get = player => player.m_utilityItem, Valid = item => item.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Utility, },
            new EquipmentSlot { Name = AzuExtendedPlayerInventoryPlugin.ChestText.Value, Get = player => player.m_chestItem, Valid = item => item.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Chest, },
            new EquipmentSlot { Name = AzuExtendedPlayerInventoryPlugin.BackText.Value, Get = player => player.m_shoulderItem, Valid = item => item.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Shoulder, },
        };

        static UpdateInventory_Patch()
        {
            // ensure the fixed slots are at end
            for (int i = 0; i < AzuExtendedPlayerInventoryPlugin.Hotkeys.Length; ++i)
            {
                slots.Add(new Slot { Name = AzuExtendedPlayerInventoryPlugin.HotkeyTexts[i].Value.IsNullOrWhiteSpace() ? AzuExtendedPlayerInventoryPlugin.Hotkeys[i].Value.ToString() : AzuExtendedPlayerInventoryPlugin.HotkeyTexts[i].Value });
            }
        }

        internal static void ResizeSlots()
        {
            float left = leftOffset;
            for (int i = 0; i < slots.Count - AzuExtendedPlayerInventoryPlugin.Hotkeys.Length; ++i)
            {
                float y = (i % 3) * -tileSize;
                // ReSharper disable once PossibleLossOfFraction
                float x = left + (i / 3) * tileSize + ((i % 3 > (slots.Count - 1) % 3 ? 1 : 0) + Math.Max(9 + AzuExtendedPlayerInventoryPlugin.Hotkeys.Length - slots.Count - 1, 0) / 3) * tileSize / 2;
                slots[i].Position = new Vector2(x, y);
            }

            for (int i = 0; i < AzuExtendedPlayerInventoryPlugin.Hotkeys.Length; ++i)
            {
                slots[slots.Count - AzuExtendedPlayerInventoryPlugin.Hotkeys.Length + i].Position = new Vector2(leftOffset + i * tileSize, 3 * -tileSize);
            }
        }

        // Dynamic way to add more slots. Just add names and positions here.
        private static void Postfix(InventoryGrid ___m_playerGrid)
        {
            if (AzuExtendedPlayerInventoryPlugin.AddEquipmentRow.Value == AzuExtendedPlayerInventoryPlugin.Toggle.Off)
                return;

            try
            {
                Player? player = Player.m_localPlayer;
                Inventory inventory = player.GetInventory();

                int requiredRows = API.GetAddedRows(inventory.GetWidth());


                // Update baseIndex based on the dynamic rows
                int baseIndex = inventory.GetWidth() * (inventory.GetHeight() - requiredRows);

                Vector2 baseGridPos = new((___m_playerGrid.GetComponent<RectTransform>().rect.width - ___m_playerGrid.GetWidgetSize().x) / 2f, 0.0f);

                for (int i = 0; i < slots.Count; ++i)
                {
                    GameObject currentChild = ___m_playerGrid.m_elements[baseIndex + i].m_go;
                    currentChild.SetActive(true);
                    ExtendedPlayerInventory.SetSlotText(slots[i].Name, currentChild.transform);
                    if (AzuExtendedPlayerInventoryPlugin.DisplayEquipmentRowSeparate.Value == AzuExtendedPlayerInventoryPlugin.Toggle.On)
                    {
                        currentChild.GetComponent<RectTransform>().anchoredPosition = slots[i].Position;
                    }
                    else
                    {
                        currentChild.GetComponent<RectTransform>().anchoredPosition = baseGridPos + new Vector2((baseIndex + i) % inventory.GetWidth() * ___m_playerGrid.m_elementSpace, (baseIndex + i) / inventory.GetWidth() * -___m_playerGrid.m_elementSpace);
                    }
                }

                for (int i = baseIndex + slots.Count; i < ___m_playerGrid.m_elements.Count; ++i)
                {
                    ___m_playerGrid.m_elements[i].m_go.SetActive(false);
                }
            }
            catch (Exception ex)
            {
                AzuExtendedPlayerInventoryPlugin.AzuExtendedPlayerInventoryLogger.LogDebug($"Exception in EPI Update Inventory: {ex}");
            }
        }
    }
}