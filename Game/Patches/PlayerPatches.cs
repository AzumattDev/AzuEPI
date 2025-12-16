using AzuEPI.Core.Input;

namespace AzuEPI.Game.Patches;

public class PlayerPatches
{
    [HarmonyPatch(typeof(Player), nameof(Player.Awake))]
    private static class PlayerAwakePatch
    {
        private static void Prefix(Player __instance, Inventory ___m_inventory)
        {
            AzuExtendedPlayerInventoryLogger.LogDebugDebug("Player_Awake");

            int height = API.GetFullHeight(__instance.m_inventory.GetWidth());
            __instance.m_inventory.m_height = height;
            __instance.m_tombstone.GetComponent<Container>().m_height = height;
        }
    }

    [HarmonyPatch(typeof(Player), nameof(Player.OnSpawned))]
    private static class PlayerOnSpawnedPatch
    {
        public const string Sentinel = "<|>";
        public static Inventory QuickSlotInventory = new(nameof(QuickSlotInventory), null, 3, 1);
        public static Inventory EquipmentSlotInventory = new(nameof(EquipmentSlotInventory), null, 5, 1);
        private static readonly MethodInfo MemberwiseCloneMethod = AccessTools.DeclaredMethod(typeof(object), "MemberwiseClone");

        public static T Clone<T>(T input) where T : notnull
        {
            return (T)MemberwiseCloneMethod.Invoke(input, Array.Empty<object>());
        }

        private static void Postfix(Player __instance)
        {
            if (Player.m_localPlayer == null || Player.m_localPlayer != __instance)
                return;

            if (!Chainloader.PluginInfos.TryGetValue("randyknapp.mods.equipmentandquickslots", out PluginInfo? RandyEAQ)) Load(__instance);
        }

        public static void Load(Player fromPlayer)
        {
            if (fromPlayer == null)
            {
                AzuExtendedPlayerInventoryLogger.LogError("Tried to load an ExtendedPlayerData with a null player!");
                return;
            }

            LoadValue(fromPlayer, "ExtendedPlayerData", out string init);

            if (LoadValue(fromPlayer, "QuickSlotInventory", out string quickSlotData))
            {
                ZPackage pkg = new(quickSlotData);
                QuickSlotInventory.Load(pkg);
                fromPlayer.m_inventory.MoveAll(QuickSlotInventory);
                foreach (ItemDrop.ItemData? item in QuickSlotInventory.GetAllItems())
                {
                    if (item.m_dropPrefab == null) continue;
                    TryAddItemToInventory(fromPlayer, item, fromPlayer.m_inventory, false);
                }

                QuickSlotInventory.RemoveAll();

                pkg = new ZPackage();
                QuickSlotInventory.Save(pkg);
                SaveValue(fromPlayer, "QuickSlotInventory", pkg.GetBase64());
            }

            if (LoadValue(fromPlayer, "EquipmentSlotInventory", out string equipSlotData))
            {
                ZPackage pkg = new(equipSlotData);
                EquipmentSlotInventory.Load(pkg);
                //fromPlayer.m_inventory.MoveAll(EquipmentSlotInventory);
                foreach (ItemDrop.ItemData? item in EquipmentSlotInventory.GetAllItems())
                {
                    if (item.m_dropPrefab == null) continue;
                    TryAddItemToInventory(fromPlayer, item, fromPlayer.m_inventory);
                }

                EquipmentSlotInventory.RemoveAll();

                pkg = new ZPackage();
                EquipmentSlotInventory.Save(pkg);
                SaveValue(fromPlayer, "EquipmentSlotInventory", pkg.GetBase64());
            }
        }

        private static bool LoadValue(Player player, string key, out string value)
        {
            if (player.m_customData.TryGetValue(key, out value))
                return true;

            bool foundInKnownTexts = player.m_knownTexts.TryGetValue(key, out value);
            if (!foundInKnownTexts)
                key = Sentinel + key;
            foundInKnownTexts = player.m_knownTexts.TryGetValue(key, out value);
            if (foundInKnownTexts)
                AzuExtendedPlayerInventoryLogger.LogWarning("Loaded data from knownTexts. Will be converted to customData on save.");

            return foundInKnownTexts;
        }

        private static void SaveValue(Player player, string key, string value)
        {
            if (player.m_knownTexts.ContainsKey(key))
            {
                AzuExtendedPlayerInventoryLogger.LogWarning("Found KnownText for save data, converting to customData");
                player.m_knownTexts.Remove(key);
            }

            if (player.m_customData.ContainsKey(key))
                player.m_customData[key] = value;
            else
                player.m_customData.Add(key, value);
        }

        public static void TryAddItemToInventory(Player player, ItemDrop.ItemData itemData, Inventory fromInventory, bool useItem = true)
        {
            if (player.m_inventory.CanAddItem(itemData))
            {
                AzuExtendedPlayerInventoryLogger.LogInfo($"Adding {Localization.instance.Localize(itemData.m_shared.m_name)} to inventory");
                fromInventory.RemoveItem(itemData);
                player.m_inventory.AddItem(itemData);

                if (useItem) player.UseItem(player.GetInventory(), itemData, false);
            }
            else
            {
                AzuExtendedPlayerInventoryLogger.LogInfo($"Dropping {Localization.instance.Localize(itemData.m_shared.m_name)}");
                Transform transform = player.transform;
                ItemDrop itemDrop = ItemDrop.DropItem(itemData, itemData.m_stack, transform.position + transform.forward + transform.up, transform.rotation);
                if (itemDrop == null) return;
                if (itemDrop.m_itemData.m_equipped) itemDrop.m_itemData.m_equipped = false;

                bool pickedUp = player.Pickup(itemDrop.gameObject, false, false);
                if (pickedUp && useItem) player.UseItem(player.GetInventory(), itemDrop.m_itemData, false);
            }
        }
    }

    [HarmonyPatch(typeof(Player), nameof(Player.Update))]
    private static class PlayerUpdatePatch
    {
        private static readonly Dictionary<Player, Container> TombstoneContainerCache = new();

        private static void Postfix(Player __instance, ref Inventory ___m_inventory)
        {
            int width = ___m_inventory.GetWidth();
            int height = API.GetFullHeight(width);
            ___m_inventory.m_height = height;

            if (!TombstoneContainerCache.TryGetValue(__instance, out Container tombstoneContainer))
            {
                tombstoneContainer = __instance.m_tombstone.GetComponent<Container>();
                TombstoneContainerCache[__instance] = tombstoneContainer;
            }
            tombstoneContainer.m_height = height;
            if (InventoryHealth.IgnoreKeyPresses(true) || AddEquipmentRow.Value.isOff() || Hotkeys.Length == 0)
                return;

            int hotkey = 0;
            while (!Hotkeys[hotkey].Value.IsKeyDown())
                if (++hotkey == Hotkeys.Length)
                    return;

            int index = (Layout.BaseInventoryHeight + ExtraRows.Value) * width + InventoryGuiPatches.UpdateInventory_Patch.slots.Count - Hotkeys.Length + hotkey;
            ItemDrop.ItemData itemAt = ___m_inventory.GetItemAt(index % width, index / width);
            if (itemAt == null)
                return;
            __instance.UseItem(null, itemAt, true);
        }

        private static void CreateTombStone()
        {
            AzuExtendedPlayerInventoryLogger.LogDebugDebug($"Height {Player.m_localPlayer.m_tombstone.GetComponent<Container>().m_height}");
            GameObject gameObject = Object.Instantiate(Player.m_localPlayer.m_tombstone, Player.m_localPlayer.GetCenterPoint(), Player.m_localPlayer.transform.rotation);
            TombStone component = gameObject.GetComponent<TombStone>();
            AzuExtendedPlayerInventoryLogger.LogDebugDebug($"Height {gameObject.GetComponent<Container>().m_height}");
            AzuExtendedPlayerInventoryLogger.LogDebugDebug($"Inv height {gameObject.GetComponent<Container>().GetInventory().GetHeight()}");
            AzuExtendedPlayerInventoryLogger.LogDebugDebug($"Inv slots {gameObject.GetComponent<Container>().GetInventory().GetEmptySlots()}");
            for (int index = 0; index < gameObject.GetComponent<Container>().GetInventory().GetEmptySlots(); ++index)
                gameObject.GetComponent<Container>().GetInventory().AddItem("SwordBronze", 1, 1, 0, 0L, "");
            AzuExtendedPlayerInventoryLogger.LogDebugDebug($"No items: {gameObject.GetComponent<Container>().GetInventory().NrOfItems()}");
            PlayerProfile playerProfile = global::Game.instance.GetPlayerProfile();
            component.Setup(playerProfile.GetName(), playerProfile.GetPlayerID());
        }
    }
}