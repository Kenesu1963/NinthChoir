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

    public override string GetStatsText()
    {
        return base.GetStatsText() + "\nDamage: " + damage + "\nHurtbox lasts: " + Num(duration) + "s";
    }

    public override void Use(ItemUseContext c)
    {
        PlayerController controller = c.controller;
        Hitbox.Spawn(Hitbox.Kind.Attack, c.owner, c.aim, size, distance, duration, damage,
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