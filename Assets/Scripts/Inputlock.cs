/// <summary>True while a menu or dialogue has taken over, so the player must stand still and use nothing.</summary>
public static class InputLock
{
    public static bool IsLocked => DialogueManager.IsOpen || InventoryMenu.IsOpen;
}