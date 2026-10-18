using UnityEngine;

/// <summary>
/// A flying enemy that shoots. It moves exactly like the Flying Imp (between two points, or around a point,
/// with the optional aggro) and fires volleys at the player like the Rifler. It has every option of both.
/// Use this component instead of Flying Imp or Rifler on the object.
/// </summary>
public class FlyingRiflerEnemy : FlyingImpEnemy
{
    [Header("Firing")]
    [Tooltip("Seconds between volleys.")]
    [SerializeField] private float fireInterval = 2.5f;
    [Tooltip("Seconds before the very first volley, once it can see you.")]
    [SerializeField] private float firstShotDelay = 1f;
    [SerializeField] private int projectilesPerVolley = 3;
    [Tooltip("Degrees between neighboring shots in the fan.")]
    [SerializeField] private float spreadAngle = 12f;

    [Header("Projectiles")]
    [SerializeField] private float projectileSpeed = 6f;
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

    private float shotTimer;

    protected override Color PlaceholderColor => new Color(0.95f, 0.35f, 0.5f, 1f);

    protected override void Awake()
    {
        base.Awake();
        shotTimer = firstShotDelay;
    }

    protected override void OnRespawn()
    {
        base.OnRespawn();
        shotTimer = firstShotDelay;
    }

    protected override void Tick(float dt)
    {
        base.Tick(dt); // the imp's flying and aggro
        UpdateShooting(dt);
    }

    private void UpdateShooting(float dt)
    {
        PlayerStats player = PlayerStats.Instance;
        if (player == null || player.IsDead) return;

        Vector2 playerPosition = player.transform.position;
        Vector2 toPlayer = playerPosition - rb.position;

        bool canShoot = (detectionRange <= 0f || toPlayer.magnitude <= detectionRange)
                        && (!needsLineOfSight || EnemyShooting.CanSee(transform, rb.position, playerPosition));

        shotTimer -= dt;
        if (shotTimer > 0f) return;

        if (!canShoot)
        {
            shotTimer = 0f; // ready, waiting for the player to come into view
            return;
        }

        EnemyShooting.FireFan(transform, rb.position, toPlayer, projectilesPerVolley, spreadAngle,
                              muzzleDistance, projectileSpeed, projectileLifetime, projectileDamage, projectileRadius);
        shotTimer = fireInterval;
    }

    protected override void OnDrawGizmosSelected()
    {
        base.OnDrawGizmosSelected(); // the imp's path or circle

        if (detectionRange <= 0f) return;
        Gizmos.color = new Color(1f, 0.6f, 0.2f, 0.6f);
        Gizmos.DrawWireSphere(transform.position, detectionRange);
    }
}