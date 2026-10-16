using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Creates a plain white sprite so hitboxes and projectiles are visible without any art.</summary>
public static class DebugVisuals
{
    private static Sprite whiteSprite;

    public static Sprite White
    {
        get
        {
            if (whiteSprite == null)
            {
                Texture2D tex = Texture2D.whiteTexture;
                whiteSprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height),
                                            new Vector2(0.5f, 0.5f), tex.width);
            }
            return whiteSprite;
        }
    }
}

/// <summary>
/// A box that exists for a short time (attack) or while an item is held (block).
/// The box's X axis points along its direction, so size.x is the length and size.y the thickness.
/// </summary>
public class Hitbox : MonoBehaviour
{
    public enum Kind { Attack, Block }

    private Kind kind;
    private Transform owner;
    private Vector2 direction;
    private Vector2 size;
    private float distance;
    private Vector2 originOffset; // shifts where the box starts (used when crouching)
    private int damage;
    private Action<Vector2> onHit;
    private bool notifiedHit;
    private readonly HashSet<IDamageable> alreadyHit = new HashSet<IDamageable>();

    /// <param name="onHit">Called once per swing the first time something is hit (used for recoil).</param>
    public static Hitbox Spawn(Kind kind, Transform owner, Vector2 direction, Vector2 size,
                               float distance, float lifetime, int damage, bool showVisual,
                               Vector2 originOffset = default, Action<Vector2> onHit = null)
    {
        GameObject go = new GameObject(kind == Kind.Attack ? "AttackHitbox" : "BlockBox");
        Hitbox hb = go.AddComponent<Hitbox>();
        hb.kind = kind;
        hb.owner = owner;
        hb.onHit = onHit;
        hb.size = size;
        hb.distance = distance;
        hb.originOffset = originOffset;
        hb.damage = damage;
        hb.SetDirection(direction);

        if (showVisual)
        {
            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = DebugVisuals.White;
            sr.sortingOrder = 20;
            sr.color = kind == Kind.Attack ? new Color(1f, 0.2f, 0.2f, 0.55f)
                                           : new Color(0.2f, 0.6f, 1f, 0.55f);
        }

        if (lifetime > 0f) Destroy(go, lifetime);
        hb.Scan();
        return hb;
    }

    public void SetOriginOffset(Vector2 newOffset)
    {
        originOffset = newOffset;
        UpdateTransform();
    }

    public void SetDirection(Vector2 newDirection)
    {
        direction = newDirection.normalized;
        UpdateTransform();
    }

    private void UpdateTransform()
    {
        if (owner == null) return;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.position = owner.position + (Vector3)(originOffset + direction * distance);
        transform.rotation = Quaternion.Euler(0f, 0f, angle);
        transform.localScale = new Vector3(size.x, size.y, 1f);
    }

    private void Update()
    {
        if (owner == null) { Destroy(gameObject); return; }
        UpdateTransform(); // follow the player while active
    }

    private void FixedUpdate()
    {
        Scan();
    }

    private void Scan()
    {
        float angle = transform.eulerAngles.z;
        Collider2D[] hits = Physics2D.OverlapBoxAll(transform.position, size, angle);

        foreach (Collider2D hit in hits)
        {
            if (owner != null && (hit.transform == owner || hit.transform.IsChildOf(owner))) continue;

            if (kind == Kind.Attack)
            {
                IDamageable target = hit.GetComponentInParent<IDamageable>();
                if (target != null && alreadyHit.Add(target))
                {
                    target.TakeDamage(damage, direction);
                    if (onHit != null && !notifiedHit)
                    {
                        notifiedHit = true;
                        onHit(direction);
                    }
                }
            }
            else // Block: stops enemy projectiles
            {
                Projectile p = hit.GetComponent<Projectile>();
                if (p != null && !p.PlayerOwned) p.Block();
            }
        }
    }
}