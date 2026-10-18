using System;
using System.Collections.Generic;
using UnityEngine;

public enum RequirementType { LevelKey, InventoryItem }

public enum GateEffect
{
    /// <summary>Starts hidden and does nothing. It appears (and starts working) once everything is collected.</summary>
    Appear,
    /// <summary>Starts visible and locked. It disappears once everything is collected (a door, a barrier).</summary>
    Disappear,
    /// <summary>Always visible, but a teleporter or scene transition only works once everything is collected.</summary>
    OnlyActivate
}

[Serializable]
public class KeyRequirement
{
    public RequirementType type = RequirementType.LevelKey;

    [Tooltip("Level Key: the Key Id of the key pickups in this level, e.g. \"RedKey\".")]
    public string keyId = "";

    [Tooltip("Inventory Item: an item the player must own (any item asset: weapon, tool, food...).")]
    public ItemData item;

    [Min(1)] public int amount = 1;
}

/// <summary>
/// Locks an object until ALL the requirements are collected. Add it to a solid object (a door), a Teleporter
/// or a Scene Transition. What happens on unlock is up to you: the object can appear, disappear, or just start working.
/// While locked, touching it shows a message with what's missing.
/// </summary>
public class KeyGate : MonoBehaviour
{
    [Header("Requirements (all of them are needed)")]
    [SerializeField] private List<KeyRequirement> requirements = new List<KeyRequirement>();

    [Header("When everything is collected")]
    [SerializeField] private GateEffect effect = GateEffect.Disappear;
    [Tooltip("Once opened, it stays open even if you lose the keys later.")]
    [SerializeField] private bool stayOpen = true;
    [Tooltip("Use up the required keys and items when it opens.")]
    [SerializeField] private bool consumeOnOpen = false;
    [Tooltip("Other objects to show or hide together with this one (for example a separate door sprite or effect). They follow the same Appear / Disappear rule.")]
    [SerializeField] private GameObject[] alsoToggle;

    [Header("Messages")]
    [Tooltip("Shown when the player touches it while locked. Leave empty for an automatic message listing what's missing.")]
    [SerializeField] private string lockedMessage = "";
    [Tooltip("Shown when it opens. Leave empty for none.")]
    [SerializeField] private string openedMessage = "";

    public bool IsOpen { get; private set; }

    private Collider2D[] colliders;
    private SpriteRenderer[] renderers;
    private float pollTimer;
    private float nextMessageTime;

    private void Awake()
    {
        colliders = GetComponentsInChildren<Collider2D>(true);
        renderers = GetComponentsInChildren<SpriteRenderer>(true);

        IsOpen = false;
        ApplyState();
    }

    private void Update()
    {
        // Checked a few times a second, so it reacts soon after you pick something up
        pollTimer -= Time.deltaTime;
        if (pollTimer > 0f) return;
        pollTimer = 0.1f;

        Refresh();
    }

    private void Refresh()
    {
        if (IsOpen && (stayOpen || consumeOnOpen)) return;

        bool met = RequirementsMet();
        if (met == IsOpen) return;

        IsOpen = met;
        if (IsOpen && consumeOnOpen) Consume();

        ApplyState();

        if (IsOpen && !string.IsNullOrEmpty(openedMessage)) StatsHUD.Show(openedMessage);
    }

    // Shows or hides the object (and turns its collisions on or off) according to the chosen effect
    private void ApplyState()
    {
        if (effect == GateEffect.OnlyActivate) return; // always visible: the Teleporter / Scene Transition checks IsOpen itself

        bool present = effect == GateEffect.Appear ? IsOpen : !IsOpen;

        foreach (Collider2D c in colliders) c.enabled = present;
        foreach (SpriteRenderer r in renderers) r.enabled = present;

        if (alsoToggle != null)
        {
            foreach (GameObject go in alsoToggle)
            {
                if (go != null) go.SetActive(present);
            }
        }
    }

    // ---------------- Requirements ----------------

    private bool RequirementsMet()
    {
        foreach (KeyRequirement r in requirements)
        {
            if (r == null) continue;
            if (Have(r) < r.amount) return false;
        }
        return true;
    }

    private int Have(KeyRequirement r)
    {
        if (r.type == RequirementType.InventoryItem)
        {
            PlayerInventory inventory = PlayerInventory.Instance;
            return inventory != null && r.item != null ? inventory.GetCount(r.item) : 0;
        }
        return LevelKeys.Instance.Count(r.keyId);
    }

    private void Consume()
    {
        PlayerInventory inventory = PlayerInventory.Instance;

        foreach (KeyRequirement r in requirements)
        {
            if (r == null) continue;

            if (r.type == RequirementType.InventoryItem)
            {
                if (inventory != null && r.item != null) inventory.Remove(r.item, r.amount);
            }
            else
            {
                LevelKeys.Instance.Remove(r.keyId, r.amount);
            }
        }
    }

    // ---------------- Feedback ----------------

    /// <summary>Shows what's missing. Teleporters and scene transitions call this when touched while locked.</summary>
    public void NotifyLocked()
    {
        if (IsOpen || Time.unscaledTime < nextMessageTime) return;

        nextMessageTime = Time.unscaledTime + 1.5f;
        StatsHUD.Show(BuildLockedMessage());
    }

    private string BuildLockedMessage()
    {
        if (!string.IsNullOrEmpty(lockedMessage)) return lockedMessage;

        List<string> missing = new List<string>();
        foreach (KeyRequirement r in requirements)
        {
            if (r == null) continue;

            int have = Have(r);
            if (have >= r.amount) continue;

            string label = r.type == RequirementType.InventoryItem
                ? (r.item != null ? r.item.itemName : "?")
                : r.keyId;
            missing.Add(label + " (" + have + "/" + r.amount + ")");
        }

        return missing.Count > 0 ? "Locked. You need: " + string.Join(", ", missing) : "Locked.";
    }

    // A solid door: bumping into it while it's locked shows the message
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (IsOpen) return;
        if (collision.collider.GetComponentInParent<PlayerStats>() != null) NotifyLocked();
    }
}