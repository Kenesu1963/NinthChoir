using UnityEngine;

/// <summary>Walks back and forth between two points, resting a moment at each end.</summary>
public class PatrolPath
{
    private readonly Vector2 a;
    private readonly Vector2 b;
    private bool headingToB = true;
    private float waitTimer;

    public PatrolPath(Vector2 a, Vector2 b)
    {
        this.a = a;
        this.b = b;
    }

    /// <summary>Returns the next position. direction is the way it's moving (zero while resting).</summary>
    public Vector2 Step(Vector2 current, float speed, float dt, float waitTime, out Vector2 direction)
    {
        direction = Vector2.zero;

        if (waitTimer > 0f)
        {
            waitTimer -= dt;
            return current;
        }

        Vector2 target = headingToB ? b : a;
        Vector2 toTarget = target - current;
        float distance = toTarget.magnitude;
        float step = speed * dt;

        if (distance <= step)
        {
            // Arrived: turn around after a short rest
            headingToB = !headingToB;
            waitTimer = waitTime;
            direction = toTarget;
            return target;
        }

        direction = toTarget / distance;
        return current + direction * step;
    }
}