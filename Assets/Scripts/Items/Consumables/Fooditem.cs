using UnityEngine;

/// <summary>Food (Body page): gives regeneration, a little health back every few seconds.</summary>
[CreateAssetMenu(fileName = "NewFood", menuName = "Items/Food (Body)")]
public class FoodItem : ConsumableItem
{
    [Header("Regeneration")]
    public int healPerTick = 1;
    public float tickInterval = 2f;   // seconds between each heal
    public int ticks = 3;             // how many times it heals

    public FoodItem()
    {
        itemName = "Bread";
        category = ItemCategory.Food;
        description = "A plain loaf. It restores a little health, slowly.";
    }

    public override string GetStatsText()
    {
        return base.GetStatsText() +
               "\nRegeneration: +" + healPerTick + " HP every " + Num(tickInterval) + "s, " + ticks +
               " times (" + (healPerTick * ticks) + " HP in total)";
    }

    public override string GetEffectSummary()
    {
        return "Regeneration: +" + healPerTick + " HP every " + Num(tickInterval) + "s (" + ticks + " times)";
    }

    protected override string CannotUseReason(PlayerStats stats)
    {
        return stats.Health >= stats.MaxHealth ? "You're already at full health." : "";
    }

    protected override string Apply(PlayerStats stats)
    {
        stats.StartRegeneration(healPerTick, tickInterval, ticks);
        return "Regeneration: +" + healPerTick + " HP every " + Num(tickInterval) + "s.";
    }
}