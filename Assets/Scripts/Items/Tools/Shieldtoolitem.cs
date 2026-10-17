using UnityEngine;

/// <summary>A tool held up while the button is down: a blocking box that stops enemy projectiles.</summary>
[CreateAssetMenu(fileName = "NewShield", menuName = "Items/Tool/Shield")]
public class ShieldToolItem : ToolItem
{
    [Header("Block box")]
    public Vector2 size = new Vector2(0.3f, 1.5f);      // thickness, height
    public float distance = 0.9f;                       // distance from the player's center
    [Range(0f, 1f)] public float moveSpeedMultiplier = 0.5f; // walking speed while blocking

    public ShieldToolItem()
    {
        itemName = "Shield";
        description = "Hold the button to raise it in the aimed direction. It blocks enemy projectiles.";
        cooldown = 0f;
    }

    public override bool IsHold => true;

    public override string GetStatsText()
    {
        return base.GetStatsText() + "\nHold to block\nWalking speed while held: x" + Num(moveSpeedMultiplier);
    }
    public override float HeldMoveSpeedMultiplier => moveSpeedMultiplier;
    public override bool LocksOtherActionsWhileHeld => true; // blocking commits you: no other actions

    public override void BeginHold(ItemUseContext c)
    {
        c.heldHitbox = Hitbox.Spawn(Hitbox.Kind.Block, c.owner, c.aim, size, distance, 0f, 0,
                                    c.showDebugVisuals, c.origin);
    }

    public override void UpdateHold(ItemUseContext c)
    {
        if (c.heldHitbox == null) return;
        c.heldHitbox.SetDirection(c.aim);
        c.heldHitbox.SetOriginOffset(c.origin);
    }

    public override void EndHold(ItemUseContext c)
    {
        if (c.heldHitbox != null) Destroy(c.heldHitbox.gameObject);
        c.heldHitbox = null;
    }
}