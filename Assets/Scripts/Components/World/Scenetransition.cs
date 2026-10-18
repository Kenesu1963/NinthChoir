using UnityEngine;

/// <summary>
/// A trigger that takes the player to a different scene. The screen fades out, the next scene loads,
/// and the player appears at an Entry Point there. Health, Divine Energy, money, items and equipment come along.
/// Put it on an object with a Collider2D (it becomes a trigger automatically).
///
/// Activation: by default you just walk into it. Tick "Activate With Interact Key" to have to stand inside it
/// and press action key 1 (J) instead.
/// In a build, the target scene must be in the Build Settings. In the editor it works without that.
/// </summary>
public class SceneTransition : MonoBehaviour
{
    [Header("Destination")]
    [Tooltip("The exact name of the scene to load (the file name, without .unity).")]
    [SerializeField] private string targetScene = "";
    [Tooltip("The Id of the Entry Point in that scene where the player appears. Leave empty to stay where the player is placed in that scene.")]
    [SerializeField] private string targetEntryId = "Start";

    [Header("Activation")]
    [Tooltip("Off: it activates when you walk into it.  On: you have to be inside it and press action key 1 (J).")]
    [SerializeField] private bool activateWithInteract = false;
    [Tooltip("The word shown in the \"[J] ...\" prompt when Activate With Interact Key is on.")]
    [SerializeField] private string interactPrompt = "Enter";

    [Header("Fade")]
    [SerializeField] private float fadeOutTime = 0.4f;
    [SerializeField] private float fadeInTime = 0.4f;

    private void Reset()
    {
        Collider2D zone = GetComponent<Collider2D>();
        if (zone != null) zone.isTrigger = true;

        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null) sr.color = new Color(0.3f, 1f, 0.4f, 0.4f);
    }

    private void Awake()
    {
        Collider2D zone = GetComponent<Collider2D>();
        if (zone == null)
        {
            Debug.LogWarning("SceneTransition on '" + name + "' needs a Collider2D (for example a Box Collider 2D).", this);
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
        if (ScreenFader.IsBusy || player.IsDead) return;

        // A Key Gate on this object keeps it locked until its requirements are collected
        KeyGate gate = GetComponent<KeyGate>();
        if (gate != null && !gate.IsOpen)
        {
            gate.NotifyLocked();
            return;
        }

        if (string.IsNullOrEmpty(targetScene))
        {
            Debug.LogWarning("SceneTransition on '" + name + "' has no Target Scene.", this);
            return;
        }

        ScreenFader.Instance.GoToScene(targetScene, targetEntryId, fadeOutTime, fadeInTime);
    }

    private void OnDrawGizmos()
    {
        Collider2D zone = GetComponent<Collider2D>();
        if (zone == null) return;

        Gizmos.color = new Color(0.3f, 1f, 0.4f, 0.9f);
        Gizmos.DrawWireCube(zone.bounds.center, zone.bounds.size);
    }
}