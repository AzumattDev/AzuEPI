namespace AzuEPI.Game.Panels.Stats;

internal static class PlayerStatsCalculator
{
    internal const int EquipMovement = 0;
    internal const int EquipHomeItemStamina = 1;
    internal const int EquipHeatResistance = 2;
    internal const int EquipJumpStamina = 3;
    internal const int EquipAttackStamina = 4;
    internal const int EquipBlockStamina = 5;
    internal const int EquipDodgeStamina = 6;
    internal const int EquipSwimStamina = 7;
    internal const int EquipSneakStamina = 8;
    internal const int EquipRunStamina = 9;

    public static float CalculateEquipmentModifierPercent(Player player, int index)
    {
        float value = 0f;

        try
        {
            float[]? values = player.m_equipmentModifierValues;
            if (values == null || index < 0 || index >= values.Length) return 0f;

            value = values[index];
            SEMan? seMan = player.m_seman;
            if (seMan == null) return value * 100f;

            switch (index)
            {
                case EquipHomeItemStamina: seMan.ModifyHomeItemStaminaUsage(1f, ref value, false); break;
                case EquipJumpStamina: seMan.ModifyJumpStaminaUsage(1f, ref value, false); break;
                case EquipAttackStamina: seMan.ModifyAttackStaminaUsage(1f, ref value, false); break;
                case EquipBlockStamina: seMan.ModifyBlockStaminaUsage(1f, ref value, false); break;
                case EquipDodgeStamina: seMan.ModifyDodgeStaminaUsage(1f, ref value, false); break;
                case EquipSwimStamina: seMan.ModifySwimStaminaUsage(1f, ref value, false); break;
                case EquipSneakStamina: seMan.ModifySneakStaminaUsage(1f, ref value, false); break;
                case EquipRunStamina: seMan.ModifyRunStaminaDrain(1f, ref value, Vector3.zero, false); break;
            }
        }
        catch
        {
            /* Ignore errors */
        }

        return value * 100f;
    }

    public static float CalculateAdditiveModifierPercent(SEMan? seMan, Func<SE_Stats, float> getModifier)
    {
        float bonusPercent = 0f;
        if (seMan?.GetStatusEffects() == null) return bonusPercent;

        foreach (StatusEffect effect in seMan.GetStatusEffects())
        {
            if (effect is not SE_Stats seStats) continue;
            float value = getModifier(seStats);
            if (value != 0f)
                bonusPercent += value * 100f;
        }

        return bonusPercent;
    }

    public static float CalculateSkillRaiseSpeed(Player player)
    {
        float multiplier = 1f;

        try
        {
            player.m_seman?.ModifyRaiseSkill(Skills.SkillType.All, ref multiplier);
        }
        catch
        {
            /* Ignore errors */
        }

        return (multiplier - 1f) * 100f;
    }

    public static float CalculateAttackSpeed(Player player)
    {
        float attackSpeed = 100f;
        try
        {
            attackSpeed = player.GetAttackSpeedFactorMovement() * 100f;
        }
        catch
        {
            /* Ignore errors */
        }
        return attackSpeed;
    }

    public static float CalculateDamageModifier(Player player)
    {
        float bonusPercent = 0f;
        if (player.m_seman?.GetStatusEffects() == null) return bonusPercent;

        foreach (StatusEffect effect in player.m_seman.GetStatusEffects())
        {
            if (effect is not SE_Stats seStats) continue;
            if (seStats.m_modifyAttackSkill != Skills.SkillType.All) continue;
            if (seStats.m_damageModifier != 1f)
                bonusPercent += (seStats.m_damageModifier - 1f) * 100f;
        }

        return bonusPercent;
    }

    public static float CalculateSpeedModifier(Player player)
    {
        float speed = 1f;

        try
        {
            player.m_seman?.ApplyStatusEffectSpeedMods(ref speed, Vector3.zero);
        }
        catch
        {
            /* Ignore errors */
        }

        return (speed - 1f) * 100f;
    }

    public static float CalculateJumpModifier(Player player)
    {
        Vector3 jump = Vector3.one;

        try
        {
            player.m_seman?.ApplyStatusEffectJumpMods(ref jump);
        }
        catch
        {
            /* Ignore errors */
        }

        return (jump.y - 1f) * 100f;
    }

    public static float CalculateHealthRegenMultiplierRaw(Player player)
    {
        float multiplier = 1f;

        try
        {
            player.m_seman?.ModifyHealthRegen(ref multiplier);
        }
        catch
        {
            /* Ignore errors */
        }

        return multiplier;
    }

    public static float CalculateStaminaRegenMultiplierRaw(Player player)
    {
        float multiplier = 1f;

        try
        {
            player.m_seman?.ModifyStaminaRegen(ref multiplier);
        }
        catch
        {
            /* Ignore errors */
        }

        return multiplier;
    }

    public static float CalculateEitrRegenMultiplierRaw(Player player)
    {
        float multiplier = 1f;

        try
        {
            player.m_seman?.ModifyEitrRegen(ref multiplier);
        }
        catch
        {
            /* Ignore errors */
        }

        return multiplier;
    }

    public static float CalculateHealthRegenMultiplier(Player player) => (CalculateHealthRegenMultiplierRaw(player) - 1f) * 100f;

    public static float CalculateStaminaRegenMultiplier(Player player) => (CalculateStaminaRegenMultiplierRaw(player) - 1f) * 100f;

    public static float CalculateEitrRegenMultiplier(Player player) => (CalculateEitrRegenMultiplierRaw(player) - 1f) * 100f;

    public static float CalculateStaggerResist(Player player)
    {
        float stagger = 0f;

        try
        {
            player.m_seman?.ModifyStagger(1f, ref stagger);
        }
        catch
        {
            /* Ignore errors */
        }

        return -stagger * 100f;
    }

    public static float CalculateTimedBlockBonus(Player player)
    {
        float bonus = 1f;

        try
        {
            player.m_seman?.ModifyTimedBlockBonus(ref bonus);
        }
        catch
        {
            /* Ignore errors */
        }

        return (bonus - 1f) * 100f;
    }

    public static float CalculateAdrenaline(Player player)
    {
        float adrenaline = 0f;

        try
        {
            player.m_seman?.ModifyAdrenaline(1f, ref adrenaline);
        }
        catch
        {
            /* Ignore errors */
        }

        return adrenaline * 100f;
    }

    public static float CalculateNoiseLevel(Player player)
    {
        float noise = 0f;

        try
        {
            player.m_seman?.ModifyNoise(1f, ref noise);
        }
        catch
        {
            /* Ignore errors */
        }

        return noise * 100f;
    }

    public static float CalculateStealthLevel(Player player)
    {
        float stealth = 0f;

        try
        {
            player.m_seman?.ModifyStealth(1f, ref stealth);
        }
        catch
        {
            /* Ignore errors */
        }

        return stealth * 100f;
    }

    public static float CalculateFallDamage(Player player)
    {
        float damage = 0f;

        try
        {
            player.m_seman?.ModifyFallDamage(1f, ref damage);
        }
        catch
        {
            /* Ignore errors */
        }

        return damage * 100f;
    }

    public static float CalculateExtraCarryWeight(Player player)
    {
        try
        {
            float baseLimit = player.m_maxCarryWeight;
            float limit = baseLimit;
            player.m_seman?.ModifyMaxCarryWeight(baseLimit, ref limit);
            return limit - baseLimit;
        }
        catch
        {
            return 0f;
        }
    }

    public static float CalculateFoodHealthRegen(Player player)
    {
        float foodRegen = 0f;
        foreach (Player.Food food in player.GetFoods())
        {
            if (food.m_item?.m_shared != null)
                foodRegen += food.m_item.m_shared.m_foodRegen;
        }
        return foodRegen;
    }

    public static float CalculateTotalHealthRegen(Player player) => CalculateFoodHealthRegen(player) * CalculateHealthRegenMultiplierRaw(player);

    public static float CalculateTotalStaminaRegen(Player player) => player.m_staminaRegen * CalculateStaminaRegenMultiplierRaw(player);

    public static float CalculateTotalEitrRegen(Player player) => player.m_eiterRegen * CalculateEitrRegenMultiplierRaw(player);

    public static string GetGuardianPowerName(Player player)
    {
        if (string.IsNullOrEmpty(player.m_guardianPower)) return "";

        StatusEffect? power = ObjectDB.instance?.GetStatusEffect(player.m_guardianPower.GetStableHashCode());
        if (power == null) return player.m_guardianPower;

        return power.m_name.StartsWith("$")
            ? Localization.instance.Localize(power.m_name)
            : power.m_name;
    }

    public static string GetBiomeName(Player player)
    {
        Heightmap.Biome biome = player.GetCurrentBiome();
        return Localization.instance.Localize("$biome_" + biome.ToString().ToLower());
    }

    public static string GetEquipmentSlotName(ItemDrop.ItemData item)
    {
        return item.m_shared.m_itemType switch
        {
            ItemDrop.ItemData.ItemType.Helmet => "Helmet",
            ItemDrop.ItemData.ItemType.Chest => "Chest",
            ItemDrop.ItemData.ItemType.Legs => "Legs",
            ItemDrop.ItemData.ItemType.Shoulder => "Cape",
            ItemDrop.ItemData.ItemType.Utility => "Utility",
            ItemDrop.ItemData.ItemType.OneHandedWeapon => "Weapon",
            ItemDrop.ItemData.ItemType.TwoHandedWeapon => "Weapon",
            ItemDrop.ItemData.ItemType.TwoHandedWeaponLeft => "Weapon",
            ItemDrop.ItemData.ItemType.Bow => "Weapon",
            ItemDrop.ItemData.ItemType.Shield => "Shield",
            ItemDrop.ItemData.ItemType.Tool => "Tool",
            ItemDrop.ItemData.ItemType.Torch => "Torch",
            _ => "Other"
        };
    }
}
