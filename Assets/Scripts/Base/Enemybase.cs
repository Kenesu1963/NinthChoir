using UnityEngine;

/// <summary>
/// Base class for every enemy: health, contact damage, hit flash, a short freeze when hit, and death.
/// Make a new kind of enemy by extending this class and filling in Tick (see WalkerEnemy).
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

    protected Rigidbody2D rb;
    protected int health;

    private SpriteRenderer sprite;
    private Color originalColor = Color.white;
    private float stunTimer;
    private float flashTimer;

    public int Health => health;
    public int MaxHealth => maxHealth;
    public int ContactDamage => contactDamage;
    public bool IsDead => health <= 0;

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

        // Enemies are moved by their scripts, not by gravity
        rb = GetComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.gravityScale = 0f;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;

        sprite = GetComponentInChildren<SpriteRenderer>();
        if (sprite != null) originalColor = sprite.color;
    }

    protected virtual void Update()
    {
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
        Destroy(gameObject);
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