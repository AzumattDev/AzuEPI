namespace AzuEPI.Game.Compatibility;

public static class ValheimPlusCompat
{
    private const string GUID = "org.bepinex.plugins.valheim_plus";
    private const string VPlusHarmonyId = "mod.valheim_plus";

    private static bool? _isLoaded;
    private static bool? _isInventoryEnabled;
    private static int? _vplusConfiguredRows;
    private static Assembly _assembly;

    private static bool IsLoaded => _isLoaded ??= Chainloader.PluginInfos.ContainsKey(GUID);

    private static bool IsInventoryFeatureEnabled
    {
        get
        {
            if (!IsLoaded) return false;
            if (_isInventoryEnabled.HasValue) return _isInventoryEnabled.Value;

            ParseVPlusConfig();
            return _isInventoryEnabled ?? false;
        }
    }

    private static void ParseVPlusConfig()
    {
        _isInventoryEnabled = false;
        _vplusConfiguredRows = Layout.BaseInventoryHeight;

        try
        {
            string configPath = Path.Combine(Paths.ConfigPath, "valheim_plus.cfg");
            if (!File.Exists(configPath))
            {
                AzuExtendedPlayerInventoryLogger.LogWarning($"ValheimPlusCompat: Config file not found at: {configPath}");
                return;
            }

            string[] lines = File.ReadAllLines(configPath);
            bool inInventorySection = false;
            bool enabled = false;
            int rows = Layout.BaseInventoryHeight;

            foreach (string line in lines)
            {
                string trimmed = line.Trim();

                if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith(";") || trimmed.StartsWith("#"))
                    continue;

                if (trimmed.StartsWith("["))
                {
                    inInventorySection = trimmed.Equals("[Inventory]", StringComparison.OrdinalIgnoreCase);
                    continue;
                }

                if (!inInventorySection) continue;

                int equalsIndex = trimmed.IndexOf('=');
                if (equalsIndex <= 0) continue;

                string key = trimmed.Substring(0, equalsIndex).Trim();
                string value = trimmed.Substring(equalsIndex + 1).Trim();

                if (key.Equals("enabled", StringComparison.OrdinalIgnoreCase))
                {
                    enabled = value.Equals("true", StringComparison.OrdinalIgnoreCase);
                }
                else if (key.Equals("playerInventoryRows", StringComparison.OrdinalIgnoreCase))
                {
                    if (int.TryParse(value, out int parsedRows))
                    {
                        rows = Mathf.Clamp(parsedRows, 4, 20);
                    }
                }
            }

            _isInventoryEnabled = enabled;
            _vplusConfiguredRows = rows;

            if (enabled)
            {
                AzuExtendedPlayerInventoryLogger.LogInfo($"ValheimPlusCompat: V+ Inventory feature enabled with {rows} rows - AzuEPI will override");
            }
        }
        catch (Exception ex)
        {
            AzuExtendedPlayerInventoryLogger.LogWarning($"ValheimPlusCompat: Failed to parse V+ config: {ex.Message}");
        }
    }

    public static void Initialize()
    {
        if (!IsLoaded)
        {
            AzuExtendedPlayerInventoryLogger.LogDebugDebug("ValheimPlusCompat: ValheimPlus not detected");
            return;
        }

        AzuExtendedPlayerInventoryLogger.LogInfo("ValheimPlusCompat: ValheimPlus detected");

        if (Chainloader.PluginInfos.TryGetValue(GUID, out PluginInfo vplusPlugin))
            _assembly = Assembly.GetAssembly(vplusPlugin.Instance.GetType());

        ParseVPlusConfig();

        if (!IsInventoryFeatureEnabled)
            return;

        RemoveConflictingPatches();
        PatchRowsGetter();
        SyncRowsToConfig();
    }

    private static void PatchRowsGetter()
    {
        if (_assembly == null) return;

        Type inventoryConfig = _assembly.GetType("ValheimPlus.Configurations.Sections.InventoryConfiguration");
        if (inventoryConfig == null)
        {
            AzuExtendedPlayerInventoryLogger.LogWarning("ValheimPlusCompat: Could not find InventoryConfiguration type");
            return;
        }

        MethodBase getter = AccessTools.PropertyGetter(inventoryConfig, "playerInventoryRows");
        if (getter == null)
        {
            AzuExtendedPlayerInventoryLogger.LogWarning("ValheimPlusCompat: Could not find playerInventoryRows getter");
            return;
        }

        context._harmony.Patch(getter, finalizer: new HarmonyMethod(typeof(ValheimPlusCompat), nameof(RowsGetterFinalizer)));
        AzuExtendedPlayerInventoryLogger.LogInfo("ValheimPlusCompat: Patched V+ playerInventoryRows getter to return AzuEPI height");
    }

    private static void RowsGetterFinalizer(ref int __result) => __result = API.GetFullHeight(Layout.BaseInventoryWidth);

    public static void SyncRowsToConfig()
    {
        if (_assembly == null) return;

        try
        {
            Type configurationType = _assembly.GetType("ValheimPlus.Configurations.Configuration");
            if (configurationType == null) return;

            object current = AccessTools.Property(configurationType, "Current")?.GetValue(null);
            if (current == null) return;

            object inventorySection = current.GetType()
                .GetProperty("Inventory", BindingFlags.Public | BindingFlags.Instance)
                ?.GetValue(current);
            if (inventorySection == null) return;

            PropertyInfo rowsProp = inventorySection.GetType()
                .GetProperty("playerInventoryRows", BindingFlags.Public | BindingFlags.Instance);
            if (rowsProp == null) return;

            int fullHeight = API.GetFullHeight(Layout.BaseInventoryWidth);
            MethodInfo setter = rowsProp.GetSetMethod(true);
            if (setter != null)
                setter.Invoke(inventorySection, [fullHeight]);
            else if (rowsProp.CanWrite)
                rowsProp.SetValue(inventorySection, fullHeight);

            AzuExtendedPlayerInventoryLogger.LogInfo($"ValheimPlusCompat: Set V+ playerInventoryRows = {fullHeight}");
        }
        catch (Exception ex)
        {
            AzuExtendedPlayerInventoryLogger.LogWarning($"ValheimPlusCompat: Failed to sync rows to V+ config: {ex.Message}");
        }
    }

    private static void RemoveConflictingPatches()
    {
        try
        {
            ConstructorInfo inventoryConstructor = AccessTools.Constructor(typeof(Inventory), [typeof(string), typeof(Sprite), typeof(int), typeof(int)]);
            MethodInfo inventoryGuiShow = AccessTools.Method(typeof(InventoryGui), nameof(InventoryGui.Show));
            MethodInfo inventoryGridUpdateGui = AccessTools.Method(typeof(InventoryGrid), nameof(InventoryGrid.UpdateGui));

            UnpatchVPlusFrom(inventoryConstructor, "Inventory constructor");
            UnpatchVPlusFrom(inventoryGuiShow, "InventoryGui.Show");
            UnpatchVPlusFrom(inventoryGridUpdateGui, "InventoryGrid.UpdateGui");

            AzuExtendedPlayerInventoryLogger.LogInfo("ValheimPlusCompat: Removed V+ inventory patches - AzuEPI now controls inventory layout");
        }
        catch (Exception ex)
        {
            AzuExtendedPlayerInventoryLogger.LogWarning($"ValheimPlusCompat: Error removing V+ patches: {ex.Message}");
        }
    }

    private static void UnpatchVPlusFrom(MethodBase method, string methodName)
    {
        if (method == null)
        {
            AzuExtendedPlayerInventoryLogger.LogWarning($"ValheimPlusCompat: Could not find {methodName}");
            return;
        }

        HarmonyLib.Patches patchInfo = Harmony.GetPatchInfo(method);
        if (patchInfo == null)
        {
            AzuExtendedPlayerInventoryLogger.LogDebugDebug($"ValheimPlusCompat: No patches found on {methodName}");
            return;
        }

        int removed = 0;

        foreach (Patch prefix in patchInfo.Prefixes)
        {
            if (prefix.owner != VPlusHarmonyId) continue;
            context._harmony.Unpatch(method, prefix.PatchMethod);
            removed++;
        }

        foreach (Patch postfix in patchInfo.Postfixes)
        {
            if (postfix.owner != VPlusHarmonyId) continue;
            context._harmony.Unpatch(method, postfix.PatchMethod);
            removed++;
        }

        foreach (Patch transpiler in patchInfo.Transpilers)
        {
            if (transpiler.owner != VPlusHarmonyId) continue;
            context._harmony.Unpatch(method, transpiler.PatchMethod);
            removed++;
        }

        foreach (Patch finalizer in patchInfo.Finalizers)
        {
            if (finalizer.owner != VPlusHarmonyId) continue;
            context._harmony.Unpatch(method, finalizer.PatchMethod);
            removed++;
        }

        if (removed > 0)
        {
            AzuExtendedPlayerInventoryLogger.LogDebugDebug($"ValheimPlusCompat: Removed {removed} V+ patch(es) from {methodName}");
        }
    }
}
