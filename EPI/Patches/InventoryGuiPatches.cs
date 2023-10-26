using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx;
using HarmonyLib;
using UnityEngine;
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

    [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.Update))]
    private static class InventoryGuiUpdatePatch
    {
        private static void Postfix(
            InventoryGui __instance,
            InventoryGrid ___m_playerGrid,
            Animator ___m_animator)
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
                for (int i = 0; i < UpdateInventory_Patch.slots.Count; i++)
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
                    if (ExtendedPlayerInventory.IsAtEquipmentSlot(inventory, t, out int which) &&
                        (which <= -1 || t != equippedItems[which]) &&
                        (which <= -1 || UpdateInventory_Patch.slots[which] is not EquipmentSlot slot || !slot.Valid(t) || ExtendedPlayerInventory.equipItems[which] == t || !player.EquipItem(t, false)))
                    {
                        Vector2i vector2I = inventory.FindEmptySlot(true);
                        if (vector2I.x < 0 || vector2I.y < 0 || vector2I.y == height - requiredRows)
                        {
                            player.DropItem(inventory, t, t.m_stack);
                        }
                        else
                        {
                            t.m_gridPos = vector2I;
                            ___m_playerGrid.UpdateInventory(inventory, player, null);
                        }
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

                    break;
                }
                case AzuExtendedPlayerInventoryPlugin.Toggle.Off when equipmentBkgTransform:
                    Object.DestroyImmediate(equipmentBkgTransform.gameObject);
                    break;
            }
        }
    }

    internal class Slot
    {
        public string Name = null!;
        public Vector2 Position;
        public EquipmentSlot? EquipmentSlot => this as EquipmentSlot;
    }

    internal class EquipmentSlot: Slot
    {
        public Func<Player, ItemDrop.ItemData?> Get = null!;
        public Func<ItemDrop.ItemData, bool> Valid = null!;
    }

    [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.UpdateInventory))]
    internal static class UpdateInventory_Patch
    {
        internal static float leftOffset = 643f;
        internal const float tileSize = 70f;

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