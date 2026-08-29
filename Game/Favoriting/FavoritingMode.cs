namespace AzuEPI.Game.Favoriting;

internal class FavoritingMode
{
    internal static bool IsExternalFavoritingModLoaded()
    {
        return Chainloader.PluginInfos.ContainsKey("Azumatt.AzuAutoStore") || Chainloader.PluginInfos.ContainsKey("goldenrevolver.quick_stack_store");
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
}
