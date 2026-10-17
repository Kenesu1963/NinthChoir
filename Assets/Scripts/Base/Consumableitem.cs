using UnityEngine;

/// <summary>
/// Base class for items you use up: food, potions, wine. They stack in the inventory.
/// You can use them from the inventory menu, or equip them on an action button and use them in the field.
/// To make a new kind of consumable, extend this class and fill in Apply (see FoodItem).
/// </summary>
public abstract class ConsumableItem : ItemData
{
    [Header("Stacking")]
    [Tooltip("How many of this item you can carry.")]
    public int maxStack = 9;

    protected ConsumableItem()
    {
        cooldown = 0.5f;
    }

    public override bool IsHold => false;

    public override string GetStatsText()
    {
        return base.GetStatsText() + "\nConsumable (carry up to " + maxStack + ")";
    }

    /// <summary>Why the item can't be used right now (e.g. healing at full health). Empty = it can be used.</summary>
    protected virtual string CannotUseReason(PlayerStats stats) { return ""; }

    /// <summary>Applies the effect and returns a short message describing what happened.</summary>
    protected abstract string Apply(PlayerStats stats);

    /// <summary>Uses one. Returns false (and a reason in message) if it couldn't be used. One is removed from the inventory.</summary>
    public bool TryUse(PlayerStats stats, out string message)
    {
        message = "";
        if (stats == null) return false;

        PlayerInventory inventory = PlayerInventory.Instance;
        if (inventory != null && inventory.GetCount(this) <= 0)
        {
            message = "You don't have any " + itemName + ".";
            return false;
        }

        string reason = CannotUseReason(stats);
        if (!string.IsNullOrEmpty(reason))
        {
            message = reason;
            return false;
        }

        message = Apply(stats);
        if (inventory != null) inventory.Remove(this, 1);
        return true;
    }

    // Used when the item is equipped on an action button
    public override void Use(ItemUseContext c)
    {
        string message;
        TryUse(c.stats, out message);
        if (!string.IsNullOrEmpty(message)) StatsHUD.Show(message);
    }
}