namespace AzuEPI.Game.Panels.Stats;

public static class StatsPanelController
{
    public const string StatsPanelName = $"{Prefix}StatsPanel";
    public const string StatsScrollRootName = "StatsPanelScrollRoot";
    public const string StatsViewportName = "StatsPanelViewport";
    public const string StatsContentName = "StatsPanelContent";
    public const string StatsScrollbarName = "StatsPanelScrollbar";
    public const string StatsToggleButtonName = $"{Prefix}StatsToggleButton";

    private static readonly Vector2 ToggleBtnAnchorMin = new(0f, 1f);
    private static readonly Vector2 ToggleBtnAnchorMax = new(0f, 1f);
    private static readonly Vector2 ToggleBtnPivot = new(0f, 1f);
    private static readonly Vector2 ToggleBtnSize = new(90f, 32f);

    private static readonly Vector2 ScrollRootOffsetMin = new(10f, 10f);
    private static readonly Vector2 ScrollRootOffsetMax = new(-10f, -10f);

    private static readonly Vector2 BarAnchorMin = new(1f, 0f);
    private static readonly Vector2 BarAnchorMax = new(1f, 1f);
    private static readonly Vector2 BarPivot = new(1f, 0.5f);
    private static readonly Vector2 BarOffsetMin = new(-20f, 10f);
    private static readonly Vector2 BarOffsetMax = new(-10f, -10f);

    private static readonly Vector2 CellSize = new(85f, 70f);
    private static readonly Vector2 Spacing = new(6f, 6f);
    private const int Columns = 4;

    private static bool _visible;
    private static RectTransform? _panel;
    private static ScrollRect? _scroll;
    private static RectTransform? _viewport;
    private static RectTransform? _content;
    private static Scrollbar? _vbar;
    private static Button? _toggleBtn;
    internal static Transform? StatsButtonGo;
    private static TMP_FontAsset? _fontAsset;
    private static Transform? _tabBorderTemplate;
    private static readonly List<StatElement> _statElements = [];

    private static TMP_Dropdown? _playerDropdown;
    private static RectTransform? _dropdownContainer;
    private static bool _isViewingRemotePlayer;
    private static long _viewingPlayerId;
    private static RemotePlayerStats? _currentRemoteStats;
    private static readonly Dictionary<long, RemotePlayerStats> _remoteStatsCache = new();
    private static bool _isLoadingRemoteStats;
    private static readonly List<ZNet.PlayerInfo> _playerList = [];

    public static RectTransform? ToggleButtonParentGlg;

    public static ScrollRect? GetScrollRect() => _scroll;

    private static readonly Dictionary<PlayerStatType, string> StatIcons = new()
    {
        { PlayerStatType.EnemyKills, "⚔️" },
        { PlayerStatType.Deaths, "💀" },
        { PlayerStatType.ArrowsShot, "🏹" },
        { PlayerStatType.EnemyHits, "🎯" },
        { PlayerStatType.HitsTakenEnemies, "🛡️" },
        { PlayerStatType.PlayerKills, "⚔️" },
        { PlayerStatType.PlayerHits, "💥" },
        { PlayerStatType.BossKills, "👹" },
        { PlayerStatType.Builds, "🏗️" },
        { PlayerStatType.Crafts, "🔨" },
        { PlayerStatType.Upgrades, "⬆️" },
        { PlayerStatType.ItemsPickedUp, "📦" },
        { PlayerStatType.DistanceTraveled, "🗺️" },
        { PlayerStatType.DistanceWalk, "🚶" },
        { PlayerStatType.DistanceRun, "🏃" },
        { PlayerStatType.DistanceSail, "⛵" },
        { PlayerStatType.DistanceAir, "✈️" },
        { PlayerStatType.TreeChops, "🌲" },
        { PlayerStatType.MineHits, "⛏️" },
        { PlayerStatType.FoodEaten, "🍖" },
        { PlayerStatType.PortalsUsed, "🌀" },
        { PlayerStatType.Jumps, "🦘" },
        { PlayerStatType.Sleep, "😴" },
        { PlayerStatType.TimeInBase, "🏠" },
        { PlayerStatType.TimeOutOfBase, "🧭" },
        { PlayerStatType.CraftsOrUpgrades, "🔧" },
        { PlayerStatType.Cheats, "💻" },
        { PlayerStatType.WorldLoads, "🌍" },
        { PlayerStatType.CreatureTamed, "🐺" },
        { PlayerStatType.DoorsOpened, "🚪" },
        { PlayerStatType.BeesHarvested, "🐝" },
    };

    private static readonly Dictionary<PlayerStatType, string> StatLabels = new()
    {
        { PlayerStatType.EnemyKills, "$azu_epi_stat_kills" },
        { PlayerStatType.EnemyHits, "$azu_epi_stat_hits" },
        { PlayerStatType.HitsTakenEnemies, "$azu_epi_stat_hit_taken" },
        { PlayerStatType.PlayerKills, "$azu_epi_stat_pvp_kills" },
        { PlayerStatType.PlayerHits, "$azu_epi_stat_pvp_hits" },
        { PlayerStatType.BossKills, "$azu_epi_stat_bosses" },
        { PlayerStatType.ItemsPickedUp, "$azu_epi_stat_items" },
        { PlayerStatType.DistanceTraveled, "$azu_epi_stat_distance" },
        { PlayerStatType.DistanceWalk, "$azu_epi_stat_walked" },
        { PlayerStatType.DistanceRun, "$azu_epi_stat_run" },
        { PlayerStatType.DistanceSail, "$azu_epi_stat_sailed" },
        { PlayerStatType.DistanceAir, "$azu_epi_stat_air" },
        { PlayerStatType.TreeChops, "$azu_epi_stat_trees" },
        { PlayerStatType.MineHits, "$azu_epi_stat_mines" },
        { PlayerStatType.FoodEaten, "$azu_epi_stat_food" },
        { PlayerStatType.PortalsUsed, "$azu_epi_stat_portals" },
        { PlayerStatType.TimeInBase, "$azu_epi_stat_in_base" },
        { PlayerStatType.TimeOutOfBase, "$azu_epi_stat_explored" },
        { PlayerStatType.CraftsOrUpgrades, "$azu_epi_stat_craft_upgrades" },
        { PlayerStatType.WorldLoads, "$azu_epi_stat_loads" },
        { PlayerStatType.CreatureTamed, "$azu_epi_stat_tamed" },
        { PlayerStatType.DoorsOpened, "$azu_epi_stat_doors" },
        { PlayerStatType.BeesHarvested, "$azu_epi_stat_bees" },
    };

    public static bool IsVisible() => _visible;
    public static bool IsViewingRemotePlayer() => _isViewingRemotePlayer;

    public static void SetVisible(bool visible)
    {
        _visible = visible;
        if (_panel) _panel.gameObject.SetActive(visible);

        if (!visible)
        {
            ResetToLocalPlayer();
            return;
        }

        if (!_panel) return;
        _panel.SetAsLastSibling();

        RefreshPlayerDropdownOptions();

        if (Player.m_localPlayer != null)
        {
            if (_isViewingRemotePlayer && _currentRemoteStats != null)
                UpdateStatsFromRemoteData(_currentRemoteStats);
            else
                UpdateStats(Player.m_localPlayer);
        }
    }

    private static void ResetToLocalPlayer()
    {
        _isViewingRemotePlayer = false;
        _currentRemoteStats = null;
        _viewingPlayerId = 0;
        _isLoadingRemoteStats = false;
        if (_playerDropdown != null)
            _playerDropdown.SetValueWithoutNotify(0);
    }

    public static void Hide() => SetVisible(false);

    public static void EnsureBuilt(InventoryGui gui)
    {
        if (_panel) return;

        _fontAsset = PanelUtilities.GetFontAsset(gui);
        if (_fontAsset == null)
        {
            AzuExtendedPlayerInventoryLogger.LogWarning("Could not find TMP font asset for stats panel. Text may not display correctly.");
        }

        _tabBorderTemplate = gui.m_crafting.transform.Find("TabsButtons/TabBorder");

        BuildPanel(gui);
        BuildScrollTree();
        PopulateStats();

        _panel?.gameObject.SetActive(false);
    }

    internal static void OnStatsConfigChanged(object sender, EventArgs e)
    {
        RebuildStats();
    }

    public static void RebuildStats()
    {
        if (!_content) return;

        foreach (Transform child in _content)
        {
            Object.Destroy(child.gameObject);
        }

        PopulateStats();

        if (_visible && Player.m_localPlayer != null)
        {
            UpdateStats(Player.m_localPlayer);
        }
    }

    private static void BuildPanel(InventoryGui gui)
    {
        _panel = PanelUtilities.BuildPanel(gui, StatsPanelName);
        _panel.GetOrAddComponent<Localize>();
    }

    private static void BuildScrollTree()
    {
        if (!_panel) return;

        GameObject scrollRoot;
        StoreGui? store = StoreGui.instance;
        if (store && store.GetComponentInChildren<ScrollRect>())
        {
            GameObject src = store.GetComponentInChildren<ScrollRect>().gameObject;
            scrollRoot = Object.Instantiate(src, _panel);
        }
        else
        {
            scrollRoot = new GameObject(StatsScrollRootName, typeof(RectTransform), typeof(ScrollRect));
            scrollRoot.transform.SetParent(_panel, false);
        }

        RectTransform rootRT = (RectTransform)scrollRoot.transform;
        PanelUtilities.AnchorFill(rootRT);
        rootRT.offsetMin = ScrollRootOffsetMin;
        rootRT.offsetMax = ScrollRootOffsetMax;

        _scroll = scrollRoot.GetComponent<ScrollRect>();
        _scroll.horizontal = false;
        _scroll.vertical = true;
        _scroll.movementType = ScrollRect.MovementType.Clamped;
        _scroll.inertia = true;
        _scroll.decelerationRate = 0.135f;
        _scroll.scrollSensitivity = 800f;

        _viewport = BuildViewport(rootRT);
        BuildContentStack(_viewport);

        _scroll.viewport = _viewport;
        _scroll.content = _content;

        _vbar = BuildScrollbar(_panel);
        _scroll.verticalScrollbar = _vbar;
        _scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHideAndExpandViewport;
    }

    private static RectTransform BuildViewport(RectTransform parent)
    {
        return PanelUtilities.BuildViewport(parent, StatsViewportName, new Color(0, 0, 0, 0.565f));
    }

    private static void BuildContentStack(RectTransform parent)
    {
        _content = PanelUtilities.BuildVerticalContent(parent, StatsContentName, new RectOffset(16, 16, 16, 16), 8f);
        _content.offsetMin = Vector2.zero;
        _content.offsetMax = Vector2.zero;
    }

    private static Scrollbar BuildScrollbar(Transform panelParent)
    {
        return PanelUtilities.BuildScrollbar(panelParent, new PanelUtilities.ScrollbarConfig
        {
            Name = StatsScrollbarName,
            AnchorMin = BarAnchorMin,
            AnchorMax = BarAnchorMax,
            Pivot = BarPivot,
            OffsetMin = BarOffsetMin,
            OffsetMax = BarOffsetMax
        });
    }

    private static void PopulateStats()
    {
        if (!_content) return;

        _statElements.Clear();

        CreatePlayerDropdown();
        CreateLiveStatsSection();

        List<PlayerStatType> selectedStats = ParseStatsList(SelectedPlayerStats.Value);
        if (selectedStats.Count == 0)
            selectedStats = GetDefaultStats();

        List<PlayerStatType> generalStats = [];
        List<PlayerStatType> combatStats = [];
        List<PlayerStatType> explorationStats = [];
        List<PlayerStatType> activityStats = [];
        List<PlayerStatType> otherStats = [];

        foreach (PlayerStatType stat in selectedStats)
        {
            switch (stat)
            {
                case PlayerStatType.WorldLoads:
                case PlayerStatType.Deaths:
                case PlayerStatType.Cheats:
                    generalStats.Add(stat);
                    break;

                case PlayerStatType.EnemyKills:
                case PlayerStatType.BossKills:
                case PlayerStatType.EnemyHits:
                case PlayerStatType.HitsTakenEnemies:
                case PlayerStatType.ArrowsShot:
                case PlayerStatType.PlayerKills:
                case PlayerStatType.PlayerHits:
                    combatStats.Add(stat);
                    break;

                case PlayerStatType.DistanceTraveled:
                case PlayerStatType.DistanceWalk:
                case PlayerStatType.DistanceRun:
                case PlayerStatType.DistanceSail:
                case PlayerStatType.DistanceAir:
                case PlayerStatType.Jumps:
                case PlayerStatType.PortalsUsed:
                    explorationStats.Add(stat);
                    break;

                case PlayerStatType.Builds:
                case PlayerStatType.Crafts:
                case PlayerStatType.Upgrades:
                case PlayerStatType.CraftsOrUpgrades:
                case PlayerStatType.ItemsPickedUp:
                case PlayerStatType.TreeChops:
                case PlayerStatType.MineHits:
                case PlayerStatType.FoodEaten:
                case PlayerStatType.Sleep:
                    activityStats.Add(stat);
                    break;

                default:
                    otherStats.Add(stat);
                    break;
            }
        }

        if (generalStats.Count > 0)
            CreateSection(Localization.instance.Localize("$azu_epi_stat_section_general"), generalStats.ToArray());
        if (combatStats.Count > 0)
            CreateSection(Localization.instance.Localize("$azu_epi_stat_section_combat"), combatStats.ToArray());
        if (explorationStats.Count > 0)
            CreateSection(Localization.instance.Localize("$azu_epi_stat_section_exploration"), explorationStats.ToArray());
        if (activityStats.Count > 0)
            CreateSection(Localization.instance.Localize("$azu_epi_stat_section_activity"), activityStats.ToArray());
        if (otherStats.Count > 0)
            CreateSection(Localization.instance.Localize("$azu_epi_stat_section_other"), otherStats.ToArray());
    }

    private static bool IsLiveStatEnabled(LiveStatType stat)
    {
        List<LiveStatType> selectedStats = ParseLiveStatsList(SelectedLiveStats.Value);
        return selectedStats.Count > 0 && selectedStats.Contains(stat);
    }

    private static void CreateLiveStatsSection()
    {
        if (!_content) return;

        bool hasAttributeStats = IsLiveStatEnabled(LiveStatType.Health) || IsLiveStatEnabled(LiveStatType.Stamina) ||
                                 IsLiveStatEnabled(LiveStatType.Eitr) || IsLiveStatEnabled(LiveStatType.Adrenaline);
        if (hasAttributeStats)
        {
            CreateSectionHeader(Localization.instance.Localize("$azu_epi_stat_section_attributes"), new Color(1f, 0.84f, 0f, 1f));
            if (IsLiveStatEnabled(LiveStatType.Health)) CreateLiveStatRow(Localization.instance.Localize("$azu_epi_stat_health"));
            if (IsLiveStatEnabled(LiveStatType.Stamina)) CreateLiveStatRow(Localization.instance.Localize("$azu_epi_stat_stamina"));
            if (IsLiveStatEnabled(LiveStatType.Eitr)) CreateLiveStatRow(Localization.instance.Localize("$azu_epi_stat_eitr"));
            if (IsLiveStatEnabled(LiveStatType.Adrenaline)) CreateLiveStatRow(Localization.instance.Localize("$azu_epi_stat_adrenaline"));
            CreateSpacer("Spacer_Attributes");
        }

        bool hasRegenStats = IsLiveStatEnabled(LiveStatType.HealthRegen) || IsLiveStatEnabled(LiveStatType.HealthRegenMulti) ||
                             IsLiveStatEnabled(LiveStatType.FoodRegen) || IsLiveStatEnabled(LiveStatType.StaminaRegen) ||
                             IsLiveStatEnabled(LiveStatType.StaminaRegenMulti) || IsLiveStatEnabled(LiveStatType.EitrRegen) ||
                             IsLiveStatEnabled(LiveStatType.EitrRegenMulti);
        if (hasRegenStats)
        {
            CreateSectionHeader(Localization.instance.Localize("$azu_epi_stat_section_regeneration"), new Color(0.4f, 1f, 0.4f, 1f));
            if (IsLiveStatEnabled(LiveStatType.HealthRegen)) CreateLiveStatRow(Localization.instance.Localize("$azu_epi_stat_health_regen"));
            if (IsLiveStatEnabled(LiveStatType.HealthRegenMulti)) CreateLiveStatRow(Localization.instance.Localize("$azu_epi_stat_health_regen_multi"));
            if (IsLiveStatEnabled(LiveStatType.FoodRegen)) CreateLiveStatRow(Localization.instance.Localize("$azu_epi_stat_food_regen"));
            if (IsLiveStatEnabled(LiveStatType.StaminaRegen)) CreateLiveStatRow(Localization.instance.Localize("$azu_epi_stat_stamina_regen"));
            if (IsLiveStatEnabled(LiveStatType.StaminaRegenMulti)) CreateLiveStatRow(Localization.instance.Localize("$azu_epi_stat_stamina_regen_multi"));
            if (IsLiveStatEnabled(LiveStatType.EitrRegen)) CreateLiveStatRow(Localization.instance.Localize("$azu_epi_stat_eitr_regen"));
            if (IsLiveStatEnabled(LiveStatType.EitrRegenMulti)) CreateLiveStatRow(Localization.instance.Localize("$azu_epi_stat_eitr_regen_multi"));
            CreateSpacer("Spacer_Regen");
        }

        bool hasCombatStats = IsLiveStatEnabled(LiveStatType.AttackSpeed) || IsLiveStatEnabled(LiveStatType.DamageModifier) ||
                              IsLiveStatEnabled(LiveStatType.StaggerResist) || IsLiveStatEnabled(LiveStatType.TimedBlockBonus) ||
                              IsLiveStatEnabled(LiveStatType.CritChance) || IsLiveStatEnabled(LiveStatType.Lifesteal);
        if (hasCombatStats)
        {
            CreateSectionHeader(Localization.instance.Localize("$azu_epi_stat_section_combat"), new Color(1f, 0.5f, 0f, 1f));
            if (IsLiveStatEnabled(LiveStatType.AttackSpeed)) CreateLiveStatRow(Localization.instance.Localize("$azu_epi_stat_attack_speed"));
            if (IsLiveStatEnabled(LiveStatType.DamageModifier)) CreateLiveStatRow(Localization.instance.Localize("$azu_epi_stat_damage_modifier"));
            if (IsLiveStatEnabled(LiveStatType.StaggerResist)) CreateLiveStatRow(Localization.instance.Localize("$azu_epi_stat_stagger_resist"));
            if (IsLiveStatEnabled(LiveStatType.TimedBlockBonus)) CreateLiveStatRow(Localization.instance.Localize("$azu_epi_stat_timed_block_bonus"));
            if (IsLiveStatEnabled(LiveStatType.CritChance)) CreateLiveStatRow(Localization.instance.Localize("$azu_epi_stat_crit_chance"));
            if (IsLiveStatEnabled(LiveStatType.Lifesteal)) CreateLiveStatRow(Localization.instance.Localize("$azu_epi_stat_lifesteal"));
            CreateSpacer("Spacer_Combat");
        }

        bool hasElementalStats = IsLiveStatEnabled(LiveStatType.BluntDamage) || IsLiveStatEnabled(LiveStatType.SlashDamage) ||
                                 IsLiveStatEnabled(LiveStatType.PierceDamage) || IsLiveStatEnabled(LiveStatType.FireDamage) ||
                                 IsLiveStatEnabled(LiveStatType.FrostDamage) || IsLiveStatEnabled(LiveStatType.LightningDamage) ||
                                 IsLiveStatEnabled(LiveStatType.PoisonDamage) || IsLiveStatEnabled(LiveStatType.SpiritDamage);
        if (hasElementalStats)
        {
            CreateSectionHeader(Localization.instance.Localize("$azu_epi_stat_section_elemental_damage"), new Color(1f, 0.6f, 0.2f, 1f));
            if (IsLiveStatEnabled(LiveStatType.BluntDamage)) CreateLiveStatRow(Localization.instance.Localize("$azu_epi_stat_blunt_damage"));
            if (IsLiveStatEnabled(LiveStatType.SlashDamage)) CreateLiveStatRow(Localization.instance.Localize("$azu_epi_stat_slash_damage"));
            if (IsLiveStatEnabled(LiveStatType.PierceDamage)) CreateLiveStatRow(Localization.instance.Localize("$azu_epi_stat_pierce_damage"));
            if (IsLiveStatEnabled(LiveStatType.FireDamage)) CreateLiveStatRow(Localization.instance.Localize("$azu_epi_stat_fire_damage"));
            if (IsLiveStatEnabled(LiveStatType.FrostDamage)) CreateLiveStatRow(Localization.instance.Localize("$azu_epi_stat_frost_damage"));
            if (IsLiveStatEnabled(LiveStatType.LightningDamage)) CreateLiveStatRow(Localization.instance.Localize("$azu_epi_stat_lightning_damage"));
            if (IsLiveStatEnabled(LiveStatType.PoisonDamage)) CreateLiveStatRow(Localization.instance.Localize("$azu_epi_stat_poison_damage"));
            if (IsLiveStatEnabled(LiveStatType.SpiritDamage)) CreateLiveStatRow(Localization.instance.Localize("$azu_epi_stat_spirit_damage"));
            CreateSpacer("Spacer_ElementalDamage");
        }

        bool hasWeightStats = IsLiveStatEnabled(LiveStatType.CurrentWeight) || IsLiveStatEnabled(LiveStatType.MaxWeight) ||
                              IsLiveStatEnabled(LiveStatType.WeightPercentage) || IsLiveStatEnabled(LiveStatType.ExtraCarryWeight);
        if (hasWeightStats)
        {
            CreateSectionHeader(Localization.instance.Localize("$azu_epi_stat_section_carry_weight"), new Color(0.7f, 0.7f, 1f, 1f));
            if (IsLiveStatEnabled(LiveStatType.CurrentWeight)) CreateLiveStatRow(Localization.instance.Localize("$azu_epi_stat_current_weight"));
            if (IsLiveStatEnabled(LiveStatType.MaxWeight)) CreateLiveStatRow(Localization.instance.Localize("$azu_epi_stat_max_weight"));
            if (IsLiveStatEnabled(LiveStatType.WeightPercentage)) CreateLiveStatRow(Localization.instance.Localize("$azu_epi_stat_weight_percentage"));
            if (IsLiveStatEnabled(LiveStatType.ExtraCarryWeight)) CreateLiveStatRow(Localization.instance.Localize("$azu_epi_stat_extra_carry_weight"));
            CreateSpacer("Spacer_CarryWeight");
        }

        bool hasStaminaUsageStats = IsLiveStatEnabled(LiveStatType.JumpStamina) || IsLiveStatEnabled(LiveStatType.AttackStamina) ||
                                    IsLiveStatEnabled(LiveStatType.BlockStamina) || IsLiveStatEnabled(LiveStatType.DodgeStamina) ||
                                    IsLiveStatEnabled(LiveStatType.SwimStamina) || IsLiveStatEnabled(LiveStatType.RunStamina) ||
                                    IsLiveStatEnabled(LiveStatType.SneakStamina) || IsLiveStatEnabled(LiveStatType.HomeItemStamina);
        if (hasStaminaUsageStats)
        {
            CreateSectionHeader(Localization.instance.Localize("$azu_epi_stat_section_stamina_usage"), new Color(1f, 1f, 0.5f, 1f));
            if (IsLiveStatEnabled(LiveStatType.JumpStamina)) CreateLiveStatRow(Localization.instance.Localize("$azu_epi_stat_jump_stamina"));
            if (IsLiveStatEnabled(LiveStatType.AttackStamina)) CreateLiveStatRow(Localization.instance.Localize("$azu_epi_stat_attack_stamina"));
            if (IsLiveStatEnabled(LiveStatType.BlockStamina)) CreateLiveStatRow(Localization.instance.Localize("$azu_epi_stat_block_stamina"));
            if (IsLiveStatEnabled(LiveStatType.DodgeStamina)) CreateLiveStatRow(Localization.instance.Localize("$azu_epi_stat_dodge_stamina"));
            if (IsLiveStatEnabled(LiveStatType.SwimStamina)) CreateLiveStatRow(Localization.instance.Localize("$azu_epi_stat_swim_stamina"));
            if (IsLiveStatEnabled(LiveStatType.RunStamina)) CreateLiveStatRow(Localization.instance.Localize("$azu_epi_stat_run_stamina"));
            if (IsLiveStatEnabled(LiveStatType.SneakStamina)) CreateLiveStatRow(Localization.instance.Localize("$azu_epi_stat_sneak_stamina"));
            if (IsLiveStatEnabled(LiveStatType.HomeItemStamina)) CreateLiveStatRow(Localization.instance.Localize("$azu_epi_stat_home_item_stamina"));
            CreateSpacer("Spacer_StaminaUsage");
        }

        bool hasEquipmentStats = IsLiveStatEnabled(LiveStatType.TotalArmor) || IsLiveStatEnabled(LiveStatType.HeatResistance) ||
                                 IsLiveStatEnabled(LiveStatType.EquipmentMovement);
        if (hasEquipmentStats)
        {
            CreateSectionHeader(Localization.instance.Localize("$azu_epi_stat_section_equipment_bonuses"), new Color(1f, 0.8f, 0.5f, 1f));
            if (IsLiveStatEnabled(LiveStatType.TotalArmor)) CreateLiveStatRow(Localization.instance.Localize("$azu_epi_stat_total_armor"));
            if (IsLiveStatEnabled(LiveStatType.HeatResistance)) CreateLiveStatRow(Localization.instance.Localize("$azu_epi_stat_heat_resistance"));
            if (IsLiveStatEnabled(LiveStatType.EquipmentMovement)) CreateLiveStatRow(Localization.instance.Localize("$azu_epi_stat_equipment_movement"));
            CreateSpacer("Spacer_EquipmentBonuses");
        }

        bool hasSkillStats = IsLiveStatEnabled(LiveStatType.TopSkills) || IsLiveStatEnabled(LiveStatType.SkillBonuses) ||
                             IsLiveStatEnabled(LiveStatType.SkillRaiseSpeed);
        if (hasSkillStats)
        {
            if (IsLiveStatEnabled(LiveStatType.TopSkills))
            {
                CreateSectionHeader(Localization.instance.Localize("$azu_epi_stat_section_top_skills"), new Color(0.8f, 0.5f, 1f, 1f));
                CreateDynamicTextRow("Top Skills");
                CreateSpacer("Spacer_TopSkills");
            }

            if (IsLiveStatEnabled(LiveStatType.SkillBonuses) || IsLiveStatEnabled(LiveStatType.SkillRaiseSpeed))
            {
                CreateSectionHeader(Localization.instance.Localize("$azu_epi_stat_section_skill_bonuses"), new Color(0.8f, 0.5f, 1f, 1f));
                if (IsLiveStatEnabled(LiveStatType.SkillBonuses)) CreateDynamicTextRow("Skill Bonuses");
                if (IsLiveStatEnabled(LiveStatType.SkillRaiseSpeed)) CreateLiveStatRow(Localization.instance.Localize("$azu_epi_stat_skill_raise_speed"));
                CreateSpacer("Spacer_SkillBonuses");
            }
        }

        bool hasUtilityStats = IsLiveStatEnabled(LiveStatType.NoiseLevel) || IsLiveStatEnabled(LiveStatType.StealthLevel) ||
                               IsLiveStatEnabled(LiveStatType.CoverPercentage) || IsLiveStatEnabled(LiveStatType.FallDamage) ||
                               IsLiveStatEnabled(LiveStatType.ComfortLevel);
        if (hasUtilityStats)
        {
            CreateSectionHeader(Localization.instance.Localize("$azu_epi_stat_section_stealth_utility"), new Color(0.6f, 0.6f, 0.8f, 1f));
            if (IsLiveStatEnabled(LiveStatType.NoiseLevel)) CreateLiveStatRow(Localization.instance.Localize("$azu_epi_stat_noise_level"));
            if (IsLiveStatEnabled(LiveStatType.StealthLevel)) CreateLiveStatRow(Localization.instance.Localize("$azu_epi_stat_stealth_level"));
            if (IsLiveStatEnabled(LiveStatType.CoverPercentage)) CreateLiveStatRow(Localization.instance.Localize("$azu_epi_stat_cover_percentage"));
            if (IsLiveStatEnabled(LiveStatType.FallDamage)) CreateLiveStatRow(Localization.instance.Localize("$azu_epi_stat_fall_damage"));
            if (IsLiveStatEnabled(LiveStatType.ComfortLevel)) CreateLiveStatRow(Localization.instance.Localize("$azu_epi_stat_comfort_level"));
            CreateSpacer("Spacer_Utility");
        }

        bool hasMovementStats = IsLiveStatEnabled(LiveStatType.MovementSpeed) || IsLiveStatEnabled(LiveStatType.SpeedModifier) ||
                                IsLiveStatEnabled(LiveStatType.RunSpeed) || IsLiveStatEnabled(LiveStatType.SwimSpeed) ||
                                IsLiveStatEnabled(LiveStatType.JumpHeight);
        if (hasMovementStats)
        {
            CreateSectionHeader(Localization.instance.Localize("$azu_epi_stat_section_movement"), new Color(0.5f, 1f, 0.5f, 1f));
            if (IsLiveStatEnabled(LiveStatType.MovementSpeed)) CreateLiveStatRow(Localization.instance.Localize("$azu_epi_stat_movement_speed"));
            if (IsLiveStatEnabled(LiveStatType.SpeedModifier)) CreateLiveStatRow(Localization.instance.Localize("$azu_epi_stat_speed_modifier"));
            if (IsLiveStatEnabled(LiveStatType.RunSpeed)) CreateLiveStatRow(Localization.instance.Localize("$azu_epi_stat_run_speed"));
            if (IsLiveStatEnabled(LiveStatType.SwimSpeed)) CreateLiveStatRow(Localization.instance.Localize("$azu_epi_stat_swim_speed"));
            if (IsLiveStatEnabled(LiveStatType.JumpHeight)) CreateLiveStatRow(Localization.instance.Localize("$azu_epi_stat_jump_height"));
            CreateSpacer("Spacer_Movement");
        }

        CreateResistancesSection();
        CreateActiveFoodSection();
        CreateActiveEffectsSection();
        CreateSetBonusesSection();
    }

    private static void CreateSectionHeader(string title, Color color)
    {
        if (!_content) return;

        GameObject headerObj = new($"Section_{title}", typeof(RectTransform));
        headerObj.SafeSetActive(false);
        RectTransform headerRect = headerObj.GetComponent<RectTransform>();
        headerRect.SetParent(_content, false);

        TextMeshProUGUI headerText = headerObj.AddComponent<TextMeshProUGUI>();
        if (_fontAsset)
        {
            headerText.font = _fontAsset;
            headerText.fontSharedMaterial = _fontAsset.material;
        }

        headerObj.SafeSetActive(true);
        headerText.text = title;
        headerText.fontSize = 20f;
        headerText.fontStyle = FontStyles.Bold;
        headerText.alignment = TextAlignmentOptions.Left;
        headerText.color = color;
        headerText.raycastTarget = false;

        LayoutElement headerLayout = headerObj.AddComponent<LayoutElement>();
        headerLayout.preferredHeight = 20f;
        headerLayout.minHeight = 20f;
    }

    private static void CreateSpacer(string name)
    {
        if (!_content) return;

        GameObject spacer = new(name, typeof(RectTransform));
        RectTransform spacerRect = spacer.GetComponent<RectTransform>();
        spacerRect.SetParent(_content, false);
        LayoutElement spacerLayout = spacer.AddComponent<LayoutElement>();
        spacerLayout.preferredHeight = 8f;
        spacerLayout.minHeight = 8f;
    }

    private static void CreateResistancesSection()
    {
        if (!_content) return;

        bool hasResistanceStats = IsLiveStatEnabled(LiveStatType.Armor) || IsLiveStatEnabled(LiveStatType.BluntResist) ||
                                  IsLiveStatEnabled(LiveStatType.SlashResist) || IsLiveStatEnabled(LiveStatType.PierceResist) ||
                                  IsLiveStatEnabled(LiveStatType.FireResist) || IsLiveStatEnabled(LiveStatType.FrostResist) ||
                                  IsLiveStatEnabled(LiveStatType.LightningResist) || IsLiveStatEnabled(LiveStatType.PoisonResist) ||
                                  IsLiveStatEnabled(LiveStatType.SpiritResist);

        if (!hasResistanceStats) return;

        GameObject headerObj = new("Section_Resistances", typeof(RectTransform));
        headerObj.SafeSetActive(false);
        RectTransform headerRect = headerObj.GetComponent<RectTransform>();
        headerRect.SetParent(_content, false);

        TextMeshProUGUI headerText = headerObj.AddComponent<TextMeshProUGUI>();
        if (_fontAsset)
        {
            headerText.font = _fontAsset;
            headerText.fontSharedMaterial = _fontAsset.material;
        }

        headerObj.SafeSetActive(true);
        headerText.text = Localization.instance.Localize("$azu_epi_stat_section_resistances");
        headerText.fontSize = 20f;
        headerText.fontStyle = FontStyles.Bold;
        headerText.alignment = TextAlignmentOptions.Left;
        headerText.color = new Color(1f, 0.84f, 0f, 1f);
        headerText.raycastTarget = false;

        LayoutElement headerLayout = headerObj.AddComponent<LayoutElement>();
        headerLayout.preferredHeight = 20f;
        headerLayout.minHeight = 20f;

        if (IsLiveStatEnabled(LiveStatType.Armor)) CreateLiveStatRow(Localization.instance.Localize("$azu_epi_stat_armor"));
        if (IsLiveStatEnabled(LiveStatType.BluntResist)) CreateLiveStatRow(Localization.instance.Localize("$azu_epi_stat_blunt_resist"));
        if (IsLiveStatEnabled(LiveStatType.SlashResist)) CreateLiveStatRow(Localization.instance.Localize("$azu_epi_stat_slash_resist"));
        if (IsLiveStatEnabled(LiveStatType.PierceResist)) CreateLiveStatRow(Localization.instance.Localize("$azu_epi_stat_pierce_resist"));
        if (IsLiveStatEnabled(LiveStatType.FireResist)) CreateLiveStatRow(Localization.instance.Localize("$azu_epi_stat_fire_resist"));
        if (IsLiveStatEnabled(LiveStatType.FrostResist)) CreateLiveStatRow(Localization.instance.Localize("$azu_epi_stat_frost_resist"));
        if (IsLiveStatEnabled(LiveStatType.LightningResist)) CreateLiveStatRow(Localization.instance.Localize("$azu_epi_stat_lightning_resist"));
        if (IsLiveStatEnabled(LiveStatType.PoisonResist)) CreateLiveStatRow(Localization.instance.Localize("$azu_epi_stat_poison_resist"));
        if (IsLiveStatEnabled(LiveStatType.SpiritResist)) CreateLiveStatRow(Localization.instance.Localize("$azu_epi_stat_spirit_resist"));

        GameObject spacer = new("Spacer_Resistances", typeof(RectTransform));
        RectTransform spacerRect = spacer.GetComponent<RectTransform>();
        spacerRect.SetParent(_content, false);
        LayoutElement spacerLayout = spacer.AddComponent<LayoutElement>();
        spacerLayout.preferredHeight = 8f;
        spacerLayout.minHeight = 8f;
    }

    private static void CreateActiveEffectsSection()
    {
        if (!_content || !IsLiveStatEnabled(LiveStatType.ActiveEffects)) return;

        CreateSectionHeader(Localization.instance.Localize("$azu_epi_stat_section_active_effects"), new Color(0.5f, 0.8f, 1f, 1f));
        CreateDynamicTextRow("ActiveEffects");

        CreateSpacer("Spacer_ActiveEffects");
    }

    private static void CreateSetBonusesSection()
    {
        if (!_content || !IsLiveStatEnabled(LiveStatType.SetBonuses)) return;

        CreateSectionHeader(Localization.instance.Localize("$azu_epi_stat_section_set_bonuses"), new Color(0.8f, 0.6f, 1f, 1f));

        CreateDynamicTextRow("SetBonuses");

        CreateSpacer("Spacer_SetBonuses");
    }

    private static void CreateActiveFoodSection()
    {
        if (!_content || !IsLiveStatEnabled(LiveStatType.FoodBuffs)) return;

        CreateSectionHeader(Localization.instance.Localize("$azu_epi_stat_section_food_buffs"), new Color(1f, 0.7f, 0.3f, 1f));

        CreateDynamicTextRow("FoodBuffs");

        CreateSpacer("Spacer_FoodBuffs");
    }

    private static void CreateDynamicTextRow(string id)
    {
        if (!_content) return;

        GameObject textObj = new($"DynamicText_{id}", typeof(RectTransform));
        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textObj.SafeSetActive(false);
        textRect.SetParent(_content, false);

        TextMeshProUGUI text = textObj.AddComponent<TextMeshProUGUI>();
        if (_fontAsset)
        {
            text.font = _fontAsset;
            text.fontSharedMaterial = _fontAsset.material;
        }

        textObj.SafeSetActive(true);
        text.text = "";
        text.fontSize = 16f;
        text.alignment = TextAlignmentOptions.TopLeft;
        text.color = new Color(0.9f, 0.9f, 0.9f, 1f);
        text.raycastTarget = false;
        text.enableWordWrapping = true;
        text.overflowMode = TextOverflowModes.Overflow;

        ContentSizeFitter sizeFitter = textObj.AddComponent<ContentSizeFitter>();
        sizeFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        sizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        LayoutElement textLayout = textObj.AddComponent<LayoutElement>();
        textLayout.minHeight = 16f;
        textLayout.preferredHeight = -1;
        textLayout.flexibleHeight = 0f; // Don't use flexible height

        _statElements.Add(new StatElement { Name = id, StatType = (PlayerStatType)(-2), ValueText = text, IsLiveStat = true });
    }

    private static void CreateLiveStatRow(string statName)
    {
        GameObject rowObj = new($"LiveStatRow_{statName}", typeof(RectTransform), typeof(HorizontalLayoutGroup));
        RectTransform rowRect = rowObj.GetComponent<RectTransform>();
        rowRect.SetParent(_content, false);

        HorizontalLayoutGroup hLayout = rowObj.GetComponent<HorizontalLayoutGroup>();
        hLayout.childAlignment = TextAnchor.MiddleLeft;
        hLayout.spacing = 4f;
        hLayout.childControlWidth = true;
        hLayout.childControlHeight = true;
        hLayout.childForceExpandWidth = true;
        hLayout.childForceExpandHeight = false;

        LayoutElement rowLayout = rowObj.AddComponent<LayoutElement>();
        rowLayout.preferredHeight = 18f;
        rowLayout.minHeight = 18f;

        GameObject labelObj = new("Label", typeof(RectTransform));
        labelObj.SafeSetActive(false);
        RectTransform labelRect = labelObj.GetComponent<RectTransform>();
        labelRect.SetParent(rowRect, false);

        TextMeshProUGUI labelText = labelObj.AddComponent<TextMeshProUGUI>();
        if (_fontAsset)
        {
            labelText.font = _fontAsset;
            labelText.fontSharedMaterial = _fontAsset.material;
        }

        labelObj.SafeSetActive(true);
        labelText.text = statName;
        labelText.fontSize = 18f;
        labelText.alignment = TextAlignmentOptions.Left;
        labelText.color = new Color(0.9f, 0.9f, 0.9f, 1f);
        labelText.raycastTarget = false;

        LayoutElement labelLayout = labelObj.AddComponent<LayoutElement>();
        labelLayout.flexibleWidth = 1f;
        labelLayout.minWidth = 100f;

        GameObject valueObj = new("Value", typeof(RectTransform));
        RectTransform valueRect = valueObj.GetComponent<RectTransform>();
        valueRect.SetParent(rowRect, false);
        valueObj.SafeSetActive(false);
        TextMeshProUGUI valueText = valueObj.AddComponent<TextMeshProUGUI>();
        if (_fontAsset)
        {
            valueText.font = _fontAsset;
            valueText.fontSharedMaterial = _fontAsset.material;
        }

        valueObj.SafeSetActive(true);
        valueText.text = "0";
        valueText.fontSize = 18f;
        valueText.alignment = TextAlignmentOptions.Right;
        valueText.textWrappingMode = TextWrappingModes.PreserveWhitespaceNoWrap;
        valueText.color = Color.white;
        valueText.raycastTarget = false;
        valueText.fontStyle = FontStyles.Bold;

        LayoutElement valueLayout = valueObj.AddComponent<LayoutElement>();
        valueLayout.minWidth = 60f;
        valueLayout.preferredWidth = 60f;

        _statElements.Add(new StatElement { Name = statName, StatType = (PlayerStatType)(-1), ValueText = valueText, IsLiveStat = true });

        CreateSeparator();
    }

    private static void CreateSeparator()
    {
        if (!_content || !_tabBorderTemplate) return;

        GameObject separator = Object.Instantiate(_tabBorderTemplate.gameObject, _content);
        separator.name = "Separator";
        RectTransform sepRT = separator.GetComponent<RectTransform>();
        sepRT.anchorMin = new Vector2(0f, 0f);
        sepRT.anchorMax = new Vector2(1f, 0f);
        sepRT.pivot = new Vector2(0.5f, 0.5f);
        sepRT.sizeDelta = new Vector2(0f, 2f);

        LayoutElement sepLayout = separator.AddComponent<LayoutElement>();
        sepLayout.preferredHeight = 2f;
        sepLayout.minHeight = 2f;

        Image? img = separator.GetComponent<Image>();
        if (img)
        {
            Color c = img.color;
            c.a = 0.3f;
            img.color = c;
        }
    }

    private static void CreateSection(string title, PlayerStatType[] stats)
    {
        if (!_content) return;

        GameObject headerObj = new($"Section_{title}", typeof(RectTransform));
        RectTransform headerRect = headerObj.GetComponent<RectTransform>();
        headerRect.SetParent(_content, false);
        headerObj.SafeSetActive(false);
        TextMeshProUGUI headerText = headerObj.AddComponent<TextMeshProUGUI>();
        if (_fontAsset)
        {
            headerText.font = _fontAsset;
            headerText.fontSharedMaterial = _fontAsset.material;
        }

        headerObj.SafeSetActive(true);
        headerText.text = title;
        headerText.fontSize = 20f;
        headerText.fontStyle = FontStyles.Bold;
        headerText.alignment = TextAlignmentOptions.Left;
        headerText.color = new Color(1f, 0.84f, 0f, 1f);
        headerText.raycastTarget = false;

        LayoutElement headerLayout = headerObj.AddComponent<LayoutElement>();
        headerLayout.preferredHeight = 20f;
        headerLayout.minHeight = 20f;

        foreach (PlayerStatType statType in stats)
        {
            string label = StatLabels.TryGetValue(statType, out string? statLabel) ? statLabel : statType.ToString();
            CreateStatRow(_content.gameObject, label, statType);
        }

        GameObject spacer = new($"Spacer_{title}", typeof(RectTransform));
        RectTransform spacerRect = spacer.GetComponent<RectTransform>();
        spacerRect.SetParent(_content, false);
        LayoutElement spacerLayout = spacer.AddComponent<LayoutElement>();
        spacerLayout.preferredHeight = 8f;
        spacerLayout.minHeight = 8f;
    }

    private static List<PlayerStatType> GetDefaultStats()
    {
        return Enum.GetValues(typeof(PlayerStatType)).Cast<PlayerStatType>().Where(stat => stat != PlayerStatType.Count).ToList();
    }

    private static void CreateStatRow(GameObject parent, string statName, PlayerStatType statType)
    {
        string localizedName = Localization.instance.Localize(statName);
        GameObject rowObj = new($"StatRow_{localizedName}", typeof(RectTransform), typeof(HorizontalLayoutGroup));
        RectTransform rowRect = rowObj.GetComponent<RectTransform>();
        rowRect.SetParent(parent.transform, false);

        HorizontalLayoutGroup hLayout = rowObj.GetComponent<HorizontalLayoutGroup>();
        hLayout.childAlignment = TextAnchor.MiddleLeft;
        hLayout.spacing = 4f;
        hLayout.childControlWidth = true;
        hLayout.childControlHeight = true;
        hLayout.childForceExpandWidth = false;
        hLayout.childForceExpandHeight = false;

        LayoutElement rowLayout = rowObj.AddComponent<LayoutElement>();
        rowLayout.preferredHeight = 18f;
        rowLayout.minHeight = 18f;

        GameObject labelObj = new("Label", typeof(RectTransform));
        labelObj.SafeSetActive(false);
        RectTransform labelRect = labelObj.GetComponent<RectTransform>();
        labelRect.SetParent(rowRect, false);

        TextMeshProUGUI labelText = labelObj.AddComponent<TextMeshProUGUI>();
        if (_fontAsset)
        {
            labelText.font = _fontAsset;
            labelText.fontSharedMaterial = _fontAsset.material;
        }

        labelObj.SafeSetActive(true);
        labelText.text = localizedName;
        labelText.fontSize = 18f;
        labelText.alignment = TextAlignmentOptions.Left;
        labelText.color = new Color(0.9f, 0.9f, 0.9f, 1f);
        labelText.raycastTarget = false;

        LayoutElement labelLayout = labelObj.AddComponent<LayoutElement>();
        labelLayout.flexibleWidth = 1f;
        labelLayout.minWidth = 100f;

        GameObject valueObj = new("Value", typeof(RectTransform));
        RectTransform valueRect = valueObj.GetComponent<RectTransform>();
        valueRect.SetParent(rowRect, false);
        valueObj.SafeSetActive(false);
        TextMeshProUGUI valueText = valueObj.AddComponent<TextMeshProUGUI>();
        if (_fontAsset)
        {
            valueText.font = _fontAsset;
            valueText.fontSharedMaterial = _fontAsset.material;
        }

        valueObj.SafeSetActive(true);

        valueText.text = "0";
        valueText.fontSize = 18f;
        valueText.alignment = TextAlignmentOptions.Right;
        valueText.textWrappingMode = TextWrappingModes.PreserveWhitespaceNoWrap;
        valueText.color = Color.white;
        valueText.raycastTarget = false;
        valueText.fontStyle = FontStyles.Bold;

        LayoutElement valueLayout = valueObj.AddComponent<LayoutElement>();
        valueLayout.minWidth = 60f;
        valueLayout.preferredWidth = 60f;

        _statElements.Add(new StatElement { Name = localizedName, StatType = statType, ValueText = valueText });

        CreateSeparator();
    }

    internal static void BuildToggleButton(InventoryGui gui)
    {
        if (ToggleButtonParentGlg == null) return;

        PanelUtilities.ButtonConfig config = new(
            name: StatsToggleButtonName,
            anchorMin: ToggleBtnAnchorMin,
            anchorMax: ToggleBtnAnchorMax,
            pivot: ToggleBtnPivot,
            anchoredPosition: new Vector2(-50f, -30f),
            size: ToggleBtnSize,
            gamepadKey: PanelUtilities.KeyCodeToZInputKey(StatsToggleGamepadKey.Value),
            gamepadKeyCode: StatsToggleGamepadKey.Value,
            label: "📋",
            labelFontSize: 20f,
            onClick: () =>
            {
                _visible = !_visible;
                SetVisible(_visible);
                if (VanityPanelController.IsVisible()) VanityPanelController.SetVisible(false);
                if (PersonalLoadoutGui.IsVisible()) PersonalLoadoutGui.Hide();
                PanelUtilities.HideCraftingElements(_visible);
            }
        );

        (StatsButtonGo, _toggleBtn) = PanelUtilities.BuildToggleButton(gui, ToggleButtonParentGlg, config);
        StatsButtonGo.gameObject.SetActive(true);
    }

    public static void UpdateStats(Player player)
    {
        if (player == null || _statElements.Count == 0) return;

        PlayerProfile? profile = global::Game.instance?.GetPlayerProfile();
        if (profile == null) return;

        foreach (StatElement element in _statElements)
        {
            if (element.IsLiveStat)
            {
                switch (element.Name)
                {
                    case "Health":
                        element.ValueText.text = $"{player.GetHealth():F0} / {player.GetMaxHealth():F0}";
                        break;
                    case "Stamina":
                        element.ValueText.text = $"{player.GetStamina():F0} / {player.GetMaxStamina():F0}";
                        break;
                    case "Eitr":
                        element.ValueText.text = $"{player.GetEitr():F0} / {player.GetMaxEitr():F0}";
                        break;
                    case "Adrenaline":
                        float adrenaline = CalculateAdrenaline(player);
                        element.ValueText.text = adrenaline > 0 ? $"{adrenaline:F0}%" : "0%";
                        break;

                    case "Health Regen":
                        float healthRegen = CalculateHealthRegen(player);
                        element.ValueText.text = $"{healthRegen:F1}/tick";
                        break;
                    case "Health Regen Multi":
                        float healthRegenMulti = CalculateHealthRegenMultiplier(player);
                        element.ValueText.text = healthRegenMulti != 0 ? $"{healthRegenMulti:+0;-0}%" : "0%";
                        break;
                    case "Food Regen":
                        float foodRegen = CalculateFoodRegen(player);
                        element.ValueText.text = $"{foodRegen:F1}/s";
                        break;
                    case "Stamina Regen":
                        element.ValueText.text = $"{player.m_staminaRegen:F1}/s";
                        break;
                    case "Stamina Regen Multi":
                        float staminaRegenMulti = CalculateStaminaRegenMultiplier(player);
                        element.ValueText.text = staminaRegenMulti != 0 ? $"{staminaRegenMulti:+0;-0}%" : "0%";
                        break;
                    case "Eitr Regen":
                        element.ValueText.text = $"{player.m_eiterRegen:F1}/s";
                        break;
                    case "Eitr Regen Multi":
                        float eitrRegenMulti = CalculateEitrRegenMultiplier(player);
                        element.ValueText.text = eitrRegenMulti != 0 ? $"{eitrRegenMulti:+0;-0}%" : "0%";
                        break;

                    case "Attack Speed":
                        float attackSpeed = CalculateAttackSpeed(player);
                        element.ValueText.text = $"{attackSpeed:F0}%";
                        break;
                    case "Damage Modifier":
                        float damageMod = CalculateDamageModifier(player);
                        element.ValueText.text = damageMod != 0 ? $"{damageMod:+0;-0}%" : "0%";
                        break;
                    case "Stagger Resist":
                        float staggerResist = CalculateStaggerResist(player);
                        element.ValueText.text = staggerResist != 0 ? $"{staggerResist:+0;-0}%" : "0%";
                        break;
                    case "Timed Block Bonus":
                        float blockBonus = CalculateTimedBlockBonus(player);
                        element.ValueText.text = blockBonus != 0 ? $"{blockBonus:+0;-0}%" : "0%";
                        break;
                    case "Crit Chance":
                        float critChance = CalculateCritChance(player);
                        element.ValueText.text = critChance > 0 ? $"{critChance:F1}%" : "0%";
                        break;
                    case "Lifesteal":
                        float lifesteal = CalculateLifesteal(player);
                        element.ValueText.text = lifesteal > 0 ? $"{lifesteal:F1}%" : "0%";
                        break;

                    case "Blunt Damage":
                        float bluntDamage = CalculateElementalDamageBonus(player, HitData.DamageType.Blunt);
                        element.ValueText.text = bluntDamage != 0 ? $"{bluntDamage:+0;-0}%" : "0%";
                        break;
                    case "Slash Damage":
                        float slashDamage = CalculateElementalDamageBonus(player, HitData.DamageType.Slash);
                        element.ValueText.text = slashDamage != 0 ? $"{slashDamage:+0;-0}%" : "0%";
                        break;
                    case "Pierce Damage":
                        float pierceDamage = CalculateElementalDamageBonus(player, HitData.DamageType.Pierce);
                        element.ValueText.text = pierceDamage != 0 ? $"{pierceDamage:+0;-0}%" : "0%";
                        break;
                    case "Fire Damage":
                        float fireDamage = CalculateElementalDamageBonus(player, HitData.DamageType.Fire);
                        element.ValueText.text = fireDamage != 0 ? $"{fireDamage:+0;-0}%" : "0%";
                        break;
                    case "Frost Damage":
                        float frostDamage = CalculateElementalDamageBonus(player, HitData.DamageType.Frost);
                        element.ValueText.text = frostDamage != 0 ? $"{frostDamage:+0;-0}%" : "0%";
                        break;
                    case "Lightning Damage":
                        float lightningDamage = CalculateElementalDamageBonus(player, HitData.DamageType.Lightning);
                        element.ValueText.text = lightningDamage != 0 ? $"{lightningDamage:+0;-0}%" : "0%";
                        break;
                    case "Poison Damage":
                        float poisonDamage = CalculateElementalDamageBonus(player, HitData.DamageType.Poison);
                        element.ValueText.text = poisonDamage != 0 ? $"{poisonDamage:+0;-0}%" : "0%";
                        break;
                    case "Spirit Damage":
                        float spiritDamage = CalculateElementalDamageBonus(player, HitData.DamageType.Spirit);
                        element.ValueText.text = spiritDamage != 0 ? $"{spiritDamage:+0;-0}%" : "0%";
                        break;

                    case "Current Weight":
                        float current = player.GetInventory()?.GetTotalWeight() ?? 0f;
                        element.ValueText.text = $"{current:F0}";
                        break;
                    case "Max Weight":
                        float max = player.GetMaxCarryWeight();
                        element.ValueText.text = $"{max:F0}";
                        break;
                    case "Weight Percentage":
                        float currentWeight = player.GetInventory()?.GetTotalWeight() ?? 0f;
                        float maxWeight = player.GetMaxCarryWeight();
                        float weightPercentage = maxWeight > 0 ? (currentWeight / maxWeight) * 100f : 0f;
                        element.ValueText.text = $"{weightPercentage:F0}%";
                        break;
                    case "Extra Carry Weight":
                        float extra = CalculateExtraCarryWeight(player);
                        element.ValueText.text = extra > 0 ? $"+{extra:F0}" : "0";
                        break;

                    case "Jump Stamina":
                        float jumpStamina = CalculateJumpStaminaUsage(player);
                        element.ValueText.text = jumpStamina != 0 ? $"{jumpStamina:+0;-0}%" : "0%";
                        break;
                    case "Attack Stamina":
                        float attackStamina = CalculateAttackStaminaUsage(player);
                        element.ValueText.text = attackStamina != 0 ? $"{attackStamina:+0;-0}%" : "0%";
                        break;
                    case "Block Stamina":
                        float blockStamina = CalculateBlockStaminaUsage(player);
                        element.ValueText.text = blockStamina != 0 ? $"{blockStamina:+0;-0}%" : "0%";
                        break;
                    case "Dodge Stamina":
                        float dodgeStamina = CalculateDodgeStaminaUsage(player);
                        element.ValueText.text = dodgeStamina != 0 ? $"{dodgeStamina:+0;-0}%" : "0%";
                        break;
                    case "Swim Stamina":
                        float swimStamina = CalculateSwimStaminaUsage(player);
                        element.ValueText.text = swimStamina != 0 ? $"{swimStamina:+0;-0}%" : "0%";
                        break;
                    case "Run Stamina":
                        float runStamina = CalculateRunStaminaUsage(player);
                        element.ValueText.text = runStamina != 0 ? $"{runStamina:+0;-0}%" : "0%";
                        break;
                    case "Sneak Stamina":
                        float sneakStamina = CalculateSneakStaminaUsage(player);
                        element.ValueText.text = sneakStamina != 0 ? $"{sneakStamina:+0;-0}%" : "0%";
                        break;
                    case "Home Item Stamina":
                        float homeStamina = CalculateHomeItemStaminaUsage(player);
                        element.ValueText.text = homeStamina != 0 ? $"{homeStamina:+0;-0}%" : "0%";
                        break;

                    case "Total Armor":
                        element.ValueText.text = $"{player.GetBodyArmor():F0}";
                        break;
                    case "Heat Resistance":
                        float heatResist = CalculateHeatResistance(player);
                        element.ValueText.text = heatResist != 0 ? $"{heatResist:+0;-0}%" : "0%";
                        break;
                    case "Equipment Movement":
                        float equipMove = CalculateEquipmentMovement(player);
                        element.ValueText.text = equipMove != 0 ? $"{equipMove:+0;-0}%" : "0%";
                        break;

                    case "Top Skills":
                        element.ValueText.text = GetTopSkills(player);
                        break;

                    case "Skill Bonuses":
                        element.ValueText.text = GetAddedSkillPoints(player);
                        break;
                    case "Skill Raise Speed":
                        float skillRaise = CalculateSkillRaiseSpeed(player);
                        element.ValueText.text = skillRaise != 0 ? $"{skillRaise:+0;-0}%" : "0%";
                        break;

                    case "Noise Level":
                        float noise = CalculateNoiseLevel(player);
                        element.ValueText.text = noise != 0 ? $"{noise:+0;-0}%" : "0%";
                        break;
                    case "Stealth Level":
                        float stealth = CalculateStealthLevel(player);
                        element.ValueText.text = stealth != 0 ? $"{stealth:+0;-0}%" : "0%";
                        break;
                    case "Cover Percentage":
                        float cover = player.m_coverPercentage;
                        element.ValueText.text = $"{cover:F0}%";
                        break;
                    case "Fall Damage":
                        float fallDamage = CalculateFallDamage(player);
                        element.ValueText.text = fallDamage != 0 ? $"{fallDamage:+0;-0}%" : "0%";
                        break;
                    case "Comfort Level":
                        int comfort = player.GetComfortLevel();
                        element.ValueText.text = $"{comfort}";
                        break;
                    case "Movement Speed":
                        element.ValueText.text = $"{player.GetJogSpeedFactor() * 100:F0}%";
                        break;
                    case "Speed Modifier":
                        float speedMod = CalculateSpeedModifier(player);
                        element.ValueText.text = speedMod != 0 ? $"{speedMod:+0;-0}%" : "0%";
                        break;
                    case "Run Speed":
                        element.ValueText.text = $"{player.GetRunSpeedFactor() * 100:F0}%";
                        break;
                    case "Swim Speed":
                        element.ValueText.text = $"{player.m_swimSpeed * player.GetAttackSpeedFactorMovement():F0}%";
                        break;
                    case "Jump Height":
                        float jumpMod = CalculateJumpModifier(player);
                        element.ValueText.text = jumpMod > 0 ? $"+{jumpMod:F0}%" : jumpMod < 0 ? $"{jumpMod:F0}%" : "100%";
                        break;

                    case "Armor":
                        element.ValueText.text = $"{player.GetBodyArmor():F0}";
                        break;
                    case "Blunt Resist":
                        element.ValueText.text = FormatResistance(GetResistance(player, HitData.DamageType.Blunt));
                        break;
                    case "Slash Resist":
                        element.ValueText.text = FormatResistance(GetResistance(player, HitData.DamageType.Slash));
                        break;
                    case "Pierce Resist":
                        element.ValueText.text = FormatResistance(GetResistance(player, HitData.DamageType.Pierce));
                        break;
                    case "Fire Resist":
                        element.ValueText.text = FormatResistance(GetResistance(player, HitData.DamageType.Fire));
                        break;
                    case "Frost Resist":
                        element.ValueText.text = FormatResistance(GetResistance(player, HitData.DamageType.Frost));
                        break;
                    case "Lightning Resist":
                        element.ValueText.text = FormatResistance(GetResistance(player, HitData.DamageType.Lightning));
                        break;
                    case "Poison Resist":
                        element.ValueText.text = FormatResistance(GetResistance(player, HitData.DamageType.Poison));
                        break;
                    case "Spirit Resist":
                        element.ValueText.text = FormatResistance(GetResistance(player, HitData.DamageType.Spirit));
                        break;

                    case "FoodBuffs":
                        element.ValueText.text = GetActiveFoodBuffs(player);
                        break;

                    case "ActiveEffects":
                        element.ValueText.text = GetActiveEffects(player);
                        break;

                    case "SetBonuses":
                        element.ValueText.text = GetActiveSetBonuses(player);
                        break;
                }

                continue;
            }

            float statValue = profile.m_playerStats[element.StatType];

            switch (element.StatType)
            {
                case PlayerStatType.DistanceTraveled:
                case PlayerStatType.DistanceWalk:
                case PlayerStatType.DistanceRun:
                case PlayerStatType.DistanceSail:
                case PlayerStatType.DistanceAir:
                    element.ValueText.text = statValue >= 1000f ? $"{(statValue / 1000f):F1}km" : $"{statValue:F0}m";
                    break;

                case PlayerStatType.TimeInBase:
                case PlayerStatType.TimeOutOfBase:
                case PlayerStatType.Sleep:
                    if (statValue >= 86400)
                        element.ValueText.text = $"{(statValue / 86400f):F1}d";
                    else if (statValue >= 3600)
                        element.ValueText.text = $"{(statValue / 3600f):F1}h";
                    else if (statValue >= 60)
                        element.ValueText.text = $"{(statValue / 60f):F0}m";
                    else
                        element.ValueText.text = $"{statValue:F0}s";
                    break;

                default:
                    if (statValue >= 1000000)
                        element.ValueText.text = $"{statValue / 1000000f:F1}M";
                    else if (statValue >= 10000)
                        element.ValueText.text = $"{statValue / 1000f:F1}k";
                    else
                        element.ValueText.text = statValue.ToString("N0");
                    break;
            }
        }
    }

    private static float CalculateHealthRegen(Player player)
    {
        float totalRegen = 0f;

        try
        {
            if (player.m_seman?.GetStatusEffects() != null)
            {
                foreach (StatusEffect effect in player.m_seman.GetStatusEffects())
                {
                    if (effect is not SE_Stats seStats) continue;
                    if (seStats.m_tickInterval > 0 && seStats.m_healthPerTick != 0)
                    {
                        totalRegen += seStats.m_healthPerTick;
                    }

                    if (seStats is { m_healthOverTime: > 0, m_healthOverTimeInterval: > 0 })
                    {
                        totalRegen += seStats.m_healthOverTimeTickHP;
                    }
                }
            }
        }
        catch
        {
            /* Ignore errors */
        }

        return totalRegen;
    }

    private static float CalculateFoodRegen(Player player)
    {
        float foodRegen = 0f;

        try
        {
            if (player.m_foods is { Count: > 0 })
            {
                foreach (Player.Food? food in player.m_foods)
                {
                    if (food is not { m_item: not null }) continue;
                    float foodHealth = food.m_item.m_shared.m_food;
                    float foodTime = food.m_item.m_shared.m_foodBurnTime;
                    if (foodTime > 0)
                    {
                        foodRegen += foodHealth / foodTime;
                    }
                }
            }
        }
        catch
        {
            /* Ignore errors */
        }

        return foodRegen;
    }

    private static float CalculateMultiplierModifier(Player player, Func<SE_Stats, float> getMultiplier) => PlayerStatsCalculator.CalculateMultiplierModifierPercent(player.m_seman, getMultiplier);

    private static float CalculateAdrenaline(Player player) => CalculateModifiers(player, seStats => seStats.m_adrenalineModifier);

    private static float CalculateHealthRegenMultiplier(Player player) => PlayerStatsCalculator.CalculateHealthRegenMultiplier(player);

    private static float CalculateStaminaRegenMultiplier(Player player) => PlayerStatsCalculator.CalculateStaminaRegenMultiplier(player);

    private static float CalculateEitrRegenMultiplier(Player player) => PlayerStatsCalculator.CalculateEitrRegenMultiplier(player);

    private static float CalculateAttackSpeed(Player player) => PlayerStatsCalculator.CalculateAttackSpeed(player);

    private static float CalculateDamageModifier(Player player) => PlayerStatsCalculator.CalculateDamageModifier(player);

    private static float CalculateStaggerResist(Player player) => PlayerStatsCalculator.CalculateStaggerResist(player);

    private static float CalculateTimedBlockBonus(Player player) => CalculateModifiers(player, seStats => seStats.m_timedBlockBonus);

    private static float CalculateMaxCarryWeight(Player player)
    {
        float maxWeight = 0f;

        try
        {
            maxWeight = player.m_maxCarryWeight;
        }
        catch
        {
            /* Ignore errors */
        }

        return maxWeight;
    }

    private static float CalculateExtraCarryWeight(Player player)
    {
        float extraWeight = 0f;

        try
        {
            if (player.m_seman?.GetStatusEffects() != null)
            {
                foreach (StatusEffect effect in player.m_seman.GetStatusEffects())
                {
                    if (effect is SE_Stats { m_addMaxCarryWeight: > 0 } seStats)
                    {
                        extraWeight += seStats.m_addMaxCarryWeight;
                    }
                }
            }
        }
        catch
        {
            /* Ignore errors */
        }

        return extraWeight;
    }

    private static float CalculateModifiers(Player player, Func<SE_Stats, float> getSeStatsModifier, Func<ItemDrop.ItemData.SharedData, float>? getItemModifier = null)
    {
        float modifier = 0f;

        try
        {
            if (player.m_seman?.GetStatusEffects() != null)
            {
                foreach (StatusEffect effect in player.m_seman.GetStatusEffects())
                {
                    if (effect is not SE_Stats seStats) continue;
                    float value = getSeStatsModifier(seStats);
                    if (value != 0f)
                        modifier += value * 100f;
                }
            }

            if (getItemModifier != null)
            {
                List<ItemDrop.ItemData> equipped = player.GetInventory()?.GetEquippedItems() ?? [];
                foreach (ItemDrop.ItemData item in equipped)
                {
                    if (item == null) continue;
                    float value = getItemModifier(item.m_shared);
                    if (value != 0f)
                        modifier += value * 100f;
                }
            }
        }
        catch
        {
            /* Ignore errors */
        }

        return modifier;
    }

    private static float CalculateJumpStaminaUsage(Player player) => CalculateModifiers(player, seStats => seStats.m_jumpStaminaUseModifier, shared => shared.m_jumpStaminaModifier);

    private static float CalculateAttackStaminaUsage(Player player) => CalculateModifiers(player, seStats => seStats.m_attackStaminaUseModifier, shared => shared.m_attackStaminaModifier);

    private static float CalculateBlockStaminaUsage(Player player) => CalculateModifiers(player, seStats => seStats.m_blockStaminaUseModifier, shared => shared.m_blockStaminaModifier);

    private static float CalculateDodgeStaminaUsage(Player player) => CalculateModifiers(player, seStats => seStats.m_dodgeStaminaUseModifier, shared => shared.m_dodgeStaminaModifier);

    private static float CalculateSwimStaminaUsage(Player player) => CalculateModifiers(player, seStats => seStats.m_swimStaminaUseModifier, shared => shared.m_swimStaminaModifier);

    private static float CalculateRunStaminaUsage(Player player) => CalculateModifiers(player, seStats => seStats.m_runStaminaUseModifier, shared => shared.m_runStaminaModifier);

    private static float CalculateSneakStaminaUsage(Player player) => CalculateModifiers(player, seStats => seStats.m_sneakStaminaUseModifier, shared => shared.m_sneakStaminaModifier);

    private static float CalculateHomeItemStaminaUsage(Player player) => CalculateModifiers(player, _ => 0f, shared => shared.m_homeItemsStaminaModifier);

    private static float CalculateNoiseLevel(Player player) => CalculateModifiers(player, seStats => seStats.m_noiseModifier);

    private static float CalculateStealthLevel(Player player) => CalculateModifiers(player, seStats => seStats.m_stealthModifier);

    private static float CalculateFallDamage(Player player) => CalculateModifiers(player, seStats => seStats.m_fallDamageModifier);

    private static float CalculateCritChance(Player player)
    {
        float critChance = 0f;

        try
        {
            if (player.m_seman?.GetStatusEffects() != null)
            {
                foreach (StatusEffect effect in player.m_seman.GetStatusEffects())
                {
                    if (effect == null) continue;

                    string effectName = effect.m_name?.ToLower() ?? "";
                    string tooltip = effect.m_tooltip?.ToLower() ?? "";

                    if (effectName.Contains("crit") || tooltip.Contains("crit"))
                    {
                        critChance += 5f;
                    }
                }
            }
        }
        catch
        {
            /* Ignore errors */
        }

        return critChance;
    }

    private static float CalculateLifesteal(Player player)
    {
        float lifesteal = 0f;

        try
        {
            if (player.m_seman?.GetStatusEffects() != null)
            {
                foreach (StatusEffect effect in player.m_seman.GetStatusEffects())
                {
                    if (effect == null) continue;

                    string effectName = effect.m_name?.ToLower() ?? "";
                    string tooltip = effect.m_tooltip?.ToLower() ?? "";

                    if (effectName.Contains("lifesteal") || tooltip.Contains("lifesteal"))
                    {
                        lifesteal += 5f;
                    }
                }
            }
        }
        catch
        {
            /* Ignore errors */
        }

        return lifesteal;
    }

    private static float CalculateJumpModifier(Player player) => PlayerStatsCalculator.CalculateJumpModifier(player);

    private static HitData.DamageModifier GetResistance(Player player, HitData.DamageType damageType)
    {
        HitData.DamageModifiers mods = player.GetBodyArmor() > 0 ? player.GetDamageModifiers() : new HitData.DamageModifiers();

        return damageType switch
        {
            HitData.DamageType.Blunt => mods.m_blunt,
            HitData.DamageType.Slash => mods.m_slash,
            HitData.DamageType.Pierce => mods.m_pierce,
            HitData.DamageType.Fire => mods.m_fire,
            HitData.DamageType.Frost => mods.m_frost,
            HitData.DamageType.Lightning => mods.m_lightning,
            HitData.DamageType.Poison => mods.m_poison,
            HitData.DamageType.Spirit => mods.m_spirit,
            _ => HitData.DamageModifier.Normal
        };
    }

    private static string FormatResistance(HitData.DamageModifier modifier)
    {
        return modifier switch
        {
            HitData.DamageModifier.Immune => Localization.instance.Localize("$azu_epi_stat_resist_immune"),
            HitData.DamageModifier.VeryResistant => Localization.instance.Localize("$azu_epi_stat_resist_very_resistant"),
            HitData.DamageModifier.Resistant => Localization.instance.Localize("$azu_epi_stat_resist_resistant"),
            HitData.DamageModifier.SlightlyResistant => Localization.instance.Localize("$azu_epi_stat_resist_slightly_resistant"),
            HitData.DamageModifier.Normal => Localization.instance.Localize("$azu_epi_stat_resist_normal"),
            HitData.DamageModifier.SlightlyWeak => Localization.instance.Localize("$azu_epi_stat_resist_slightly_weak"),
            HitData.DamageModifier.Weak => Localization.instance.Localize("$azu_epi_stat_resist_weak"),
            HitData.DamageModifier.VeryWeak => Localization.instance.Localize("$azu_epi_stat_resist_very_weak"),
            _ => Localization.instance.Localize("$azu_epi_stat_resist_unknown")
        };
    }

    private static float CalculateHeatResistance(Player player) => CalculateModifiers(player, _ => 0f, shared => shared.m_heatResistanceModifier);

    private static float CalculateEquipmentMovement(Player player) => CalculateModifiers(player, _ => 0f, shared => shared.m_movementModifier);

    private static string GetTopSkills(Player player)
    {
        try
        {
            if (player.m_skills == null)
                return Localization.instance.Localize("$azu_epi_stat_none");

            System.Text.StringBuilder sb = new();
            List<Skills.Skill> skillList = player.m_skills.GetSkillList();

            List<Skills.Skill> topSkills = skillList
                .Where(s => s is { m_level: > 0 })
                .OrderByDescending(s => s.m_level)
                .Take(5)
                .ToList();

            if (topSkills.Count == 0)
                return Localization.instance.Localize("$azu_epi_stat_none");

            foreach (Skills.Skill skill in topSkills)
            {
                string skillName = skill.m_info.m_skill.ToString();
                sb.Append($"• {skillName}: {skill.m_level:F0}");

                if (skill != topSkills.Last())
                    sb.AppendLine();
            }

            return sb.ToString();
        }
        catch (Exception ex)
        {
            AzuExtendedPlayerInventoryLogger.LogWarning($"Error getting skills: {ex.Message}");
            return Localization.instance.Localize("$azu_epi_stat_error");
        }
    }

    private static string GetAddedSkillPoints(Player player)
    {
        try
        {
            Skills? playerSkills = player.GetSkills();
            System.Text.StringBuilder sb = new();
            List<Skills.Skill> skillList = playerSkills.GetSkillList();

            if (skillList.Count == 0)
                return Localization.instance.Localize("$azu_epi_stat_none");

            foreach (Skills.Skill skill in skillList)
            {
                float skillLevel = playerSkills.GetSkillLevel(skill.m_info.m_skill);
                bool flag = Math.Abs((double)skillLevel - Mathf.Floor(skill.m_level)) > 0.01f;
                string skillName = skill.m_info.m_skill.ToString();
                if (!flag) continue;
                float num2 = skillLevel - skill.m_level;
                sb.Append($"• {skillName}: {num2:+0}");
            }

            return sb.Length > 0 ? sb.ToString() : Localization.instance.Localize("$azu_epi_stat_none");
        }
        catch (Exception ex)
        {
            AzuExtendedPlayerInventoryLogger.LogWarning($"Error getting skills: {ex.Message}");
            return Localization.instance.Localize("$azu_epi_stat_error");
        }
    }

    private static string GetActiveEffects(Player player)
    {
        try
        {
            if (player.m_seman?.GetStatusEffects() == null)
                return Localization.instance.Localize("$azu_epi_stat_none");

            List<StatusEffect> effects = player.m_seman.GetStatusEffects();
            if (effects.Count == 0)
                return Localization.instance.Localize("$azu_epi_stat_none");

            System.Text.StringBuilder sb = new();
            int count = 0;
            int skipped = 0;

            foreach (StatusEffect effect in effects)
            {
                if (effect == null || string.IsNullOrEmpty(effect.m_name))
                    continue;

                if (effect.m_name.Contains("Food") || effect.m_name.Contains("food"))
                    continue;

                if (effect.m_name.Contains("set") || effect.m_name.Contains("Set"))
                {
                    skipped++;
                    continue;
                }

                string timeStr = "";
                if (effect.m_ttl > 0)
                {
                    float remaining = effect.m_time;
                    if (remaining > 60)
                        timeStr = $" {remaining / 60:F0}m";
                    else if (remaining > 0)
                        timeStr = $" {remaining:F0}s";
                }

                sb.Append($"• {effect.m_name}{timeStr}");
                count++;

                if (count < effects.Count - skipped)
                    sb.AppendLine();

                if (count < 5) continue;
                {
                    int remaining = effects.Count - count - skipped;
                    if (remaining > 0)
                        sb.Append($"\n+{remaining} more...");
                    break;
                }
            }

            return count > 0 ? sb.ToString() : Localization.instance.Localize("$azu_epi_stat_none");
        }
        catch (Exception ex)
        {
            AzuExtendedPlayerInventoryLogger.LogWarning($"Error getting active effects: {ex.Message}");
            return Localization.instance.Localize("$azu_epi_stat_error");
        }
    }

    private static string GetActiveSetBonuses(Player player)
    {
        Dictionary<string, int> setCount = new();
        Dictionary<string, int> setSize = new();

        try
        {
            List<ItemDrop.ItemData> equipped = player.GetInventory()?.GetEquippedItems() ?? [];

            foreach (ItemDrop.ItemData item in equipped)
            {
                if (item == null || string.IsNullOrEmpty(item.m_shared.m_setName)) continue;

                string setName = item.m_shared.m_setName;
                if (!setCount.ContainsKey(setName))
                {
                    setCount[setName] = 0;
                    setSize[setName] = item.m_shared.m_setSize;
                }

                setCount[setName]++;
            }

            if (setCount.Count == 0)
                return Localization.instance.Localize("$azu_epi_stat_none");

            System.Text.StringBuilder sb = new();
            int displayCount = 0;
            foreach (KeyValuePair<string, int> kvp in setCount.OrderByDescending(x => x.Value))
            {
                string setName = kvp.Key;
                int count = kvp.Value;
                int required = setSize.TryGetValue(setName, out int size) ? size : 1;

                bool active = count >= required;
                string activeMarker = active ? "✓" : "✕";
                string color = active ? "#00FF00" : "#FF6666";

                sb.Append($"<color={color}>{activeMarker}</color> {setName} ({count}/{required})");

                displayCount++;

                if (displayCount < setCount.Count)
                    sb.AppendLine();

                if (displayCount < 5) continue;
                int remaining = setCount.Count - displayCount;
                if (remaining > 0)
                    sb.Append($"\n+{remaining} more...");
                break;
            }

            return sb.ToString();
        }
        catch (Exception ex)
        {
            AzuExtendedPlayerInventoryLogger.LogWarning($"Error getting set bonuses: {ex.Message}");
            return Localization.instance.Localize("$azu_epi_stat_error");
        }
    }

    internal static void HandleScrollInput()
    {
        if (!IsVisible())
            return;

        ScrollRect? scroll = GetScrollRect();
        if (scroll == null || scroll.content == null)
            return;

        if (!ZInput.IsGamepadActive())
            return;

        float scrollInput = 0f;

        if (ZInput.instance != null)
        {
            try
            {
                scrollInput = ZInput.GetJoyRightStickY();
            }
            catch
            {
            }
        }

        if (Mathf.Approximately(scrollInput, 0f))
        {
            try
            {
                scrollInput = Input.GetAxis("Joy2 Axis 5");
                if (Mathf.Approximately(scrollInput, 0f))
                    scrollInput = Input.GetAxis("Joy2 Axis 10");
            }
            catch
            {
            }
        }

        if (Mathf.Abs(scrollInput) > 0.1f)
        {
            float scrollDelta = -scrollInput * 1f * Time.deltaTime;
            float newValue = Mathf.Clamp01(scroll.verticalNormalizedPosition + scrollDelta);
            scroll.verticalNormalizedPosition = newValue;
        }
    }

    private static float CalculateElementalDamageBonus(Player player, HitData.DamageType damageType)
    {
        float bonus = 0f;

        try
        {
            if (player.m_seman?.GetStatusEffects() != null)
            {
                foreach (StatusEffect effect in player.m_seman.GetStatusEffects())
                {
                    if (effect is not SE_Stats seStats) continue;

                    float modifier = damageType switch
                    {
                        HitData.DamageType.Blunt => seStats.m_percentigeDamageModifiers.m_blunt,
                        HitData.DamageType.Slash => seStats.m_percentigeDamageModifiers.m_slash,
                        HitData.DamageType.Pierce => seStats.m_percentigeDamageModifiers.m_pierce,
                        HitData.DamageType.Fire => seStats.m_percentigeDamageModifiers.m_fire,
                        HitData.DamageType.Frost => seStats.m_percentigeDamageModifiers.m_frost,
                        HitData.DamageType.Lightning => seStats.m_percentigeDamageModifiers.m_lightning,
                        HitData.DamageType.Poison => seStats.m_percentigeDamageModifiers.m_poison,
                        HitData.DamageType.Spirit => seStats.m_percentigeDamageModifiers.m_spirit,
                        _ => 0f
                    };

                    if (modifier != 0f)
                        bonus += modifier * 100f;
                }
            }
        }
        catch
        {
            /* Ignore errors */
        }

        return bonus;
    }

    private static float CalculateSkillRaiseSpeed(Player player) => CalculateMultiplierModifier(player, seStats => seStats.m_raiseSkillModifier);

    private static float CalculateSpeedModifier(Player player) => CalculateMultiplierModifier(player, seStats => seStats.m_speedModifier);

    private static string GetActiveFoodBuffs(Player player)
    {
        try
        {
            if (player.m_foods == null || player.m_foods.Count == 0)
                return Localization.instance.Localize("$azu_epi_stat_none");

            System.Text.StringBuilder sb = new();
            int count = 0;

            foreach (Player.Food food in player.m_foods)
            {
                if (food?.m_item == null) continue;

                string foodName = food.m_item.m_shared.m_name;
                float timeLeft = food.m_time;
                string timeStr = "";

                if (timeLeft > 0)
                {
                    if (timeLeft >= 60)
                        timeStr = $" ({timeLeft / 60:F0}m)";
                    else
                        timeStr = $" ({timeLeft:F0}s)";
                }

                float hp = food.m_item.m_shared.m_food;
                float stam = food.m_item.m_shared.m_foodStamina;
                float eitr = food.m_item.m_shared.m_foodEitr;

                string stats = "";
                if (hp > 0) stats += $" +{hp:F0}HP";
                if (stam > 0) stats += $" +{stam:F0}Stam";
                if (eitr > 0) stats += $" +{eitr:F0}Eitr";

                sb.Append($"• {foodName}{stats}{timeStr}");
                count++;

                if (count < player.m_foods.Count)
                    sb.AppendLine();
            }

            return count > 0 ? sb.ToString() : Localization.instance.Localize("$azu_epi_stat_none");
        }
        catch (Exception ex)
        {
            AzuExtendedPlayerInventoryLogger.LogWarning($"Error getting food buffs: {ex.Message}");
            return Localization.instance.Localize("$azu_epi_stat_error");
        }
    }

    private class StatElement
    {
        public string Name { get; set; } = string.Empty;
        public PlayerStatType StatType { get; set; }
        public TextMeshProUGUI ValueText { get; set; } = null!;
        public bool IsLiveStat { get; set; }
    }

    #region Remote Player Stats

    private static void CreatePlayerDropdown()
    {
        if (!_content || _playerDropdown != null) return;

        GameObject containerGo = new("PlayerDropdownContainer", typeof(RectTransform), typeof(HorizontalLayoutGroup));
        containerGo.transform.SetParent(_content, false);
        containerGo.transform.SetAsFirstSibling();

        _dropdownContainer = (RectTransform)containerGo.transform;
        _dropdownContainer.sizeDelta = new Vector2(0, 36);

        HorizontalLayoutGroup hlg = containerGo.GetComponent<HorizontalLayoutGroup>();
        hlg.childAlignment = TextAnchor.MiddleLeft;
        hlg.childControlWidth = true;
        hlg.childControlHeight = true;
        hlg.childForceExpandWidth = true;
        hlg.childForceExpandHeight = false;
        hlg.spacing = 8;
        hlg.padding = new RectOffset(0, 0, 4, 4);

        LayoutElement containerLE = containerGo.AddComponent<LayoutElement>();
        containerLE.minHeight = 36;
        containerLE.preferredHeight = 36;

        GameObject labelGo = new("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        labelGo.transform.SetParent(containerGo.transform, false);

        TextMeshProUGUI label = labelGo.GetComponent<TextMeshProUGUI>();
        label.text = Localization.instance.Localize("$azu_epi_stat_player_label");
        label.fontSize = 16;
        label.fontStyle = FontStyles.Bold;
        label.color = Color.white;
        label.alignment = TextAlignmentOptions.MidlineLeft;
        if (_fontAsset != null) label.font = _fontAsset;

        LayoutElement labelLE = labelGo.AddComponent<LayoutElement>();
        labelLE.minWidth = 60;
        labelLE.preferredWidth = 60;
        labelLE.flexibleWidth = 0;

        TMP_Dropdown? templateDropdown = null; //Resources.FindObjectsOfTypeAll<TMP_Dropdown>().FirstOrDefault();

        GameObject dropdownGo;
        if (templateDropdown != null)
        {
            dropdownGo = Object.Instantiate(templateDropdown.gameObject, containerGo.transform);
            dropdownGo.name = "PlayerDropdown";
            dropdownGo.SetActive(true);
            _playerDropdown = dropdownGo.GetComponent<TMP_Dropdown>();

            Transform? template = dropdownGo.transform.Find("Template");
            if (template != null)
            {
                _playerDropdown.template = template.GetComponent<RectTransform>();
                template.gameObject.SetActive(false);

                Transform? itemLabel = template.Find("Viewport/Content/Item/Item Label");
                if (itemLabel != null)
                {
                    _playerDropdown.itemText = itemLabel.GetComponent<TextMeshProUGUI>();
                }
            }

            Transform? captionLabel = dropdownGo.transform.Find("Label");
            if (captionLabel != null)
            {
                _playerDropdown.captionText = captionLabel.GetComponent<TextMeshProUGUI>();
            }
        }
        else
        {
            dropdownGo = new GameObject("PlayerDropdown", typeof(RectTransform), typeof(TMP_Dropdown), typeof(Image));
            dropdownGo.transform.SetParent(containerGo.transform, false);
            _playerDropdown = dropdownGo.GetComponent<TMP_Dropdown>();

            Image bgImage = dropdownGo.GetComponent<Image>();
            bgImage.color = new Color(0.1f, 0.1f, 0.1f, 0.8f);

            GameObject captionGo = new("Label", typeof(RectTransform));
            captionGo.transform.SetParent(dropdownGo.transform, false);
            captionGo.SafeSetActive(false);
            RectTransform captionRect = captionGo.GetComponent<RectTransform>();
            captionRect.anchorMin = Vector2.zero;
            captionRect.anchorMax = Vector2.one;
            captionRect.offsetMin = new Vector2(10, 2);
            captionRect.offsetMax = new Vector2(-25, -2);
            TextMeshProUGUI captionText = captionGo.AddComponent<TextMeshProUGUI>();
            if (_fontAsset != null)
            {
                captionText.font = _fontAsset;
                captionText.fontSharedMaterial = _fontAsset.material;
            }

            captionGo.SafeSetActive(true);
            captionText.alignment = TextAlignmentOptions.MidlineLeft;
            captionText.fontSize = 16;
            captionText.color = new Color(1f, 0.84f, 0f, 1f);
            captionText.fontStyle = FontStyles.Bold;
            captionText.raycastTarget = false;
            _playerDropdown.captionText = captionText;

            GameObject arrowGo = new("Arrow", typeof(RectTransform));
            arrowGo.transform.SetParent(dropdownGo.transform, false);
            arrowGo.SafeSetActive(false);
            RectTransform arrowRect = arrowGo.GetComponent<RectTransform>();
            arrowRect.anchorMin = new Vector2(1, 0.5f);
            arrowRect.anchorMax = new Vector2(1, 0.5f);
            arrowRect.pivot = new Vector2(1, 0.5f);
            arrowRect.anchoredPosition = new Vector2(-6, 0);
            arrowRect.sizeDelta = new Vector2(20, 20);
            TextMeshProUGUI arrowText = arrowGo.AddComponent<TextMeshProUGUI>();
            if (_fontAsset != null)
            {
                arrowText.font = _fontAsset;
                arrowText.fontSharedMaterial = _fontAsset.material;
            }

            arrowGo.SafeSetActive(true);
            arrowText.text = "\u25BC";
            arrowText.fontSize = 12;
            arrowText.alignment = TextAlignmentOptions.Center;
            arrowText.color = new Color(1f, 0.84f, 0f, 1f);
            arrowText.raycastTarget = false;

            GameObject templateGo = new("Template", typeof(RectTransform), typeof(Image), typeof(ScrollRect));
            templateGo.transform.SetParent(dropdownGo.transform, false);
            RectTransform templateRect = templateGo.GetComponent<RectTransform>();
            templateRect.anchorMin = new Vector2(0, 0);
            templateRect.anchorMax = new Vector2(1, 0);
            templateRect.pivot = new Vector2(0.5f, 1);
            templateRect.anchoredPosition = Vector2.zero;
            templateRect.sizeDelta = new Vector2(0, 150);
            Image templateBg = templateGo.GetComponent<Image>();
            templateBg.color = new Color(0.05f, 0.05f, 0.05f, 0.95f);

            GameObject viewportGo = new("Viewport", typeof(RectTransform), typeof(Mask), typeof(Image));
            viewportGo.transform.SetParent(templateGo.transform, false);
            RectTransform viewportRect = viewportGo.GetComponent<RectTransform>();
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.offsetMin = Vector2.zero;
            viewportRect.offsetMax = Vector2.zero;
            viewportGo.GetComponent<Image>().color = Color.white;
            viewportGo.GetComponent<Mask>().showMaskGraphic = false;

            GameObject contentGo = new("Content", typeof(RectTransform));
            contentGo.transform.SetParent(viewportGo.transform, false);
            RectTransform contentRect = contentGo.GetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0, 1);
            contentRect.anchorMax = new Vector2(1, 1);
            contentRect.pivot = new Vector2(0.5f, 1);
            contentRect.anchoredPosition = Vector2.zero;
            contentRect.sizeDelta = new Vector2(0, 28);

            ScrollRect scrollRect = templateGo.GetComponent<ScrollRect>();
            scrollRect.content = contentRect;
            scrollRect.viewport = viewportRect;
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;

            GameObject itemGo = new("Item", typeof(RectTransform), typeof(UnityEngine.UI.Toggle), typeof(Image));
            itemGo.transform.SetParent(contentGo.transform, false);
            RectTransform itemRect = itemGo.GetComponent<RectTransform>();
            itemRect.anchorMin = new Vector2(0, 0.5f);
            itemRect.anchorMax = new Vector2(1, 0.5f);
            itemRect.sizeDelta = new Vector2(0, 30);
            UnityEngine.UI.Toggle itemToggle = itemGo.GetComponent<UnityEngine.UI.Toggle>();
            Image itemBg = itemGo.GetComponent<Image>();
            itemBg.color = new Color(0.08f, 0.08f, 0.08f, 0.9f);

            GameObject itemBgGo = new("Item Background", typeof(RectTransform), typeof(Image));
            itemBgGo.transform.SetParent(itemGo.transform, false);
            RectTransform itemBgRect = itemBgGo.GetComponent<RectTransform>();
            itemBgRect.anchorMin = Vector2.zero;
            itemBgRect.anchorMax = Vector2.one;
            itemBgRect.offsetMin = Vector2.zero;
            itemBgRect.offsetMax = Vector2.zero;
            Image highlightImage = itemBgGo.GetComponent<Image>();
            highlightImage.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);
            itemToggle.targetGraphic = highlightImage;

            GameObject checkmarkGo = new("Item Checkmark", typeof(RectTransform), typeof(Image));
            checkmarkGo.transform.SetParent(itemGo.transform, false);
            RectTransform checkRect = checkmarkGo.GetComponent<RectTransform>();
            checkRect.anchorMin = new Vector2(0, 0.5f);
            checkRect.anchorMax = new Vector2(0, 0.5f);
            checkRect.pivot = new Vector2(0, 0.5f);
            checkRect.anchoredPosition = new Vector2(8, 0);
            checkRect.sizeDelta = new Vector2(14, 14);
            Image checkImage = checkmarkGo.GetComponent<Image>();
            checkImage.color = new Color(1f, 0.84f, 0f, 1f);
            itemToggle.graphic = checkImage;

            GameObject itemLabelGo = new("Item Label", typeof(RectTransform));
            itemLabelGo.transform.SetParent(itemGo.transform, false);
            itemLabelGo.SafeSetActive(false);
            RectTransform itemLabelRect = itemLabelGo.GetComponent<RectTransform>();
            itemLabelRect.anchorMin = Vector2.zero;
            itemLabelRect.anchorMax = Vector2.one;
            itemLabelRect.offsetMin = new Vector2(28, 2);
            itemLabelRect.offsetMax = new Vector2(-10, -2);
            TextMeshProUGUI itemText = itemLabelGo.AddComponent<TextMeshProUGUI>();
            if (_fontAsset != null)
            {
                itemText.font = _fontAsset;
                itemText.fontSharedMaterial = _fontAsset.material;
            }

            itemLabelGo.SafeSetActive(true);
            itemText.alignment = TextAlignmentOptions.MidlineLeft;
            itemText.fontSize = 15;
            itemText.color = new Color(1f, 0.84f, 0f, 1f);
            itemText.raycastTarget = false;
            _playerDropdown.itemText = itemText;

            _playerDropdown.template = templateRect;
            templateGo.SetActive(false);
        }

        LayoutElement dropdownLE = dropdownGo.GetOrAddComponent<LayoutElement>();
        dropdownLE.flexibleWidth = 1;
        dropdownLE.minHeight = 28;
        dropdownLE.preferredHeight = 28;

        _playerDropdown.interactable = true;

        // Disable navigation to prevent gamepad issues
        Navigation nav = _playerDropdown.navigation;
        nav.mode = Navigation.Mode.None;
        _playerDropdown.navigation = nav;

        _playerDropdown.ClearOptions();
        _playerDropdown.onValueChanged.RemoveAllListeners();

        _playerDropdown.onValueChanged.AddListener(new UnityEngine.Events.UnityAction<int>(OnPlayerDropdownValueChanged));

        AzuExtendedPlayerInventoryLogger.LogDebug($"Player dropdown created, template={_playerDropdown.template != null}, captionText={_playerDropdown.captionText != null}");

        RefreshPlayerDropdownOptions();

        CreateSpacer("Spacer_PlayerDropdown");
    }

    private static void RefreshPlayerDropdownOptions()
    {
        if (_playerDropdown == null) return;

        _playerList.Clear();
        if (ZNet.instance != null)
            _playerList.AddRange(ZNet.instance.GetPlayerList());

        List<TMP_Dropdown.OptionData> options = new()
        {
            new TMP_Dropdown.OptionData(Localization.instance.Localize("$azu_epi_stat_self"))
        };

        long myUid = ZNet.instance != null ? ZNet.GetUID() : 0;
        foreach (ZNet.PlayerInfo playerInfo in _playerList)
        {
            if (playerInfo.m_characterID.UserID == myUid) continue;
            string playerName = CensorShittyWords.FilterUGC(playerInfo.m_name, UGCType.CharacterName, playerInfo.m_userInfo.m_id);
            options.Add(new TMP_Dropdown.OptionData(playerName));
        }

        foreach ((long id, string name) in _fakePlayersForTesting)
        {
            options.Add(new TMP_Dropdown.OptionData($"{name} (Test)"));
        }

        int previousValue = _playerDropdown.value;
        _playerDropdown.ClearOptions();
        _playerDropdown.AddOptions(options);

        bool isMultiplayer = true;
        bool hasFakePlayers = _fakePlayersForTesting.Count > 0;
        bool showDropdown = isMultiplayer || hasFakePlayers;

        if (_dropdownContainer != null)
            _dropdownContainer.gameObject.SetActive(showDropdown);

        if (previousValue < options.Count)
            _playerDropdown.SetValueWithoutNotify(previousValue);
        else
        {
            _playerDropdown.SetValueWithoutNotify(0);
            _isViewingRemotePlayer = false;
            _currentRemoteStats = null;
        }
    }

    public static void OnPlayerListChanged()
    {
        if (_playerDropdown == null) return;

        AzuExtendedPlayerInventoryLogger.LogDebug("Player list changed, refreshing dropdown");
        RefreshPlayerDropdownOptions();
    }

    private static void OnPlayerDropdownValueChanged(int index)
    {
        AzuExtendedPlayerInventoryLogger.LogDebug($"OnPlayerDropdownValueChanged fired with index: {index}");
        OnPlayerDropdownChanged(index);
    }

    private static void OnPlayerDropdownChanged(int index)
    {
        AzuExtendedPlayerInventoryLogger.LogDebug($"OnPlayerDropdownChanged processing index: {index}");
        if (index == 0)
        {
            ResetToLocalPlayer();
            if (Player.m_localPlayer != null)
                UpdateStats(Player.m_localPlayer);
            return;
        }

        long fakePlayerId = GetFakePlayerIdAtDropdownIndex(index);
        if (fakePlayerId != 0)
        {
            AzuExtendedPlayerInventoryLogger.LogDebug($"Fake player selected at index {index}, ID: {fakePlayerId}");
            SimulateFakePlayerStats(fakePlayerId);
            return;
        }

        long myUid = ZNet.GetUID();
        int remoteIndex = 0;
        long targetPlayerId = 0;

        foreach (ZNet.PlayerInfo playerInfo in _playerList)
        {
            if (playerInfo.m_characterID.UserID == myUid) continue;
            remoteIndex++;
            if (remoteIndex == index)
            {
                targetPlayerId = playerInfo.m_characterID.UserID;
                break;
            }
        }

        if (targetPlayerId == 0) return;

        _isViewingRemotePlayer = true;
        _viewingPlayerId = targetPlayerId;

        if (_remoteStatsCache.TryGetValue(targetPlayerId, out RemotePlayerStats? cached))
        {
            long age = DateTimeOffset.UtcNow.ToUnixTimeSeconds() - cached.Timestamp;
            if (age < 30)
            {
                _currentRemoteStats = cached;
                UpdateStatsFromRemoteData(cached);
                return;
            }
        }

        _isLoadingRemoteStats = true;
        ShowLoadingState();
        RemoteStatsRPC.RequestStats(targetPlayerId);
    }

    public static void OnRemoteStatsReceived(RemotePlayerStats? stats)
    {
        _isLoadingRemoteStats = false;

        if (stats == null)
        {
            ShowErrorState(Localization.instance.Localize("$azu_epi_stat_unavailable"));
            return;
        }

        _remoteStatsCache[stats.PlayerId] = stats;

        if (!_isViewingRemotePlayer || _viewingPlayerId != stats.PlayerId) return;
        _currentRemoteStats = stats;
        UpdateStatsFromRemoteData(stats);
    }

    public static void ForceDisplayRemoteStats(RemotePlayerStats stats)
    {
        _isViewingRemotePlayer = true;
        _viewingPlayerId = stats.PlayerId;
        _currentRemoteStats = stats;
        _remoteStatsCache[stats.PlayerId] = stats;

        if (_dropdownContainer != null)
            _dropdownContainer.gameObject.SetActive(true);

        UpdateStatsFromRemoteData(stats);
    }

    public static void EnableTestMode(long targetPlayerId = 0)
    {
        if (_dropdownContainer != null)
            _dropdownContainer.gameObject.SetActive(true);

        _isViewingRemotePlayer = true;
        _viewingPlayerId = targetPlayerId > 0 ? targetPlayerId : ZNet.GetUID();
        _isLoadingRemoteStats = true;

        if (_visible)
            ShowLoadingState();

        AzuExtendedPlayerInventoryLogger.LogDebug($"Test mode enabled - waiting for RPC response from player {_viewingPlayerId}");
    }

    private static readonly List<(long id, string name)> _fakePlayersForTesting = [];

    public static void AddFakePlayerToDropdown(string playerName)
    {
        long fakeId = -1000 - _fakePlayersForTesting.Count;
        _fakePlayersForTesting.Add((fakeId, playerName));

        RefreshPlayerDropdownOptions();

        AzuExtendedPlayerInventoryLogger.LogInfo($"Added fake player '{playerName}' (ID: {fakeId}) to dropdown");
    }

    public static void ClearFakePlayers()
    {
        _fakePlayersForTesting.Clear();
        RefreshPlayerDropdownOptions();
        AzuExtendedPlayerInventoryLogger.LogInfo("Cleared all fake players from dropdown");
    }

    private static long GetFakePlayerIdAtDropdownIndex(int index)
    {
        if (_fakePlayersForTesting.Count == 0) return 0;

        long myUid = ZNet.instance != null ? ZNet.GetUID() : 0;
        int realPlayerCount = _playerList.Count(p => p.m_characterID.UserID != myUid);
        int fakeStartIndex = 1 + realPlayerCount;

        if (index >= fakeStartIndex && index < fakeStartIndex + _fakePlayersForTesting.Count)
        {
            return _fakePlayersForTesting[index - fakeStartIndex].id;
        }

        return 0;
    }

    private static void SimulateFakePlayerStats(long fakePlayerId)
    {
        string? fakeName = _fakePlayersForTesting.FirstOrDefault(f => f.id == fakePlayerId).name;
        if (fakeName == null) return;

        _isViewingRemotePlayer = true;
        _viewingPlayerId = fakePlayerId;
        _isLoadingRemoteStats = true;

        if (_visible)
            ShowLoadingState();

        AzuExtendedPlayerInventoryLogger.LogDebug($"Simulating RPC for fake player '{fakeName}'...");

        RemotePlayerStats fakeStats = FakePlayerStats.Generate(fakePlayerId, fakeName);
        OnRemoteStatsReceived(fakeStats);
        AzuExtendedPlayerInventoryLogger.LogInfo($"Fake stats delivered for '{fakeName}'");
    }

    private static void ShowLoadingState()
    {
        foreach (StatElement element in _statElements)
        {
            element.ValueText.text = "...";
        }
    }

    private static void ShowErrorState(string message)
    {
        foreach (StatElement element in _statElements)
        {
            element.ValueText.text = "-";
        }
    }

    private static void UpdateStatsFromRemoteData(RemotePlayerStats stats)
    {
        if (_statElements.Count == 0) return;

        foreach (StatElement element in _statElements)
        {
            if (element.IsLiveStat)
            {
                switch (element.Name)
                {
                    case "Health":
                        element.ValueText.text = $"{stats.CurrentHealth:F0} / {stats.MaxHealth:F0}";
                        break;
                    case "Stamina":
                        element.ValueText.text = $"{stats.CurrentStamina:F0} / {stats.MaxStamina:F0}";
                        break;
                    case "Eitr":
                        element.ValueText.text = $"{stats.CurrentEitr:F0} / {stats.MaxEitr:F0}";
                        break;
                    case "Total Armor":
                    case "Armor":
                        element.ValueText.text = $"{stats.BodyArmor:F0}";
                        break;
                    case "Current Weight":
                        element.ValueText.text = $"{stats.CurrentCarryWeight:F0}";
                        break;
                    case "Max Weight":
                        element.ValueText.text = $"{stats.MaxCarryWeight:F0}";
                        break;
                    case "Weight Percentage":
                        float weightPercentage = stats.MaxCarryWeight > 0 ? (stats.CurrentCarryWeight / stats.MaxCarryWeight) * 100f : 0f;
                        element.ValueText.text = $"{weightPercentage:F0}%";
                        break;
                    case "Health Regen":
                        element.ValueText.text = $"{stats.HealthRegen:F1}/tick";
                        break;
                    case "Stamina Regen":
                        element.ValueText.text = $"{stats.StaminaRegen:F1}/s";
                        break;
                    case "Eitr Regen":
                        element.ValueText.text = $"{stats.EitrRegen:F1}/s";
                        break;
                    case "Attack Speed":
                        element.ValueText.text = $"{stats.AttackSpeedModifier * 100f:F0}%";
                        break;
                    case "Damage Modifier":
                        float damageMod = (stats.DamageModifier - 1f) * 100f;
                        element.ValueText.text = damageMod != 0 ? $"{damageMod:+0;-0}%" : "0%";
                        break;
                    case "Movement Speed":
                        float moveMod = (stats.MovementSpeedModifier - 1f) * 100f;
                        element.ValueText.text = moveMod != 0 ? $"{moveMod:+0;-0}%" : "0%";
                        break;
                    case "FoodBuffs":
                        element.ValueText.text = FormatRemoteFoods(stats.ActiveFoods);
                        break;
                    case "ActiveEffects":
                        element.ValueText.text = FormatRemoteEffects(stats.ActiveEffectNames);
                        break;
                    default:
                        element.ValueText.text = "N/A";
                        break;
                }

                continue;
            }

            if (stats.PlayerStats.TryGetValue(element.StatType, out float statValue))
            {
                switch (element.StatType)
                {
                    case PlayerStatType.DistanceTraveled:
                    case PlayerStatType.DistanceWalk:
                    case PlayerStatType.DistanceRun:
                    case PlayerStatType.DistanceSail:
                    case PlayerStatType.DistanceAir:
                        element.ValueText.text = statValue >= 1000f ? $"{(statValue / 1000f):F1}km" : $"{statValue:F0}m";
                        break;

                    case PlayerStatType.TimeInBase:
                    case PlayerStatType.TimeOutOfBase:
                    case PlayerStatType.Sleep:
                        if (statValue >= 86400)
                            element.ValueText.text = $"{(statValue / 86400f):F1}d";
                        else if (statValue >= 3600)
                            element.ValueText.text = $"{(statValue / 3600f):F1}h";
                        else if (statValue >= 60)
                            element.ValueText.text = $"{(statValue / 60f):F0}m";
                        else
                            element.ValueText.text = $"{statValue:F0}s";
                        break;

                    default:
                        if (statValue >= 1000000)
                            element.ValueText.text = $"{statValue / 1000000f:F1}M";
                        else if (statValue >= 10000)
                            element.ValueText.text = $"{statValue / 1000f:F1}k";
                        else
                            element.ValueText.text = statValue.ToString("N0");
                        break;
                }
            }
            else
            {
                element.ValueText.text = "0";
            }
        }
    }

    private static string FormatRemoteFoods(List<FoodSnapshot> foods)
    {
        if (foods.Count == 0) return Localization.instance.Localize("$azu_epi_stat_none");

        System.Text.StringBuilder sb = new();
        for (int i = 0; i < foods.Count && i < 5; i++)
        {
            FoodSnapshot food = foods[i];
            float remaining = food.RemainingTime / 60f;
            sb.Append($"• {food.Name} ({remaining:F0}m)");
            if (i < foods.Count - 1 && i < 4)
                sb.AppendLine();
        }

        if (foods.Count > 5)
            sb.Append($"\n+{foods.Count - 5} more...");

        return sb.ToString();
    }

    private static string FormatRemoteEffects(List<string> effects)
    {
        if (effects.Count == 0) return Localization.instance.Localize("$azu_epi_stat_none");

        System.Text.StringBuilder sb = new();
        for (int i = 0; i < effects.Count && i < 5; i++)
        {
            sb.Append($"• {effects[i]}");
            if (i < effects.Count - 1 && i < 4)
                sb.AppendLine();
        }

        if (effects.Count > 5)
            sb.Append($"\n+{effects.Count - 5} more...");

        return sb.ToString();
    }

    #endregion
}