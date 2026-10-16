using UnityEngine;

/// <summary>
/// A heavy melee weapon: it winds up before it hits, but the hurtbox is big.
/// It's a MeleeWeaponItem with different numbers, so you can tune everything in the Inspector.
/// </summary>
[CreateAssetMenu(fileName = "NewSaber", menuName = "Items/Weapon/Saber")]
public class SaberItem : MeleeWeaponItem
{
    public SaberItem()
    {
        itemName = "Saber";
        description = "A heavy blade. It takes a moment to wind up, but the sweep is huge and hits hard.";

        damage = 3;
        duration = 0.15f;
        size = new Vector2(2.6f, 1.8f);   // big hurtbox
        distance = 1.5f;

        windUpTime = 0.35f;               // the delay before the hit
        windUpMoveSpeedMultiplier = 0.5f; // you're slower while winding up

        recoilSpeed = 5f;
        recoilDuration = 0.15f;
        cooldown = 0.9f;
    }
}