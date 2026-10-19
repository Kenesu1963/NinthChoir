using UnityEngine;

/// <summary>
/// A round bubble that rises through the water and wobbles from side to side. If the player touches it, it pops:
/// the player's air is fully restored, and the player is paralyzed for a moment.
/// Bubbles are created by a Bubble Generator. You never add this yourself.
/// </summary>
public class Bubble : MonoBehaviour
{
    private static Sprite roundSprite;

    private float radius;
    private float riseSpeed;
    private float wobbleAmount;
    private float wobbleSpeed;
    private float lifetime;
    private float paralysisSeconds;
    private bool popAtSurface;

    private float age;
    private float phase;
    private bool wasInWater;
    private Vector2 basePosition;

    private PlayerStats player;
    private PlayerController controller;

    public static Bubble Spawn(Vector2 position, float radius, float riseSpeed, float wobbleAmount, float wobbleSpeed,
                               float lifetime, float paralysisSeconds, bool popAtSurface, Color color, int sortingOrder)
    {
        GameObject go = new GameObject("Bubble");
        go.transform.position = position;

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = GetRoundSprite();
        sr.color = color;
        sr.sortingOrder = sortingOrder;

        Bubble bubble = go.AddComponent<Bubble>();
        bubble.radius = radius;
        bubble.riseSpeed = riseSpeed;
        bubble.wobbleAmount = wobbleAmount;
        bubble.wobbleSpeed = wobbleSpeed;
        bubble.lifetime = lifetime;
        bubble.paralysisSeconds = paralysisSeconds;
        bubble.popAtSurface = popAtSurface;
        bubble.basePosition = position;
        bubble.phase = Random.Range(0f, Mathf.PI * 2f);

        bubble.player = PlayerStats.Instance;
        if (bubble.player != null) bubble.controller = bubble.player.GetComponent<PlayerController>();

        go.transform.localScale = Vector3.one * radius * 2f;
        return bubble;
    }

    private void Update()
    {
        float dt = Time.deltaTime;
        if (dt <= 0f) return; // paused

        age += dt;
        if (age >= lifetime)
        {
            Destroy(gameObject);
            return;
        }

        // Rise, with a gentle wobble and a slight breathing in size
        basePosition += Vector2.up * riseSpeed * dt;
        Vector2 wobble = Vector2.right * Mathf.Sin(age * wobbleSpeed + phase) * wobbleAmount;
        transform.position = basePosition + wobble;

        float pulse = 1f + 0.05f * Mathf.Sin(age * 5f + phase);
        transform.localScale = Vector3.one * radius * 2f * pulse;

        // Bubbles pop when they reach the surface
        bool inWater = WaterZone.Find(transform.position) != null;
        if (inWater) wasInWater = true;
        else if (popAtSurface && wasInWater)
        {
            Destroy(gameObject);
            return;
        }

        CheckTouch();
    }

    private void CheckTouch()
    {
        if (player == null || player.IsDead) return;

        Bounds body = controller != null ? controller.BodyBounds : new Bounds(player.transform.position, Vector3.one);
        Vector2 center = transform.position;

        // The point of the player's body closest to the bubble's center
        Vector2 closest = new Vector2(Mathf.Clamp(center.x, body.min.x, body.max.x),
                                      Mathf.Clamp(center.y, body.min.y, body.max.y));

        if ((closest - center).sqrMagnitude > radius * radius) return;

        Pop();
    }

    private void Pop()
    {
        player.RefillBreath(); // the underwater timer is restored completely
        bool paralyzed = player.Paralyze(paralysisSeconds);

        StatsHUD.Show(paralyzed ? "Air restored, but the bubble stunned you!" : "Air restored.");
        Destroy(gameObject);
    }

    // A soft round sprite: faint inside, a bright rim and a small highlight
    private static Sprite GetRoundSprite()
    {
        if (roundSprite != null) return roundSprite;

        const int size = 64;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Bilinear;

        float center = (size - 1) / 2f;
        float edge = size / 2f - 1f;
        Color[] pixels = new Color[size * size];

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = x - center;
                float dy = y - center;
                float d = Mathf.Sqrt(dx * dx + dy * dy) / edge; // 0 at the center, 1 at the rim

                float alpha = 0f;
                if (d <= 1f)
                {
                    float rim = Mathf.SmoothStep(0.65f, 1f, d);
                    alpha = Mathf.Lerp(0.18f, 0.9f, rim);
                    alpha *= 1f - Mathf.SmoothStep(0.93f, 1f, d); // smooth outer edge

                    // Highlight in the upper left
                    float hx = x - size * 0.34f;
                    float hy = y - size * 0.68f;
                    float highlight = 1f - Mathf.Clamp01(Mathf.Sqrt(hx * hx + hy * hy) / (size * 0.11f));
                    alpha = Mathf.Max(alpha, highlight * 0.95f);
                }

                pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
            }
        }

        texture.SetPixels(pixels);
        texture.Apply();

        roundSprite = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        return roundSprite;
    }
}