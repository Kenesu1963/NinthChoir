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

        if (!inventory.CanAdd(item, 1))
        {
            if (inventory.Has(item))
            {
                StatsHUD.Show(item is ConsumableItem
                    ? "You can't carry any more " + item.itemName + "."
                    : "You already have the " + item.itemName + ".");
            }
            else
            {
                StatsHUD.Show("Your " + page + " page is full.");
            }
            return;
        }

        inventory.Add(item, 1);
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

        // Visual: the item's icon if it has one, otherwise a diamond in its page color (red, yellow or blue)
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
            sr.color = ItemPages.ColorOf(item.category);
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
}