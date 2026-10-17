using UnityEngine;

public enum PotionEffect { InstantHealing, Strength }

/// <summary>A potion (Mind page): either heals instantly or makes your attacks stronger for a while.</summary>
[CreateAssetMenu(fileName = "NewPotion", menuName = "Items/Potion (Mind)")]
public class PotionItem : ConsumableItem
{
    public PotionEffect effect = PotionEffect.InstantHealing;

    [Header("Instant healing")]
    public int healAmount = 5;

    [Header("Strength")]
    [Tooltip("Damage is multiplied by this while the potion lasts.")]
    public float damageMultiplier = 1.5f;
    public float duration = 30f;       // seconds

    public PotionItem()
    {
        itemName = "Healing Potion";
        category = ItemCategory.Potion;
        description = "A bitter red draught. It closes wounds at once.";
    }

    public override string GetStatsText()
    {
        if (effect == PotionEffect.InstantHealing)
            return base.GetStatsText() + "\nHeals " + healAmount + " HP instantly";
        return base.GetStatsText() + "\nDamage x" + Num(damageMultiplier) + " for " + Num(duration) + "s";
    }

    public override string GetEffectSummary()
    {
        return effect == PotionEffect.InstantHealing
            ? "Heals " + healAmount + " HP instantly"
            : "Damage x" + Num(damageMultiplier) + " for " + Num(duration) + "s";
    }

    protected override string CannotUseReason(PlayerStats stats)
    {
        if (effect == PotionEffect.InstantHealing && stats.Health >= stats.MaxHealth)
            return "You're already at full health.";
        return "";
    }

    protected override string Apply(PlayerStats stats)
    {
        if (effect == PotionEffect.InstantHealing)
        {
            stats.Heal(healAmount);
            return "Healed " + healAmount + " HP.";
        }

        stats.ApplyStrength(damageMultiplier, duration);
        return "Strength: damage x" + Num(damageMultiplier) + " for " + Num(duration) + "s.";
    }
}