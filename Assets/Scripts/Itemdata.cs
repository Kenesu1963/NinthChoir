using System.Globalization;
using UnityEngine;

/// <summary>
/// What kind of item it is. This is just a label stored on the item: it does NOT decide which
/// action button the item can use. Any item can be equipped on any action button.
/// </summary>
public enum ItemCategory { Weapon, Tool, Incantation }

/// <summary>
/// Everything an item needs while it's being used. One of these exists per action-button slot,
/// so items themselves (which are shared assets) never have to store any state.
/// </summary>
public class ItemUseContext
{
    public Transform owner;
    public PlayerController controller;
    public PlayerStats stats;

    public Vector2 aim;       // direction of the held keys (8 directions) or the way you're facing
    public Vector2 origin;    // where attacks start, relative to the player (lower while crouching)
    public bool showDebugVisuals;

    public Hitbox heldHitbox; // used by hold items such as the shield
}

/// <summary>
/// Base class for every item. Create items in the Project panel with
/// right-click -> Create -> Items -> ... and equip them in the Player Combat component.
/// </summary>
public abstract class ItemData : ScriptableObject
{
    [Header("Item")]
    public string itemName = "New Item";
    public ItemCategory category;
    [TextArea(2, 4)] public string description;
    public Sprite icon;

    [Header("Use")]
    [Tooltip("Divine Energy spent every time the item is used. 0 = free.")]
    public int divineEnergyCost = 0;
    [Tooltip("Seconds before the item can be used again.")]
    public float cooldown = 0.25f;

    /// <summary>True = active while the button is held (shield). False = fires once per press.</summary>
    public abstract bool IsHold { get; }

    /// <summary>Walking speed multiplier while a hold item is active.</summary>
    public virtual float HeldMoveSpeedMultiplier => 1f;

    /// <summary>If true, the other action buttons do nothing while this item is held.</summary>
    public virtual bool LocksOtherActionsWhileHeld => false;

    /// <summary>Stats shown by the inventory's "Check" option. Item types add their own lines.</summary>
    public virtual string GetStatsText()
    {
        return "Category: " + category +
               "\nDivine Energy cost: " + (divineEnergyCost > 0 ? divineEnergyCost.ToString() : "none") +
               "\nCooldown: " + Num(cooldown) + "s";
    }

    protected static string Num(float value)
    {
        return value.ToString("0.##", CultureInfo.InvariantCulture);
    }

    public virtual void Use(ItemUseContext context) { }        // press items
    public virtual void BeginHold(ItemUseContext context) { }  // hold items: button pressed
    public virtual void UpdateHold(ItemUseContext context) { } // hold items: every frame while held
    public virtual void EndHold(ItemUseContext context) { }    // hold items: released or interrupted
}