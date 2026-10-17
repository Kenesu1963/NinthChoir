using UnityEngine;

/// <summary>Wine (Soul page): recovers Divine Energy.</summary>
[CreateAssetMenu(fileName = "NewWine", menuName = "Items/Wine (Soul)")]
public class WineItem : ConsumableItem
{
    [Header("Divine Energy")]
    public int divineEnergyRestored = 5;

    public WineItem()
    {
        itemName = "Wine";
        category = ItemCategory.Wine;
        description = "A deep red wine blessed by the temple. It restores Divine Energy.";
    }

    public override string GetStatsText()
    {
        return base.GetStatsText() + "\nRestores " + divineEnergyRestored + " Divine Energy";
    }

    public override string GetEffectSummary()
    {
        return "Restores " + divineEnergyRestored + " Divine Energy";
    }

    protected override string CannotUseReason(PlayerStats stats)
    {
        return stats.DivineEnergy >= stats.MaxDivineEnergy ? "Your Divine Energy is already full." : "";
    }

    protected override string Apply(PlayerStats stats)
    {
        stats.RestoreDivineEnergy(divineEnergyRestored);
        return "Recovered " + divineEnergyRestored + " Divine Energy.";
    }
}