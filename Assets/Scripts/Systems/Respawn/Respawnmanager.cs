using UnityEngine;

/// <summary>
/// Remembers where the player comes back after dying: the last checkpoint they saved,
/// or the default respawn point if they haven't saved one. Creates itself, nothing to set up.
/// It lives only inside the scene, so restarting the level forgets the checkpoints.
/// </summary>
public class RespawnManager : MonoBehaviour
{
    private static RespawnManager instance;

    public static RespawnManager Instance
    {
        get
        {
            if (instance == null)
            {
                GameObject go = new GameObject("RespawnManager");
                instance = go.AddComponent<RespawnManager>();
            }
            return instance;
        }
    }

    /// <summary>Makes sure it exists (and remembers where the level starts).</summary>
    public static void EnsureExists()
    {
        Checkpoint ignored = Instance.current;
    }

    private Vector2 defaultSpawn;
    private bool hasExplicitPoint;
    private Checkpoint current;

    public bool HasCheckpoint => current != null;

    private void Awake()
    {
        instance = this;
        FindDefaultSpawn();
    }

    private void OnDestroy()
    {
        if (instance == this) instance = null;
    }

    private void FindDefaultSpawn()
    {
        RespawnPoint chosen = null;
        foreach (RespawnPoint point in FindObjectsByType<RespawnPoint>(FindObjectsSortMode.None))
        {
            if (point.IsDefault) { chosen = point; break; }
            if (chosen == null) chosen = point;
        }

        if (chosen != null)
        {
            defaultSpawn = chosen.Position;
            hasExplicitPoint = true;
        }
        else if (PlayerStats.Instance != null)
        {
            defaultSpawn = PlayerStats.Instance.transform.position;
            Debug.LogWarning("No RespawnPoint in the scene: using the spot where the player starts.");
        }
    }

    /// <summary>The player just arrived through a scene transition. If the scene has no RespawnPoint, this spot becomes the default respawn.</summary>
    public void ArrivedAt(Vector2 position)
    {
        if (!hasExplicitPoint) defaultSpawn = position;
    }

    public void SetCheckpoint(Checkpoint checkpoint)
    {
        if (current != null && current != checkpoint) current.SetActiveVisual(false);
        current = checkpoint;
        if (current != null) current.SetActiveVisual(true);
    }

    public Vector2 GetRespawnPosition()
    {
        return current != null ? current.SpawnPosition : defaultSpawn;
    }

    /// <summary>Brings the player back to life at the last checkpoint (or the default spawn).</summary>
    public void RespawnPlayer()
    {
        PlayerStats stats = PlayerStats.Instance;
        if (stats == null) return;

        PlayerController controller = stats.GetComponent<PlayerController>();
        Vector2 position = GetRespawnPosition();

        if (controller != null) controller.TeleportTo(position);
        else stats.transform.position = position;

        stats.Revive();

        // The camera jumps to the player instead of sliding across the level
        CameraFollow cam = FindFirstObjectByType<CameraFollow>();
        if (cam != null) cam.SnapToTarget();
    }
}