using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Base class for anything the player can interact with using action key 1 (J by default).
/// No collider is needed: the player just has to be within Interact Radius of the object.
/// </summary>
public abstract class Interactable : MonoBehaviour
{
    private static readonly List<Interactable> all = new List<Interactable>();

    [Header("Interaction")]
    [SerializeField] private float interactRadius = 1.5f;
    [SerializeField] protected string prompt = "Interact"; // shown as "[J] Interact"

    public string Prompt => prompt;

    /// <summary>False = ignored for now (e.g. an opened chest, or loot still in the air).</summary>
    public virtual bool IsAvailable => true;

    public abstract void Interact();

    protected virtual void OnEnable()
    {
        all.Add(this);
    }

    protected virtual void OnDisable()
    {
        all.Remove(this);
    }

    /// <summary>The closest interactable whose radius contains the position, or null.</summary>
    public static Interactable FindNearest(Vector2 position)
    {
        Interactable best = null;
        float bestSqr = float.MaxValue;

        foreach (Interactable i in all)
        {
            if (!i.IsAvailable) continue;
            float sqr = ((Vector2)i.transform.position - position).sqrMagnitude;
            if (sqr <= i.interactRadius * i.interactRadius && sqr < bestSqr)
            {
                best = i;
                bestSqr = sqr;
            }
        }
        return best;
    }

    protected virtual void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactRadius);
    }
}