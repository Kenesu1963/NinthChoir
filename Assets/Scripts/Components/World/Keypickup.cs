using UnityEngine;

public enum KeyCollectMode { Touch, Interact }

/// <summary>
/// A key (or gem, crystal, any collectible) placed in the level. Collecting it adds one to the count of its Key Id,
/// which Key Gates can require. Use the same Key Id on several pickups to make a set (e.g. three "Crystal"s).
/// </summary>
public class KeyPickup : Interactable
{
    [Header("Key")]
    [Tooltip("The name Key Gates look for, e.g. \"RedKey\". Pickups with the same Id add up.")]
    [SerializeField] private string keyId = "Key";
    [Tooltip("The name shown in messages, e.g. \"Red Key\".")]
    [SerializeField] private string displayName = "Key";
    [SerializeField] private int amount = 1;

    [Header("Collecting")]
    [Tooltip("Touch: collected by walking into it. Interact: press action key 1 next to it.")]
    [SerializeField] private KeyCollectMode collectMode = KeyCollectMode.Touch;
    [SerializeField] private float touchRadius = 0.8f;

    [Header("Placeholder")]
    [Tooltip("Tints the sprite gold. Untick when you have real art.")]
    [SerializeField] private bool applyColor = true;

    private bool collected;

    // In Touch mode there's no prompt: it just gets picked up
    public override bool IsAvailable => !collected && collectMode == KeyCollectMode.Interact;

    private void Reset()
    {
        prompt = "Pick up";
    }

    private void Awake()
    {
        prompt = "Pick up " + displayName;

        if (applyColor)
        {
            SpriteRenderer sr = GetComponentInChildren<SpriteRenderer>();
            if (sr != null) sr.color = new Color(1f, 0.85f, 0.3f, 1f);
        }
    }

    private void Update()
    {
        if (collected || collectMode != KeyCollectMode.Touch) return;

        PlayerStats player = PlayerStats.Instance;
        if (player == null || player.IsDead) return;

        float distance = Vector2.Distance(player.transform.position, transform.position);
        if (distance <= touchRadius) Collect();
    }

    public override void Interact()
    {
        Collect();
    }

    private void Collect()
    {
        if (collected) return;
        collected = true;

        LevelKeys.Instance.Add(keyId, amount);
        StatsHUD.Show("Got " + displayName + (amount > 1 ? " x" + amount : ""));
        Destroy(gameObject);
    }
}