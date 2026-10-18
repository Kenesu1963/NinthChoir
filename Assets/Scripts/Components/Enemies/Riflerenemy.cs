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

    protected override void OnRespawn()
    {
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
                        && (!needsLineOfSight || EnemyShooting.CanSee(transform, rb.position, playerPosition));

        timer -= dt;
        if (timer > 0f) return;

        if (!canShoot)
        {
            timer = 0f; // ready, waiting for the player to come into view
            return;
        }

        EnemyShooting.FireFan(transform, rb.position, toPlayer, projectilesPerVolley, spreadAngle,
                              muzzleDistance, projectileSpeed, projectileLifetime, projectileDamage, projectileRadius);
        timer = fireInterval;
    }

    private void OnDrawGizmosSelected()
    {
        if (detectionRange <= 0f) return;
        Gizmos.color = new Color(1f, 0.6f, 0.2f, 0.6f);
        Gizmos.DrawWireSphere(transform.position, detectionRange);
    }
}