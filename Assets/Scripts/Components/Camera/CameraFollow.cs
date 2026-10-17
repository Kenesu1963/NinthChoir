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

    /// <summary>Jumps straight to the player without smoothing (used after respawning).</summary>
    public void SnapToTarget()
    {
        if (target == null) return;

        Vector3 start = target.position + (Vector3)offset;
        transform.position = new Vector3(start.x, start.y, transform.position.z);
        followVelocity = Vector2.zero;
        currentLookOffset = 0f;
        lookVelocity = 0f;
    }

    private void LateUpdate()
    {
        if (target == null) return;
        if (Time.deltaTime <= 0f) return; // game is paused (inventory open)

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