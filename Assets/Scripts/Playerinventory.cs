using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Everything Maor owns, sorted by category: Weapons (Body), Tools (Mind), Incantations (Soul).
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

    private List<ItemData>[] itemsByCategory;
    private PlayerCombat combat;

    /// <summary>Fired when an item is added.</summary>
    public event Action Changed;

    public static PlayerInventory Instance { get; private set; }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void Awake()
    {
        Instance = this;
        int categories = Enum.GetValues(typeof(ItemCategory)).Length;
        itemsByCategory = new List<ItemData>[categories];
        for (int i = 0; i < categories; i++) itemsByCategory[i] = new List<ItemData>();
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

    public IReadOnlyList<ItemData> GetItems(ItemCategory category)
    {
        return itemsByCategory[(int)category];
    }

    /// <summary>Adds an item to the page of its category. Returns false if it's already owned or the page is full.</summary>
    public bool Add(ItemData item)
    {
        if (item == null) return false;

        List<ItemData> list = itemsByCategory[(int)item.category];
        if (list.Contains(item) || list.Count >= SlotsPerPage) return false;

        list.Add(item);
        Changed?.Invoke();
        return true;
    }

    public PlayerCombat Combat => combat;

    /// <summary>The name of the inventory page for a category.</summary>
    public static string PageName(ItemCategory category)
    {
        switch (category)
        {
            case ItemCategory.Weapon: return "Body";
            case ItemCategory.Tool: return "Mind";
            default: return "Soul";
        }
    }

    public bool Has(ItemData item)
    {
        return item != null && itemsByCategory[(int)item.category].Contains(item);
    }

    public bool IsPageFull(ItemCategory category)
    {
        return itemsByCategory[(int)category].Count >= SlotsPerPage;
    }

    public bool IsEquipped(ItemData item)
    {
        return combat != null && combat.IsEquipped(item);
    }
}