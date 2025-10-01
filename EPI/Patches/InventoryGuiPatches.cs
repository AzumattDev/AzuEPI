using System.Reflection.Emit;
using AzuEPI;
using AzuEPI.EPI;
using AzuEPI.EPI.Utilities;
using AzuEPI.Loadout;
using AzuEPI.PlayerPreview;
using AzuEPI.Vanity;
using AzuExtendedPlayerInventory;

namespace AzuExtendedPlayerInventory.EPI.Patches;

public class InventoryGuiPatches
{
    [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.Awake))]
    [HarmonyPriority(Priority.Last)]
    static class ReparentPlayerGridInventoryGuiAwakePatch
    {
        internal static RectTransform _epiPreviewRect;

        static void Postfix(InventoryGui __instance)
        {
            var selectedFrame = __instance.m_crafting.Find("selected_frame").GetComponent<RectTransform>();
            selectedFrame.anchorMin = new Vector2(-0.80f, 0);

            Transform bkg = Object.Instantiate(__instance.m_crafting.Find("Bkg"), __instance.m_crafting);
            var index = selectedFrame.GetSiblingIndex();
            bkg.SetSiblingIndex(index + 2);
            bkg.name = "AzuPlayerBkg";
            bkg.GetComponent<RectTransform>().anchorMin = new Vector2(-0.80f, 0);
            __instance.m_crafting.Find("RepairSimple").GetComponent<RectTransform>().anchoredPosition += new Vector2(-480f, 0f);
            __instance.m_crafting.Find("RepairButton").GetComponent<RectTransform>().anchoredPosition += new Vector2(-480f, 0f);
            __instance.m_crafting.SetSiblingIndex(1);

            if (AzuEPICharacterPanel.instance == null)
                new GameObject("AzuEPI_RuntimePanel").AddComponent<AzuEPICharacterPanel>();

            var panel = AzuEPICharacterPanel.instance;

            var previewParent = new GameObject("AzuEPI_PlayerPreview", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(RectMask2D), typeof(PlayerRotationController));
            var previewParentRT = (RectTransform)previewParent.transform;
            previewParentRT.SetParent(__instance.m_crafting, false);

            var img = previewParent.GetComponent<Image>();
            img.color = new Color(0f, 0f, 0f, 0.565f);
            previewParentRT.anchorMin = new Vector2(0f, 0.14f);
            previewParentRT.anchorMax = new Vector2(1f, 0.885f);
            previewParentRT.sizeDelta = new Vector2(-300f, 0f);
            previewParentRT.anchoredPosition = new Vector2(-507f, 0f);

            var go = new GameObject("PlayerPreview", typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage));
            var rt = (RectTransform)go.transform;
            rt.SetParent(previewParentRT, false);

            Vector2 size = new Vector2(500f, 630f);
            rt.sizeDelta = size;

            rt.anchoredPosition = new Vector2(0, 0);

            var raw = go.GetComponent<RawImage>();
            raw.raycastTarget = false;
            raw.color = Color.white;

            panel.render = rt;
            panel.renderRawImage = raw;

            PlayerPreviewManager.Initialize();
            PlayerPreviewManager.Instance.CreatePreviewCamera();
            PlayerPreviewManager.Instance.CreatePreviewLights();
            PlayerPreviewManager.Instance.UpdateRenderTexture();

            _epiPreviewRect = rt;

            VanityPanelController.EnsureBuilt(__instance);
            VanityPanelController.SetVisible(false);

            PersonalLoadoutGui.BuildToggleButton(__instance);

            var charName = Object.Instantiate(__instance.m_info.transform.Find("TitlePanel"), previewParentRT);
            charName.name = "AzuEPI_CharacterName";
            charName.GetComponentsInChildren<TextMeshProUGUI>().FirstOrDefault()!.text = Game.instance.GetPlayerProfile().GetName();
            foreach (Transform child in charName)
                if (child.name.Contains("BraidLine"))
                    Object.Destroy(child.gameObject);
            __instance.m_crafting.Find("Bkg").GetComponent<Image>().enabled = false;
        }
    }

    [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.Show))]
    private static class InventoryGuiShowPatch
    {
        private static void Postfix(InventoryGui __instance)
        {
            if (Player.m_localPlayer == null)
                return;
            Utilities.InventoryFix();
        }
    }

    [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.OnSelectedItem))]
    private static class InventoryGuiOnSelectedItemPatch
    {
        private static void Prefix(InventoryGui __instance, InventoryGrid grid, ItemDrop.ItemData item, Vector2i pos, InventoryGrid.Modifier mod)
        {
            Player localPlayer = Player.m_localPlayer;
            if (localPlayer.IsTeleporting())
                return;
            if (__instance.m_dragGo && localPlayer.IsItemEquiped(__instance.m_dragItem))
            {
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

                int width = inventory.GetWidth();
                int height = inventory.GetHeight();
                int requiredRows = API.GetAddedRows(width);

                int num = width * (height - requiredRows);
                ItemDrop.ItemData?[] equippedItems = new ItemDrop.ItemData[UpdateInventory_Patch.slots.Count];
                for (int i = 0; i < UpdateInventory_Patch.slots.Count; ++i)
                {
                    Slot? slot = UpdateInventory_Patch.slots[i];
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
                            (which <= -1 || UpdateInventory_Patch.slots[which] is not EquipmentSlot slot || !slot.Valid(t) || ExtendedPlayerInventory.equipItems[which] == t || (AzuExtendedPlayerInventoryPlugin.AutoEquip.Value == AzuExtendedPlayerInventoryPlugin.Toggle.On && !player.EquipItem(t, false))))
                        {
                            Vector2i vector2I = inventory.FindEmptySlot(true);
                            if (vector2I.x < 0 || vector2I.y < 0 || vector2I.y >= height - requiredRows)
                            {
                                // it will drop them simply because it cannot be added to the inventory and it's "outside" the normal inventory when it breaks.
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

                if (___m_animator.GetBool(ExtendedPlayerInventory.Visible))
                {
                    if (AzuEPICharacterPanel.playerPreviewComp && Player.m_localPlayer)
                        VECloneSync.MirrorFrom(Player.m_localPlayer, AzuEPICharacterPanel.playerPreviewComp);
                }
            }

            if (!___m_animator.GetBool(ExtendedPlayerInventory.Visible))
                return;

            RectTransform bkgRect = __instance.m_player.Find("Bkg").GetComponent<RectTransform>();
            bkgRect.anchorMin = new Vector2(0.0f, (AzuExtendedPlayerInventoryPlugin.ExtraRows.Value + (AzuExtendedPlayerInventoryPlugin.AddEquipmentRow.Value == AzuExtendedPlayerInventoryPlugin.Toggle.Off || AzuExtendedPlayerInventoryPlugin.DisplayEquipmentRowSeparate.Value == AzuExtendedPlayerInventoryPlugin.Toggle.On ? 0 : API.GetAddedRows(Player.m_localPlayer.m_inventory.GetWidth()))) * -0.25f);

            if (AzuExtendedPlayerInventoryPlugin.AddEquipmentRow.Value == AzuExtendedPlayerInventoryPlugin.Toggle.Off)
                return;

            var equipmentBkgTransform = __instance.m_player.Find(ExtendedPlayerInventory.AzuBkgName);
            var dropallButton = __instance.m_player.Find(ExtendedPlayerInventory.DropAllButtonName);

            switch (AzuExtendedPlayerInventoryPlugin.DisplayEquipmentRowSeparate.Value)
            {
                /*case AzuExtendedPlayerInventoryPlugin.Toggle.On when equipmentBkgTransform == null:
                {
                    Transform transform = Object.Instantiate(bkgRect.transform, __instance.m_player);
                    transform.SetAsFirstSibling();
                    transform.name = ExtendedPlayerInventory.AzuBkgName;
                    RectTransform rectTransform = transform.GetComponent<RectTransform>();
                    rectTransform.anchorMin = new Vector2(1f, 0.0f);
                    Vector2 maxAnchor = new(1.13f + Math.Max(AzuExtendedPlayerInventoryPlugin.Hotkeys.Length, (UpdateInventory_Patch.slots.Count - 1) / 3) * UpdateInventory_Patch.tileSize / 570, 1f);
                    if (Chainloader.PluginInfos.TryGetValue(ExtendedPlayerInventory.MinimalUiguid, out var pluginInfo) && pluginInfo is not null) maxAnchor.x += 0.03f;

                    rectTransform.anchorMax = maxAnchor;
                    InventoryGui.instance.m_playerGrid.m_gridRoot.GetComponent<RectTransform>().anchorMax = maxAnchor;
                    InventoryGui.instance.m_playerGrid.m_gridRoot.GetComponent<Image>().raycastTarget = false;

                    break;
                }*/
                case AzuExtendedPlayerInventoryPlugin.Toggle.On when equipmentBkgTransform == null:
                {
                    /*Transform transform = Object.Instantiate(bkgRect.transform, __instance.m_player);
                    transform.SetAsFirstSibling();
                    transform.name = ExtendedPlayerInventory.AzuBkgName;
                    RectTransform rectTransform = transform.GetComponent<RectTransform>();
                    rectTransform.anchorMin = new Vector2(1f, 0f);*/

                    float columns = 2f;
                    float gapTiles = 4f;
                    float padding = 0.6f;

                    float extraTiles = columns + gapTiles + padding;
                    float extraX = (extraTiles * UpdateInventory_Patch.tileSize) / 570f;

                    Vector2 maxAnchor = new(1f + extraX, 1f);
                    if (Chainloader.PluginInfos.TryGetValue(ExtendedPlayerInventory.MinimalUiguid, out var pi) && pi != null)
                        maxAnchor.x += 0.03f;

                    //rectTransform.anchorMax = maxAnchor;
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
                RectTransform dropAllButtonTransform = null!;

                if (dropallButton == null)
                {
                    Transform dropAllButtonPrefab = __instance.m_takeAllButton.transform;
                    dropAllButtonTransform = Object.Instantiate(dropAllButtonPrefab, __instance.m_player).GetComponent<RectTransform>();
                    dropAllButtonTransform.name = ExtendedPlayerInventory.DropAllButtonName;
                    dropAllButtonTransform.GetComponentInChildren<TMP_Text>().text = "Drop All";
                    var buttonComp = dropAllButtonTransform.GetComponent<Button>();
                    buttonComp.onClick.RemoveAllListeners();
                    buttonComp.onClick.AddListener(() => Console.instance.TryRunCommand("azuepi.dropall"));
                }
                else
                {
                    dropAllButtonTransform = dropallButton.GetComponent<RectTransform>();
                }

                dropAllButtonTransform.SetAsFirstSibling();
                dropAllButtonTransform.anchorMin = new Vector2(0.0f, 1.0f);
                dropAllButtonTransform.anchorMax = new Vector2(0.0f, 1.0f);
                dropAllButtonTransform.pivot = new Vector2(0.0f, 1.0f);
                dropAllButtonTransform.anchoredPosition = AzuExtendedPlayerInventoryPlugin.DropAllButtonPosition.Value;
                dropAllButtonTransform.sizeDelta = new Vector2(100, 30);
            }
            else
            {
                if (dropallButton != null) Object.DestroyImmediate(dropallButton.gameObject);
            }
        }
    }

    internal class Slot
    {
        public string Name = null!;
        public Vector2 Position;
        public bool IsQuickSlot = false;
        public bool Occupied = false;
        public EquipmentSlot? EquipmentSlot => this as EquipmentSlot;
    }

    internal class EquipmentSlot : Slot
    {
        public Func<Player, ItemDrop.ItemData?> Get = null!;
        public Func<ItemDrop.ItemData, bool> Valid = null!;
    }

    [HarmonyPatch(typeof(InventoryGrid), nameof(InventoryGrid.CreateItemTooltip), typeof(ItemDrop.ItemData), typeof(UITooltip))]
    public static class ItemTooltipControllerFollowSelectionPatch
    {
        [HarmonyPriority(Priority.Last)]
        public static bool Prefix(ItemDrop.ItemData item, UITooltip tooltip, out string __state)
        {
            __state = null;
            if (ZInput.IsGamepadActive() && !ZInput.IsMouseActive())
            {
                tooltip.Set(item.m_shared.m_name, item.GetTooltip());
                return false;
            }

            return true;
        }
    }

    [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.UpdateInventory))]
    internal static class UpdateInventory_Patch
    {
        internal const float tileSize = 70f;
        //internal static float leftOffset = 693f;

        internal static float equipOriginX = -430f;

        internal static float equipOriginY = -75f;

        internal static float columnGapTiles = 4f;

        internal static readonly List<Slot?> slots = new()
        {
            new EquipmentSlot { Name = AzuExtendedPlayerInventoryPlugin.HelmetText.Value, IsQuickSlot = false, Get = player => player.m_helmetItem, Valid = item => item.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Helmet },
            new EquipmentSlot { Name = AzuExtendedPlayerInventoryPlugin.ChestText.Value, IsQuickSlot = false, Get = player => player.m_chestItem, Valid = item => item.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Chest },
            new EquipmentSlot { Name = AzuExtendedPlayerInventoryPlugin.LegsText.Value, IsQuickSlot = false, Get = player => player.m_legItem, Valid = item => item.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Legs },
            new EquipmentSlot { Name = AzuExtendedPlayerInventoryPlugin.BackText.Value, IsQuickSlot = false, Get = player => player.m_shoulderItem, Valid = item => item.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Shoulder },
            new EquipmentSlot { Name = AzuExtendedPlayerInventoryPlugin.UtilityText.Value, IsQuickSlot = false, Get = player => player.m_utilityItem, Valid = item => item.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Utility },
        };

        static UpdateInventory_Patch()
        {
            for (int i = 0; i < AzuExtendedPlayerInventoryPlugin.Hotkeys.Length; ++i)
                slots.Add(new Slot
                {
                    Name = AzuExtendedPlayerInventoryPlugin.HotkeyTexts[i].Value.IsNullOrWhiteSpace()
                        ? AzuExtendedPlayerInventoryPlugin.Hotkeys[i].Value.ToString()
                        : AzuExtendedPlayerInventoryPlugin.HotkeyTexts[i].Value,
                    IsQuickSlot = true,
                });
        }

        internal static void ResizeSlots()
        {
            const int rowsPerCol = 8;
            float leftX = equipOriginX;
            float rightX = equipOriginX + tileSize * (1f + columnGapTiles);
            float yBase = equipOriginY;

            int equipCount = 0;
            while (equipCount < slots.Count && slots[equipCount] is EquipmentSlot) equipCount++;

            int leftUsed = Math.Min(rowsPerCol, equipCount);
            int rightUsed = Math.Max(0, Math.Min(rowsPerCol, equipCount - rowsPerCol));

            for (int i = 0; i < equipCount; ++i)
            {
                bool leftCol = i < rowsPerCol;
                int row = leftCol ? i : (i - rowsPerCol);
                float x = leftCol ? leftX : rightX;
                float y = yBase - row * tileSize;
                slots[i]!.Position = new Vector2(x, y);
            }

            int quickCount = AzuExtendedPlayerInventoryPlugin.Hotkeys.Length;
            int quickStart = equipCount;

            float tallestRows = Mathf.Max(leftUsed, rightUsed);
            float bottomY = yBase - (tallestRows - 1) * tileSize;

            float spanWidth = (rightX - leftX) + tileSize;
            float rowWidth = quickCount * tileSize;
            float startX = leftX + (spanWidth - rowWidth) * 0.5f;

            for (int i = 0; i < quickCount; ++i)
                slots[quickStart + i]!.Position = new Vector2(startX + i * tileSize, bottomY);
        }

        private static void Postfix(InventoryGrid ___m_playerGrid)
        {
            if (AzuExtendedPlayerInventoryPlugin.AddEquipmentRow.Value == AzuExtendedPlayerInventoryPlugin.Toggle.Off)
                return;

            try
            {
                Player? player = Player.m_localPlayer;
                Inventory inventory = player.GetInventory();

                int requiredRows = API.GetAddedRows(inventory.GetWidth());

                int baseIndex = inventory.GetWidth() * (inventory.GetHeight() - requiredRows);

                Vector2 baseGridPos = new((___m_playerGrid.GetComponent<RectTransform>().rect.width - ___m_playerGrid.GetWidgetSize().x) / 2f, 0.0f);

                for (int i = 0; i < slots.Count; ++i)
                {
                    var currentElement = ___m_playerGrid.m_elements[baseIndex + i];
                    GameObject currentChild = currentElement.m_go;
                    currentChild.SetActive(true);
                    currentChild.name = $"AzuEPI_Slot_{slots[i]?.Name}";
                    // if .m_used assume it's occupied
                    slots[i].Occupied = currentElement.m_used;
                    ExtendedPlayerInventory.SetSlotText(slots[i]?.Name, currentChild.transform);
                    if (AzuExtendedPlayerInventoryPlugin.DisplayEquipmentRowSeparate.Value == AzuExtendedPlayerInventoryPlugin.Toggle.On)
                    {
                        if (InventoryGui.instance)
                            currentChild.GetComponent<RectTransform>().SetParent(InventoryGui.instance.m_crafting);
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
                    ___m_playerGrid.m_elements[i].m_used = true;
                }
            }
            catch (Exception ex)
            {
                AzuExtendedPlayerInventoryPlugin.AzuExtendedPlayerInventoryLogger.LogDebug($"Exception in EPI Update Inventory: {ex}");
            }
        }
    }

    [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.DoCrafting))]
    internal static class UpgradeInPlace_Patch
    {
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator il)
        {
            var list = new List<CodeInstruction>(instructions);

            var f_mCraftUpgradeItem = AccessTools.Field(typeof(InventoryGui), nameof(InventoryGui.m_craftUpgradeItem));

            var m_GetMaxDurability = AccessTools.Method(typeof(ItemDrop.ItemData), nameof(ItemDrop.ItemData.GetMaxDurability));
            var m_NoCostCheat = AccessTools.Method(typeof(Player), nameof(Player.NoCostCheat));
            var m_GetInventory = AccessTools.Method(typeof(Humanoid), nameof(Humanoid.GetInventory));
            var m_RemoveItem_byName = AccessTools.Method(typeof(Inventory), nameof(Inventory.RemoveItem), new[] { typeof(string), typeof(int), typeof(int), typeof(bool) });
            var m_UpdateCraftingPanel = AccessTools.Method(typeof(InventoryGui), nameof(InventoryGui.UpdateCraftingPanel), new[] { typeof(bool) });
            var m_ConsumeResources = AccessTools.Method(typeof(Player), nameof(Player.ConsumeResources), new[] { typeof(Piece.Requirement[]), typeof(int), typeof(int), typeof(int) });

            var m_GetGlobalKey_Enum = AccessTools.Method(typeof(ZoneSystem), nameof(ZoneSystem.GetGlobalKey), new[] { typeof(GlobalKeys) });
            var m_get_instance = AccessTools.PropertyGetter(typeof(ZoneSystem), nameof(ZoneSystem.instance));

            var obj_opImplicit = AccessTools.Method(typeof(UnityEngine.Object), "op_Implicit", new[] { typeof(UnityEngine.Object) });

            int guardIdx = -1, afterIfIdx = -1;
            for (int i = 0; i < list.Count - 2; ++i)
            {
                if (list[i].opcode == OpCodes.Ldarg_0 &&
                    list[i + 1].LoadsField(f_mCraftUpgradeItem) &&
                    (list[i + 2].opcode == OpCodes.Brfalse_S || list[i + 2].opcode == OpCodes.Brfalse))
                {
                    guardIdx = i;
                    var target = (Label)list[i + 2].operand;
                    afterIfIdx = IndexOfLabel(list, target);
                    break;
                }
            }

            if (guardIdx < 0 || afterIfIdx < 0)
            {
                Debug.LogError("[EPI] Transpiler: couldn’t find upgrade guard; leaving IL unchanged.");
                return list;
            }

            int effectsIdx = -1;
            for (int i = afterIfIdx; i < list.Count - 1; ++i)
            {
                if ((list[i].opcode == OpCodes.Ldloc_S || list[i].opcode == OpCodes.Ldloc) &&
                    list[i + 1].opcode == OpCodes.Call &&
                    Equals(list[i + 1].operand, obj_opImplicit))
                {
                    effectsIdx = i;
                    break;
                }
            }

            if (effectsIdx < 0)
            {
                AzuExtendedPlayerInventoryPlugin.AzuExtendedPlayerInventoryLogger.LogError("UpgradeInPlace_Patch Transpiler: couldn’t find EFFECTS anchor; leaving IL unchanged.");
                return list;
            }

            var effectsLbl = il.DefineLabel();
            list[effectsIdx].labels ??= new List<Label>();
            list[effectsIdx].labels.Add(effectsLbl);

            int trueBlockStart = guardIdx + 3;
            int trueBlockEnd = afterIfIdx;

            var IL = new List<CodeInstruction>();

            // upgradeItem.m_quality = num1;
            IL.Add(new CodeInstruction(OpCodes.Ldarg_0));
            IL.Add(CodeInstruction.LoadField(typeof(InventoryGui), nameof(InventoryGui.m_craftUpgradeItem)));
            IL.Add(new CodeInstruction(OpCodes.Ldloc_0));
            IL.Add(CodeInstruction.StoreField(typeof(ItemDrop.ItemData), nameof(ItemDrop.ItemData.m_quality)));

            // upgradeItem.m_durability = upgradeItem.GetMaxDurability();
            IL.Add(new CodeInstruction(OpCodes.Ldarg_0));
            IL.Add(CodeInstruction.LoadField(typeof(InventoryGui), nameof(InventoryGui.m_craftUpgradeItem)));
            IL.Add(new CodeInstruction(OpCodes.Dup));
            IL.Add(new CodeInstruction(OpCodes.Callvirt, m_GetMaxDurability));
            IL.Add(CodeInstruction.StoreField(typeof(ItemDrop.ItemData), nameof(ItemDrop.ItemData.m_durability)));

            // if (!player.NoCostCheat() && !ZoneSystem.instance.GetGlobalKey(GlobalKeys.NoCraftCost)) { consume … }
            var costDoneLbl = il.DefineLabel();
            var haveSingleLbl = il.DefineLabel();
            var afterCostLbl = il.DefineLabel();

            // if (player.NoCostCheat()) goto costDone;
            IL.Add(new CodeInstruction(OpCodes.Ldarg_1));
            IL.Add(new CodeInstruction(OpCodes.Callvirt, m_NoCostCheat));
            IL.Add(new CodeInstruction(OpCodes.Brtrue_S, costDoneLbl));

            // if (ZoneSystem.instance.GetGlobalKey(GlobalKeys.NoCraftCost)) goto costDone;
            IL.Add(new CodeInstruction(OpCodes.Call, m_get_instance));
            IL.Add(new CodeInstruction(OpCodes.Ldc_I4, (int)GlobalKeys.NoCraftCost));
            IL.Add(new CodeInstruction(OpCodes.Callvirt, m_GetGlobalKey_Enum));
            IL.Add(new CodeInstruction(OpCodes.Brtrue_S, costDoneLbl));

            // if (singleReqItem != null) RemoveItem(singleReqItem.m_shared.m_name, need, singleReqItem.m_quality, true);
            IL.Add(new CodeInstruction(OpCodes.Ldloc_S, (byte)4));
            IL.Add(new CodeInstruction(OpCodes.Brfalse_S, haveSingleLbl));

            // player.GetInventory().RemoveItem(name, need, quality, true);
            IL.Add(new CodeInstruction(OpCodes.Ldarg_1));
            IL.Add(new CodeInstruction(OpCodes.Callvirt, m_GetInventory));
            IL.Add(new CodeInstruction(OpCodes.Ldloc_S, (byte)4));
            IL.Add(CodeInstruction.LoadField(typeof(ItemDrop.ItemData), nameof(ItemDrop.ItemData.m_shared)));
            IL.Add(CodeInstruction.LoadField(typeof(ItemDrop.ItemData.SharedData), nameof(ItemDrop.ItemData.SharedData.m_name)));
            IL.Add(new CodeInstruction(OpCodes.Ldloc_3));
            IL.Add(new CodeInstruction(OpCodes.Ldloc_S, (byte)4));
            IL.Add(CodeInstruction.LoadField(typeof(ItemDrop.ItemData), nameof(ItemDrop.ItemData.m_quality)));
            IL.Add(new CodeInstruction(OpCodes.Ldc_I4_1));
            IL.Add(new CodeInstruction(OpCodes.Callvirt, m_RemoveItem_byName));
            IL.Add(new CodeInstruction(OpCodes.Br_S, afterCostLbl));

            IL.Add(MarkLabel(new CodeInstruction(OpCodes.Nop), haveSingleLbl));
            IL.Add(new CodeInstruction(OpCodes.Ldarg_1));
            IL.Add(new CodeInstruction(OpCodes.Ldarg_0));
            IL.Add(CodeInstruction.LoadField(typeof(InventoryGui), nameof(InventoryGui.m_craftRecipe)));
            IL.Add(CodeInstruction.LoadField(typeof(Recipe), nameof(Recipe.m_resources)));
            IL.Add(new CodeInstruction(OpCodes.Ldloc_0));
            IL.Add(new CodeInstruction(OpCodes.Ldc_I4_M1));
            IL.Add(new CodeInstruction(OpCodes.Ldc_I4_1));
            IL.Add(new CodeInstruction(OpCodes.Callvirt, m_ConsumeResources));
            IL.Add(MarkLabel(new CodeInstruction(OpCodes.Nop), afterCostLbl));

            IL.Add(MarkLabel(new CodeInstruction(OpCodes.Nop), costDoneLbl));

            // UpdateCraftingPanel(false);
            IL.Add(new CodeInstruction(OpCodes.Ldarg_0));
            IL.Add(new CodeInstruction(OpCodes.Ldc_I4_0));
            IL.Add(new CodeInstruction(OpCodes.Call, m_UpdateCraftingPanel));

            // jump to vanilla EFFECTS/STATS tail
            IL.Add(new CodeInstruction(OpCodes.Br, effectsLbl));

            list.RemoveRange(trueBlockStart, trueBlockEnd - trueBlockStart);
            list.InsertRange(trueBlockStart, IL);

            return list;
        }

        private static int IndexOfLabel(List<CodeInstruction> list, Label target)
        {
            for (int i = 0; i < list.Count; ++i)
            {
                var labels = list[i].labels;
                if (labels != null && labels.Contains(target))
                    return i;
            }

            return -1;
        }

        private static CodeInstruction MarkLabel(CodeInstruction ci, Label label)
        {
            ci.labels ??= new List<Label>();
            ci.labels.Add(label);
            return ci;
        }
    }
}