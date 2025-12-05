using AzuEPI.Core.Input;
using AzuEPI.Core.InventoryHandlers;

namespace AzuEPI.Game.Patches;

public class TombstonePatches
{
    [HarmonyPatch(typeof(TombStone), nameof(TombStone.Awake))]
    private static class TombStoneAwakePatch
    {
        private static void Prefix(TombStone __instance)
        {
            AzuExtendedPlayerInventoryLogger.LogDebug("TombStone_Awake");

            int height = API.GetFullHeight(__instance.GetComponent<Container>().m_width);

            __instance.GetComponent<Container>().m_height = height;
        }
    }

    [HarmonyPatch(typeof(TombStone), nameof(TombStone.Interact))]
    private static class TombStoneInteractPatch
    {
        private static void Prefix(TombStone __instance, bool hold, Container ___m_container)
        {
            if (hold) return;
            AzuExtendedPlayerInventoryLogger.LogDebugDebug("TombStone_Interact");
            int num = API.GetFullHeight(___m_container.m_width);
            ___m_container.m_height = num;
            string base64String = ___m_container.m_nview.GetZDO().GetString(ZDOVars.s_items);
            if (string.IsNullOrEmpty(base64String))
                return;
            ZPackage pkg = new(base64String);
            ___m_container.m_loading = true;
            ___m_container.m_inventory.Load(pkg);
            ___m_container.m_loading = false;
            ___m_container.m_lastRevision = ___m_container.m_nview.GetZDO().DataRevision;
            ___m_container.m_lastDataString = base64String;
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

            if (!AutoEquip.Value.isOn()) return;
            IEnumerable<SlotSnapshot> snaps = API.GetEquipmentSlotSnapshots(localPlayer.GetInventory());
            foreach (SlotSnapshot snap in snaps)
            {
                ItemDrop.ItemData? itemAt = localPlayer.GetInventory().GetItemAt(snap.GridPos.x, snap.GridPos.y);
                if (itemAt != null)
                    localPlayer.EquipItem(itemAt);
            }
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
            if (AutoEquip.Value.isOn())
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
            __state.Height = AddEquipmentRow.Value.isOn() ? API.GetAddedRows(Player.m_localPlayer.m_inventory.m_width) : 0;
            player.m_maxCarryWeight += __state.TempWeight;
            player.m_inventory.m_height += __state.Height;
        }

        private static void Postfix()
        {
            InventoryHealth.InventoryFix();
        }

        private static void Finalizer(Player player, TempState __state)
        {
            if (__state.ChangedPickup)
                Player.m_enableAutoPickup = __state.PrevPickup;
            player.m_maxCarryWeight -= __state.TempWeight;
            player.m_inventory.m_height -= __state.Height;
        }
    }
}