using UnityEngine;

/// <summary>A tool held up while the button is down: a blocking box that stops enemy projectiles.</summary>
[CreateAssetMenu(fileName = "NewShield", menuName = "Items/Tool/Shield")]
public class ShieldToolItem : ToolItem
{
    [Header("Block box")]
    public Vector2 size = new Vector2(0.3f, 1.5f);      // thickness, height
    public float distance = 0.9f;                       // distance from the player's center
    [Range(0f, 1f)] public float moveSpeedMultiplier = 0.5f; // walking speed while blocking

    [Header("Parry")]
    [Tooltip("Raise the shield just as an enemy shot arrives (or with the shot already inside the shield) and it flies back at the shooter with the same damage. " +
             "This is how many seconds after raising the shield still count. 0 = only shots already inside the shield at that moment.")]
    public float parryWindow = 0.12f;
    [Tooltip("Speed of a returned shot compared to its original speed. 1 = the same.")]
    public float parryReturnSpeedMultiplier = 1.3f;

    public ShieldToolItem()
    {
        itemName = "Shield";
        description = "Hold the button to raise it in the aimed direction. It blocks enemy projectiles. Raise it right as a shot arrives to send it back.";
        cooldown = 0f;
    }

    public override bool IsHold => true;

    public override string GetStatsText()
    {
        return base.GetStatsText() + "\nHold to block\nWalking speed while held: x" + Num(moveSpeedMultiplier) +
               "\nPerfect parry: raise it as a shot arrives (" + Num(parryWindow) + "s) to return it";
    }
    public override float HeldMoveSpeedMultiplier => moveSpeedMultiplier;
    public override bool LocksOtherActionsWhileHeld => true; // blocking commits you: no other actions

    public override void BeginHold(ItemUseContext c)
    {
        c.heldHitbox = Hitbox.Spawn(Hitbox.Kind.Block, c.owner, c.aim, size, distance, 0f, 0,
                                    c.showDebugVisuals, c.origin, null,
                                    parryWindow, parryReturnSpeedMultiplier, () => StatsHUD.Show("Parry!"));
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