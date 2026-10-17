using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Everything Maor owns, sorted into three pages:
///     Body = weapons and food     Mind = tools and potions     Soul = incantations and wine
/// Equipment is unique (you own it or not). Consumables (food, potions, wine) stack up to their max stack.
/// The items equipped in Player Combat are added automatically at the start.
/// </summary>
public class PlayerInventory : MonoBehaviour
{
    public const int SlotsPerPage = 10; // one triangle of slots: 4 + 3 + 2 + 1

    [Header("Starting items (optional)")]
    [Tooltip("Extra items to start with. The items equipped in Player Combat are added automatically.")]
    [SerializeField] private List<ItemData> startingItems = new List<ItemData>();

    [Header("Menu")]
    [SerializeField] private bool enableMenu = true;

    private List<ItemData>[] pages;
    private readonly Dictionary<ItemData, int> counts = new Dictionary<ItemData, int>();
    private PlayerCombat combat;

    public static PlayerInventory Instance { get; private set; }

    /// <summary>Fired when something is added or removed.</summary>
    public event Action Changed;

    public PlayerCombat Combat => combat;

    private void Awake()
    {
        Instance = this;

        int count = Enum.GetValues(typeof(InventoryPage)).Length;
        pages = new List<ItemData>[count];
        for (int i = 0; i < count; i++) pages[i] = new List<ItemData>();
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void Start()
    {
        combat = GetComponent<PlayerCombat>();

        foreach (ItemData item in startingItems) Add(item);

        if (combat != null)
        {
            Add(combat.GetEquipped(GameAction.Action1));
            Add(combat.GetEquipped(GameAction.Action2));
            Add(combat.GetEquipped(GameAction.Action3));
        }

        if (enableMenu) InventoryMenu.Create(this);
    }

    // ---------------- Reading ----------------

    public IReadOnlyList<ItemData> GetItems(InventoryPage page)
    {
        return pages[(int)page];
    }

    public int GetCount(ItemData item)
    {
        int count;
        return item != null && counts.TryGetValue(item, out count) ? count : 0;
    }

    public bool Has(ItemData item)
    {
        return GetCount(item) > 0;
    }

    public bool IsPageFull(InventoryPage page)
    {
        return pages[(int)page].Count >= SlotsPerPage;
    }

    public bool IsPageFull(ItemCategory category)
    {
        return IsPageFull(ItemPages.PageOf(category));
    }

    public static string PageName(ItemCategory category)
    {
        return ItemPages.NameOf(ItemPages.PageOf(category));
    }

    public bool IsEquipped(ItemData item)
    {
        return combat != null && combat.IsEquipped(item);
    }

    private static int MaxStackOf(ItemData item)
    {
        ConsumableItem consumable = item as ConsumableItem;
        return consumable != null ? Mathf.Max(1, consumable.maxStack) : 1;
    }

    // ---------------- Adding and removing ----------------

    public bool CanAdd(ItemData item, int amount = 1)
    {
        if (item == null || amount <= 0) return false;

        int current = GetCount(item);
        if (current + amount > MaxStackOf(item)) return false;          // already owned (equipment) or stack full
        if (current == 0 && IsPageFull(item.category)) return false;    // needs a free slot
        return true;
    }

    public bool Add(ItemData item, int amount = 1)
    {
        if (!CanAdd(item, amount)) return false;

        if (!counts.ContainsKey(item))
        {
            counts[item] = 0;
            pages[(int)ItemPages.PageOf(item.category)].Add(item);
        }
        counts[item] += amount;

        Changed?.Invoke();
        return true;
    }

    /// <summary>Removes some of an item. If none are left, it's unequipped too.</summary>
    public bool Remove(ItemData item, int amount = 1)
    {
        if (item == null || amount <= 0 || GetCount(item) < amount) return false;

        counts[item] -= amount;

        if (counts[item] <= 0)
        {
            counts.Remove(item);
            pages[(int)ItemPages.PageOf(item.category)].Remove(item);

            GameAction action;
            if (combat != null && combat.TryGetEquippedAction(item, out action)) combat.Unequip(action);
        }

        Changed?.Invoke();
        return true;
    }

    /// <summary>
    /// Would giving away "remove" and receiving "add" fit? Used by trades, so swapping a full page's item for another works.
    /// </summary>
    public bool CanApply(List<ItemStack> remove, List<ItemStack> add)
    {
        Dictionary<ItemData, int> sim = new Dictionary<ItemData, int>(counts);

        if (remove != null)
        {
            foreach (ItemStack stack in remove)
            {
                if (stack == null || stack.item == null || stack.amount <= 0) continue;

                int have;
                if (!sim.TryGetValue(stack.item, out have) || have < stack.amount) return false;

                have -= stack.amount;
                if (have == 0) sim.Remove(stack.item);
                else sim[stack.item] = have;
            }
        }

        if (add != null)
        {
            foreach (ItemStack stack in add)
            {
                if (stack == null || stack.item == null || stack.amount <= 0) continue;

                int have;
                sim.TryGetValue(stack.item, out have);
                if (have + stack.amount > MaxStackOf(stack.item)) return false;
                sim[stack.item] = have + stack.amount;
            }
        }

        int[] perPage = new int[pages.Length];
        foreach (KeyValuePair<ItemData, int> pair in sim)
        {
            perPage[(int)ItemPages.PageOf(pair.Key.category)]++;
        }
        foreach (int used in perPage)
        {
            if (used > SlotsPerPage) return false;
        }
        return true;
    }
}