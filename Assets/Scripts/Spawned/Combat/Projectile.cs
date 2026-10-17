using UnityEngine;

/// <summary>Flies in a straight line, damages the first thing it hits and disappears.</summary>
public class Projectile : MonoBehaviour
{
    public bool PlayerOwned { get; private set; } = true;

    private Transform owner;
    private Vector2 direction;
    private float speed;
    private int damage;
    private float radius;
    private float lifetime;
    private float lifeTimer;
    private SpriteRenderer sr;

    public static Projectile Spawn(Transform owner, Vector2 position, Vector2 direction, float speed,
                                   float lifetime, int damage, float radius, bool playerOwned, bool showVisual)
    {
        GameObject go = new GameObject("Projectile");
        go.transform.position = position;

        Projectile p = go.AddComponent<Projectile>();
        p.owner = owner;
        p.direction = direction.normalized;
        p.speed = speed;
        p.damage = damage;
        p.radius = radius;
        p.lifetime = lifetime;
        p.lifeTimer = lifetime;
        p.PlayerOwned = playerOwned;

        // Trigger collider so blocks can detect it
        CircleCollider2D col = go.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = radius;

        if (showVisual)
        {
            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            p.sr = sr;
            sr.sprite = DebugVisuals.White;
            sr.color = playerOwned ? new Color(1f, 0.85f, 0.1f, 1f) : new Color(1f, 0.3f, 0.2f, 1f); // enemy shots are red
            sr.sortingOrder = 20;
            go.transform.localScale = new Vector3(radius * 2f, radius * 2f, 1f);
        }

        return p;
    }

    private void FixedUpdate()
    {
        lifeTimer -= Time.fixedDeltaTime;
        if (lifeTimer <= 0f)
        {
            Destroy(gameObject);
            return;
        }

        float step = speed * Time.fixedDeltaTime;
        RaycastHit2D[] hits = Physics2D.CircleCastAll(transform.position, radius, direction, step);

        foreach (RaycastHit2D hit in hits)
        {
            Collider2D c = hit.collider;
            if (c == null || c.isTrigger) continue;
            if (owner != null && (c.transform == owner || c.transform.IsChildOf(owner))) continue;

            IDamageable target = c.GetComponentInParent<IDamageable>();
            // Each side only hurts the other: enemy shots fly through other enemies,
            // and the player's shots never hurt the player
            if (target != null)
            {
                bool targetIsPlayer = target is PlayerStats;
                if (targetIsPlayer == PlayerOwned) continue;
                target.TakeDamage(damage, direction);
            }

            Destroy(gameObject); // hit something solid (enemy, wall, floor)
            return;
        }

        transform.position += (Vector3)(direction * step);
    }

    /// <summary>
    /// A perfect parry: the projectile now belongs to the player and flies back at whoever fired it,
    /// with exactly the damage it had.
    /// </summary>
    public void Parry(Transform newOwner, float speedMultiplier)
    {
        Vector2 newDirection = -direction;
        if (owner != null)
        {
            Vector2 toShooter = (Vector2)owner.position - (Vector2)transform.position;
            if (toShooter.sqrMagnitude > 0.01f) newDirection = toShooter.normalized;
        }

        owner = newOwner;
        direction = newDirection;
        speed *= Mathf.Max(0.1f, speedMultiplier);
        PlayerOwned = true;
        lifeTimer = lifetime;

        if (sr != null) sr.color = new Color(1f, 0.85f, 0.1f, 1f); // it turns yellow: it's yours now
    }

    public void Block()
    {
        Destroy(gameObject);
    }
}