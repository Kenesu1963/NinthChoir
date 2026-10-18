using UnityEngine;

public enum TeleportMode { Instant, CameraFollows }

/// <summary>
/// A trigger that moves the player to another spot in the same scene.
/// Put it on an object with a Collider2D (a Box Collider 2D works well): it becomes a trigger automatically.
/// Make a two-way portal by placing a second teleporter at the destination that points back.
///
///   Instant         the camera cuts to the new place at once.
///   CameraFollows   the player appears at the new place and the camera glides there (the player waits meanwhile).
///
/// Activation: by default you just walk into it. Tick "Activate With Interact Key" to have to stand inside it
/// and press action key 1 (J) instead.
/// </summary>
public class Teleporter : MonoBehaviour
{
    [Header("Destination")]
    [Tooltip("An empty object marking where the player appears.")]
    [SerializeField] private Transform destination;
    [Tooltip("Shifts the arrival spot a little.")]
    [SerializeField] private Vector2 offset = Vector2.zero;

    [Header("Activation")]
    [Tooltip("Off: it activates when you walk into it.  On: you have to be inside it and press action key 1 (J).")]
    [SerializeField] private bool activateWithInteract = false;
    [Tooltip("The word shown in the \"[J] ...\" prompt when Activate With Interact Key is on.")]
    [SerializeField] private string interactPrompt = "Enter";

    [Header("Camera")]
    [SerializeField] private TeleportMode mode = TeleportMode.Instant;
    [Tooltip("CameraFollows only: seconds the camera takes to glide to the new place.")]
    [SerializeField] private float cameraTravelTime = 1f;

    [Header("Safety")]
    [Tooltip("After any teleport, teleporters are ignored for this long. Stops two portals from sending you back and forth.")]
    [SerializeField] private float blockSeconds = 0.75f;

    private static float blockedUntil;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        blockedUntil = 0f;
    }

    private void Reset()
    {
        // Runs when the component is added: make it a trigger and tint the placeholder sprite cyan
        Collider2D zone = GetComponent<Collider2D>();
        if (zone != null) zone.isTrigger = true;

        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null) sr.color = new Color(0.2f, 0.9f, 1f, 0.4f);
    }

    private void Awake()
    {
        Collider2D zone = GetComponent<Collider2D>();
        if (zone == null)
        {
            Debug.LogWarning("Teleporter on '" + name + "' needs a Collider2D (for example a Box Collider 2D).", this);
            return;
        }
        zone.isTrigger = true;

        if (activateWithInteract) TriggerInteract.Attach(gameObject, zone, interactPrompt, UseWithInteractKey);
    }

    // Walking into it (only when Activate With Interact Key is off)
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (activateWithInteract) return;

        PlayerStats player = other.GetComponentInParent<PlayerStats>();
        if (player != null) TryActivate(player);
    }

    // Pressing the interact key while inside it (only when Activate With Interact Key is on)
    private void UseWithInteractKey()
    {
        PlayerStats player = PlayerStats.Instance;
        if (player != null) TryActivate(player);
    }

    private void TryActivate(PlayerStats player)
    {
        if (Time.time < blockedUntil) return;
        if (player.IsDead || InputLock.IsLocked) return;

        // A Key Gate on this object keeps it locked until its requirements are collected
        KeyGate gate = GetComponent<KeyGate>();
        if (gate != null && !gate.IsOpen)
        {
            gate.NotifyLocked();
            return;
        }

        if (destination == null)
        {
            Debug.LogWarning("Teleporter on '" + name + "' has no Destination.", this);
            return;
        }

        Teleport(player);
    }

    private void Teleport(PlayerStats player)
    {
        Vector2 position = (Vector2)destination.position + offset;

        PlayerController controller = player.GetComponent<PlayerController>();
        if (controller != null) controller.TeleportTo(position);
        else player.transform.position = position;

        CameraFollow cam = FindFirstObjectByType<CameraFollow>();
        if (cam != null)
        {
            if (mode == TeleportMode.Instant) cam.SnapToTarget();
            else cam.TravelToTarget(cameraTravelTime);
        }

        blockedUntil = Time.time + blockSeconds;
    }

    // Scene view: the trigger area, a line to the destination and a marker where you arrive
    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(0.2f, 0.9f, 1f, 0.9f);

        Collider2D zone = GetComponent<Collider2D>();
        if (zone != null) Gizmos.DrawWireCube(zone.bounds.center, zone.bounds.size);

        if (destination != null)
        {
            Vector3 arrival = destination.position + (Vector3)offset;
            Gizmos.DrawLine(transform.position, arrival);
            Gizmos.DrawWireSphere(arrival, 0.4f);
        }
    }
}