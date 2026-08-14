namespace AzuEPI.Game.Panels.Stats;

public static class FakePlayerStats
{
    private static readonly string[] FakeNames = ["Viking Bob", "Odin's Fury", "Meadmaster", "Troll Slayer", "Greydwarf Hunter"];
    private static readonly string[] FakeEffects = ["Rested", "Cold", "Wet", "Resting", "Burning", "Poisoned", "Frost resistance"];
    private static readonly string[] FakeFoods = ["Cooked meat", "Honey", "Carrot soup", "Sausages", "Lox meat pie", "Serpent stew"];
    private static readonly string[] FakeBiomes = ["Meadows", "Black Forest", "Swamp", "Mountain", "Plains", "Mistlands", "Ashlands"];
    private static readonly string[] FakeGuardianPowers = ["Eikthyr", "The Elder", "Bonemass", "Moder", "Yagluth", "The Queen"];
    private static readonly string[] FakeEquipment = ["Bronze Helmet", "Iron Armor", "Wolf Cape", "Padded Greaves", "Blackmetal Sword", "Silver Shield"];

    private static readonly System.Random _random = new();

    public static RemotePlayerStats Generate(long? playerId = null, string? playerName = null)
    {
        float maxHealth = 100 + _random.Next(0, 150);
        float maxStamina = 100 + _random.Next(0, 100);
        float maxEitr = _random.Next(0, 100) > 50 ? _random.Next(50, 150) : 0;
        float maxCarryWeight = 300 + _random.Next(0, 150);

        RemotePlayerStats stats = new()
        {
            PlayerId = playerId ?? (long)_random.Next(1000, 9999999),
            PlayerName = playerName ?? FakeNames[_random.Next(FakeNames.Length)],
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),

            CurrentHealth = maxHealth * (0.3f + (float)_random.NextDouble() * 0.7f),
            CurrentStamina = maxStamina * (0.2f + (float)_random.NextDouble() * 0.8f),
            CurrentEitr = maxEitr * (float)_random.NextDouble(),

            MaxHealth = maxHealth,
            MaxStamina = maxStamina,
            MaxEitr = maxEitr,
            BodyArmor = _random.Next(10, 100),

            CurrentCarryWeight = maxCarryWeight * (0.1f + (float)_random.NextDouble() * 0.7f),
            MaxCarryWeight = maxCarryWeight,

            GuardianPowerName = _random.Next(0, 100) > 30 ? FakeGuardianPowers[_random.Next(FakeGuardianPowers.Length)] : "",
            GuardianPowerCooldown = _random.Next(0, 100) > 50 ? _random.Next(0, 1200) : 0,

            AttackSpeedModifier = 1f + (float)(_random.NextDouble() * 0.3 - 0.1),
            DamageModifier = 1f + (float)(_random.NextDouble() * 0.5),
            MovementSpeedModifier = 1f + (float)(_random.NextDouble() * 0.2 - 0.1),
            JumpModifier = 1f + (float)(_random.NextDouble() * 0.3),

            HealthRegen = 1f + (float)(_random.NextDouble() * 5),
            StaminaRegen = 5f + (float)(_random.NextDouble() * 10),
            EitrRegen = maxEitr > 0 ? 2f + (float)(_random.NextDouble() * 5) : 0,

            IsPvPEnabled = _random.Next(0, 100) > 80,
            CurrentBiome = FakeBiomes[_random.Next(FakeBiomes.Length)],
            ComfortLevel = _random.Next(0, 18)
        };

        GenerateSkills(stats);

        GeneratePlayerStats(stats);

        int effectCount = _random.Next(0, 4);
        for (int i = 0; i < effectCount; i++)
        {
            string effect = FakeEffects[_random.Next(FakeEffects.Length)];
            if (!stats.ActiveEffectNames.Contains(effect))
                stats.ActiveEffectNames.Add(effect);
        }

        int foodCount = _random.Next(0, 4);
        for (int i = 0; i < foodCount; i++)
        {
            string foodName = FakeFoods[_random.Next(FakeFoods.Length)];
            if (stats.ActiveFoods.All(f => f.Name != foodName))
            {
                stats.ActiveFoods.Add(new FoodSnapshot
                {
                    Name = foodName,
                    RemainingTime = _random.Next(60, 1800),
                    MaxTime = 1800
                });
            }
        }

        int equipCount = _random.Next(3, 7);
        List<string> usedSlots = [];
        for (int i = 0; i < equipCount; i++)
        {
            string slot = i switch
            {
                0 => "Helmet",
                1 => "Chest",
                2 => "Legs",
                3 => "Cape",
                4 => "Weapon",
                5 => "Shield",
                _ => "Utility"
            };

            if (usedSlots.Contains(slot)) continue;
            usedSlots.Add(slot);

            float maxDurability = 100 + _random.Next(0, 200);
            stats.EquippedItems.Add(new EquippedItemSnapshot
            {
                Name = FakeEquipment[_random.Next(FakeEquipment.Length)],
                Slot = slot,
                Quality = _random.Next(1, 5),
                Durability = maxDurability * (0.3f + (float)_random.NextDouble() * 0.7f),
                MaxDurability = maxDurability
            });
        }

        return stats;
    }

    private static void GenerateSkills(RemotePlayerStats stats)
    {
        Skills.SkillType[] commonSkills =
        [
            Skills.SkillType.Swords,
            Skills.SkillType.Axes,
            Skills.SkillType.Bows,
            Skills.SkillType.Blocking,
            Skills.SkillType.Run,
            Skills.SkillType.Jump,
            Skills.SkillType.Sneak,
            Skills.SkillType.Swim,
            Skills.SkillType.WoodCutting,
            Skills.SkillType.Pickaxes
        ];

        foreach (Skills.SkillType skill in commonSkills)
        {
            if (_random.Next(0, 100) > 20)
            {
                stats.SkillLevels[skill] = _random.Next(1, 100);
            }
        }
    }

    private static void GeneratePlayerStats(RemotePlayerStats stats)
    {
        stats.PlayerStats[PlayerStatType.EnemyKills] = _random.Next(0, 5000);
        stats.PlayerStats[PlayerStatType.Deaths] = _random.Next(0, 100);
        stats.PlayerStats[PlayerStatType.EnemyHits] = _random.Next(0, 20000);
        stats.PlayerStats[PlayerStatType.HitsTakenEnemies] = _random.Next(0, 10000);
        stats.PlayerStats[PlayerStatType.BossKills] = _random.Next(0, 20);
        stats.PlayerStats[PlayerStatType.PlayerKills] = _random.Next(0, 50);
        stats.PlayerStats[PlayerStatType.PlayerHits] = _random.Next(0, 200);
        stats.PlayerStats[PlayerStatType.ArrowsShot] = _random.Next(0, 5000);

        stats.PlayerStats[PlayerStatType.DistanceTraveled] = _random.Next(10000, 500000);
        stats.PlayerStats[PlayerStatType.DistanceWalk] = _random.Next(5000, 200000);
        stats.PlayerStats[PlayerStatType.DistanceRun] = _random.Next(5000, 200000);
        stats.PlayerStats[PlayerStatType.DistanceSail] = _random.Next(0, 100000);
        stats.PlayerStats[PlayerStatType.DistanceAir] = _random.Next(0, 10000);
        stats.PlayerStats[PlayerStatType.PortalsUsed] = _random.Next(0, 500);
        stats.PlayerStats[PlayerStatType.Jumps] = _random.Next(0, 10000);

        stats.PlayerStats[PlayerStatType.Builds] = _random.Next(0, 5000);
        stats.PlayerStats[PlayerStatType.Crafts] = _random.Next(0, 2000);
        stats.PlayerStats[PlayerStatType.Upgrades] = _random.Next(0, 500);
        stats.PlayerStats[PlayerStatType.CraftsOrUpgrades] = _random.Next(0, 2500);
        stats.PlayerStats[PlayerStatType.ItemsPickedUp] = _random.Next(0, 50000);
        stats.PlayerStats[PlayerStatType.TreeChops] = _random.Next(0, 10000);
        stats.PlayerStats[PlayerStatType.MineHits] = _random.Next(0, 20000);
        stats.PlayerStats[PlayerStatType.FoodEaten] = _random.Next(0, 1000);

        stats.PlayerStats[PlayerStatType.TimeInBase] = _random.Next(3600, 360000);
        stats.PlayerStats[PlayerStatType.TimeOutOfBase] = _random.Next(3600, 360000);
        stats.PlayerStats[PlayerStatType.Sleep] = _random.Next(0, 36000);

        stats.PlayerStats[PlayerStatType.WorldLoads] = _random.Next(1, 500);
        stats.PlayerStats[PlayerStatType.CreatureTamed] = _random.Next(0, 50);
        stats.PlayerStats[PlayerStatType.DoorsOpened] = _random.Next(0, 5000);
        stats.PlayerStats[PlayerStatType.BeesHarvested] = _random.Next(0, 500);
        stats.PlayerStats[PlayerStatType.Cheats] = _random.Next(0, 10);
    }

    public static void SimulateReceiveStats(string? playerName = null)
    {
        RemotePlayerStats fakeStats = Generate(playerName: playerName);
        AzuExtendedPlayerInventoryLogger.LogInfo($"Simulating stats received from fake player: {fakeStats.PlayerName}");
        StatsPanelController.ForceDisplayRemoteStats(fakeStats);
    }

    public static bool TestCompression()
    {
        try
        {
            RemotePlayerStats original = Generate();
            byte[] compressed = original.ToCompressedBytes();
            RemotePlayerStats? decompressed = RemotePlayerStats.FromCompressedBytes(compressed);

            if (decompressed == null)
            {
                AzuExtendedPlayerInventoryLogger.LogError("Compression test failed: decompression returned null");
                return false;
            }

            bool success = original.PlayerId == decompressed.PlayerId &&
                           original.PlayerName == decompressed.PlayerName &&
                           original.MaxHealth == decompressed.MaxHealth &&
                           original.CurrentHealth == decompressed.CurrentHealth &&
                           original.CurrentBiome == decompressed.CurrentBiome &&
                           original.GuardianPowerName == decompressed.GuardianPowerName &&
                           original.SkillLevels.Count == decompressed.SkillLevels.Count &&
                           original.PlayerStats.Count == decompressed.PlayerStats.Count &&
                           original.EquippedItems.Count == decompressed.EquippedItems.Count &&
                           original.ActiveFoods.Count == decompressed.ActiveFoods.Count;

            AzuExtendedPlayerInventoryLogger.LogInfo($"Compression test: {(success ? "PASSED" : "FAILED")}");
            AzuExtendedPlayerInventoryLogger.LogInfo($"  Skills: {original.SkillLevels.Count}, Stats: {original.PlayerStats.Count}");
            AzuExtendedPlayerInventoryLogger.LogInfo($"  Equipment: {original.EquippedItems.Count}, Foods: {original.ActiveFoods.Count}");
            AzuExtendedPlayerInventoryLogger.LogInfo($"  Compressed size: {compressed.Length} bytes");

            return success;
        }
        catch (Exception ex)
        {
            AzuExtendedPlayerInventoryLogger.LogError($"Compression test failed with exception: {ex.Message}");
            return false;
        }
    }
}
