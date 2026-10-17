/// <summary>
/// Base class for everything in the Incantation category. Create a new kind of incantation by making
/// a script that extends this class (see ProjectileIncantationItem for an example).
/// </summary>
public abstract class IncantationItem : ItemData
{
    protected IncantationItem()
    {
        category = ItemCategory.Incantation;
    }
}