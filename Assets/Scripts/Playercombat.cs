using System;
using UnityEngine;

/// <summary>
/// The three action buttons (J, K, L by default) have no built-in function.
/// Each one uses whatever item is equipped in its slot. An item's category (Weapon, Tool,
/// Incantation) is just a label on the item, so any item can go on any button.
/// </summary>
[RequireComponent(typeof(PlayerController))]
[RequireComponent(typeof(PlayerStats))]
[RequireComponent(typeof(PlayerInventory))]
public class PlayerCombat : MonoBehaviour
{
    [Header("Equipped items (drag item assets here)")]
    [Tooltip("Item used with action button 1 (J by default).")]
    [SerializeField] private ItemData action1Item;
    [Tooltip("Item used with action button 2 (K by default).")]
    [SerializeField] private ItemData action2Item;
    [Tooltip("Item used with action button 3 (L by default).")]
    [SerializeField] private ItemData action3Item;
    [Tooltip("If all three slots are empty, use a default Rapier / Shield / Divine Bolt so the game works right away.")]
    [SerializeField] private bool useDefaultItemsIfEmpty = true;

    [Header("Crouching")]
    [SerializeField] private float crouchOriginOffsetY = -0.25f; // items are used from this much lower while crouched

    [Header("Debug")]
    [SerializeField] private bool showDebugVisuals = true; // draws hitboxes and projectiles so you can see them

    private class Slot
    {
        public GameAction action;
        public ItemData item;
        public float nextUseTime;
        public bool holding;
        public readonly ItemUseContext context = new ItemUseContext();
    }

    private Slot[] slots;
    private PlayerController controller;
    private PlayerStats stats;

    /// <summary>Fired when an item is equipped or removed.</summary>
    public event Action LoadoutChanged;

    // ---------------- Public API (for a future inventory / build screen) ----------------

    public ItemData GetEquipped(GameAction action)
    {
        foreach (Slot s in slots)
        {
            if (s.action == action) return s.item;
        }
        return null;
    }

    /// <summary>Which action button the item is equipped on (false if it isn't equipped).</summary>
    public bool TryGetEquippedAction(ItemData item, out GameAction action)
    {
        action = GameAction.Action1;
        if (item == null || slots == null) return false;
        foreach (Slot s in slots)
        {
            if (s.item == item)
            {
                action = s.action;
                return true;
            }
        }
        return false;
    }

    public void Unequip(GameAction action)
    {
        Equip(action, null);
    }

    /// <summary>Exchanges the items of two action buttons (an empty button just receives the item).</summary>
    public void SwapSlots(GameAction a, GameAction b)
    {
        Slot slotA = null;
        Slot slotB = null;
        foreach (Slot s in slots)
        {
            if (s.action == a) slotA = s;
            if (s.action == b) slotB = s;
        }
        if (slotA == null || slotB == null || slotA == slotB) return;

        foreach (Slot s in new[] { slotA, slotB })
        {
            if (s.holding && s.item != null)
            {
                s.item.EndHold(s.context);
                s.holding = false;
            }
        }

        ItemData tempItem = slotA.item;
        slotA.item = slotB.item;
        slotB.item = tempItem;

        float tempTime = slotA.nextUseTime; // the cooldown follows the item
        slotA.nextUseTime = slotB.nextUseTime;
        slotB.nextUseTime = tempTime;

        LoadoutChanged?.Invoke();
    }

    public bool IsEquipped(ItemData item)
    {
        if (item == null || slots == null) return false;
        foreach (Slot s in slots)
        {
            if (s.item == item) return true;
        }
        return false;
    }

    public void Equip(GameAction action, ItemData item)
    {
        foreach (Slot s in slots)
        {
            if (s.action != action) continue;

            if (s.holding && s.item != null)
            {
                s.item.EndHold(s.context);
                s.holding = false;
            }
            // An item can only be on one button: if it's already equipped elsewhere, it moves here
            if (item != null)
            {
                foreach (Slot other in slots)
                {
                    if (other != s && other.item == item)
                    {
                        if (other.holding)
                        {
                            other.item.EndHold(other.context);
                            other.holding = false;
                        }
                        other.item = null;
                    }
                }
            }

            s.item = item;
            LoadoutChanged?.Invoke();
            return;
        }
    }

    // ---------------- Unity ----------------

    private void Awake()
    {
        controller = GetComponent<PlayerController>();
        stats = GetComponent<PlayerStats>();
        if (stats == null) stats = gameObject.AddComponent<PlayerStats>();
        if (GetComponent<PlayerInventory>() == null) gameObject.AddComponent<PlayerInventory>();

        slots = new Slot[]
        {
            new Slot { action = GameAction.Action1, item = action1Item },
            new Slot { action = GameAction.Action2, item = action2Item },
            new Slot { action = GameAction.Action3, item = action3Item },
        };

        if (useDefaultItemsIfEmpty && action1Item == null && action2Item == null && action3Item == null)
        {
            slots[0].item = ScriptableObject.CreateInstance<MeleeWeaponItem>();
            slots[1].item = ScriptableObject.CreateInstance<ShieldToolItem>();
            slots[2].item = ScriptableObject.CreateInstance<ProjectileIncantationItem>();
            Debug.Log("PlayerCombat: no items assigned, using the default Rapier / Shield / Divine Bolt.");
        }

        foreach (Slot s in slots)
        {
            s.context.owner = transform;
            s.context.controller = controller;
            s.context.stats = stats;
        }
    }

    private void Update()
    {
        // --- Inventory open: the game is paused, so no items can be used ---
        if (InventoryMenu.IsOpen)
        {
            ReleaseAllHolds();
            return;
        }

        // --- Talking: the player is frozen and can't use items until the dialogue closes ---
        if (DialogueManager.IsOpen)
        {
            ReleaseAllHolds();
            return;
        }
        if (DialogueManager.JustClosed) return; // the key press that closed the dialogue must not also use an item

        // --- Is there a sign or NPC in range? (only while standing on the ground) ---
        Interactable nearby = controller.IsGrounded && !controller.IsDashing
            ? Interactable.FindNearest(transform.position)
            : null;
        DialogueManager.SetPrompt(nearby != null
            ? "[" + GameControls.GetKey(GameAction.Action1) + "] " + nearby.Prompt
            : null);

        // --- No items while dashing ---
        if (controller.IsDashing)
        {
            ReleaseAllHolds();
            return;
        }

        // --- Interact (action button 1): signs and NPCs take priority ---
        if (nearby != null && GameControls.Pressed(GameAction.Action1))
        {
            ReleaseAllHolds();
            nearby.Interact();
            return;
        }

        Vector2 aim = controller.AimDirection;
        Vector2 origin = controller.IsCrouching ? new Vector2(0f, crouchOriginOffsetY) : Vector2.zero;

        // --- Use the item in each slot ---
        foreach (Slot slot in slots)
        {
            if (slot.item == null) continue;

            ItemUseContext ctx = slot.context;
            ctx.aim = aim;
            ctx.origin = origin;
            ctx.showDebugVisuals = showDebugVisuals;

            bool pressed = GameControls.Pressed(slot.action);
            bool held = GameControls.Held(slot.action);

            if (slot.item.IsHold)
            {
                if (slot.holding)
                {
                    if (held)
                    {
                        slot.item.UpdateHold(ctx);
                    }
                    else
                    {
                        slot.item.EndHold(ctx);
                        slot.holding = false;
                    }
                }
                else if (pressed && !LockedByOther(slot) && TryActivate(slot))
                {
                    slot.holding = true;
                    slot.item.BeginHold(ctx);
                }
            }
            else if (pressed && !LockedByOther(slot) && TryActivate(slot))
            {
                slot.item.Use(ctx);
            }
        }

        UpdateSpeedMultiplier();
    }

    private void OnDisable()
    {
        ReleaseAllHolds();
    }

    // ---------------- Helpers ----------------

    // Checks cooldown and Divine Energy, and pays the cost.
    private bool TryActivate(Slot slot)
    {
        if (Time.time < slot.nextUseTime) return false;

        int cost = slot.item.divineEnergyCost;
        if (cost > 0 && !stats.TrySpendDivineEnergy(cost))
        {
            Debug.Log("Not enough Divine Energy for " + slot.item.itemName + " (needs " + cost + ").");
            return false;
        }

        slot.nextUseTime = Time.time + slot.item.cooldown;
        return true;
    }

    // True if another slot is holding an item that blocks all other actions (e.g. the shield).
    private bool LockedByOther(Slot self)
    {
        foreach (Slot other in slots)
        {
            if (other != self && other.holding && other.item != null && other.item.LocksOtherActionsWhileHeld)
            {
                return true;
            }
        }
        return false;
    }

    private void UpdateSpeedMultiplier()
    {
        float multiplier = 1f;
        foreach (Slot s in slots)
        {
            if (s.holding && s.item != null)
            {
                multiplier = Mathf.Min(multiplier, s.item.HeldMoveSpeedMultiplier);
            }
        }
        controller.SpeedMultiplier = multiplier;
    }

    private void ReleaseAllHolds()
    {
        if (slots == null) return;

        foreach (Slot s in slots)
        {
            if (s.holding && s.item != null) s.item.EndHold(s.context);
            s.holding = false;
        }
        if (controller != null) controller.SpeedMultiplier = 1f;
    }
}