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
    [SerializeField] private float fallSpeedThreshold = 6f;          // fall speed needed to trigger it
    [SerializeField] private float lookDownSmoothTime = 0.35f;       // how slowly it moves down
    [SerializeField] private float lookResetSmoothTime = 0.2f;       // how fast it comes back up

    private Rigidbody2D targetRb;
    private Vector2 followVelocity;
    private float currentLookOffset;
    private float lookVelocity;

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

    private void LateUpdate()
    {
        if (target == null) return;

        // --- Look down while falling ---
        bool isFalling = targetRb != null && targetRb.linearVelocity.y < -fallSpeedThreshold; // use .velocity on Unity 2022 or older
        float desiredLook = isFalling ? -fallLookDownAmount : 0f;
        float smooth = isFalling ? lookDownSmoothTime : lookResetSmoothTime;
        currentLookOffset = Mathf.SmoothDamp(currentLookOffset, desiredLook, ref lookVelocity, smooth);

        // --- Follow the player ---
        Vector2 desired = (Vector2)target.position + offset + new Vector2(0f, currentLookOffset);
        Vector2 smoothed = Vector2.SmoothDamp(transform.position, desired, ref followVelocity, followSmoothTime);

        transform.position = new Vector3(smoothed.x, smoothed.y, transform.position.z);
    }
}