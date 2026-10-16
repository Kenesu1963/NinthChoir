using UnityEngine;

/// <summary>
/// A tool you hold. While the button is held, touching a wall at your side makes you cling to it.
/// Release the button and you fall. Press jump and you jump off as normal.
/// </summary>
[CreateAssetMenu(fileName = "NewPick", menuName = "Items/Tool/Pick")]
public class PickToolItem : ToolItem
{
    [Header("Wall check")]
    [Tooltip("How far from the sides of the body to look for a wall.")]
    public float checkDistance = 0.15f;

    public PickToolItem()
    {
        itemName = "Pick";
        description = "Hold the button next to a wall to cling to it. Jump to leap off, or let go to fall.";
        cooldown = 0f;
    }

    public override bool IsHold => true;

    public override string GetStatsText()
    {
        return base.GetStatsText() + "\nHold next to a wall to cling\nJump to leap off";
    }

    public override void BeginHold(ItemUseContext c)
    {
        TryCling(c);
    }

    public override void UpdateHold(ItemUseContext c)
    {
        TryCling(c);
    }

    public override void EndHold(ItemUseContext c)
    {
        c.controller.ReleaseCling(); // let go: you fall
    }

    private void TryCling(ItemUseContext c)
    {
        int wallSide = FindWallSide(c);

        if (c.controller.IsClinging)
        {
            if (wallSide == 0) c.controller.StopCling(); // the wall is gone
            return;
        }

        if (wallSide != 0) c.controller.TryBeginCling(wallSide);
    }

    // -1 = wall on the left, +1 = wall on the right, 0 = no wall. Prefers the side you're facing.
    private int FindWallSide(ItemUseContext c)
    {
        Bounds b = c.controller.BodyBounds;
        int first = c.controller.FacingRight ? 1 : -1;

        for (int i = 0; i < 2; i++)
        {
            int side = i == 0 ? first : -first;
            Vector2 center = new Vector2(b.center.x + side * (b.extents.x + checkDistance * 0.5f), b.center.y);
            Vector2 size = new Vector2(checkDistance, b.size.y * 0.7f); // not the full height, so the floor doesn't count

            foreach (Collider2D hit in Physics2D.OverlapBoxAll(center, size, 0f))
            {
                if (hit.isTrigger) continue;
                if (hit.transform == c.owner || hit.transform.IsChildOf(c.owner)) continue;
                if (hit.GetComponentInParent<IDamageable>() != null) continue; // enemies aren't walls
                return side;
            }
        }
        return 0;
    }
}