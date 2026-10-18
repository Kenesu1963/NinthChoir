using System;
using UnityEngine;

/// <summary>
/// Added automatically (at runtime) to a Teleporter or Scene Transition that is set to "Activate With Interact Key".
/// The player has to be inside the trigger area and press action key 1 (J). You never add this yourself.
/// Going through the Interactable system means the key press is used for this and not for an attack at the same time,
/// and the usual "[J] ..." prompt appears at the bottom of the screen.
/// </summary>
public class TriggerInteract : Interactable
{
    private Collider2D zone;
    private Action onInteract;

    public static TriggerInteract Attach(GameObject target, Collider2D zone, string promptText, Action onInteract)
    {
        TriggerInteract helper = target.AddComponent<TriggerInteract>();
        helper.zone = zone;
        helper.prompt = promptText;
        helper.onInteract = onInteract;
        return helper;
    }

    // Not a distance from the object: the player has to be INSIDE the trigger area
    public override bool IsInRange(Vector2 position, out float sqrDistance)
    {
        sqrDistance = float.MaxValue;
        if (zone == null || !zone.enabled) return false;

        sqrDistance = ((Vector2)zone.bounds.center - position).sqrMagnitude;
        return zone.OverlapPoint(position);
    }

    public override void Interact()
    {
        if (onInteract != null) onInteract();
    }
}