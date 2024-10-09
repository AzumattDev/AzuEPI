using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using UnityEngine;
using static AzuExtendedPlayerInventory.AzuExtendedPlayerInventoryPlugin;

namespace AzuExtendedPlayerInventory.EPI.QAB
{
    [HarmonyPatch(typeof(HotkeyBar), nameof(HotkeyBar.UpdateIcons))]
    internal static class HotkeyBar_UpdateIcons_QuickAccessBar
    {
        [HarmonyPriority(Priority.Last)]
        internal static bool Prefix(HotkeyBar __instance, Player player)
        {
            if (__instance.name != ExtendedPlayerInventory.QABName)
                return true;

            ExtendedPlayerInventory.QuickSlots.UpdateHotkeyBar(__instance, player);

            return false;
        }
    }

    [HarmonyPatch(typeof(HotkeyBar), nameof(HotkeyBar.Update))]
    public static class HotkeyBar_Update_PreventCall
    {
        public static bool Prefix()
        {
            // Everything controlled in HotkeyBarController.UpdateHotkeyBars() run from Hud.Update
            return false;
        }
    }

    internal static class HotkeyBarController
    {
        public static List<HotkeyBar> HotkeyBars { get; set; } = null!;

        public static int SelectedHotkeyBarIndex { get; set; } = -1;

        // Runs every frame Hud.Update
        internal static void UpdateHotkeyBars()
        {
            var player = Player.m_localPlayer;
            if (HotkeyBars == null)
            {
                try
                {
                    HotkeyBars = Hud.instance.transform.parent.GetComponentsInChildren<HotkeyBar>().ToList();
                }
                catch
                {
                    AzuExtendedPlayerInventoryLogger.LogError($"Failed to get hotkey bars from Hud. The parent transform may have changed. {Hud.instance.transform.parent.name}");
                    return;
                }
            }

            if (player != null)
                if (IsValidHotkeyBarIndex())
                    UpdateHotkeyBarInput(HotkeyBars[SelectedHotkeyBarIndex]);
                else
                    UpdateInitialHotkeyBarInput();

            foreach (var hotkeyBar in HotkeyBars)
            {
                if (hotkeyBar != null && hotkeyBar.m_elements != null)
                {
                    ValidateHotkeyBarSelection(hotkeyBar);
                    hotkeyBar.UpdateIcons(player);
                }
            }
        }

        internal static void ClearBars()
        {
            HotkeyBars = null!;
            SelectedHotkeyBarIndex = -1;
        }

        internal static void DeselectBars()
        {
            SelectedHotkeyBarIndex = -1;

            if (HotkeyBars != null)
                foreach (var hotkeyBar in HotkeyBars)
                    hotkeyBar.m_selected = -1;
        }

        private static bool IsValidHotkeyBarIndex() => SelectedHotkeyBarIndex >= 0 && SelectedHotkeyBarIndex < HotkeyBars.Count;

        private static void UpdateInitialHotkeyBarInput()
        {
            if (ZInput.GetButtonDown("JoyDPadLeft") || ZInput.GetButtonDown("JoyDPadRight"))
                SelectHotkeyBar(0, false);
        }

        private static void UpdateHotkeyBarInput(HotkeyBar hotkeyBar)
        {
            var player = Player.m_localPlayer;
            var canUseItem = hotkeyBar.m_selected >= 0 && player != null && !InventoryGui.IsVisible()
                             && !Menu.IsVisible() && !GameCamera.InFreeFly();
            if (canUseItem && player != null)
            {
                if (ZInput.GetButtonDown("JoyDPadLeft"))
                {
                    if (hotkeyBar.m_selected == 0 && ShowQuickSlots.Value.IsOn())
                    {
                        GoToHotkeyBar(SelectedHotkeyBarIndex - 1);
                    }
                    else
                    {
                        hotkeyBar.m_selected = Mathf.Max(0, hotkeyBar.m_selected - 1);
                    }
                }
                else if (ZInput.GetButtonDown("JoyDPadRight"))
                {
                    if (hotkeyBar.m_selected == hotkeyBar.m_elements.Count - 1 && ShowQuickSlots.Value.IsOn())
                    {
                        GoToHotkeyBar(SelectedHotkeyBarIndex + 1);
                    }
                    else
                    {
                        hotkeyBar.m_selected = Mathf.Min(hotkeyBar.m_elements.Count - 1, hotkeyBar.m_selected + 1);
                    }
                }

                if (ZInput.GetButtonDown("JoyDPadUp"))
                {
                    if (hotkeyBar.name == "QuickAccessBar" && ShowQuickSlots.Value.IsOn())
                    {
                        var quickSlotInventory = player.m_inventory;
                        int width = quickSlotInventory.GetWidth();
                        int adjustedHeight = quickSlotInventory.GetHeight() - API.GetAddedRows(width);
                        int index = adjustedHeight * width + ExtendedPlayerInventory.EquipmentSlotsCount + hotkeyBar.m_selected;

                        var item = quickSlotInventory.GetItemAt(index % width, index / width);
                        if (item != null)
                        {
                            AzuExtendedPlayerInventoryLogger.LogInfo($"QuickAccessBar item {item.m_shared.m_name}");
                            player.UseItem(null, item, false);
                        }
                    }
                    else
                    {
                        if (ZInput.GetButtonDown("JoyHotbarUse") && !ZInput.GetButton("JoyAltKeys"))
                            player.UseHotbarItem(hotkeyBar.m_selected + 1);
                    }
                }
            }

            ValidateHotkeyBarSelection(hotkeyBar);
        }

        private static void ValidateHotkeyBarSelection(HotkeyBar hotkeyBar)
        {
            if (hotkeyBar.m_elements != null && hotkeyBar.m_selected > hotkeyBar.m_elements.Count - 1)
                hotkeyBar.m_selected = Mathf.Max(0, hotkeyBar.m_elements.Count - 1);
        }

        private static void GoToHotkeyBar(int newIndex)
        {
            if (newIndex < 0 || newIndex >= HotkeyBars.Count)
                return;

            var fromRight = newIndex < SelectedHotkeyBarIndex;
            SelectHotkeyBar(newIndex, fromRight);
        }

        private static void SelectHotkeyBar(int index, bool fromRight)
        {
            if (index < 0 || index >= HotkeyBars.Count)
                return;

            SelectedHotkeyBarIndex = index;
            for (var i = 0; i < HotkeyBars.Count; ++i)
            {
                var hotkeyBar = HotkeyBars[i];
                if (i == index)
                {
                    hotkeyBar.m_selected = fromRight ? hotkeyBar.m_elements.Count - 1 : 0;
                }
                else
                {
                    hotkeyBar.m_selected = -1;
                }
            }
        }
    }
}