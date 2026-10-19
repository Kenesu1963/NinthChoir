using System.Globalization;
using UnityEngine;

/// <summary>
/// What kind of item it is. This is just a label stored on the item: it does NOT decide which
/// action button the item can use. Any item can be equipped on any action button.
/// </summary>
public enum ItemCategory { Weapon, Tool, Incantation, Food, Potion, Wine }

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

    // Scratch space for whichever item is being used in this slot
    public Hitbox heldHitbox; // shield block box, weapon wind-up preview
    public GameObject effect; // e.g. the charge glow
    public float chargeTimer;
    public bool charged;
    public bool fired;

    /// <summary>True when a hold item is stopped by something other than releasing the button (dash, dialogue, unequip...).</summary>
    public bool interrupted;
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
    [Tooltip("Seconds before the item can be used again, counted from the moment the button is pressed.")]
    public float cooldown = 0.25f;
    [Tooltip("If unticked, the item can't be used while the player is swimming.")]
    public bool usableUnderwater = true;

    /// <summary>True = active while the button is held (shield, pick, charged spells). False = fires once per press.</summary>
    public abstract bool IsHold { get; }

    /// <summary>Walking speed multiplier while a hold item is active.</summary>
    public virtual float HeldMoveSpeedMultiplier => 1f;

    /// <summary>If true, the other action buttons do nothing while this item is held.</summary>
    public virtual bool LocksOtherActionsWhileHeld => false;

    /// <summary>Seconds between pressing the button and the item taking effect (press items only).</summary>
    public virtual float WindUpTime => 0f;

    /// <summary>Walking speed multiplier during the wind-up.</summary>
    public virtual float WindUpMoveSpeedMultiplier => 1f;

    /// <summary>
    /// True = the Divine Energy cost is paid when the button is pressed.
    /// False = the item charges the cost itself (e.g. a charged spell that is only paid when it fires).
    /// </summary>
    public virtual bool SpendsCostOnActivate => true;

    /// <summary>Stats shown by the inventory's "Check" option. Item types add their own lines.</summary>
    public virtual string GetStatsText()
    {
        return "Category: " + category +
               "\nDivine Energy cost: " + (divineEnergyCost > 0 ? divineEnergyCost.ToString() : "none") +
               "\nCooldown: " + Num(cooldown) + "s" +
               (usableUnderwater ? "" : "\nCannot be used underwater");
    }

    protected static string Num(float value)
    {
        return value.ToString("0.##", CultureInfo.InvariantCulture);
    }

    /// <summary>One short line about what the item does, shown in shops. Empty = just the description.</summary>
    public virtual string GetEffectSummary()
    {
        return "";
    }

    /// <summary>Applies the player's damage multiplier (strength potions) to an item's damage.</summary>
    protected static int Scaled(ItemUseContext c, int baseDamage)
    {
        float multiplier = c.stats != null ? c.stats.DamageMultiplier : 1f;
        return Mathf.Max(1, Mathf.RoundToInt(baseDamage * multiplier));
    }

    public virtual void Use(ItemUseContext context) { }          // press items: when it takes effect
    public virtual void BeginWindUp(ItemUseContext context) { }  // press items with a wind-up
    public virtual void UpdateWindUp(ItemUseContext context) { }
    public virtual void EndWindUp(ItemUseContext context) { }    // wind-up finished or cancelled
    public virtual void BeginHold(ItemUseContext context) { }    // hold items: button pressed
    public virtual void UpdateHold(ItemUseContext context) { }   // hold items: every frame while held
    public virtual void EndHold(ItemUseContext context) { }      // hold items: released or interrupted
}