#if !API
using LocalizationManager;
#endif

namespace AzuExtendedPlayerInventory
{
    [PublicAPI]
    public class API
    {
        public delegate void SlotAddedHandler(string slotName);

        public delegate void SlotRemovedHandler(string slotName);

        private static readonly Dictionary<SlotAddedHandler, AzuEPI.API.SlotAddedHandler> _slotAddedMap = new();

        private static readonly Dictionary<SlotRemovedHandler, AzuEPI.API.SlotRemovedHandler> _slotRemovedMap = new();

        private static readonly Dictionary<Action<Hud>, Action<Hud>> _hudAwakeMap = new();
        private static readonly Dictionary<Action<Hud>, Action<Hud>> _hudAwakeCompleteMap = new();
        private static readonly Dictionary<Action<Hud>, Action<Hud>> _hudUpdateMap = new();
        private static readonly Dictionary<Action<Hud>, Action<Hud>> _hudUpdateCompleteMap = new();

        public static event SlotAddedHandler? SlotAdded
        {
            add
            {
                if (value == null || _slotAddedMap.ContainsKey(value)) return;

                AzuEPI.API.SlotAddedHandler wrapper = s =>
                {
                    try
                    {
                        value(s);
                    }
                    catch
                    {
                        /* swallow to keep parity with old API */
                    }
                };
                _slotAddedMap[value] = wrapper;
                AzuEPI.API.SlotAdded += wrapper;
            }
            remove
            {
                if (value == null) return;
                if (_slotAddedMap.TryGetValue(value, out AzuEPI.API.SlotAddedHandler? wrapper))
                {
                    AzuEPI.API.SlotAdded -= wrapper;
                    _slotAddedMap.Remove(value);
                }
            }
        }

        public static event SlotRemovedHandler? SlotRemoved
        {
            add
            {
                if (value == null || _slotRemovedMap.ContainsKey(value)) return;

                AzuEPI.API.SlotRemovedHandler wrapper = s =>
                {
                    try
                    {
                        value(s);
                    }
                    catch
                    {
                    }
                };
                _slotRemovedMap[value] = wrapper;
                AzuEPI.API.SlotRemoved += wrapper;
            }
            remove
            {
                if (value == null) return;
                if (_slotRemovedMap.TryGetValue(value, out AzuEPI.API.SlotRemovedHandler? wrapper))
                {
                    AzuEPI.API.SlotRemoved -= wrapper;
                    _slotRemovedMap.Remove(value);
                }
            }
        }

        public static event Action<Hud>? OnHudAwake
        {
            add => AddHudEvent(value, _hudAwakeMap, h => AzuEPI.API.OnHudAwake += h);
            remove => RemoveHudEvent(value, _hudAwakeMap, h => AzuEPI.API.OnHudAwake -= h);
        }

        public static event Action<Hud>? OnHudAwakeComplete
        {
            add => AddHudEvent(value, _hudAwakeCompleteMap, h => AzuEPI.API.OnHudAwakeComplete += h);
            remove => RemoveHudEvent(value, _hudAwakeCompleteMap, h => AzuEPI.API.OnHudAwakeComplete -= h);
        }

        public static event Action<Hud>? OnHudUpdate
        {
            add => AddHudEvent(value, _hudUpdateMap, h => AzuEPI.API.OnHudUpdate += h);
            remove => RemoveHudEvent(value, _hudUpdateMap, h => AzuEPI.API.OnHudUpdate -= h);
        }

        public static event Action<Hud>? OnHudUpdateComplete
        {
            add => AddHudEvent(value, _hudUpdateCompleteMap, h => AzuEPI.API.OnHudUpdateComplete += h);
            remove => RemoveHudEvent(value, _hudUpdateCompleteMap, h => AzuEPI.API.OnHudUpdateComplete -= h);
        }

        private static void AddHudEvent(Action<Hud>? handler, Dictionary<Action<Hud>, Action<Hud>> map, Action<Action<Hud>> subscribe)
        {
            if (handler == null) return;
            if (map.ContainsKey(handler)) return;
            Action<Hud> wrapper = h =>
            {
                try
                {
                    handler(h);
                }
                catch
                {
                }
            };
            map[handler] = wrapper;
#if !API
            subscribe(wrapper);
#endif
        }

        private static void RemoveHudEvent(Action<Hud>? handler, Dictionary<Action<Hud>, Action<Hud>> map, Action<Action<Hud>> unsubscribe)
        {
            if (handler == null) return;
            if (map.TryGetValue(handler, out Action<Hud>? wrapper))
            {
#if !API
                unsubscribe(wrapper);
#endif
                map.Remove(handler);
            }
        }

        public static bool IsLoaded() => AzuEPI.API.IsLoaded();

        public static bool AddSlot(string slotName, Func<Player, ItemDrop.ItemData?> getItem, Func<ItemDrop.ItemData, bool> isValid, int index = -1)
        {
#if !API
            if (Localization.instance.Localize(slotName).Contains("["))
            {
                Localizer.OnLocalizationComplete += () => AzuEPI.API.AddSlot(slotName, getItem, isValid, index);
            }
            else
            {
                AzuEPI.API.AddSlot(slotName, getItem, isValid, index);
            }

            return true;
#else
            return false;
#endif
        }

        public static bool RemoveSlot(string slotName) => AzuEPI.API.RemoveSlot(slotName);

        public static SlotInfo GetSlots()
        {
            AzuEPI.SlotInfo n = AzuEPI.API.GetSlots();
            return new SlotInfo
            {
                SlotNames = n.SlotNames ?? Array.Empty<string>(),
                SlotPositions = n.SlotPositions ?? Array.Empty<Vector2>(),
                GetItemFuncs = n.GetItemFuncs ?? Array.Empty<Func<Player, ItemDrop.ItemData?>?>(),
                IsValidFuncs = n.IsValidFuncs ?? Array.Empty<Func<ItemDrop.ItemData, bool>?>()
            };
        }

        public static SlotInfo GetQuickSlots()
        {
            AzuEPI.SlotInfo n = AzuEPI.API.GetQuickSlots();
            return new SlotInfo
            {
                SlotNames = n.SlotNames ?? Array.Empty<string>(),
                SlotPositions = n.SlotPositions ?? Array.Empty<Vector2>(),
                GetItemFuncs = n.GetItemFuncs ?? Array.Empty<Func<Player, ItemDrop.ItemData?>?>(),
                IsValidFuncs = n.IsValidFuncs ?? Array.Empty<Func<ItemDrop.ItemData, bool>?>()
            };
        }

        public static List<ItemDrop.ItemData> GetQuickSlotsItems() => AzuEPI.API.GetQuickSlotsItems();

        public static int GetAddedRows(int width) => AzuEPI.API.GetAddedRows(width);

        public static void HudAwake(Hud h)
        {
#if !API
            SafeInvoke(() => AzuEPI.API.HudAwake(h));
#endif
        }

        public static void HudAwakeComplete(Hud h)
        {
#if !API
            SafeInvoke(() => AzuEPI.API.HudAwakeComplete(h));
#endif
        }

        public static void HudUpdate(Hud h)
        {
#if !API
            SafeInvoke(() => AzuEPI.API.HudUpdate(h));
#endif
        }

        public static void HudUpdateComplete(Hud h)
        {
#if !API
            SafeInvoke(() => AzuEPI.API.HudUpdateComplete(h));
#endif
        }

        private static void SafeInvoke(Action a)
        {
            try
            {
                a();
            }
            catch
            {
            }
        }
    }

    [PublicAPI]
    public class SlotInfo
    {
        public string[] SlotNames { get; set; } = Array.Empty<string>();
        public Vector2[] SlotPositions { get; set; } = Array.Empty<Vector2>();
        public Func<Player, ItemDrop.ItemData?>?[] GetItemFuncs { get; set; } = Array.Empty<Func<Player, ItemDrop.ItemData?>?>();
        public Func<ItemDrop.ItemData, bool>?[] IsValidFuncs { get; set; } = Array.Empty<Func<ItemDrop.ItemData, bool>?>();
    }
}