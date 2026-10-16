using UnityEngine;

/// <summary>
/// Any item lying in the world. Press action key 1 (J) next to it to pick it up.
/// Chests create these automatically. You can also place one yourself: add this to an object and set Item.
/// </summary>
public class ItemPickup : Interactable
{
    [Header("Item")]
    [SerializeField] private ItemData item;

    private LootBody body;
    private Transform visual;

    public ItemData Item => item;

    // Can't be picked up while it's still flying out of a chest
    public override bool IsAvailable => body == null || body.IsLanded;

    private void Reset()
    {
        prompt = "Pick up";
    }

    private void Awake()
    {
        body = GetComponent<LootBody>();
        RefreshPrompt();
    }

    private void RefreshPrompt()
    {
        if (item != null) prompt = "Pick up " + item.itemName;
    }

    private void Update()
    {
        // A gentle float once it has landed
        if (visual != null && IsAvailable)
        {
            visual.localPosition = new Vector3(0f, 0.12f + Mathf.Sin(Time.time * 3f) * 0.05f, 0f);
        }
    }

    public override void Interact()
    {
        if (item == null) return;

        PlayerInventory inventory = PlayerInventory.Instance;
        if (inventory == null) return;

        string page = PlayerInventory.PageName(item.category);

        if (inventory.Has(item))
        {
            StatsHUD.Show("You already have the " + item.itemName + ".");
            return;
        }
        if (inventory.IsPageFull(item.category))
        {
            StatsHUD.Show("Your " + page + " page is full.");
            return;
        }

        inventory.Add(item);
        StatsHUD.Show("Picked up " + item.itemName + "   (" + page + ")");
        Destroy(gameObject);
    }

    // =====================================================================

    public static ItemPickup Spawn(ItemData item, Vector2 position, Vector2 launchVelocity, Transform ignoreRoot)
    {
        GameObject go = new GameObject("Pickup_" + item.itemName);
        go.transform.position = position;

        ItemPickup pickup = go.AddComponent<ItemPickup>();
        pickup.item = item;
        pickup.RefreshPrompt();

        // Visual: the item's icon if it has one, otherwise a colored diamond for its category
        GameObject v = new GameObject("Visual");
        v.transform.SetParent(go.transform, false);
        SpriteRenderer sr = v.AddComponent<SpriteRenderer>();
        sr.sortingOrder = 15;
        if (item.icon != null)
        {
            sr.sprite = item.icon;
            Vector3 size = item.icon.bounds.size;
            v.transform.localScale = Vector3.one * (0.7f / Mathf.Max(size.x, size.y, 0.01f));
        }
        else
        {
            sr.sprite = DebugVisuals.White;
            sr.color = CategoryColor(item.category);
            v.transform.localScale = new Vector3(0.45f, 0.45f, 1f);
            v.transform.localRotation = Quaternion.Euler(0f, 0f, 45f);
        }
        pickup.visual = v.transform;

        LootBody body = go.AddComponent<LootBody>();
        body.ignoreRoot = ignoreRoot;
        body.radius = 0.28f;
        pickup.body = body;
        body.Launch(launchVelocity);

        return pickup;
    }

    private static Color CategoryColor(ItemCategory category)
    {
        switch (category)
        {
            case ItemCategory.Weapon: return new Color(0.92f, 0.38f, 0.3f);
            case ItemCategory.Tool: return new Color(0.35f, 0.65f, 0.95f);
            default: return new Color(0.78f, 0.5f, 0.95f);
        }
    }
}