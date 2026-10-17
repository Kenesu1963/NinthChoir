using UnityEngine;

/// <summary>
/// The three realms of the inventory. Every item category belongs to one of them:
///     Body = weapons and food      Mind = tools and potions      Soul = incantations and wine
/// </summary>
public enum InventoryPage { Body, Mind, Soul }

public static class ItemPages
{
    // Body = red, Mind = yellow, Soul = blue
    private static readonly Color Red = new Color(0.88f, 0.27f, 0.25f, 1f);
    private static readonly Color Yellow = new Color(0.97f, 0.82f, 0.25f, 1f);
    private static readonly Color Blue = new Color(0.32f, 0.58f, 0.97f, 1f);

    public static InventoryPage PageOf(ItemCategory category)
    {
        switch (category)
        {
            case ItemCategory.Weapon:
            case ItemCategory.Food:
                return InventoryPage.Body;
            case ItemCategory.Tool:
            case ItemCategory.Potion:
                return InventoryPage.Mind;
            default:
                return InventoryPage.Soul;
        }
    }

    public static string NameOf(InventoryPage page)
    {
        switch (page)
        {
            case InventoryPage.Body: return "Body";
            case InventoryPage.Mind: return "Mind";
            default: return "Soul";
        }
    }

    public static Color ColorOf(InventoryPage page)
    {
        switch (page)
        {
            case InventoryPage.Body: return Red;
            case InventoryPage.Mind: return Yellow;
            default: return Blue;
        }
    }

    public static Color ColorOf(ItemCategory category)
    {
        return ColorOf(PageOf(category));
    }

    /// <summary>Hex code for rich text, e.g. "E04540".</summary>
    public static string Hex(Color color)
    {
        return ColorUtility.ToHtmlStringRGB(color);
    }
}