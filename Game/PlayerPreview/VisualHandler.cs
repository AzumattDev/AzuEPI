using System.Reflection.Emit;
using AzuEPI.Game.Compatibility;
using AzuEPI.Game.Compatibility.AdvBackpacks;

namespace AzuEPI.Game.PlayerPreview;

public class CustomEquipVisuals
{
    private static readonly Dictionary<string, (string slot, bool bypass, string visual)> _managed = new(StringComparer.Ordinal);

    public static void Register(string prefabName)
    {
        if (string.IsNullOrWhiteSpace(prefabName)) return;
        _registered.Add(prefabName);
    }

    public static void RegisterForSlot(string prefabName, string slotName, string? visualName = null)
    {
        if (string.IsNullOrWhiteSpace(prefabName)) return;
        _registered.Add(prefabName);
        _managed[prefabName] = (slotName ?? "", true, visualName ?? "");
    }

    private static string GetSlotOf(string prefabName)
    {
        return _managed.TryGetValue(prefabName, out (string slot, bool bypass, string visual) meta) ? (meta.slot ?? "") : "";
    }

    private static string GetSlotOf(ItemDrop.ItemData item)
    {
        string? name = item?.m_dropPrefab?.name;
        return string.IsNullOrEmpty(name) ? "" : GetSlotOf(name);
    }

    internal static bool IsManaged(ItemDrop.ItemData item) => item?.m_dropPrefab != null && _managed.ContainsKey(item.m_dropPrefab.name);

    internal static bool HasActiveSlot(ItemDrop.ItemData item)
    {
        if (item?.m_dropPrefab == null) return false;
        if (!_managed.TryGetValue(item.m_dropPrefab.name, out (string slot, bool bypass, string visual) meta)) return true;
        if (string.IsNullOrEmpty(meta.slot)) return true;
        if (API.TryGetSlotIndexByName(meta.slot, out _)) return true;
        if (IsJewelcraftingGem(item)) return false;
        // Otherwise assume slot exists (timing issue during load)
        return true;
    }

    private static bool IsJewelcraftingGem(ItemDrop.ItemData item)
    {
        if (item?.m_dropPrefab == null) return false;
        string prefabName = item.m_dropPrefab.name;
        if (prefabName == "Wishbone") return JewelcraftingCompat.IsWishboneAGem();
        if (prefabName == "Demister") return JewelcraftingCompat.IsWisplightAGem();
        return false;
    }

    private static readonly HashSet<string> _registered = new(StringComparer.Ordinal);
    internal static readonly Dictionary<VisEquipment, State> _states = new();

    private const string ZdoKeyPrefix = "AzuEPICEV_";

    internal sealed class State
    {
        public readonly VisEquipment Vis;

        public readonly Dictionary<string, EquippedEntry> Equipped = new(StringComparer.Ordinal);

        public State(VisEquipment vis) => Vis = vis;

        public bool ForceSetEquipped(string prefabName, int hashFromZdo)
        {
            if (!Equipped.TryGetValue(prefabName, out EquippedEntry? entry))
                Equipped[prefabName] = entry = new EquippedEntry(prefabName);

            if (entry.CurrentHash == hashFromZdo)
                return false;

            if (entry.Instances.Count > 0)
            {
                foreach (GameObject? go in entry.Instances)
                    if (go)
                        Object.Destroy(go);
                entry.Instances.Clear();
            }

            entry.CurrentHash = hashFromZdo;

            if (hashFromZdo != 0)
            {
                entry.Instances = Vis.AttachArmor(hashFromZdo);
                if (prefabName is "BackpackBlackForest" or "BackpackMountains")
                {
                    BoneReorder.ReorderBones(Vis, hashFromZdo, entry.Instances);
                }
            }

            return true;
        }

        public void UpdateAllVisuals()
        {
            bool changedAny = false;
            ZNetView? nview = Vis.m_nview;
            ZDO? zdo = nview ? nview.GetZDO() : null;
            bool isOwner = nview && nview.IsOwner();

            foreach (string? prefabName in _registered)
            {
                bool shouldShow = false;
                string displayLocal = "";
                if (Equipped.TryGetValue(prefabName, out EquippedEntry? e) && e.Item != null)
                {
                    shouldShow = e.Item.m_equipped;
                    displayLocal = e.DisplayName ?? "";
                }

                if (!shouldShow)
                {
                    if (isOwner && zdo != null)
                        zdo.Set(ZdoKeyFor(prefabName), 0);

                    if (Equipped.TryGetValue(prefabName, out EquippedEntry? e2) && !string.IsNullOrEmpty(e2.DisplayName))
                        e2.DisplayName = "";

                    if (ForceSetEquipped(prefabName, 0))
                        changedAny = true;

                    continue;
                }

                int hash = 0;
                if (zdo != null)
                    hash = zdo.GetInt(ZdoKeyFor(prefabName));

                if (hash == 0)
                {
                    string key = !string.IsNullOrEmpty(displayLocal)
                        ? displayLocal
                        : prefabName;

                    hash = key.GetStableHashCode();
                    if (isOwner && zdo != null)
                        zdo.Set(ZdoKeyFor(prefabName), hash);
                }

                if (ForceSetEquipped(prefabName, hash))
                    changedAny = true;
            }

            if (changedAny)
                Vis.UpdateLodgroup();
        }

        public void SetDisplayName(string prefabName, string name)
        {
            if (!Equipped.TryGetValue(prefabName, out EquippedEntry? entry))
            {
                entry = new EquippedEntry(prefabName);
                Equipped[prefabName] = entry;
            }

            if (entry.DisplayName == name) return;
            entry.DisplayName = name;

            if (Vis.m_nview && Vis.m_nview.IsOwner())
            {
                ZDO? zdo = Vis.m_nview.GetZDO();
                zdo?.Set(ZdoKeyFor(prefabName), string.IsNullOrEmpty(name) ? 0 : name.GetStableHashCode());
            }
        }
    }

    internal sealed class EquippedEntry
    {
        public readonly string PrefabName;
        public ItemDrop.ItemData Item;
        public string DisplayName = "";
        public List<GameObject> Instances = [];
        public int CurrentHash;

        public EquippedEntry(string prefabName) => PrefabName = prefabName;
    }

    internal static string ZdoKeyFor(string prefabName) => ZdoKeyPrefix + prefabName;

    [HarmonyPatch(typeof(Player), nameof(Player.Awake))]
    private static class PlayerAwake
    {
        [HarmonyPriority(Priority.First)]
        private static void Prefix(Player __instance)
        {
            VisEquipment? ve = __instance.GetComponent<VisEquipment>();
            if (!ve) return;
            if (!_states.ContainsKey(ve))
                _states[ve] = new State(ve);
        }
    }

    [HarmonyPatch(typeof(VisEquipment), nameof(VisEquipment.OnEnable))]
    private static class VisOnEnable
    {
        private static void Postfix(VisEquipment __instance)
        {
            if (!__instance.m_isPlayer) return;
            if (!_states.ContainsKey(__instance))
                _states[__instance] = new State(__instance);
        }
    }

    [HarmonyPatch(typeof(VisEquipment), nameof(VisEquipment.OnDisable))]
    private static class VisOnDisable
    {
        private static void Postfix(VisEquipment __instance)
        {
            _states.Remove(__instance);
        }
    }

    [HarmonyPatch(typeof(Humanoid), nameof(Humanoid.SetupVisEquipment))]
    private static class SetupVis
    {
        private static void Prefix(Humanoid __instance)
        {
            if (__instance is not Player p) return;
            VisEquipment? ve = p.m_visEquipment;
            if (!ve) return;
            if (!_states.TryGetValue(ve, out State? st)) return;

            foreach (string? prefabName in _registered)
            {
                string display = st.Equipped.TryGetValue(prefabName, out EquippedEntry? e) ? (e.DisplayName ?? "") : "";
                st.SetDisplayName(prefabName, display);
            }
        }
    }

    [HarmonyPatch(typeof(VisEquipment), nameof(VisEquipment.UpdateEquipmentVisuals))]
    private static class VisUpdate
    {
        private static void Postfix(VisEquipment __instance)
        {
            if (!__instance.m_isPlayer) return;
            if (_states.TryGetValue(__instance, out State? st))
            {
                st.UpdateAllVisuals();
                if (__instance == Player.m_localPlayer?.m_visEquipment)
                    VECloneSync.ResetStamp();
            }
        }
    }

    [HarmonyPatch(typeof(Humanoid), nameof(Humanoid.GetSetCount))]
    static class HumanoidGetSetCountPatch
    {
        static void Postfix(Humanoid __instance, string setName, ref int __result)
        {
            if (__instance is not Player p) return;
            VisEquipment? ve = p.m_visEquipment;
            if (!ve) return;
            if (!_states.TryGetValue(ve, out State? st)) return;

            int count = 0;
            foreach (EquippedEntry? entry in st.Equipped.Values)
            {
                ItemDrop.ItemData? item = entry.Item;
                if (item != null && item.m_shared.m_setName == setName)
                {
                    ++count;
                }
            }

            __result += count;
        }
    }

    [HarmonyPatch(typeof(Humanoid), nameof(Humanoid.UnequipAllItems))]
    private static class UnequipAll
    {
        private static void Prefix(Humanoid __instance)
        {
            if (__instance is not Player p) return;
            VisEquipment? ve = p.m_visEquipment;
            if (!ve) return;
            if (!_states.TryGetValue(ve, out State? st)) return;

            foreach (EquippedEntry? kv in st.Equipped.Values.ToList())
            {
                if (kv.Item != null)
                    p.UnequipItem(kv.Item, false);
            }
        }
    }

    [HarmonyPatch(typeof(Humanoid), nameof(Humanoid.UpdateEquipmentStatusEffects))]
    private static class InjectStatusEffects
    {
        private static void Collect(Humanoid humanoid, HashSet<StatusEffect> set)
        {
            if (humanoid is not Player p) return;
            VisEquipment? ve = p.m_visEquipment;
            if (!ve) return;
            if (!_states.TryGetValue(ve, out State? st)) return;

            foreach (EquippedEntry? entry in st.Equipped.Values)
            {
                ItemDrop.ItemData? item = entry.Item;
                if (item?.m_shared?.m_equipStatusEffect is { } eff)
                {
                    set.Add(eff);
                }

                if (item != null && humanoid.HaveSetEffect(item))
                {
                    StatusEffect? se = item.m_shared.m_setStatusEffect;
                    if (se != null) set.Add(se);
                }
            }
        }

        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instrs)
        {
            List<CodeInstruction> list = instrs.ToList();

            list.InsertRange(2, [
                new CodeInstruction(OpCodes.Ldarg_0), // this (Humanoid)
                new CodeInstruction(OpCodes.Ldloc_0),
                new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(typeof(InjectStatusEffects), nameof(Collect)))
            ]);
            return list;
        }
    }

    [HarmonyPatch(typeof(ItemDrop.ItemData), nameof(ItemDrop.ItemData.IsEquipable))]
    private static class IsEquipablePatch
    {
        private static void Postfix(ItemDrop.ItemData __instance, ref bool __result)
        {
            if (__instance?.m_dropPrefab == null) return;
            string name = __instance.m_dropPrefab.name;
            if (_registered.Contains(name) && HasActiveSlot(__instance))
            {
                __result = true;
            }
        }
    }

    [HarmonyPatch(typeof(Humanoid), nameof(Humanoid.IsItemEquiped))]
    private static class IsItemEquipedPatch
    {
        private static void Postfix(Humanoid __instance, ItemDrop.ItemData item, ref bool __result)
        {
            if (__instance is not Player p) return;
            if (item == null || item.m_dropPrefab == null) return;

            VisEquipment? ve = p.m_visEquipment;
            if (!ve) return;
            if (!_states.TryGetValue(ve, out State? st)) return;

            string prefabName = item.m_dropPrefab.name;
            if (_registered.Contains(prefabName) && st.Equipped.TryGetValue(prefabName, out EquippedEntry? entry) && entry.Item == item)
            {
                __result = true;
            }
        }
    }

    [HarmonyPatch(typeof(Humanoid), nameof(Humanoid.EquipItem))]
    internal static class HideTypeWhileEquipping
    {
        private static bool IsReserved(ItemDrop.ItemData.ItemType t) =>
            t is ItemDrop.ItemData.ItemType.Helmet
                or ItemDrop.ItemData.ItemType.Chest
                or ItemDrop.ItemData.ItemType.Shoulder
                or ItemDrop.ItemData.ItemType.Legs
                or ItemDrop.ItemData.ItemType.Utility
                or ItemDrop.ItemData.ItemType.Trinket;

        [HarmonyPrefix]
        [HarmonyPriority(Priority.First)]
        private static void Prefix(Humanoid __instance, ItemDrop.ItemData item, ref ItemDrop.ItemData.ItemType? __state)
        {
            if (__instance is not Player || item?.m_dropPrefab == null) return;
            if (!IsManaged(item)) return;
            if (!HasActiveSlot(item)) return;
            if (!IsReserved(item.m_shared.m_itemType)) return;
            if (__instance.IsItemEquiped(item)) return;

            __state = item.m_shared.m_itemType;
            item.m_shared.m_itemType = API.GetFakeItemType(); // hide from vanilla/other mods
            if (__instance.m_visEquipment && __instance.m_visEquipment.m_isPlayer)
                item.m_shared.m_equipEffect.Create(__instance.transform.position + Vector3.up, __instance.transform.rotation);
        }

        [HarmonyPostfix]
        [HarmonyPriority(Priority.Last)]
        private static void Postfix(Humanoid __instance, ItemDrop.ItemData item, bool triggerEquipEffects, ItemDrop.ItemData.ItemType? __state, ref bool __result)
        {
            if (__instance is not Player p) return;
            if (__state is not { } original) return;
            if (item == null) return;
            if (item.m_shared.m_itemType != API.GetFakeItemType()) return;

            item.m_shared.m_itemType = original;

            if (!p.IsItemEquiped(item))
            {
                __instance.SetupEquipment();
                return;
            }

            VisEquipment? ve = p.m_visEquipment;
            if (!ve || !_states.TryGetValue(ve, out State? st))
            {
                __instance.SetupEquipment();
                return;
            }

            string thisSlot = GetSlotOf(item);
            if (!string.IsNullOrEmpty(thisSlot))
            {
                foreach (KeyValuePair<string, EquippedEntry> kv in st.Equipped.ToList())
                {
                    EquippedEntry? otherEntry = kv.Value;
                    ItemDrop.ItemData? other = otherEntry.Item;
                    if (other == null || other == item) continue;

                    if (string.Equals(GetSlotOf(other), thisSlot, StringComparison.OrdinalIgnoreCase))
                    {
                        p.UnequipItem(other, triggerEquipEffects);

                        st.ForceSetEquipped(kv.Key, 0);
                        otherEntry.Item = null;
                        otherEntry.DisplayName = "";
                        st.SetDisplayName(kv.Key, "");
                    }
                }
            }

            string name = item.m_dropPrefab.name;
            if (_registered.Contains(name))
            {
                st.ForceSetEquipped(name, name.GetStableHashCode());
            }

            item.m_equipped = true;
            __result = true;

            __instance.SetupEquipment();
        }
    }

    [HarmonyPatch(typeof(Humanoid), nameof(Humanoid.EquipItem))]
    private static class EquipItemPatch
    {
        private static void OnEquip(Humanoid humanoid, ItemDrop.ItemData item, bool trigger)
        {
            if (humanoid is not Player p) return;
            if (item?.m_dropPrefab == null) return;

            string prefab = item.m_dropPrefab.name;
            if (!_registered.Contains(prefab)) return;

            VisEquipment? ve = p.m_visEquipment;
            if (!ve) return;
            if (!_states.TryGetValue(ve, out State? st)) return;

            if (!st.Equipped.TryGetValue(prefab, out EquippedEntry? entry))
                st.Equipped[prefab] = entry = new EquippedEntry(prefab);

            entry.Item = item;

            string visualKey = prefab;
            if (_managed.TryGetValue(prefab, out (string slot, bool bypass, string visual) meta) && !string.IsNullOrEmpty(meta.visual))
                visualKey = meta.visual;

            entry.DisplayName = visualKey;
            st.SetDisplayName(prefab, visualKey);
        }

        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructionsEnumerable)
        {
            List<CodeInstruction> instructions = instructionsEnumerable.ToList();
            MethodInfo? isItemEquiped = AccessTools.DeclaredMethod(typeof(Humanoid), nameof(Humanoid.IsItemEquiped));

            int idx = instructions.FindLastIndex(ci => ci.Calls(isItemEquiped));
            if (idx <= 1) return instructions;

            CodeInstruction? labelCarrier = instructions[idx - 2];

            instructions.InsertRange(idx - 2, [
                new CodeInstruction(OpCodes.Ldarg_0) { labels = labelCarrier.labels }, // this (Humanoid)
                new CodeInstruction(OpCodes.Ldarg_1),
                new CodeInstruction(OpCodes.Ldarg_2),
                new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(typeof(EquipItemPatch), nameof(OnEquip)))
            ]);

            labelCarrier.labels = [];
            return instructions;
        }
    }

    [HarmonyPatch(typeof(Humanoid), nameof(Humanoid.UnequipItem))]
    private static class UnequipItemPatch
    {
        private static void OnUnequip(Humanoid humanoid, ItemDrop.ItemData item)
        {
            if (humanoid is not Player p) return;
            if (item == null || item.m_dropPrefab == null) return;

            string name = item.m_dropPrefab.name;
            if (!_registered.Contains(name)) return;

            VisEquipment? ve = p.m_visEquipment;
            if (!ve) return;
            if (!_states.TryGetValue(ve, out State? st)) return;

            if (st.Equipped.TryGetValue(name, out EquippedEntry? entry) && entry.Item == item)
            {
                entry.Item = null;
                entry.DisplayName = "";
                st.SetDisplayName(name, "");
            }

            /*PlayerPreviewManager.DestroyPlayerPreview();
            PlayerPreviewManager.CreatePlayerPreviewShow();*/
        }

        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructionEnumerable)
        {
            List<CodeInstruction> list = instructionEnumerable.ToList();
            MethodInfo? setupEquipment = AccessTools.DeclaredMethod(typeof(Humanoid), nameof(Humanoid.SetupEquipment));

            int idx = list.FindIndex(ci => ci.Calls(setupEquipment));
            if (idx < 1) return list;

            list.InsertRange(idx - 1, [
                new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Ldarg_1),
                new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(typeof(UnequipItemPatch), nameof(OnUnequip)))
            ]);

            return list;
        }
    }
}