using UnityEngine;

/// <summary>
/// A spot where the player can arrive when coming from another scene.
/// Put it on an empty object in the destination scene and give it a name (Id).
/// A Scene Transition in the other scene asks for this name. It shows as a blue circle in the Scene view.
/// </summary>
public class EntryPoint : MonoBehaviour
{
    [Tooltip("The name that scene transitions use to find this spot, e.g. \"FromCave\".")]
    [SerializeField] private string entryId = "Start";

    public string Id => entryId;
    public Vector2 Position => transform.position;

    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(0.3f, 0.6f, 1f, 0.9f);
        Gizmos.DrawWireSphere(transform.position, 0.5f);
        Gizmos.DrawLine(transform.position, transform.position + Vector3.up * 1.2f);
    }
}