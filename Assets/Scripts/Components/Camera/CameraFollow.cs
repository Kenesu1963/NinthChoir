using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Transform target;

    [Header("Follow")]
    [SerializeField] private Vector2 offset = new Vector2(0f, 1.5f); // camera sits slightly above the player
    [SerializeField] private float followSmoothTime = 0.15f;         // lower = tighter follow

    [Header("Look down when falling")]
    [SerializeField] private float fallLookDownAmount = 3f;          // how far the camera moves down
    [SerializeField] private float fallTimeBeforeLookDown = 0.4f;    // seconds of continuous falling before the camera starts looking down
    [SerializeField] private float lookDownSmoothTime = 0.35f;       // how slowly it moves down
    [SerializeField] private float lookResetSmoothTime = 0.2f;       // how fast it comes back up

    private Rigidbody2D targetRb;
    private Vector2 followVelocity;
    private float currentLookOffset;
    private float lookVelocity;

    // Smooth trip to a new place (after a teleport)
    private bool travelling;
    private Vector3 travelFrom;
    private float travelElapsed;
    private float travelDuration = 1f;

    /// <summary>True while the camera is gliding to a new place. The player can't act meanwhile.</summary>
    public static bool IsTraveling { get; private set; }
    private float fallTimer;
    private const float MinFallSpeed = 0.5f; // anything slower than this doesn't count as falling

    private void Start()
    {
        if (target != null)
        {
            targetRb = target.GetComponent<Rigidbody2D>();
            // Start directly on the player so the camera doesn't slide in at the beginning
            Vector3 start = target.position + (Vector3)offset;
            transform.position = new Vector3(start.x, start.y, transform.position.z);
        }
    }

    /// <summary>Glides from where the camera is now to the player over the given time (used after a teleport).</summary>
    public void TravelToTarget(float duration)
    {
        if (target == null || duration <= 0f)
        {
            SnapToTarget();
            return;
        }

        travelFrom = transform.position;
        travelDuration = duration;
        travelElapsed = 0f;
        travelling = true;
        IsTraveling = true;
        followVelocity = Vector2.zero;
        currentLookOffset = 0f;
        lookVelocity = 0f;
    }

    /// <summary>Jumps straight to the player without smoothing (used after respawning).</summary>
    public void SnapToTarget()
    {
        if (target == null) return;

        Vector3 start = target.position + (Vector3)offset;
        transform.position = new Vector3(start.x, start.y, transform.position.z);
        followVelocity = Vector2.zero;
        currentLookOffset = 0f;
        lookVelocity = 0f;
        travelling = false;
        IsTraveling = false;
    }

    private void OnDestroy()
    {
        IsTraveling = false;
    }

    private void LateUpdate()
    {
        if (target == null) return;
        if (Time.deltaTime <= 0f) return; // game is paused (inventory open)

        // --- Gliding to a new place ---
        if (travelling)
        {
            travelElapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, travelElapsed / travelDuration);
            Vector3 goal = target.position + (Vector3)offset;
            Vector3 position = Vector3.Lerp(travelFrom, goal, t);
            transform.position = new Vector3(position.x, position.y, transform.position.z);

            if (travelElapsed >= travelDuration)
            {
                travelling = false;
                IsTraveling = false;
                followVelocity = Vector2.zero;
            }
            return;
        }

        // --- Look down while falling ---
        bool fallingNow = targetRb != null && targetRb.linearVelocity.y < -MinFallSpeed; // use .velocity on Unity 2022 or older
        if (fallingNow) fallTimer += Time.deltaTime;
        else fallTimer = 0f;

        bool isFalling = fallTimer >= fallTimeBeforeLookDown;
        float desiredLook = isFalling ? -fallLookDownAmount : 0f;
        float smooth = isFalling ? lookDownSmoothTime : lookResetSmoothTime;
        currentLookOffset = Mathf.SmoothDamp(currentLookOffset, desiredLook, ref lookVelocity, smooth);

        // --- Follow the player ---
        Vector2 desired = (Vector2)target.position + offset + new Vector2(0f, currentLookOffset);
        Vector2 smoothed = Vector2.SmoothDamp(transform.position, desired, ref followVelocity, followSmoothTime);

        transform.position = new Vector3(smoothed.x, smoothed.y, transform.position.z);
    }
}