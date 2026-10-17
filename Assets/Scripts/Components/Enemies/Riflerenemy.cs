using UnityEngine;

/// <summary>
/// Stands still and fires a volley of projectiles (3 by default) toward the player at regular intervals.
/// Every number is adjustable: how often it fires, how many shots, how wide the fan is, how fast and how hard they hit.
/// </summary>
public class RiflerEnemy : EnemyBase
{
    [Header("Firing")]
    [Tooltip("Seconds between volleys.")]
    [SerializeField] private float fireInterval = 2f;
    [Tooltip("Seconds before the very first volley, once it can see you.")]
    [SerializeField] private float firstShotDelay = 1f;
    [SerializeField] private int projectilesPerVolley = 3;
    [Tooltip("Degrees between neighboring shots in the fan.")]
    [SerializeField] private float spreadAngle = 12f;

    [Header("Projectiles")]
    [SerializeField] private float projectileSpeed = 7f;
    [SerializeField] private int projectileDamage = 1;
    [SerializeField] private float projectileLifetime = 4f;
    [SerializeField] private float projectileRadius = 0.18f;
    [Tooltip("How far from the enemy's center the shots appear.")]
    [SerializeField] private float muzzleDistance = 0.7f;

    [Header("Targeting")]
    [Tooltip("It only fires when you're this close. 0 = any distance.")]
    [SerializeField] private float detectionRange = 12f;
    [Tooltip("If ticked, walls between it and you stop it from firing.")]
    [SerializeField] private bool needsLineOfSight = true;

    private float timer;

    protected override Color PlaceholderColor => new Color(0.95f, 0.55f, 0.2f, 1f);

    protected override void Awake()
    {
        base.Awake();
        timer = firstShotDelay;
    }

    protected override void Tick(float dt)
    {
        PlayerStats player = PlayerStats.Instance;
        if (player == null || player.IsDead) return;

        Vector2 playerPosition = player.transform.position;
        Vector2 toPlayer = playerPosition - rb.position;
        FaceDirection(toPlayer.x);

        bool canShoot = (detectionRange <= 0f || toPlayer.magnitude <= detectionRange)
                        && (!needsLineOfSight || CanSee(playerPosition));

        timer -= dt;
        if (timer > 0f) return;

        if (!canShoot)
        {
            timer = 0f; // ready, waiting for the player to come into view
            return;
        }

        Fire(toPlayer);
        timer = fireInterval;
    }

    // A fan of shots centered on the player
    private void Fire(Vector2 toPlayer)
    {
        float baseAngle = Mathf.Atan2(toPlayer.y, toPlayer.x) * Mathf.Rad2Deg;
        int count = Mathf.Max(1, projectilesPerVolley);

        for (int i = 0; i < count; i++)
        {
            float angle = (baseAngle + (i - (count - 1) / 2f) * spreadAngle) * Mathf.Deg2Rad;
            Vector2 direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            Vector2 spawn = rb.position + direction * muzzleDistance;

            Projectile.Spawn(transform, spawn, direction, projectileSpeed, projectileLifetime,
                             projectileDamage, projectileRadius, false, true);
        }
    }

    // True if no wall is between this enemy and the target
    private bool CanSee(Vector2 target)
    {
        foreach (RaycastHit2D hit in Physics2D.LinecastAll(rb.position, target))
        {
            Collider2D c = hit.collider;
            if (c == null || c.isTrigger) continue;
            if (c.transform == transform || c.transform.IsChildOf(transform)) continue;
            if (c.GetComponentInParent<IDamageable>() != null) continue; // the player or other enemies
            return false;
        }
        return true;
    }

    private void OnDrawGizmosSelected()
    {
        if (detectionRange <= 0f) return;
        Gizmos.color = new Color(1f, 0.6f, 0.2f, 0.6f);
        Gizmos.DrawWireSphere(transform.position, detectionRange);
    }
}