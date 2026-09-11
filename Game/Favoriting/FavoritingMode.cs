namespace AzuEPI.Game.Favoriting;

internal class FavoritingMode
{
    private const string AutoStoreGuid = "Azumatt.AzuAutoStore";
    private const string QuickStackStoreGuid = "goldenrevolver.quick_stack_store";
    private const string AutoStoreFavoritingSection = "3 - Favoriting";
    private const float HintCooldownSeconds = 10f;

    private static float _lastHintTime = float.NegativeInfinity;

    internal static bool IsExternalFavoritingModLoaded()
    {
        return Chainloader.PluginInfos.ContainsKey(AutoStoreGuid) || Chainloader.PluginInfos.ContainsKey(QuickStackStoreGuid);
    }

    internal static void RefreshDisplay()
    {
        if (InventoryGui.instance && Player.m_localPlayer)
            InventoryGui.instance.m_playerGrid.UpdateGui(Player.m_localPlayer, null);
    }

    internal static bool IsInFavoritingMode()
    {
		return !IsExternalFavoritingModLoaded() && FavoritingModifierKeybind.Value.IsKeyHeld();
	}

	internal static void TryShowExternalFavoritingHint()
	{
		if (!Player.m_localPlayer || !IsExternalFavoritingModLoaded() || !FavoritingModifierKeybind.Value.IsKeyHeld())
			return;

		KeyboardShortcut[] autoStoreKeys = GetAutoStoreFavoritingKeys();
		if (autoStoreKeys.Any(shortcut => shortcut.IsKeyHeld()) || Time.time - _lastHintTime < HintCooldownSeconds)
			return;

		_lastHintTime = Time.time;
		Player.m_localPlayer.Message(MessageHud.MessageType.Center, BuildHintMessage(autoStoreKeys));
	}

	private static string BuildHintMessage(KeyboardShortcut[] autoStoreKeys)
	{
		if (!Chainloader.PluginInfos.ContainsKey(AutoStoreGuid))
			return "Favoriting is handled by Quick Stack Store, use its favoriting key instead";

		if (autoStoreKeys.Length == 0)
			return "Favoriting is handled by AzuAutoStore, use its favoriting key instead";

		return $"Favoriting is handled by AzuAutoStore, hold {autoStoreKeys[0]} instead";
	}

	private static KeyboardShortcut[] GetAutoStoreFavoritingKeys()
	{
		if (!Chainloader.PluginInfos.TryGetValue(AutoStoreGuid, out PluginInfo autoStoreInfo) || autoStoreInfo.Instance == null)
			return [];

		ConfigFile autoStoreConfig = autoStoreInfo.Instance.Config;
		List<KeyboardShortcut> favoritingKeys = [];

		foreach (string keyName in new[] { "FavoritingModifierKeybind1", "FavoritingModifierKeybind2" })
		{
			ConfigDefinition definition = new(AutoStoreFavoritingSection, keyName);
			if (autoStoreConfig.ContainsKey(definition) && autoStoreConfig[definition].BoxedValue is KeyboardShortcut shortcut && shortcut.MainKey != KeyCode.None)
				favoritingKeys.Add(shortcut);
		}

		return [.. favoritingKeys];
	}
}
