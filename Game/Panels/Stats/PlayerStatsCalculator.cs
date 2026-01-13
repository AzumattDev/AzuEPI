namespace AzuEPI.Game.Panels.Stats;

internal static class PlayerStatsCalculator
{
    public static float CalculateMultiplierModifierPercent(SEMan? seMan, Func<SE_Stats, float> getMultiplier)
    {
        float bonusPercent = 0f;
        if (seMan?.GetStatusEffects() == null) return bonusPercent;

        foreach (StatusEffect effect in seMan.GetStatusEffects())
        {
            if (effect is not SE_Stats seStats) continue;
            float value = getMultiplier(seStats);
            if (value != 1f)
                bonusPercent += (value - 1f) * 100f;
        }

        return bonusPercent;
    }

    public static float CalculateMultiplierModifierRaw(SEMan? seMan, Func<SE_Stats, float> getMultiplier)
    {
        float value = 1f;
        if (seMan?.GetStatusEffects() == null) return value;

        foreach (StatusEffect effect in seMan.GetStatusEffects())
        {
            if (effect is SE_Stats seStats)
            {
                float mod = getMultiplier(seStats);
                if (mod != 1f)
                    value *= mod;
            }
        }

        return value;
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

    public static float CalculateDamageModifier(Player player) =>
        CalculateMultiplierModifierPercent(player.m_seman, seStats => seStats.m_damageModifier);

    public static float CalculateSpeedModifier(Player player) =>
        CalculateMultiplierModifierPercent(player.m_seman, seStats => seStats.m_speedModifier);

    public static float CalculateJumpModifier(Player player)
    {
        float jumpModifier = 0f;
        try
        {
            if (player.m_seman?.GetStatusEffects() != null)
            {
                foreach (StatusEffect effect in player.m_seman.GetStatusEffects())
                {
                    if (effect is SE_Stats seStats)
                    {
                        jumpModifier += seStats.m_jumpStaminaUseModifier * 100f;
                    }
                }
            }
        }
        catch
        {
            /* Ignore errors */
        }
        return jumpModifier;
    }

    public static float CalculateHealthRegenMultiplier(Player player) =>
        CalculateMultiplierModifierPercent(player.m_seman, seStats => seStats.m_healthRegenMultiplier);

    public static float CalculateStaminaRegenMultiplier(Player player) =>
        CalculateMultiplierModifierPercent(player.m_seman, seStats => seStats.m_staminaRegenMultiplier);

    public static float CalculateEitrRegenMultiplier(Player player) =>
        CalculateMultiplierModifierPercent(player.m_seman, seStats => seStats.m_eitrRegenMultiplier);

    public static float CalculateStaggerResist(Player player)
    {
        float stagger = 0f;
        try
        {
            if (player.m_seman?.GetStatusEffects() != null)
            {
                foreach (StatusEffect effect in player.m_seman.GetStatusEffects())
                {
                    if (effect is SE_Stats seStats && seStats.m_staggerModifier != 0f)
                    {
                        stagger -= seStats.m_staggerModifier * 100f;
                    }
                }
            }
        }
        catch
        {
            /* Ignore errors */
        }
        return stagger;
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

    public static float CalculateTotalHealthRegen(Player player)
    {
        float foodRegen = CalculateFoodHealthRegen(player);
        float multiplier = CalculateMultiplierModifierRaw(player.m_seman, seStats => seStats.m_healthRegenMultiplier);
        return foodRegen * multiplier;
    }

    public static float CalculateTotalStaminaRegen(Player player)
    {
        float baseRegen = player.m_staminaRegen;
        float multiplier = CalculateMultiplierModifierRaw(player.m_seman, seStats => seStats.m_staminaRegenMultiplier);
        return baseRegen * multiplier;
    }

    public static float CalculateTotalEitrRegen(Player player)
    {
        float baseRegen = player.m_eiterRegen;
        float multiplier = CalculateMultiplierModifierRaw(player.m_seman, seStats => seStats.m_eitrRegenMultiplier);
        return baseRegen * multiplier;
    }

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
