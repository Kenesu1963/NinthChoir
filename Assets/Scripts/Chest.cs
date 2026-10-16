using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A loot chest you place anywhere. Press action key 1 (J) next to it to open it:
/// it pops, and coins and items jump out one after another.
/// Put ANY item in the Items list (items must be assets, see Items -> Create Default Item Assets).
/// </summary>
public class Chest : Interactable
{
    [Header("Loot")]
    [Tooltip("Items that come out of the chest. Any item works: weapons, tools or incantations.")]
    [SerializeField] private List<ItemData> items = new List<ItemData>();
    [SerializeField] private int coins = 5;           // how many coins jump out
    [SerializeField] private int aureusPerCoin = 1;   // how much each coin is worth

    [Header("Animation")]
    [SerializeField] private float dropInterval = 0.08f;  // seconds between things coming out
    [SerializeField] private float launchSpeedUp = 8f;
    [SerializeField] private float launchSpeedSide = 3.5f;
    [SerializeField] private float spawnHeight = 0.6f;    // how far above the chest's center they start

    [Header("Placeholder colors")]
    [Tooltip("Tints the chest sprite so the placeholder square looks like a chest. Untick when you have real art.")]
    [SerializeField] private bool applyColors = true;
    [SerializeField] private Color closedColor = new Color(0.85f, 0.62f, 0.2f, 1f);
    [SerializeField] private Color openedColor = new Color(0.5f, 0.38f, 0.2f, 1f);

    private bool opened;
    private SpriteRenderer sr;

    public override bool IsAvailable => !opened;

    private void Reset()
    {
        prompt = "Open";
    }

    private void Awake()
    {
        sr = GetComponentInChildren<SpriteRenderer>();
        if (sr != null && applyColors) sr.color = closedColor;
    }

    public override void Interact()
    {
        if (opened) return;
        opened = true;
        StartCoroutine(OpenRoutine());
    }

    private IEnumerator OpenRoutine()
    {
        if (sr != null && applyColors) sr.color = openedColor;
        StartCoroutine(Punch());

        Vector2 origin = (Vector2)transform.position + Vector2.up * spawnHeight;
        int coinsLeft = Mathf.Max(0, coins);
        int itemIndex = 0;
        int count = 0;

        // Items and coins come out mixed together: roughly one item for every two coins
        while (coinsLeft > 0 || itemIndex < items.Count)
        {
            bool dropItem = itemIndex < items.Count && (coinsLeft == 0 || count % 3 == 0);

            if (dropItem)
            {
                ItemData item = items[itemIndex++];
                if (item != null) ItemPickup.Spawn(item, origin, RandomLaunch(0.8f), transform);
            }
            else
            {
                CoinPickup.Spawn(origin, aureusPerCoin, RandomLaunch(1f), transform);
                coinsLeft--;
            }

            count++;
            yield return new WaitForSeconds(dropInterval);
        }
    }

    // The chest hops and settles back
    private IEnumerator Punch()
    {
        Vector3 baseScale = transform.localScale;
        const float duration = 0.25f;
        float t = 0f;

        while (t < duration)
        {
            t += Time.deltaTime;
            float k = Mathf.Sin(Mathf.Clamp01(t / duration) * Mathf.PI);
            transform.localScale = new Vector3(baseScale.x * (1f + 0.15f * k), baseScale.y * (1f - 0.1f * k), baseScale.z);
            yield return null;
        }
        transform.localScale = baseScale;
    }

    private Vector2 RandomLaunch(float spread)
    {
        float x = Random.Range(-launchSpeedSide, launchSpeedSide) * spread;
        float y = Random.Range(launchSpeedUp * 0.75f, launchSpeedUp * 1.15f);
        return new Vector2(x, y);
    }
}