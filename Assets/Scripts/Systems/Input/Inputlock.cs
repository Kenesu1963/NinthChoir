/// <summary>True while a menu, dialogue, death, paralysis, camera trip or scene change has taken over, so the player must stand still and use nothing.</summary>
public static class InputLock
{
    public static bool IsLocked =>
        DialogueManager.IsOpen || InventoryMenu.IsOpen || ShopMenu.IsOpen || DeathScreen.IsOpen ||
        CameraFollow.IsTraveling || ScreenFader.IsBusy ||
        (PlayerStats.Instance != null && (PlayerStats.Instance.IsDead || PlayerStats.Instance.IsParalyzed));
}