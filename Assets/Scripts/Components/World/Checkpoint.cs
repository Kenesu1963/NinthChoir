using UnityEngine;

/// <summary>
/// A save zone. Press action key 1 (J) next to it to save: if you die, you come back here.
/// Put this on any object (a square works as a placeholder). The Interact Radius shows as a yellow circle when selected.
/// </summary>
public class Checkpoint : Interactable
{
    [Header("Checkpoint")]
    [Tooltip("Where the player appears, relative to this object.")]
    [SerializeField] private Vector2 spawnOffset = Vector2.zero;
    [Tooltip("Saving here also restores full health and Divine Energy.")]
    [SerializeField] private bool restoreOnSave = false;

    [Header("Placeholder colors")]
    [Tooltip("Tints the sprite grey until saved and gold when it's the active checkpoint. Untick when you have real art.")]
    [SerializeField] private bool applyColors = true;
    [SerializeField] private Color inactiveColor = new Color(0.55f, 0.6f, 0.7f, 1f);
    [SerializeField] private Color activeColor = new Color(1f, 0.85f, 0.3f, 1f);

    private SpriteRenderer sr;

    public Vector2 SpawnPosition => (Vector2)transform.position + spawnOffset;

    private void Reset()
    {
        prompt = "Save";
    }

    private void Awake()
    {
        sr = GetComponentInChildren<SpriteRenderer>();
        SetActiveVisual(false);
    }

    public override void Interact()
    {
        RespawnManager.Instance.SetCheckpoint(this);

        PlayerStats stats = PlayerStats.Instance;
        if (restoreOnSave && stats != null)
        {
            stats.Heal(stats.MaxHealth);
            stats.RestoreDivineEnergy(stats.MaxDivineEnergy);
        }

        StatsHUD.Show("Checkpoint saved.");
    }

    /// <summary>Called by the respawn manager when this checkpoint becomes (or stops being) the active one.</summary>
    public void SetActiveVisual(bool active)
    {
        if (sr != null && applyColors) sr.color = active ? activeColor : inactiveColor;
    }

    protected override void OnDrawGizmosSelected()
    {
        base.OnDrawGizmosSelected();

        // The green dot is where the player will appear
        Gizmos.color = new Color(0.2f, 1f, 0.4f, 0.9f);
        Gizmos.DrawWireSphere(SpawnPosition, 0.3f);
    }
}