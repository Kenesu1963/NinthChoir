/// <summary>
/// Base class for everything in the Weapon category. Create a new kind of weapon by making a script
/// that extends this class (see MeleeWeaponItem for an example).
/// </summary>
public abstract class WeaponItem : ItemData
{
    protected WeaponItem()
    {
        category = ItemCategory.Weapon;
    }
}