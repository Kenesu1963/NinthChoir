using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Makes round bubbles that rise through the water. Touching a bubble restores the player's air completely,
/// but paralyzes the player for a moment (0.5 seconds by default).
/// Put it on an empty object inside (or under) a Water Zone. The bubbles appear at its position, spread across Spawn Width.
/// </summary>
public class BubbleGenerator : MonoBehaviour
{
    [Header("Spawning")]
    [Tooltip("Seconds between bubbles.")]
    [SerializeField] private float spawnInterval = 2f;
    [Tooltip("The interval changes randomly by up to this many seconds, so the bubbles don't look mechanical.")]
    [SerializeField] private float intervalVariation = 0.5f;
    [SerializeField] private int bubblesPerBurst = 1;
    [Tooltip("Bubbles appear anywhere along this width, centered on the generator.")]
    [SerializeField] private float spawnWidth = 1f;
    [Tooltip("It stops making bubbles while this many of its bubbles are still around.")]
    [SerializeField] private int maxBubbles = 8;
    [Tooltip("It only makes bubbles while the player is within this distance. 0 = always.")]
    [SerializeField] private float activeRange = 25f;

    [Header("Bubbles")]
    [SerializeField] private float bubbleRadius = 0.4f;
    [Tooltip("Each bubble's size changes randomly by up to this much.")]
    [SerializeField] private float sizeVariation = 0.15f;
    [SerializeField] private float riseSpeed = 1.5f;
    [Tooltip("How far a bubble wobbles from side to side.")]
    [SerializeField] private float wobbleAmount = 0.25f;
    [SerializeField] private float wobbleSpeed = 2f;
    [Tooltip("A bubble pops by itself after this many seconds.")]
    [SerializeField] private float lifetime = 15f;
    [Tooltip("Bubbles pop when they reach the surface of the water.")]
    [SerializeField] private bool popAtSurface = true;
    [SerializeField] private Color color = new Color(0.7f, 0.95f, 1f, 0.9f);
    [Tooltip("Higher draws in front. 13 is just in front of the water.")]
    [SerializeField] private int sortingOrder = 13;

    [Header("When the player touches a bubble")]
    [Tooltip("How long the player is paralyzed. The air is always restored completely.")]
    [SerializeField] private float paralysisSeconds = 0.5f;

    private readonly List<Bubble> alive = new List<Bubble>();
    private float timer;

    private void Awake()
    {
        // If you started from a placeholder square, hide it: only the bubbles are drawn
        SpriteRenderer placeholder = GetComponent<SpriteRenderer>();
        if (placeholder != null) placeholder.enabled = false;

        timer = Random.Range(0f, spawnInterval);
    }

    private void Update()
    {
        float dt = Time.deltaTime;
        if (dt <= 0f) return;

        timer -= dt;
        if (timer > 0f) return;

        timer = Mathf.Max(0.1f, spawnInterval + Random.Range(-intervalVariation, intervalVariation));

        if (activeRange > 0f)
        {
            PlayerStats player = PlayerStats.Instance;
            if (player == null) return;
            if (Vector2.Distance(player.transform.position, transform.position) > activeRange) return;
        }

        alive.RemoveAll(b => b == null);
        for (int i = 0; i < bubblesPerBurst && alive.Count < maxBubbles; i++)
        {
            SpawnBubble();
        }
    }

    private void SpawnBubble()
    {
        float x = Random.Range(-spawnWidth * 0.5f, spawnWidth * 0.5f);
        Vector2 position = (Vector2)transform.position + Vector2.right * x;
        float radius = Mathf.Max(0.1f, bubbleRadius + Random.Range(-sizeVariation, sizeVariation));

        alive.Add(Bubble.Spawn(position, radius, riseSpeed, wobbleAmount, wobbleSpeed, lifetime,
                               paralysisSeconds, popAtSurface, color, sortingOrder));
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(0.7f, 0.95f, 1f, 0.9f);
        Vector3 center = transform.position;
        Gizmos.DrawLine(center + Vector3.left * spawnWidth * 0.5f, center + Vector3.right * spawnWidth * 0.5f);
        Gizmos.DrawWireSphere(center, bubbleRadius);
        Gizmos.DrawLine(center, center + Vector3.up * 1.5f);
    }
}