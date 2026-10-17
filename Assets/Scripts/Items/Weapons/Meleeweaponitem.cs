using UnityEngine;

/// <summary>A weapon that creates a short-lived hurtbox in the aimed direction (e.g. the rapier).</summary>
[CreateAssetMenu(fileName = "NewRapier", menuName = "Items/Weapon/Melee (Rapier)")]
public class MeleeWeaponItem : WeaponItem
{
    [Header("Hurtbox")]
    public int damage = 1;
    public float duration = 0.1f;                       // how long the hurtbox exists
    public Vector2 size = new Vector2(1.5f, 0.5f);      // length, thickness
    public float distance = 1.1f;                       // distance from the player's center

    [Header("Wind-up (0 = instant)")]
    [Tooltip("Seconds between pressing the button and the hit. A faint box shows where it will land.")]
    public float windUpTime = 0f;
    [Range(0f, 1f)] public float windUpMoveSpeedMultiplier = 1f;

    [Header("Hit feedback")]
    public float recoilSpeed = 7f;                      // pushback on the player when it connects
    public float recoilDuration = 0.12f;
    public bool bounceOnDownAttack = true;              // hitting something below gives a small hop

    public MeleeWeaponItem()
    {
        itemName = "Rapier";
        description = "A quick thrust in the direction you aim. It pushes you back a little when it hits.";
        cooldown = 0.25f;
    }

    public override bool IsHold => false;
    public override float WindUpTime => windUpTime;

    public override string GetEffectSummary()
    {
        return "Damage: " + damage + (windUpTime > 0f ? "   Wind-up: " + Num(windUpTime) + "s" : "");
    }
    public override float WindUpMoveSpeedMultiplier => windUpMoveSpeedMultiplier;

    public override string GetStatsText()
    {
        string text = base.GetStatsText() + "\nDamage: " + damage + "\nHurtbox lasts: " + Num(duration) + "s";
        if (windUpTime > 0f) text += "\nWind-up: " + Num(windUpTime) + "s";
        return text;
    }

    // --- Wind-up: a faint preview of the hurtbox follows your aim until the hit lands ---

    public override void BeginWindUp(ItemUseContext c)
    {
        c.heldHitbox = Hitbox.Spawn(Hitbox.Kind.Telegraph, c.owner, c.aim, size, distance, 0f, 0,
                                    c.showDebugVisuals, c.origin);
    }

    public override void UpdateWindUp(ItemUseContext c)
    {
        if (c.heldHitbox == null) return;
        c.heldHitbox.SetDirection(c.aim);
        c.heldHitbox.SetOriginOffset(c.origin);
    }

    public override void EndWindUp(ItemUseContext c)
    {
        if (c.heldHitbox != null) Destroy(c.heldHitbox.gameObject);
        c.heldHitbox = null;
    }

    // --- The hit ---

    public override void Use(ItemUseContext c)
    {
        PlayerController controller = c.controller;
        Hitbox.Spawn(Hitbox.Kind.Attack, c.owner, c.aim, size, distance, duration, Scaled(c, damage),
                     c.showDebugVisuals, c.origin, hitDirection => Recoil(controller, hitDirection));
    }

    private void Recoil(PlayerController controller, Vector2 attackDirection)
    {
        if (controller == null) return;

        Vector2 recoil = new Vector2(-attackDirection.x, 0f) * recoilSpeed;
        if (bounceOnDownAttack && attackDirection.y < -0.01f)
        {
            recoil.y = -attackDirection.y * recoilSpeed;
        }
        controller.ApplyRecoil(recoil, recoilDuration);
    }
}