using UnityEngine;

/// <summary>
/// Makes something fly in an arc, bounce on the ground and settle. No Rigidbody, so loot never
/// pushes the player around, and it needs no physics layers to be set up.
/// </summary>
public class LootBody : MonoBehaviour
{
    public float gravity = 28f;
    public float radius = 0.15f;
    public float bounciness = 0.45f;
    public Transform ignoreRoot; // e.g. the chest the loot came out of

    public bool IsLanded { get; private set; }

    private Vector2 velocity;
    private float airTime;
    private ContactFilter2D filter;
    private readonly RaycastHit2D[] hits = new RaycastHit2D[8];

    private void Awake()
    {
        filter = new ContactFilter2D();
        filter.useTriggers = false;
    }

    public void Launch(Vector2 launchVelocity)
    {
        velocity = launchVelocity;
        IsLanded = false;
        airTime = 0f;
    }

    private void Update()
    {
        if (IsLanded) return;

        float dt = Time.deltaTime;
        if (dt <= 0f) return; // paused

        airTime += dt;
        if (airTime > 6f) { IsLanded = true; return; } // safety: never fly forever

        velocity.y -= gravity * dt;

        Vector2 position = transform.position;
        Vector2 move = velocity * dt;
        float distance = move.magnitude;

        if (distance > 0.0001f)
        {
            int count = Physics2D.CircleCast(position, radius, move / distance, filter, hits, distance);
            for (int i = 0; i < count; i++)
            {
                RaycastHit2D hit = hits[i];
                if (hit.collider == null || IsIgnored(hit.collider)) continue;
                if (Vector2.Dot(velocity, hit.normal) >= 0f) continue; // already moving away from it

                // Stop just before the surface and bounce off it
                transform.position = hit.centroid + hit.normal * 0.01f;
                velocity = Vector2.Reflect(velocity, hit.normal) * bounciness;

                if (hit.normal.y > 0.5f)
                {
                    velocity.x *= 0.7f; // friction on the floor
                    if (Mathf.Abs(velocity.y) < 2f)
                    {
                        velocity = Vector2.zero;
                        IsLanded = true;
                    }
                }
                return;
            }
        }

        transform.position = position + move;
    }

    private bool IsIgnored(Collider2D c)
    {
        if (ignoreRoot != null && c.transform.IsChildOf(ignoreRoot)) return true;
        if (c.GetComponentInParent<IDamageable>() != null) return true; // the player and enemies
        return false;
    }
}