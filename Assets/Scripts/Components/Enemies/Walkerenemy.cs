using UnityEngine;

/// <summary>Walks back and forth between two points. Hurts on touch.</summary>
public class WalkerEnemy : EnemyBase
{
    [Header("Patrol")]
    [Tooltip("Optional. If both points are set, the walker goes back and forth between them. Make them separate objects, not children of the enemy.")]
    [SerializeField] private Transform pointA;
    [SerializeField] private Transform pointB;
    [Tooltip("Used when no points are set: how far it walks to each side of where it starts.")]
    [SerializeField] private float patrolDistance = 3f;
    [SerializeField] private float speed = 2f;
    [SerializeField] private float waitAtEnds = 0.5f;

    private PatrolPath path;

    protected override Color PlaceholderColor => new Color(0.4f, 0.8f, 0.4f, 1f);

    protected override void Awake()
    {
        base.Awake();
        BuildPath();
    }

    protected override void OnRespawn()
    {
        BuildPath(); // starts the patrol over
    }

    private void BuildPath()
    {
        Vector2 start = StartPosition;
        Vector2 a = pointA != null ? (Vector2)pointA.position : start + Vector2.left * patrolDistance;
        Vector2 b = pointB != null ? (Vector2)pointB.position : start + Vector2.right * patrolDistance;
        path = new PatrolPath(a, b);
    }

    protected override void Tick(float dt)
    {
        Vector2 direction;
        Vector2 next = path.Step(rb.position, speed, dt, waitAtEnds, out direction);
        rb.MovePosition(next);
        FaceDirection(direction.x);
    }

    private void OnDrawGizmosSelected()
    {
        Vector2 start = transform.position;
        Vector2 a = pointA != null ? (Vector2)pointA.position : start + Vector2.left * patrolDistance;
        Vector2 b = pointB != null ? (Vector2)pointB.position : start + Vector2.right * patrolDistance;

        Gizmos.color = new Color(0.4f, 0.9f, 0.4f, 0.9f);
        Gizmos.DrawLine(a, b);
        Gizmos.DrawWireSphere(a, 0.2f);
        Gizmos.DrawWireSphere(b, 0.2f);
    }
}