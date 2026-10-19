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
        public bool winding;
        public float windTimer;
        public readonly ItemUseContext context = new ItemUseContext();
    }

    private Slot[] slots;
    private PlayerController controller;
    private PlayerStats stats;

    /// <summary>Fired when an item is equipped, removed or swapped.</summary>
    public event Action LoadoutChanged;

    // ---------------- Public API (used by the inventory menu) ----------------

    public ItemData GetEquipped(GameAction action)
    {
        Slot s = FindSlot(action);
        return s != null ? s.item : null;
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

    public bool IsEquipped(ItemData item)
    {
        GameAction ignored;
        return TryGetEquippedAction(item, out ignored);
    }

    public void Unequip(GameAction action)
    {
        Equip(action, null);
    }

    public void Equip(GameAction action, ItemData item)
    {
        Slot target = FindSlot(action);
        if (target == null) return;

        StopActivity(target);

        // An item can only be on one button: if it's already equipped elsewhere, it moves here
        if (item != null)
        {
            foreach (Slot other in slots)
            {
                if (other != target && other.item == item)
                {
                    StopActivity(other);
                    other.item = null;
                }
            }
        }

        target.item = item;
        LoadoutChanged?.Invoke();
    }

    /// <summary>Exchanges the items of two action buttons (an empty button just receives the item).</summary>
    public void SwapSlots(GameAction a, GameAction b)
    {
        Slot slotA = FindSlot(a);
        Slot slotB = FindSlot(b);
        if (slotA == null || slotB == null || slotA == slotB) return;

        StopActivity(slotA);
        StopActivity(slotB);

        ItemData tempItem = slotA.item;
        slotA.item = slotB.item;
        slotB.item = tempItem;

        float tempTime = slotA.nextUseTime; // the cooldown follows the item
        slotA.nextUseTime = slotB.nextUseTime;
        slotB.nextUseTime = tempTime;

        LoadoutChanged?.Invoke();
    }

    // ---------------- Unity ----------------

    private void Awake()
    {
        controller = GetComponent<PlayerController>();
        stats = GetComponent<PlayerStats>();
        if (stats == null) stats = gameObject.AddComponent<PlayerStats>();
        if (GetComponent<PlayerInventory>() == null) gameObject.AddComponent<PlayerInventory>();
        stats.Damaged += StopAllActivity; // a hit interrupts wind-ups, charging and held items

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
        if (InputLock.IsLocked) // any menu, dialogue, death, camera trip or scene change
        {
            StopAllActivity();
            return;
        }

        // --- Talking: the player is frozen and can't use items until the dialogue closes ---
        if (DialogueManager.IsOpen)
        {
            StopAllActivity();
            return;
        }
        if (DialogueManager.JustClosed) return; // the key press that closed the dialogue must not also use an item
        if (ShopMenu.JustClosed || DeathScreen.JustClosed) return;        // same for the shop screen

        // --- Is there something to interact with in range? (only while standing on the ground) ---
        Interactable nearby = controller.IsGrounded && !controller.IsDashing
            ? Interactable.FindNearest(transform.position)
            : null;
        DialogueManager.SetPrompt(nearby != null
            ? "[" + GameControls.GetKey(GameAction.Action1) + "] " + nearby.Prompt
            : null);

        // --- No items while dashing ---
        if (controller.IsDashing)
        {
            StopAllActivity();
            return;
        }

        // --- Interact (action button 1): signs, NPCs, chests and loot take priority ---
        if (nearby != null && GameControls.Pressed(GameAction.Action1))
        {
            StopAllActivity();
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

            // Under water, items that aren't marked "usable underwater" can't be used
            if (controller.IsSwimming && !slot.item.usableUnderwater)
            {
                if (slot.winding || slot.holding) StopActivity(slot);
                if (pressed) StatsHUD.Show(slot.item.itemName + " can't be used underwater.");
                continue;
            }

            // Wind-up in progress: count down, then the item takes effect
            if (slot.winding)
            {
                slot.item.UpdateWindUp(ctx);
                slot.windTimer -= Time.deltaTime;
                if (slot.windTimer <= 0f)
                {
                    slot.winding = false;
                    slot.item.EndWindUp(ctx);
                    slot.item.Use(ctx);
                }
                continue;
            }

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
                        // Released normally
                        ctx.interrupted = false;
                        slot.item.EndHold(ctx);
                        slot.holding = false;
                    }
                }
                else if (pressed && !LockedByOther(slot) && TryActivate(slot))
                {
                    slot.holding = true;
                    ctx.interrupted = false;
                    slot.item.BeginHold(ctx);
                }
            }
            else if (pressed && !LockedByOther(slot) && TryActivate(slot))
            {
                float windUp = slot.item.WindUpTime;
                if (windUp > 0f)
                {
                    slot.winding = true;
                    slot.windTimer = windUp;
                    slot.item.BeginWindUp(ctx);
                }
                else
                {
                    slot.item.Use(ctx);
                }
            }
        }

        UpdateSpeedMultiplier();
    }

    private void OnDestroy()
    {
        if (stats != null) stats.Damaged -= StopAllActivity;
    }

    private void OnDisable()
    {
        StopAllActivity();
    }

    // ---------------- Helpers ----------------

    private Slot FindSlot(GameAction action)
    {
        if (slots == null) return null;
        foreach (Slot s in slots)
        {
            if (s.action == action) return s;
        }
        return null;
    }

    // Checks cooldown and Divine Energy, and pays the cost (unless the item pays it itself later).
    private bool TryActivate(Slot slot)
    {
        if (Time.time < slot.nextUseTime) return false;

        int cost = slot.item.divineEnergyCost;
        if (cost > 0)
        {
            bool canPay = slot.item.SpendsCostOnActivate
                ? stats.TrySpendDivineEnergy(cost)
                : stats.DivineEnergy >= cost;

            if (!canPay)
            {
                Debug.Log("Not enough Divine Energy for " + slot.item.itemName + " (needs " + cost + ").");
                return false;
            }
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
            if (s.item == null) continue;
            if (s.holding) multiplier = Mathf.Min(multiplier, s.item.HeldMoveSpeedMultiplier);
            if (s.winding) multiplier = Mathf.Min(multiplier, s.item.WindUpMoveSpeedMultiplier);
        }
        controller.SpeedMultiplier = multiplier;
    }

    // Cancels a wind-up and/or releases a held item because something interrupted it.
    private void StopActivity(Slot s)
    {
        if (s.item != null)
        {
            if (s.winding) s.item.EndWindUp(s.context);
            if (s.holding)
            {
                s.context.interrupted = true;
                s.item.EndHold(s.context);
                s.context.interrupted = false;
            }
        }
        s.winding = false;
        s.holding = false;
    }

    private void StopAllActivity()
    {
        if (slots == null) return;

        foreach (Slot s in slots) StopActivity(s);
        if (controller != null) controller.SpeedMultiplier = 1f;
    }
}