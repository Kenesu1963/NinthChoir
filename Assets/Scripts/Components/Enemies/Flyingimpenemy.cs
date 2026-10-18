using UnityEngine;

public enum ImpMovement { BetweenTwoPoints, RadiusAroundPoint }

/// <summary>
/// A flying enemy. It either flies back and forth between two points, or wanders randomly
/// inside a circle around a point. It hovers with a gentle bob and hurts on touch.
/// Optionally it notices the player entering its area: it stops for a moment, then flies at the player.
/// </summary>
public class FlyingImpEnemy : EnemyBase
{
    [Header("Movement")]
    [SerializeField] private ImpMovement movement = ImpMovement.RadiusAroundPoint;
    [SerializeField] private float speed = 2.5f;
    [Tooltip("Seconds it rests at each point before moving on.")]
    [SerializeField] private float waitAtTargets = 0.3f;

    [Header("Between two points")]
    [Tooltip("Optional. Make them separate objects, not children of the enemy.")]
    [SerializeField] private Transform pointA;
    [SerializeField] private Transform pointB;
    [Tooltip("Used when no points are set: how far it flies to each side of where it starts.")]
    [SerializeField] private float patrolDistance = 3f;

    [Header("Around a point")]
    [Tooltip("The center it flies around. Leave empty to use the spot where it starts. Not a child of the enemy.")]
    [SerializeField] private Transform centerPoint;
    [SerializeField] private float radius = 3f;

    [Header("Aggro")]
    [Tooltip("When ticked, the imp notices you when you enter its area, pauses, then flies at you. " +
             "Around-a-point mode: the area is the circle. Between-two-points mode: the area is a corridor around the path.")]
    [SerializeField] private bool aggroWhenPlayerEntersArea = false;
    [Tooltip("Seconds it freezes (and shakes) after noticing you, before it comes at you.")]
    [SerializeField] private float aggroPause = 1f;
    [SerializeField] private float chaseSpeed = 4f;
    [Tooltip("Between-two-points mode only: how far from the path counts as its area.")]
    [SerializeField] private float pathAreaDistance = 3f;
    [Tooltip("It gives up and goes back to flying around when you get this much farther away than its area.")]
    [SerializeField] private float loseInterestDistance = 2f;

    [Header("Hover")]
    [SerializeField] private float bobHeight = 0.2f;
    [SerializeField] private float bobSpeed = 3f;

    private enum ImpState { Patrol, Alert, Chase }

    private ImpState state = ImpState.Patrol;
    private float alertTimer;

    private PatrolPath path;
    private Vector2 pathA;
    private Vector2 pathB;
    private Vector2 center;
    private Vector2 basePosition; // where it is, without the bobbing
    private Vector2 target;
    private float waitTimer;

    protected override Color PlaceholderColor => new Color(0.7f, 0.4f, 0.95f, 1f);

    protected override void Awake()
    {
        base.Awake();

        Vector2 start = transform.position;
        basePosition = start;

        pathA = pointA != null ? (Vector2)pointA.position : start + Vector2.left * patrolDistance;
        pathB = pointB != null ? (Vector2)pointB.position : start + Vector2.right * patrolDistance;
        center = centerPoint != null ? (Vector2)centerPoint.position : start;

        if (movement == ImpMovement.BetweenTwoPoints) path = new PatrolPath(pathA, pathB);
        else target = RandomPointInCircle();
    }

    protected override void OnRespawn()
    {
        state = ImpState.Patrol;
        alertTimer = 0f;
        waitTimer = 0f;
        basePosition = StartPosition;

        if (movement == ImpMovement.BetweenTwoPoints) path = new PatrolPath(pathA, pathB);
        else target = RandomPointInCircle();
    }

    protected override void Tick(float dt)
    {
        PlayerStats player = PlayerStats.Instance;
        UpdateAggro(player, dt);

        Vector2 direction = Vector2.zero;
        Vector2 shake = Vector2.zero;

        switch (state)
        {
            case ImpState.Alert:
                // Frozen in place, shaking, staring at the player
                shake = Vector2.right * Mathf.Sin(Time.time * 70f) * 0.06f;
                direction = (Vector2)player.transform.position - basePosition;
                break;

            case ImpState.Chase:
                Vector2 playerPosition = player.transform.position;
                direction = playerPosition - basePosition;
                basePosition = Vector2.MoveTowards(basePosition, playerPosition, chaseSpeed * dt);
                break;

            default:
                basePosition = movement == ImpMovement.BetweenTwoPoints
                    ? path.Step(basePosition, speed, dt, waitAtTargets, out direction)
                    : StepAroundCenter(dt, out direction);
                break;
        }

        float bob = Mathf.Sin(Time.time * bobSpeed) * bobHeight;
        rb.MovePosition(basePosition + Vector2.up * bob + shake);
        FaceDirection(direction.x);
    }

    // ---------------- Aggro ----------------

    private void UpdateAggro(PlayerStats player, float dt)
    {
        if (!aggroWhenPlayerEntersArea || player == null || player.IsDead)
        {
            state = ImpState.Patrol;
            return;
        }

        Vector2 playerPosition = player.transform.position;

        switch (state)
        {
            case ImpState.Patrol:
                if (IsInArea(playerPosition, 0f))
                {
                    state = ImpState.Alert;
                    alertTimer = aggroPause;
                }
                break;

            case ImpState.Alert:
                alertTimer -= dt;
                if (!IsInArea(playerPosition, loseInterestDistance)) state = ImpState.Patrol;
                else if (alertTimer <= 0f) state = ImpState.Chase;
                break;

            case ImpState.Chase:
                if (!IsInArea(playerPosition, loseInterestDistance)) state = ImpState.Patrol;
                break;
        }
    }

    // Is the point inside the imp's area? "extra" makes the area bigger (used so it doesn't give up too easily).
    private bool IsInArea(Vector2 point, float extra)
    {
        if (movement == ImpMovement.RadiusAroundPoint)
        {
            return Vector2.Distance(point, center) <= radius + extra;
        }
        return DistanceToSegment(point, pathA, pathB) <= pathAreaDistance + extra;
    }

    private static float DistanceToSegment(Vector2 point, Vector2 a, Vector2 b)
    {
        Vector2 ab = b - a;
        float lengthSquared = ab.sqrMagnitude;
        if (lengthSquared < 0.0001f) return Vector2.Distance(point, a);

        float t = Mathf.Clamp01(Vector2.Dot(point - a, ab) / lengthSquared);
        return Vector2.Distance(point, a + ab * t);
    }

    // ---------------- Wandering ----------------

    // Flies to a random spot inside the circle, rests, then picks another
    private Vector2 StepAroundCenter(float dt, out Vector2 direction)
    {
        direction = Vector2.zero;

        if (waitTimer > 0f)
        {
            waitTimer -= dt;
            return basePosition;
        }

        Vector2 toTarget = target - basePosition;
        float distance = toTarget.magnitude;
        float step = speed * dt;

        if (distance <= step)
        {
            Vector2 arrived = target;
            target = RandomPointInCircle();
            waitTimer = waitAtTargets;
            direction = toTarget;
            return arrived;
        }

        direction = toTarget / distance;
        return basePosition + direction * step;
    }

    private Vector2 RandomPointInCircle()
    {
        return center + Random.insideUnitCircle * radius;
    }

    // ---------------- Scene view ----------------

    protected virtual void OnDrawGizmosSelected()
    {
        Vector2 start = transform.position;
        Vector2 a = pointA != null ? (Vector2)pointA.position : start + Vector2.left * patrolDistance;
        Vector2 b = pointB != null ? (Vector2)pointB.position : start + Vector2.right * patrolDistance;

        Gizmos.color = new Color(0.8f, 0.5f, 1f, 0.9f);

        if (movement == ImpMovement.BetweenTwoPoints)
        {
            Gizmos.DrawLine(a, b);
            Gizmos.DrawWireSphere(a, 0.2f);
            Gizmos.DrawWireSphere(b, 0.2f);

            if (aggroWhenPlayerEntersArea)
            {
                // The corridor around the path where it notices you (orange)
                Gizmos.color = new Color(1f, 0.6f, 0.2f, 0.7f);
                Vector2 along = b - a;
                if (along.sqrMagnitude > 0.0001f)
                {
                    Vector2 side = new Vector2(-along.y, along.x).normalized * pathAreaDistance;
                    Gizmos.DrawLine(a + side, b + side);
                    Gizmos.DrawLine(a - side, b - side);
                }
                Gizmos.DrawWireSphere(a, pathAreaDistance);
                Gizmos.DrawWireSphere(b, pathAreaDistance);
            }
        }
        else
        {
            Vector2 c = Application.isPlaying ? center : (centerPoint != null ? (Vector2)centerPoint.position : start);
            Gizmos.DrawWireSphere(c, radius);
        }
    }
}