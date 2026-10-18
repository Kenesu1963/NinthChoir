using UnityEngine;

/// <summary>Shooting helpers shared by the enemies that fire at the player.</summary>
public static class EnemyShooting
{
    /// <summary>Fires a fan of projectiles toward a target, centered on it.</summary>
    public static void FireFan(Transform owner, Vector2 origin, Vector2 toTarget, int count, float spreadAngle,
                               float muzzleDistance, float speed, float lifetime, int damage, float radius)
    {
        float baseAngle = Mathf.Atan2(toTarget.y, toTarget.x) * Mathf.Rad2Deg;
        count = Mathf.Max(1, count);

        for (int i = 0; i < count; i++)
        {
            float angle = (baseAngle + (i - (count - 1) / 2f) * spreadAngle) * Mathf.Deg2Rad;
            Vector2 direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));

            Projectile.Spawn(owner, origin + direction * muzzleDistance, direction, speed, lifetime,
                             damage, radius, false, true);
        }
    }

    /// <summary>True if no wall is between the two points.</summary>
    public static bool CanSee(Transform owner, Vector2 from, Vector2 target)
    {
        foreach (RaycastHit2D hit in Physics2D.LinecastAll(from, target))
        {
            Collider2D c = hit.collider;
            if (c == null || c.isTrigger) continue;
            if (c.transform == owner || c.transform.IsChildOf(owner)) continue;
            if (c.GetComponentInParent<IDamageable>() != null) continue; // the player or other enemies
            return false;
        }
        return true;
    }
}