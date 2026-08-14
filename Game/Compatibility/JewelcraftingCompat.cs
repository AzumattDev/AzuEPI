namespace AzuEPI.Game.Compatibility;

public static class JewelcraftingCompat
{
    private const string JewelcraftingGuid = "org.bepinex.plugins.jewelcrafting";

    private static ConfigEntryBase? _wisplightGemConfig;
    private static ConfigEntryBase? _wishboneGemConfig;
    private static Delegate? _wisplightHandler;
    private static Delegate? _wishboneHandler;

    public static bool IsLoaded { get; private set; }

    public static bool IsWisplightAGem()
    {
        if (!IsLoaded || _wisplightGemConfig == null) return false;
        return Convert.ToInt32(_wisplightGemConfig.BoxedValue) == 1;
    }

    public static bool IsWishboneAGem()
    {
        if (!IsLoaded || _wishboneGemConfig == null) return false;
        return Convert.ToInt32(_wishboneGemConfig.BoxedValue) == 1;
    }

    public static void Init()
    {
        if (!Chainloader.PluginInfos.TryGetValue(JewelcraftingGuid, out PluginInfo? jcInfo))
            return;

        if (jcInfo?.Instance == null)
            return;

        IsLoaded = true;

        // Find config entries by iterating - can't use TryGetEntry<T> since we don't have access to Jewelcrafting's Toggle type
        ConfigFile jcConfig = jcInfo.Instance.Config;
        string sectionName = jcInfo.Metadata.Version >= new System.Version("2.0.0") ? "7 - Other" : "6 - Other";
        foreach (ConfigDefinition key in jcConfig.Keys)
        {
            if (key.Section != sectionName) continue;
            if (key.Key == "Wisplight Gem")
                _wisplightGemConfig = jcConfig[key];
            else if (key.Key == "Wishbone Gem")
                _wishboneGemConfig = jcConfig[key];
        }

        if (_wisplightGemConfig != null)
        {
            _wisplightHandler = SubscribeToSettingChanged(_wisplightGemConfig, HandleWisplightChange);
            AzuExtendedPlayerInventoryLogger.LogDebug($"JewelcraftingCompat: Found wisplightGem config (current: {_wisplightGemConfig.BoxedValue})");
        }

        if (_wishboneGemConfig != null)
        {
            _wishboneHandler = SubscribeToSettingChanged(_wishboneGemConfig, HandleWishboneChange);
            AzuExtendedPlayerInventoryLogger.LogDebug($"JewelcraftingCompat: Found wishboneGem config (current: {_wishboneGemConfig.BoxedValue})");
        }

        HandleInitialState();
    }

    private static Delegate? SubscribeToSettingChanged(ConfigEntryBase entry, Action handler)
    {
        // Use reflection to subscribe to the event
        EventInfo? settingChangedEvent = entry.GetType().GetEvent("SettingChanged");
        if (settingChangedEvent == null) return null;

        // BepInEx ConfigEntry.SettingChanged uses EventHandler (non-generic)
        EventHandler eventHandler = (_, _) => handler();
        settingChangedEvent.AddEventHandler(entry, eventHandler);
        return eventHandler;
    }

    private static void UnsubscribeFromSettingChanged(ConfigEntryBase? entry, Delegate? handler)
    {
        if (entry == null || handler == null) return;

        EventInfo? settingChangedEvent = entry.GetType().GetEvent("SettingChanged");
        settingChangedEvent?.RemoveEventHandler(entry, handler);
    }

    public static void Cleanup()
    {
        UnsubscribeFromSettingChanged(_wisplightGemConfig, _wisplightHandler);
        UnsubscribeFromSettingChanged(_wishboneGemConfig, _wishboneHandler);

        _wisplightGemConfig = null;
        _wishboneGemConfig = null;
        _wisplightHandler = null;
        _wishboneHandler = null;
        IsLoaded = false;
    }

    private static void HandleInitialState()
    {
        bool removedAny = false;

        if (IsWishboneAGem())
        {
            API.RemoveSlot("$item_wishbone");
            if (Localization.m_instance != null)
                API.RemoveSlot(Localization.m_instance.Localize("$item_wishbone"));
            AzuExtendedPlayerInventoryLogger.LogDebug("JewelcraftingCompat: Removed Wishbone slot (Jewelcrafting has it as a gem)");
            removedAny = true;
        }

        if (IsWisplightAGem())
        {
            API.RemoveSlot("$item_demister");
            if (Localization.m_instance != null)
                API.RemoveSlot(Localization.m_instance.Localize("$item_demister"));
            AzuExtendedPlayerInventoryLogger.LogDebug("JewelcraftingCompat: Removed Wisplight slot (Jewelcrafting has it as a gem)");
            removedAny = true;
        }

        if (removedAny)
        {
            context.FullRebuild();
        }
    }

    private static void HandleWisplightChange()
    {
        bool isGem = IsWisplightAGem();
        AzuExtendedPlayerInventoryLogger.LogDebug($"JewelcraftingCompat: wisplightGem changed to {(isGem ? "On (gem)" : "Off (utility)")}");

        if (Player.m_localPlayer == null)
        {
            return;
        }

        if (isGem)
        {
            API.RemoveSlot("$item_demister");
            if (Localization.m_instance != null)
                API.RemoveSlot(Localization.m_instance.Localize("$item_demister"));
        }
        else
        {
            if (WispLightSlot.Value.isOn() && !IsSlotMarkedForRemoval("$item_demister"))
            {
                int index = WishboneSlot.Value.isOn() && !IsWishboneAGem() ? 6 : 5;
                API.AddSlot("$item_demister", "Demister", index);
            }
        }

        context.FullRebuild();
    }

    private static void HandleWishboneChange()
    {
        bool isGem = IsWishboneAGem();
        AzuExtendedPlayerInventoryLogger.LogDebug($"JewelcraftingCompat: wishboneGem changed to {(isGem ? "On (gem)" : "Off (utility)")}");

        if (Player.m_localPlayer == null)
        {
            return;
        }

        if (isGem)
        {
            API.RemoveSlot("$item_wishbone");
            if (Localization.instance != null)
                API.RemoveSlot(Localization.instance.Localize("$item_wishbone"));
        }
        else
        {
            if (WishboneSlot.Value.isOn() && !IsSlotMarkedForRemoval("$item_wishbone"))
            {
                API.AddSlot("$item_wishbone", "Wishbone", 5);
            }
        }

        context.FullRebuild();
    }
}