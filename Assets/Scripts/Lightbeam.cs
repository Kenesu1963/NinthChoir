using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// The beam itself. Walls and ground (solid things that aren't IDamageable) stop it; enemies don't.
/// It follows the player for its short life and damages everything inside it in regular ticks.
/// </summary>
public class LightBeam : MonoBehaviour
{
    private Transform owner;
    private Vector2 direction;
    private Vector2 originOffset;
    private float startDistance;
    private float maxLength;
    private float width;
    private float duration;
    private float tickInterval;
    private int damagePerTick;

    private float age;
    private float tickTimer;
    private float length;
    private SpriteRenderer sr;
    private readonly HashSet<IDamageable> hitThisTick = new HashSet<IDamageable>();

    public static LightBeam Spawn(Transform owner, Vector2 direction, Vector2 originOffset, float startDistance,
                                  float maxLength, float width, float duration, float tickInterval, int damagePerTick)
    {
        GameObject go = new GameObject("LightBeam");
        LightBeam beam = go.AddComponent<LightBeam>();
        beam.owner = owner;
        beam.direction = direction.normalized;
        beam.originOffset = originOffset;
        beam.startDistance = startDistance;
        beam.maxLength = maxLength;
        beam.width = width;
        beam.duration = duration;
        beam.tickInterval = Mathf.Max(0.02f, tickInterval);
        beam.damagePerTick = damagePerTick;

        beam.sr = go.AddComponent<SpriteRenderer>();
        beam.sr.sprite = DebugVisuals.White;
        beam.sr.sortingOrder = 22;

        beam.Refresh(0f); // place it and deal the first tick right away
        return beam;
    }

    private void Update()
    {
        if (owner == null) { Destroy(gameObject); return; }

        float dt = Time.deltaTime;
        age += dt;
        if (age >= duration) { Destroy(gameObject); return; }

        Refresh(dt);
    }

    private void Refresh(float dt)
    {
        Vector2 origin = (Vector2)owner.position + originOffset + direction * startDistance;
        length = MeasureLength(origin);

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.position = origin + direction * (length * 0.5f);
        transform.rotation = Quaternion.Euler(0f, 0f, angle);

        float flicker = 0.85f + 0.15f * Mathf.Sin(age * 70f);
        float fade = 1f - 0.5f * Mathf.Clamp01(age / duration);
        transform.localScale = new Vector3(Mathf.Max(length, 0.01f), width * flicker, 1f);
        sr.color = new Color(1f, 0.97f, 0.7f, 0.9f * fade);

        tickTimer -= dt;
        if (tickTimer <= 0f)
        {
            tickTimer = tickInterval;
            DealTick(origin, angle);
        }
    }

    // How far the beam reaches: until the first solid thing that isn't an enemy.
    private float MeasureLength(Vector2 origin)
    {
        float best = maxLength;

        foreach (RaycastHit2D hit in Physics2D.RaycastAll(origin, direction, maxLength))
        {
            Collider2D c = hit.collider;
            if (c == null || c.isTrigger) continue;
            if (owner != null && (c.transform == owner || c.transform.IsChildOf(owner))) continue;
            if (c.GetComponentInParent<IDamageable>() != null) continue; // enemies don't stop the beam

            if (hit.distance < best) best = hit.distance;
        }
        return best;
    }

    private void DealTick(Vector2 origin, float angle)
    {
        hitThisTick.Clear();

        Vector2 center = origin + direction * (length * 0.5f);
        foreach (Collider2D c in Physics2D.OverlapBoxAll(center, new Vector2(length, width), angle))
        {
            if (owner != null && (c.transform == owner || c.transform.IsChildOf(owner))) continue;

            IDamageable target = c.GetComponentInParent<IDamageable>();
            if (target != null && hitThisTick.Add(target))
            {
                target.TakeDamage(damagePerTick, direction);
            }
        }
    }
}