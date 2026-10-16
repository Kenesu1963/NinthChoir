using UnityEngine;

/// <summary>Anything that can be hurt by the player's attacks or projectiles.</summary>
public interface IDamageable
{
    void TakeDamage(int amount, Vector2 hitDirection);
}