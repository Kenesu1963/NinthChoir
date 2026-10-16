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
        p.PlayerOwned = playerOwned;

        // Trigger collider so blocks can detect it
        CircleCollider2D col = go.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = radius;

        if (showVisual)
        {
            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = DebugVisuals.White;
            sr.color = new Color(1f, 0.85f, 0.1f, 1f);
            sr.sortingOrder = 20;
            go.transform.localScale = new Vector3(radius * 2f, radius * 2f, 1f);
        }

        Destroy(go, lifetime);
        return p;
    }

    private void FixedUpdate()
    {
        float step = speed * Time.fixedDeltaTime;
        RaycastHit2D[] hits = Physics2D.CircleCastAll(transform.position, radius, direction, step);

        foreach (RaycastHit2D hit in hits)
        {
            Collider2D c = hit.collider;
            if (c == null || c.isTrigger) continue;
            if (owner != null && (c.transform == owner || c.transform.IsChildOf(owner))) continue;

            IDamageable target = c.GetComponentInParent<IDamageable>();
            if (target != null) target.TakeDamage(damage, direction);

            Destroy(gameObject); // hit something solid (enemy, wall, floor)
            return;
        }

        transform.position += (Vector3)(direction * step);
    }

    public void Block()
    {
        Destroy(gameObject);
    }
}