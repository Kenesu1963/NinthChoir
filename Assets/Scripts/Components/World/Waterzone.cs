using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A body of water. Put it on an object with a Box Collider 2D: the box IS the water (set its Size to the size of the pool).
/// While playing it draws a translucent blue filter over the whole box, with a wavy surface, foam and moving light lines.
/// The player swims inside it (see the Swimming settings on the Player Controller).
/// Tip: change the size with the Box Collider's Size and keep the object's Scale at 1.
/// </summary>
[RequireComponent(typeof(BoxCollider2D))]
public class WaterZone : MonoBehaviour
{
    private static readonly List<WaterZone> all = new List<WaterZone>();

    [Header("Colors")]
    [Tooltip("Color at the surface.")]
    [SerializeField] private Color surfaceColor = new Color(0.45f, 0.8f, 1f, 0.45f);
    [Tooltip("Color at the bottom. Make it darker and more opaque for deep water.")]
    [SerializeField] private Color deepColor = new Color(0.05f, 0.25f, 0.6f, 0.65f);
    [SerializeField] private Color foamColor = new Color(1f, 1f, 1f, 0.75f);

    [Header("Waves")]
    [SerializeField] private float waveHeight = 0.12f;
    [Tooltip("Distance between two wave crests.")]
    [SerializeField] private float waveLength = 2.5f;
    [SerializeField] private float waveSpeed = 1.5f;
    [Tooltip("Thin moving light lines inside the water. 0 = none.")]
    [SerializeField] private int lightLines = 3;
    [SerializeField] private float foamThickness = 0.12f;

    [Header("Thickness (changes how it feels to move)")]
    [Tooltip("1 = water. Below 1 = thin and slippery: faster, quicker to speed up, glides further, slower to kill a dive. " +
             "Above 1 = thick like mud or syrup: slower, slow to speed up, stops quickly, kills a dive almost at once. " +
             "It also slows enemies more.")]
    [Range(0.2f, 4f)] [SerializeField] private float thickness = 1f;
    [Tooltip("A constant vertical push in units per second when the player isn't swimming up or down. " +
             "Positive floats you up, negative sinks you, 0 = neutral (stay where you are).")]
    [SerializeField] private float drift = 0f;

    [Header("Drawing")]
    [Tooltip("Higher draws in front. The default draws the water over the player and enemies.")]
    [SerializeField] private int sortingOrder = 12;

    private BoxCollider2D box;
    private Mesh mesh;
    private Material material;

    private readonly List<Vector3> vertices = new List<Vector3>();
    private readonly List<Color> colors = new List<Color>();
    private readonly List<int> triangles = new List<int>();

    /// <summary>The height of the water's surface in the world.</summary>
    public float SurfaceY
    {
        get
        {
            if (box == null) return transform.position.y;
            Vector2 min, max;
            GetBounds(out min, out max);
            return max.y;
        }
    }

    // How thickness changes the movement of the player and of enemies
    public float Thickness => Mathf.Max(0.05f, thickness);
    public float SwimSpeedFactor => Mathf.Pow(Thickness, -0.7f);     // thick = slower top speed
    public float AccelerationFactor => Mathf.Pow(Thickness, -0.5f);  // thick = slower to get going
    public float GlideFactor => Thickness;                           // thick = stops faster, thin = glides further
    public float DashFactor => Mathf.Pow(Thickness, -0.5f);          // thick = shorter, slower dash
    public float Drift => drift;

    /// <summary>How much an enemy's speed is multiplied in this water. baseMultiplier is the enemy's own setting (0.5 = half speed in normal water).</summary>
    public float EnemySpeedFactor(float baseMultiplier)
    {
        return Mathf.Clamp(Mathf.Pow(Mathf.Clamp(baseMultiplier, 0.05f, 1f), Thickness), 0.05f, 1f);
    }

    public static int Count => all.Count;

    /// <summary>For the on-screen debug line: the nearest water and its rectangle.</summary>
    public static string Describe(Vector2 point)
    {
        if (all.Count == 0) return "no Water Zone is active in the scene";

        WaterZone nearest = null;
        float best = float.MaxValue;
        foreach (WaterZone zone in all)
        {
            if (zone.box == null) continue;
            Vector2 min, max;
            zone.GetBounds(out min, out max);
            Vector2 closest = new Vector2(Mathf.Clamp(point.x, min.x, max.x), Mathf.Clamp(point.y, min.y, max.y));
            float d = (closest - point).sqrMagnitude;
            if (d < best) { best = d; nearest = zone; }
        }
        if (nearest == null) return "none usable";

        Vector2 lo, hi;
        nearest.GetBounds(out lo, out hi);
        return nearest.name + " x[" + lo.x.ToString("0.0") + " to " + hi.x.ToString("0.0") + "]  y[" +
               lo.y.ToString("0.0") + " to " + hi.y.ToString("0.0") + "]" + (nearest.box.enabled ? "" : "  (collider disabled!)");
    }

    /// <summary>The water that contains this point (or null).</summary>
    public static WaterZone Find(Vector2 point)
    {
        foreach (WaterZone zone in all)
        {
            if (zone.Contains(point)) return zone;
        }
        return null;
    }

    /// <summary>Is the point inside the water? A plain box test from the object's position and the collider's size.</summary>
    public bool Contains(Vector2 point)
    {
        if (box == null || !box.enabled) return false;

        Vector2 min, max;
        GetBounds(out min, out max);
        return point.x >= min.x && point.x <= max.x && point.y >= min.y && point.y <= max.y;
    }

    // The water's rectangle in the world, worked out directly (it doesn't wait for the physics engine to update)
    private void GetBounds(out Vector2 min, out Vector2 max)
    {
        Vector2 center = transform.TransformPoint(box.offset);
        Vector3 scale = transform.lossyScale;
        Vector2 half = new Vector2(box.size.x * Mathf.Abs(scale.x), box.size.y * Mathf.Abs(scale.y)) * 0.5f;
        min = center - half;
        max = center + half;
    }

    private void Reset()
    {
        BoxCollider2D b = GetComponent<BoxCollider2D>();
        if (b != null)
        {
            b.isTrigger = true;
            b.size = new Vector2(8f, 4f);
        }
    }

    private void Awake()
    {
        box = GetComponent<BoxCollider2D>();
        box.isTrigger = true;

        // If you started from a placeholder square, hide it: the water draws itself
        SpriteRenderer placeholder = GetComponent<SpriteRenderer>();
        if (placeholder != null) placeholder.enabled = false;

        CreateVisual();
    }

    private void OnEnable()
    {
        all.Add(this);
    }

    private void OnDisable()
    {
        all.Remove(this);
    }

    private void OnDestroy()
    {
        if (mesh != null) Destroy(mesh);
        if (material != null) Destroy(material);
    }

    private void Update()
    {
        if (mesh != null) BuildMesh(Time.time * Mathf.Pow(Thickness, -0.5f)); // thick water ripples slowly
    }

    // =====================================================================
    //  Drawing
    // =====================================================================

    private void CreateVisual()
    {
        GameObject visual = new GameObject("WaterVisual");
        visual.transform.SetParent(transform, false);

        MeshFilter filter = visual.AddComponent<MeshFilter>();
        MeshRenderer meshRenderer = visual.AddComponent<MeshRenderer>();

        mesh = new Mesh();
        mesh.name = "WaterMesh";
        mesh.MarkDynamic();
        filter.sharedMesh = mesh;

        Shader shader = Shader.Find("Sprites/Default"); // reads vertex colors and supports transparency
        material = new Material(shader);
        material.mainTexture = Texture2D.whiteTexture;

        meshRenderer.sharedMaterial = material;
        meshRenderer.sortingOrder = sortingOrder;
        meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        meshRenderer.receiveShadows = false;
    }

    private void BuildMesh(float time)
    {
        vertices.Clear();
        colors.Clear();
        triangles.Clear();

        Vector2 size = box.size;
        Vector2 offset = box.offset;
        float left = offset.x - size.x * 0.5f;
        float right = offset.x + size.x * 0.5f;
        float top = offset.y + size.y * 0.5f;
        float bottom = offset.y - size.y * 0.5f;

        // Waves are measured in world units, even if the object is scaled
        Vector3 scale = transform.lossyScale;
        float sx = Mathf.Max(0.0001f, Mathf.Abs(scale.x));
        float sy = Mathf.Max(0.0001f, Mathf.Abs(scale.y));

        int segments = Mathf.Clamp(Mathf.CeilToInt(size.x * sx / 0.25f), 4, 300);
        float k = Mathf.PI * 2f / Mathf.Max(0.1f, waveLength);

        // --- The body of the water, with a wavy top edge ---
        for (int i = 0; i <= segments; i++)
        {
            float x = Mathf.Lerp(left, right, i / (float)segments);
            vertices.Add(new Vector3(x, top + Wave(x, sx, sy, k, time), 0f));
            colors.Add(surfaceColor);
            vertices.Add(new Vector3(x, bottom, 0f));
            colors.Add(deepColor);
        }
        AddStrip(0, segments);

        // --- Foam along the surface, fading downward ---
        int foamStart = vertices.Count;
        Color foamFade = new Color(foamColor.r, foamColor.g, foamColor.b, 0f);
        for (int i = 0; i <= segments; i++)
        {
            float x = Mathf.Lerp(left, right, i / (float)segments);
            float y = top + Wave(x, sx, sy, k, time);
            vertices.Add(new Vector3(x, y, 0f));
            colors.Add(foamColor);
            vertices.Add(new Vector3(x, y - foamThickness / sy, 0f));
            colors.Add(foamFade);
        }
        AddStrip(foamStart, segments);

        // --- Thin wavy light lines inside the water ---
        Color lineColor = new Color(1f, 1f, 1f, 0.12f);
        for (int line = 0; line < lightLines; line++)
        {
            float fraction = (line + 1f) / (lightLines + 1f);
            float baseY = Mathf.Lerp(top, bottom, fraction);
            int start = vertices.Count;

            for (int i = 0; i <= segments; i++)
            {
                float x = Mathf.Lerp(left, right, i / (float)segments);
                float wobble = Mathf.Sin(x * sx * k * 0.8f + time * waveSpeed * 0.7f + line * 1.9f) * waveHeight * 0.8f / sy;
                float y = baseY + wobble;

                vertices.Add(new Vector3(x, y + 0.05f / sy, 0f));
                colors.Add(lineColor);
                vertices.Add(new Vector3(x, y - 0.05f / sy, 0f));
                colors.Add(lineColor);
            }
            AddStrip(start, segments);
        }

        mesh.Clear();
        mesh.SetVertices(vertices);
        mesh.SetColors(colors);
        mesh.SetTriangles(triangles, 0);
        mesh.RecalculateBounds();
    }

    private float Wave(float localX, float sx, float sy, float k, float time)
    {
        float worldX = localX * sx;
        float wave = Mathf.Sin(worldX * k + time * waveSpeed) * 0.6f +
                     Mathf.Sin(worldX * k * 2.3f - time * waveSpeed * 1.3f) * 0.4f;
        return wave * waveHeight / sy;
    }

    // Triangles for a strip made of columns: each column is two vertices (top, bottom)
    private void AddStrip(int start, int segments)
    {
        for (int i = 0; i < segments; i++)
        {
            int a = start + i * 2;
            triangles.Add(a);
            triangles.Add(a + 1);
            triangles.Add(a + 2);
            triangles.Add(a + 1);
            triangles.Add(a + 3);
            triangles.Add(a + 2);
        }
    }

    // In the editor the water shows as a blue box (the waves appear when you press Play)
    private void OnDrawGizmos()
    {
        BoxCollider2D b = box != null ? box : GetComponent<BoxCollider2D>();
        if (b == null) return;

        Gizmos.color = new Color(0.2f, 0.5f, 1f, 0.2f);
        Gizmos.DrawCube(b.bounds.center, b.bounds.size);
        Gizmos.color = new Color(0.3f, 0.6f, 1f, 0.9f);
        Gizmos.DrawWireCube(b.bounds.center, b.bounds.size);
    }
}