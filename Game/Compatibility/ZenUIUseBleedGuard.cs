using AzuEPI.Game.PlayerPreview;

namespace AzuEPI.Game.Compatibility;

[HarmonyPatch]
internal static class ZenUIUseBleedGuard
{
    private static bool _gateActive;
    private static float _gateExpireTime;
    private static float _warpUntilTime;
    private static CursorLockMode _previousLockState = CursorLockMode.None;

    private const float GateDurationSeconds = 0.3f;
    private const float WarpDurationSeconds = 0.15f;

    private static bool InWarpWindow => Time.unscaledTime < _warpUntilTime;
    private static bool InGateWindow => _gateActive && Time.unscaledTime <= _gateExpireTime && InventoryGui.IsVisible();
    private static bool IsUseName(string name) => name is "Use" or "JoyUse";

    private static void StartWarpWindow() => _warpUntilTime = Time.unscaledTime + WarpDurationSeconds;

    private static void ArmGate()
    {
        _gateActive = true;
        _gateExpireTime = Time.unscaledTime + GateDurationSeconds;
    }

    private static void DisarmGate() => _gateActive = false;

    private static bool GetRawButtonState(string name)
    {
        try
        {
            return ZInput.instance?.GetButtonDef(name)?.Held ?? false;
        }
        catch
        {
            return false;
        }
    }

    private static void TryWarpCursor()
    {
        bool proceed = false;
        if (!Chainloader.PluginInfos.ContainsKey("org.bepinex.plugins.jewelcrafting") || !Chainloader.PluginInfos.TryGetValue("ZenDragon.ZenUI", out PluginInfo zenInfo)) return;
        if (zenInfo != null && zenInfo.Instance)
        {
            zenInfo.Instance.Config.TryGetEntry("General", "Enable Slide Animation", out ConfigEntry<bool> entry);
            if (entry is { Value: false })
            {
                proceed = true;
            }
        }
        if (!proceed || !InWarpWindow || !InventoryGui.IsVisible()) return;

        AzuEPICharacterPanel panel = AzuEPICharacterPanel.instance;
        if (panel?.render == null) return;

        Vector3[] corners = new Vector3[4];
        panel.render.GetWorldCorners(corners);
        Vector3 center = (corners[0] + corners[2]) * 0.5f;

        Canvas canvas = panel.render.GetComponentInParent<Canvas>();
        if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay && canvas.worldCamera != null)
        {
            center = canvas.worldCamera.WorldToScreenPoint(center);
        }

        ZInput.SetMousePosition(new Vector2(center.x, center.y));
    }

    private static bool ShouldSuppressUseButton(string name, out bool checkGate)
    {
        checkGate = false;
        if (!IsUseName(name)) return false;

        if (InWarpWindow) return true;

        checkGate = true;
        return false;
    }

    #region Container/Inventory Patches

    [HarmonyPatch(typeof(Container), nameof(Container.Interact))]
    [HarmonyPrefix]
    private static void Container_Interact_Prefix(Humanoid character, bool hold)
    {
        if (character != Player.m_localPlayer || hold) return;

        bool useHeld = GetRawButtonState("Use") || GetRawButtonState("JoyUse");
        StartWarpWindow();

        if (useHeld) ArmGate();
    }

    [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.Show))]
    [HarmonyPrefix]
    [HarmonyPriority(Priority.First)]
    private static void InventoryGui_Show_Prefix()
    {
        StartWarpWindow();
        _previousLockState = Cursor.lockState;
        if (Cursor.lockState == CursorLockMode.Locked) TryWarpCursor();
    }

    [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.Show))]
    [HarmonyPostfix]
    [HarmonyPriority(Priority.First)]
    private static void InventoryGui_Show_Postfix(Container container)
    {
        TryWarpCursor();
        if (container != null && (GetRawButtonState("Use") || GetRawButtonState("JoyUse")))
        {
            ArmGate();
        }
    }

    [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.Hide))]
    [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.CloseContainer))]
    [HarmonyPostfix]
    private static void InventoryGui_Close_Postfix() => DisarmGate();

    #endregion

    #region Input Suppression Patches

    [HarmonyPatch(typeof(ZInput), nameof(ZInput.GetButton))]
    [HarmonyPatch(typeof(ZInput), nameof(ZInput.GetButtonDown))]
    [HarmonyPrefix]
    [HarmonyPriority(Priority.First)]
    [HarmonyBefore("org.bepinex.plugins.jewelcrafting")]
    private static bool ZInput_GetButton_Prefix(string name, ref bool __result)
    {
        if (ShouldSuppressUseButton(name, out bool checkGate))
        {
            __result = false;
            return false;
        }

        if (!checkGate || !InGateWindow) return true;

        InventoryGui inv = InventoryGui.instance;
        if (inv?.m_playerGrid == null) return true;

        Vector2 pos = Input.mousePosition;
        if (inv.m_playerGrid.GetItem(new Vector2i(Mathf.RoundToInt(pos.x), Mathf.RoundToInt(pos.y))) == null)
        {
            return true;
        }

        if (!GetRawButtonState(name))
        {
            DisarmGate();
        }

        __result = false;
        return false;
    }

    [HarmonyPatch(typeof(ZInput), nameof(ZInput.GetButtonUp))]
    [HarmonyPostfix]
    private static void ZInput_GetButtonUp_Postfix(string name)
    {
        if (_gateActive && IsUseName(name) && Time.unscaledTime > _gateExpireTime)
        {
            DisarmGate();
        }
    }

    [HarmonyPatch(typeof(InventoryGrid), nameof(InventoryGrid.GetItem), typeof(Vector2i))]
    [HarmonyPrefix]
    [HarmonyPriority(Priority.First)]
    private static bool InventoryGrid_GetItem_Prefix(ref ItemDrop.ItemData __result)
    {
        if (!InWarpWindow) return true;
        __result = null;
        return false;
    }

    #endregion

    #region Cursor Warp Patches

    [HarmonyPatch(typeof(Player), nameof(Player.Update))]
    [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.Update))]
    [HarmonyPrefix]
    [HarmonyPriority(Priority.First)]
    private static void Update_Prefix() => TryWarpCursor();

    [HarmonyPatch(typeof(GameCamera), nameof(GameCamera.UpdateMouseCapture))]
    [HarmonyPostfix]
    private static void GameCamera_UpdateMouseCapture_Postfix()
    {
        CursorLockMode currentLockState = Cursor.lockState;

        TryWarpCursor();

        if (_previousLockState == CursorLockMode.Locked && currentLockState == CursorLockMode.None && InventoryGui.IsVisible())
        {
            _warpUntilTime = Mathf.Max(_warpUntilTime, Time.unscaledTime + WarpDurationSeconds);
            TryWarpCursor();
        }

        _previousLockState = currentLockState;
    }

    #endregion
}