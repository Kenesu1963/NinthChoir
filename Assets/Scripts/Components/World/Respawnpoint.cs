using UnityEngine;

/// <summary>
/// The default respawn point: where the player comes back if they die before saving any checkpoint.
/// Put this on an empty object at the start of the level (move it in the Scene view). It shows as a green circle.
/// If the scene has no RespawnPoint, the spot where the player starts is used instead.
/// </summary>
public class RespawnPoint : MonoBehaviour
{
    [Tooltip("If the scene has several respawn points, the one ticked here is the default.")]
    [SerializeField] private bool isDefault = true;

    public bool IsDefault => isDefault;
    public Vector2 Position => transform.position;

    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(0.2f, 1f, 0.4f, 0.9f);
        Gizmos.DrawWireSphere(transform.position, 0.5f);
        Gizmos.DrawLine(transform.position, transform.position + Vector3.up * 1.2f);
    }
}