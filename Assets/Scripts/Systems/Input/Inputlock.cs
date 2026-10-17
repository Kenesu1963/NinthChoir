/// <summary>True while a menu, dialogue or death has taken over, so the player must stand still and use nothing.</summary>
public static class InputLock
{
    public static bool IsLocked =>
        DialogueManager.IsOpen || InventoryMenu.IsOpen || ShopMenu.IsOpen || DeathScreen.IsOpen ||
        (PlayerStats.Instance != null && PlayerStats.Instance.IsDead);
}