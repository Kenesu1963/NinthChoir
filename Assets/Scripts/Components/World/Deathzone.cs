using UnityEngine;

/// <summary>
/// Anything that kills the player on touch: a death plane under the level, spikes, lava...
/// Put this on an object with a Collider2D (a Box Collider 2D works well). The collider becomes a trigger automatically.
/// To make a death plane, stretch a wide, thin box below the level.
/// </summary>
public class DeathZone : MonoBehaviour
{
    private void Reset()
    {
        // Runs when the component is added: make it a trigger and tint the placeholder sprite red
        Collider2D zone = GetComponent<Collider2D>();
        if (zone != null) zone.isTrigger = true;

        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null) sr.color = new Color(1f, 0.15f, 0.15f, 0.4f);
    }

    private void Awake()
    {
        Collider2D zone = GetComponent<Collider2D>();
        if (zone == null)
        {
            Debug.LogWarning("DeathZone on '" + name + "' needs a Collider2D (for example a Box Collider 2D).", this);
            return;
        }
        zone.isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        TryKill(other);
    }

    // Also while staying inside, in case the player is already in the zone when it matters
    private void OnTriggerStay2D(Collider2D other)
    {
        TryKill(other);
    }

    private void TryKill(Collider2D other)
    {
        PlayerStats stats = other.GetComponentInParent<PlayerStats>();
        if (stats != null) stats.Kill();
    }

    // A red outline in the Scene view so you can always see where the zone is
    private void OnDrawGizmos()
    {
        Collider2D zone = GetComponent<Collider2D>();
        if (zone == null) return;

        Gizmos.color = new Color(1f, 0.1f, 0.1f, 0.9f);
        Gizmos.DrawWireCube(zone.bounds.center, zone.bounds.size);
    }
}