namespace AzuEPI.Game.Patches;

public class TombstonePatches
{
    [HarmonyPatch(typeof(TombStone), nameof(TombStone.Awake))]
    private static class TombStoneAwakePatch
    {
        private static void Postfix(TombStone __instance)
        {
            Container container = __instance.GetComponent<Container>();
            int height = API.GetFullHeight(container.m_width);
            container.m_height = height;
            if (container.m_inventory != null)
                container.m_inventory.m_height = height;
        }
    }

    [HarmonyPatch(typeof(TombStone), nameof(TombStone.Interact))]
    private static class TombStoneInteractPatch
    {
        [HarmonyPriority(Priority.First)]
        private static void Prefix(TombStone __instance, bool hold, Container ___m_container)
        {
            if (hold) return;

            int targetHeight = API.GetFullHeight(___m_container.m_width);

            bool containerNeedsFix = ___m_container.m_height < targetHeight;
            bool inventoryNeedsFix = ___m_container.m_inventory != null && ___m_container.m_inventory.m_height < targetHeight;

            if (!containerNeedsFix && !inventoryNeedsFix) return;

            if (containerNeedsFix)
            {
                AzuExtendedPlayerInventoryLogger.LogDebug($"TombStone Interact: Adjusting container height {___m_container.m_height} -> {targetHeight}");
                ___m_container.m_height = targetHeight;
            }

            if (inventoryNeedsFix)
            {
                AzuExtendedPlayerInventoryLogger.LogDebug($"TombStone Interact: Adjusting inventory height {___m_container.m_inventory.m_height} -> {targetHeight}");
                ___m_container.m_inventory.m_height = targetHeight;
            }

            ___m_container.m_lastRevision = 0;
        }
    }

    [HarmonyPatch(typeof(TombStone), nameof(TombStone.OnTakeAllSuccess))]
    private static class AutoEquipAfterTombstoneGrab
    {
        private static void Postfix(TombStone __instance)
        {
            // Mimic vanilla check
            Player localPlayer = Player.m_localPlayer;
            if (!localPlayer || localPlayer.GetInventory() == null)
                return;

            if (__instance.m_body)
            {
                if (Player.m_enableAutoPickup && __instance.m_body.transform.root.gameObject != __instance.gameObject && __instance.TryGetComponent(out FloatingTerrain floatingTerrain))
                {
                    floatingTerrain.m_lastHeightmap = null;
                    Object.Destroy(__instance.m_body.gameObject);
                }
            }

            if (AutoEquip.Value.isOn())
            {
                Inventory inventory = localPlayer.GetInventory();
                IEnumerable<SlotSnapshot> snaps = API.GetEquipmentSlotSnapshots(inventory);
                foreach (SlotSnapshot snap in snaps)
                {
                    ItemDrop.ItemData? itemAt = inventory.GetItemAt(snap.GridPos.x, snap.GridPos.y);
                    if (itemAt != null)
                        localPlayer.EquipItem(itemAt);
                }
            }

            Container? container = __instance.m_container;
            if (!(container?.GetInventory()?.NrOfItems() > 0)) return;
            AzuExtendedPlayerInventoryLogger.LogDebug($"TombStone still has {container.GetInventory().NrOfItems()} items after TakeAll, opening container UI");
            container.Interact(localPlayer, false, false);
        }
    }

    [HarmonyPatch(typeof(TombStone), nameof(TombStone.EasyFitInInventory))]
    private static class EasyFitInInventory_SimulateFit
    {
        [HarmonyPriority(Priority.Low)]
        private static bool Prefix(TombStone __instance, Player player, ref bool __result)
        {
            Inventory? playerInv = player?.GetInventory();
            Inventory? tombInv = __instance.m_container?.GetInventory();
            if (playerInv == null || tombInv == null) return true;

            if (SimulateFit(tombInv, playerInv)) return true; // slot check passed – let vanilla do the weight check
            AzuExtendedPlayerInventoryLogger.LogDebug("EasyFitInInventory simulation: not all items fit – opening container instead of auto-looting.");
            __result = false;
            return false;
        }

        private static bool SimulateFit(Inventory tombInv, Inventory playerInv)
        {
            int width = playerInv.GetWidth();
            int normalRows = Layout.BaseInventoryHeight + ExtraRows.Value;

            HashSet<Vector2i> claimed = [];
            foreach (ItemDrop.ItemData existing in playerInv.GetAllItems())
                claimed.Add(existing.m_gridPos);

            foreach (ItemDrop.ItemData item in tombInv.GetAllItems())
            {
                Vector2i pos = FindVirtualFreePos(playerInv, item, claimed, width, normalRows);
                if (pos.x < 0)
                {
                    AzuExtendedPlayerInventoryLogger.LogDebug($"EasyFitInInventory simulation: '{item.m_shared.m_name}' has no available slot.");
                    return false;
                }

                claimed.Add(pos);
            }

            return true;
        }

        private static Vector2i FindVirtualFreePos(Inventory inv, ItemDrop.ItemData item, HashSet<Vector2i> claimed, int width, int normalRows)
        {
            // the simulation must do the same – otherwise it "spends" normal row
            foreach (Vector2i pos in inv.EnumerateEquipmentCells())
            {
                if (claimed.Contains(pos)) continue;
                if (!API.TryGetSlotIndexAtGridPos(inv, pos, out int slotIndex)) continue;
                if (!API.SlotValidates(slotIndex, item)) continue;
                return pos;
            }

            for (int y = 0; y < normalRows; y++)
            for (int x = 0; x < width; x++)
            {
                Vector2i pos = new(x, y);
                if (!claimed.Contains(pos)) return pos;
            }

            foreach (Vector2i pos in inv.EnumerateQuickCells())
            {
                if (!claimed.Contains(pos)) return pos;
            }

            return new Vector2i(-1, -1);
        }
    }

    [HarmonyPatch(typeof(TombStone), nameof(TombStone.EasyFitInInventory))]
    private static class TemporarilyIncreaseCarryWeightAndInventoryHeight
    {
        private struct TempState
        {
            public int Height;
            public float TempWeight;
            public bool PrevPickup;
            public bool ChangedPickup;
        }

        private static void Prefix(TombStone __instance, Player player, out TempState __state)
        {
            __state = default;
            __state.PrevPickup = Player.m_enableAutoPickup;
            if (Player.m_enableAutoPickup)
            {
                Player.m_enableAutoPickup = false; // Temporarily disable auto pickup to prevent NRE
                __state.ChangedPickup = true;
            }

            float tempWeight = 0f;
            tempWeight += __instance?.m_lootStatusEffect is SE_Stats { m_addMaxCarryWeight: > 0f } se ? se.m_addMaxCarryWeight : 0f;
            {
                Inventory? tombInv = __instance?.m_container?.GetInventory();
                float sum = 0f;
                if (tombInv != null)
                {
                    foreach (ItemDrop.ItemData itemData in tombInv.GetAllItems())
                    {
                        if (itemData?.m_shared?.m_equipStatusEffect is not SE_Stats { m_addMaxCarryWeight: > 0f } equipSe) continue;
                        if (API.TryGetSlotIndexAtGridPos(tombInv, itemData.m_gridPos, out int slotIndex) && API.SlotValidates(slotIndex, itemData))
                        {
                            sum += equipSe.m_addMaxCarryWeight;
                        }
                    }
                }

                tempWeight += sum;
            }

            __state.TempWeight += tempWeight + 150f;
            __state.Height = 0; // Don't adjust height - inventory already has correct size from UpdateInventorySize()
            player.m_maxCarryWeight += __state.TempWeight;
        }

        private static void Finalizer(Player player, TempState __state)
        {
            if (__state.ChangedPickup)
                Player.m_enableAutoPickup = __state.PrevPickup;
            player.m_maxCarryWeight -= __state.TempWeight;
            // Don't restore height - we didn't change it
        }
    }
}