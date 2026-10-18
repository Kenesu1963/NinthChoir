using UnityEngine;

/// <summary>
/// Base class for every enemy: health, contact damage, hit flash, a short freeze when hit, death,
/// and optional respawning. Make a new kind of enemy by extending this class and filling in Tick
/// (see WalkerEnemy). If your enemy has its own state, reset it in OnRespawn.
///
/// Setup for any enemy: a sprite, a SOLID Box/Capsule Collider 2D (not a trigger, or the player's
/// projectiles will fly through it), and one enemy script. The Rigidbody 2D is added automatically.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public abstract class EnemyBase : MonoBehaviour, IDamageable
{
    [Header("Stats")]
    [SerializeField] protected int maxHealth = 3;
    [Tooltip("Damage dealt to the player when they touch this enemy. 0 = harmless to touch.")]
    [SerializeField] protected int contactDamage = 1;
    [Tooltip("Seconds the enemy freezes when it takes a hit.")]
    [SerializeField] protected float hitStunTime = 0.12f;
    [Tooltip("Aureus coins that jump out when it dies.")]
    [SerializeField] protected int coinsOnDeath = 0;

    [Header("Respawn")]
    [Tooltip("If ticked, the enemy comes back after it dies (with full health, at the spot where it started).")]
    [SerializeField] protected bool respawnable = false;
    [Tooltip("Seconds after dying before it comes back.")]
    [SerializeField] protected float respawnTime = 30f;
    [Tooltip("It waits to come back while the player is closer than this to its starting spot, so it never appears on top of you. 0 = comes back regardless.")]
    [SerializeField] protected float respawnPlayerDistance = 3f;

    protected Rigidbody2D rb;
    protected int health;

    private SpriteRenderer sprite;
    private SpriteRenderer[] renderers;
    private Collider2D[] colliders;
    private Color originalColor = Color.white;
    private float stunTimer;
    private float flashTimer;

    private Vector3 startPosition;
    private Vector3 startScale;
    private float respawnTimer;

    public int Health => health;
    public int MaxHealth => maxHealth;
    public int ContactDamage => contactDamage;
    public bool IsDead => health <= 0;

    /// <summary>Where the enemy started in the scene (and where it comes back to).</summary>
    protected Vector3 StartPosition => startPosition;

    /// <summary>The color the placeholder sprite gets when you add the script. Real art isn't touched.</summary>
    protected virtual Color PlaceholderColor => Color.white;

    protected virtual void Reset()
    {
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null && sr.color == Color.white) sr.color = PlaceholderColor;
    }

    protected virtual void Awake()
    {
        health = maxHealth;
        startPosition = transform.position;
        startScale = transform.localScale;

        // Enemies are moved by their scripts, not by gravity
        rb = GetComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.gravityScale = 0f;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;

        sprite = GetComponentInChildren<SpriteRenderer>();
        if (sprite != null) originalColor = sprite.color;

        renderers = GetComponentsInChildren<SpriteRenderer>();
        colliders = GetComponentsInChildren<Collider2D>();
    }

    protected virtual void Update()
    {
        if (IsDead)
        {
            if (respawnable) UpdateRespawn();
            return;
        }

        if (sprite == null) return;

        sprite.color = flashTimer > 0f ? new Color(1f, 0.3f, 0.3f, originalColor.a) : originalColor;
        if (flashTimer > 0f) flashTimer -= Time.deltaTime;
    }

    private void FixedUpdate()
    {
        if (IsDead) return;

        if (stunTimer > 0f)
        {
            stunTimer -= Time.fixedDeltaTime;
            return;
        }

        Tick(Time.fixedDeltaTime);
    }

    /// <summary>The enemy's behavior: move, shoot... Called every physics step while it's alive and not frozen.</summary>
    protected abstract void Tick(float dt);

    /// <summary>Called when the enemy comes back to life. Reset your own state here (patrol, timers...).</summary>
    protected virtual void OnRespawn() { }

    // ---------------- Taking damage ----------------

    public virtual void TakeDamage(int amount, Vector2 hitDirection)
    {
        if (IsDead || amount <= 0) return;

        health -= amount;
        flashTimer = 0.12f;
        stunTimer = hitStunTime;

        if (health <= 0) Die();
    }

    protected virtual void Die()
    {
        for (int i = 0; i < coinsOnDeath; i++)
        {
            Vector2 launch = new Vector2(Random.Range(-3f, 3f), Random.Range(5f, 8f));
            CoinPickup.Spawn(transform.position, 1, launch, transform);
        }

        if (respawnable)
        {
            // Not destroyed: it just disappears for a while
            respawnTimer = respawnTime;
            SetPresent(false);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // ---------------- Respawning ----------------

    private void UpdateRespawn()
    {
        respawnTimer -= Time.deltaTime;
        if (respawnTimer > 0f) return;

        // Don't pop up right next to the player. Keep waiting until they've moved away.
        PlayerStats player = PlayerStats.Instance;
        if (respawnPlayerDistance > 0f && player != null && !player.IsDead &&
            Vector2.Distance(player.transform.position, startPosition) < respawnPlayerDistance)
        {
            return;
        }

        Respawn();
    }

    private void Respawn()
    {
        health = maxHealth;
        stunTimer = 0f;
        flashTimer = 0f;

        transform.position = startPosition;
        transform.localScale = startScale; // undoes any left/right flipping
        rb.position = startPosition;

        SetPresent(true);
        if (sprite != null) sprite.color = originalColor;

        OnRespawn();
    }

    // Shows or hides the enemy and turns its collisions on or off
    private void SetPresent(bool present)
    {
        foreach (SpriteRenderer r in renderers) r.enabled = present;
        foreach (Collider2D c in colliders) c.enabled = present;
        rb.simulated = present;
    }

    // ---------------- Contact damage ----------------

    private void OnCollisionEnter2D(Collision2D collision) { TryHurtPlayer(collision.collider); }
    private void OnCollisionStay2D(Collision2D collision) { TryHurtPlayer(collision.collider); }
    private void OnTriggerEnter2D(Collider2D other) { TryHurtPlayer(other); }
    private void OnTriggerStay2D(Collider2D other) { TryHurtPlayer(other); }

    private void TryHurtPlayer(Collider2D other)
    {
        if (contactDamage <= 0 || IsDead) return;

        PlayerStats player = other.GetComponentInParent<PlayerStats>();
        if (player == null) return;

        Vector2 direction = (Vector2)player.transform.position - (Vector2)transform.position;
        if (direction.sqrMagnitude < 0.0001f) direction = Vector2.right;

        // The player has a short invulnerability after a hit, so touching every frame is fine
        player.TakeDamage(contactDamage, direction.normalized);
    }

    // ---------------- Helpers ----------------

    /// <summary>Flips the sprite to face left or right (the art is expected to face right).</summary>
    protected void FaceDirection(float x)
    {
        if (Mathf.Abs(x) < 0.01f) return;

        Vector3 scale = transform.localScale;
        scale.x = Mathf.Abs(scale.x) * (x > 0f ? 1f : -1f);
        transform.localScale = scale;
    }
}