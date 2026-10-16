using UnityEngine;

/// <summary>
/// Hold the button to charge. When it's fully charged, release to fire a beam of light.
/// The beam lasts a short time and damages in ticks. Walls and ground stop it, enemies don't.
/// Releasing early cancels it and costs nothing.
/// </summary>
[CreateAssetMenu(fileName = "NewLightBeam", menuName = "Items/Incantation/Light Beam")]
public class LightBeamIncantationItem : IncantationItem
{
    [Header("Charge")]
    public float chargeTime = 1f;
    [Tooltip("If ticked, it fires the moment it's charged, without waiting for you to release the button.")]
    public bool fireAutomaticallyWhenCharged = false;
    [Range(0f, 1f)] public float moveSpeedWhileCharging = 0.4f;

    [Header("Beam")]
    public float beamDuration = 0.5f;
    public float tickInterval = 0.1f;      // time between damage ticks
    public int damagePerTick = 1;
    public float maxLength = 14f;
    public float beamWidth = 0.7f;
    public float startDistance = 0.6f;     // how far from the player the beam starts

    public LightBeamIncantationItem()
    {
        itemName = "Light Beam";
        description = "Hold to gather light, then release to fire a beam that burns everything in its path. Walls stop it, enemies don't.";
        divineEnergyCost = 3;
        cooldown = 0f;
    }

    public override bool IsHold => true;
    public override float HeldMoveSpeedMultiplier => moveSpeedWhileCharging;
    public override bool LocksOtherActionsWhileHeld => true; // charging commits you
    public override bool SpendsCostOnActivate => false;      // the Divine Energy is only paid when the beam fires

    public override string GetStatsText()
    {
        int ticks = Mathf.Max(1, Mathf.CeilToInt(beamDuration / Mathf.Max(0.02f, tickInterval)));
        return base.GetStatsText() +
               "\nCharge time: " + Num(chargeTime) + "s" +
               "\nBeam lasts: " + Num(beamDuration) + "s" +
               "\nDamage: " + damagePerTick + " x " + ticks + " ticks" +
               "\nPierces enemies, stopped by walls";
    }

    public override void BeginHold(ItemUseContext c)
    {
        c.chargeTimer = 0f;
        c.charged = false;
        c.fired = false;
        c.effect = ChargeGlow.Create(c.owner, c.origin).gameObject;
    }

    public override void UpdateHold(ItemUseContext c)
    {
        if (c.fired) return;

        c.chargeTimer += Time.deltaTime;
        if (!c.charged && c.chargeTimer >= chargeTime)
        {
            c.charged = true;
            if (fireAutomaticallyWhenCharged)
            {
                Fire(c);
                return;
            }
        }

        if (c.effect != null)
        {
            ChargeGlow glow = c.effect.GetComponent<ChargeGlow>();
            if (glow != null)
            {
                glow.SetOrigin(c.origin);
                glow.SetCharge(Mathf.Clamp01(c.chargeTimer / Mathf.Max(0.01f, chargeTime)), c.charged);
            }
        }
    }

    public override void EndHold(ItemUseContext c)
    {
        // Released normally and fully charged: fire. Interrupted (dash, dialogue...) or too early: nothing happens.
        if (!c.interrupted && c.charged && !c.fired) Fire(c);

        if (c.effect != null) Destroy(c.effect);
        c.effect = null;
    }

    private void Fire(ItemUseContext c)
    {
        c.fired = true;
        if (c.effect != null) Destroy(c.effect);
        c.effect = null;

        if (!c.stats.TrySpendDivineEnergy(divineEnergyCost))
        {
            Debug.Log("Not enough Divine Energy for " + itemName + " (needs " + divineEnergyCost + ").");
            return;
        }

        LightBeam.Spawn(c.owner, c.aim, c.origin, startDistance, maxLength, beamWidth,
                        beamDuration, tickInterval, damagePerTick);
    }
}