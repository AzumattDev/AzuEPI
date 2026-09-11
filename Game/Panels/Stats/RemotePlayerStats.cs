using System.IO.Compression;

namespace AzuEPI.Game.Panels.Stats;

public class RemotePlayerStats
{
    public long PlayerId;
    public string PlayerName = "";
    public long Timestamp;

    public float CurrentHealth;
    public float CurrentStamina;
    public float CurrentEitr;

    public float MaxHealth;
    public float MaxStamina;
    public float MaxEitr;
    public float BodyArmor;

    public float CurrentCarryWeight;
    public float MaxCarryWeight;

    public string GuardianPowerName = "";
    public float GuardianPowerCooldown;

    public float AttackSpeedModifier;
    public float DamageModifier;
    public float MovementSpeedModifier;
    public float JumpModifier;

    public float HealthRegen;
    public float StaminaRegen;
    public float EitrRegen;

    public bool IsPvPEnabled;
    public string CurrentBiome = "";
    public int ComfortLevel;

    public Dictionary<Skills.SkillType, float> SkillLevels = new();

    public Dictionary<PlayerStatType, float> PlayerStats = new();

    public List<string> ActiveEffectNames = [];
    public List<FoodSnapshot> ActiveFoods = [];

    public List<EquippedItemSnapshot> EquippedItems = [];

    public void Serialize(ZPackage pkg)
    {
        pkg.Write(PlayerId);
        pkg.Write(PlayerName);
        pkg.Write(Timestamp);

        pkg.Write(CurrentHealth);
        pkg.Write(CurrentStamina);
        pkg.Write(CurrentEitr);

        pkg.Write(MaxHealth);
        pkg.Write(MaxStamina);
        pkg.Write(MaxEitr);
        pkg.Write(BodyArmor);

        pkg.Write(CurrentCarryWeight);
        pkg.Write(MaxCarryWeight);

        pkg.Write(GuardianPowerName);
        pkg.Write(GuardianPowerCooldown);

        pkg.Write(AttackSpeedModifier);
        pkg.Write(DamageModifier);
        pkg.Write(MovementSpeedModifier);
        pkg.Write(JumpModifier);

        pkg.Write(HealthRegen);
        pkg.Write(StaminaRegen);
        pkg.Write(EitrRegen);

        pkg.Write(IsPvPEnabled);
        pkg.Write(CurrentBiome);
        pkg.Write(ComfortLevel);

        pkg.Write(SkillLevels.Count);
        foreach (KeyValuePair<Skills.SkillType, float> kvp in SkillLevels)
        {
            pkg.Write((int)kvp.Key);
            pkg.Write(kvp.Value);
        }

        pkg.Write(PlayerStats.Count);
        foreach (KeyValuePair<PlayerStatType, float> kvp in PlayerStats)
        {
            pkg.Write((int)kvp.Key);
            pkg.Write(kvp.Value);
        }

        pkg.Write(ActiveEffectNames.Count);
        foreach (string effect in ActiveEffectNames)
        {
            pkg.Write(effect);
        }

        pkg.Write(ActiveFoods.Count);
        foreach (FoodSnapshot food in ActiveFoods)
        {
            pkg.Write(food.Name);
            pkg.Write(food.RemainingTime);
            pkg.Write(food.MaxTime);
        }

        pkg.Write(EquippedItems.Count);
        foreach (EquippedItemSnapshot item in EquippedItems)
        {
            pkg.Write(item.Name);
            pkg.Write(item.Slot);
            pkg.Write(item.Quality);
            pkg.Write(item.Durability);
            pkg.Write(item.MaxDurability);
        }
    }

    public static RemotePlayerStats Deserialize(ZPackage pkg)
    {
        RemotePlayerStats stats = new()
        {
            PlayerId = pkg.ReadLong(),
            PlayerName = pkg.ReadString(),
            Timestamp = pkg.ReadLong(),

            CurrentHealth = pkg.ReadSingle(),
            CurrentStamina = pkg.ReadSingle(),
            CurrentEitr = pkg.ReadSingle(),

            MaxHealth = pkg.ReadSingle(),
            MaxStamina = pkg.ReadSingle(),
            MaxEitr = pkg.ReadSingle(),
            BodyArmor = pkg.ReadSingle(),

            CurrentCarryWeight = pkg.ReadSingle(),
            MaxCarryWeight = pkg.ReadSingle(),

            GuardianPowerName = pkg.ReadString(),
            GuardianPowerCooldown = pkg.ReadSingle(),

            AttackSpeedModifier = pkg.ReadSingle(),
            DamageModifier = pkg.ReadSingle(),
            MovementSpeedModifier = pkg.ReadSingle(),
            JumpModifier = pkg.ReadSingle(),

            HealthRegen = pkg.ReadSingle(),
            StaminaRegen = pkg.ReadSingle(),
            EitrRegen = pkg.ReadSingle(),

            IsPvPEnabled = pkg.ReadBool(),
            CurrentBiome = pkg.ReadString(),
            ComfortLevel = pkg.ReadInt(),
		};

        int skillsCount = pkg.ReadInt();
        for (int i = 0; i < skillsCount; i++)
        {
            Skills.SkillType skillType = (Skills.SkillType)pkg.ReadInt();
            float level = pkg.ReadSingle();
            stats.SkillLevels[skillType] = level;
        }

        int statsCount = pkg.ReadInt();
        for (int i = 0; i < statsCount; i++)
        {
            PlayerStatType statType = (PlayerStatType)pkg.ReadInt();
            float value = pkg.ReadSingle();
            stats.PlayerStats[statType] = value;
        }

        int effectsCount = pkg.ReadInt();
        for (int i = 0; i < effectsCount; i++)
        {
            stats.ActiveEffectNames.Add(pkg.ReadString());
        }

        int foodsCount = pkg.ReadInt();
        for (int i = 0; i < foodsCount; i++)
        {
            stats.ActiveFoods.Add(new FoodSnapshot
            {
                Name = pkg.ReadString(),
                RemainingTime = pkg.ReadSingle(),
                MaxTime = pkg.ReadSingle(),
			});
        }

        int equippedCount = pkg.ReadInt();
        for (int i = 0; i < equippedCount; i++)
        {
            stats.EquippedItems.Add(new EquippedItemSnapshot
            {
                Name = pkg.ReadString(),
                Slot = pkg.ReadString(),
                Quality = pkg.ReadInt(),
                Durability = pkg.ReadSingle(),
                MaxDurability = pkg.ReadSingle(),
			});
        }

        return stats;
    }

    public byte[] ToCompressedBytes()
    {
        ZPackage pkg = new();
        Serialize(pkg);
        byte[] raw = pkg.GetArray();

        using MemoryStream ms = new();
        using (GZipStream gz = new(ms, System.IO.Compression.CompressionLevel.Optimal))
        {
            gz.Write(raw, 0, raw.Length);
        }
        return ms.ToArray();
    }

    public static RemotePlayerStats? FromCompressedBytes(byte[] data)
    {
        try
        {
            using MemoryStream compressedMs = new(data);
            using GZipStream gz = new(compressedMs, CompressionMode.Decompress);
            using MemoryStream decompressedMs = new();
            gz.CopyTo(decompressedMs);
            byte[] decompressed = decompressedMs.ToArray();

            ZPackage pkg = new(decompressed);
            return Deserialize(pkg);
        }
        catch (Exception ex)
        {
            AzuExtendedPlayerInventoryLogger.LogError($"Failed to decompress remote player stats: {ex.Message}");
            return null;
        }
    }

    public static RemotePlayerStats GatherFromLocalPlayer()
    {
        Player? player = Player.m_localPlayer;
        if (player == null) return new RemotePlayerStats();

        PlayerProfile? profile = global::Game.instance?.GetPlayerProfile();
        SEMan? seMan = player.GetSEMan();

        RemotePlayerStats stats = new()
        {
            PlayerId = ZNet.GetUID(),
            PlayerName = player.GetPlayerName(),
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),

            CurrentHealth = player.GetHealth(),
            CurrentStamina = player.GetStamina(),
            CurrentEitr = player.GetEitr(),

            MaxHealth = player.GetMaxHealth(),
            MaxStamina = player.GetMaxStamina(),
            MaxEitr = player.GetMaxEitr(),
            BodyArmor = player.GetBodyArmor(),

            CurrentCarryWeight = player.GetInventory()?.GetTotalWeight() ?? 0f,
            MaxCarryWeight = player.GetMaxCarryWeight(),

            GuardianPowerName = PlayerStatsCalculator.GetGuardianPowerName(player),
            GuardianPowerCooldown = player.m_guardianPowerCooldown,

            AttackSpeedModifier = PlayerStatsCalculator.CalculateAttackSpeed(player) / 100f,
            DamageModifier = 1f + PlayerStatsCalculator.CalculateDamageModifier(player) / 100f,
            MovementSpeedModifier = 1f + PlayerStatsCalculator.CalculateSpeedModifier(player) / 100f,
            JumpModifier = PlayerStatsCalculator.CalculateJumpModifier(player) / 100f,

            HealthRegen = PlayerStatsCalculator.CalculateTotalHealthRegen(player),
            StaminaRegen = PlayerStatsCalculator.CalculateTotalStaminaRegen(player),
            EitrRegen = PlayerStatsCalculator.CalculateTotalEitrRegen(player),

            IsPvPEnabled = player.IsPVPEnabled(),
            CurrentBiome = PlayerStatsCalculator.GetBiomeName(player),
            ComfortLevel = player.GetComfortLevel(),
		};

        Skills? skills = player.GetSkills();
        if (skills != null)
        {
            foreach (Skills.SkillType skillType in Enum.GetValues(typeof(Skills.SkillType)))
            {
                if (skillType == Skills.SkillType.None || skillType == Skills.SkillType.All) continue;

                float level = skills.GetSkillLevel(skillType);
                if (level > 0)
                {
                    stats.SkillLevels[skillType] = level;
                }
            }
        }

        if (profile != null)
        {
            foreach (PlayerStatType statType in Enum.GetValues(typeof(PlayerStatType)))
            {
                if (statType == PlayerStatType.Count) continue;

                try
                {
                    stats.PlayerStats[statType] = profile.GetStat(statType);
                }
                catch (KeyNotFoundException)
                {
                }
            }
        }

        List<StatusEffect> effects = seMan?.GetStatusEffects() ?? [];
        foreach (StatusEffect effect in effects)
        {
            if (string.IsNullOrEmpty(effect.m_name)) continue;

            string effectName = effect.m_name.StartsWith("$")
                ? Localization.instance.Localize(effect.m_name)
                : effect.m_name;

            if (!string.IsNullOrEmpty(effectName) && !stats.ActiveEffectNames.Contains(effectName))
            {
                stats.ActiveEffectNames.Add(effectName);
            }
        }

        foreach (Player.Food food in player.GetFoods())
        {
            if (food.m_item?.m_shared == null) continue;
            stats.ActiveFoods.Add(new FoodSnapshot
            {
                Name = Localization.instance.Localize(food.m_item.m_shared.m_name),
                RemainingTime = food.m_time,
                MaxTime = food.m_item.m_shared.m_foodBurnTime,
			});
        }

        Inventory? inventory = player.GetInventory();
        if (inventory != null)
        {
            foreach (ItemDrop.ItemData item in inventory.GetEquippedItems())
            {
                stats.EquippedItems.Add(new EquippedItemSnapshot
                {
                    Name = Localization.instance.Localize(item.m_shared.m_name),
                    Slot = PlayerStatsCalculator.GetEquipmentSlotName(item),
                    Quality = item.m_quality,
                    Durability = item.m_durability,
                    MaxDurability = item.GetMaxDurability(),
				});
            }
        }

        return stats;
    }
}

public struct FoodSnapshot
{
    public string Name;
    public float RemainingTime;
    public float MaxTime;
}

public struct EquippedItemSnapshot
{
    public string Name;
    public string Slot;
    public int Quality;
    public float Durability;
    public float MaxDurability;
}
