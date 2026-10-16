/// <summary>
/// Base class for everything in the Tool category. Create a new kind of tool by making a script
/// that extends this class (see ShieldToolItem for an example).
/// </summary>
public abstract class ToolItem : ItemData
{
    protected ToolItem()
    {
        category = ItemCategory.Tool;
    }
}