using UnityEngine;

/// <summary>An incantation that fires a projectile in a straight line in the aimed direction.</summary>
[CreateAssetMenu(fileName = "NewProjectileIncantation", menuName = "Items/Incantation/Projectile")]
public class ProjectileIncantationItem : IncantationItem
{
    [Header("Projectile")]
    public int damage = 1;
    public float speed = 18f;
    public float lifetime = 2f;
    public float radius = 0.2f;
    public float spawnDistance = 0.9f;                  // how far from the player it appears

    public ProjectileIncantationItem()
    {
        itemName = "Divine Bolt";
        divineEnergyCost = 1;
        cooldown = 0.4f;
    }

    public override bool IsHold => false;

    public override void Use(ItemUseContext c)
    {
        Vector2 spawnPos = (Vector2)c.owner.position + c.origin + c.aim * spawnDistance;
        Projectile.Spawn(c.owner, spawnPos, c.aim, speed, lifetime, damage, radius, true, c.showDebugVisuals);
    }
}