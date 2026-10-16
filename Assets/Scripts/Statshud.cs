using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// On-screen character info (top-left): name, health, DE, money, and what is equipped on each action button.
/// Created automatically by PlayerStats, so there is nothing to set up.
/// </summary>
public class StatsHUD : MonoBehaviour
{
    private PlayerStats stats;
    private PlayerCombat combat;
    private Font font;
    private Text infoText;
    private Text loadoutText;

    public static StatsHUD Create(PlayerStats stats)
    {
        GameObject go = new GameObject("StatsHUD");
        StatsHUD hud = go.AddComponent<StatsHUD>();
        hud.Init(stats);
        return hud;
    }

    private void Init(PlayerStats owner)
    {
        stats = owner;
        combat = owner.GetComponent<PlayerCombat>();

        BuildUI();

        stats.Changed += Refresh;
        if (combat != null) combat.LoadoutChanged += Refresh;
        Refresh();
    }

    private void OnDestroy()
    {
        if (stats != null) stats.Changed -= Refresh;
        if (combat != null) combat.LoadoutChanged -= Refresh;
    }

    private void Refresh()
    {
        if (stats == null) return;

        infoText.text =
            "<b><color=#FFD966>" + stats.CharacterName + "</color></b>\n" +
            "HP  " + stats.Health + " / " + stats.MaxHealth + "\n" +
            "DE  " + stats.DivineEnergy + " / " + stats.MaxDivineEnergy + "\n" +
            stats.CurrencyName + "  " + stats.Money;

        loadoutText.text =
            SlotLine(GameAction.Action1) + "\n" +
            SlotLine(GameAction.Action2) + "\n" +
            SlotLine(GameAction.Action3);
    }

    private string SlotLine(GameAction action)
    {
        string key = GameControls.GetKey(action).ToString();
        ItemData item = combat != null ? combat.GetEquipped(action) : null;
        if (item == null) return "[" + key + "]  -";
        return "[" + key + "]  " + item.itemName + "  <color=#9AA0AA>" + item.category + "</color>";
    }

    private void BuildUI()
    {
        font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        GameObject canvasGO = new GameObject("HUDCanvas");
        canvasGO.transform.SetParent(transform, false);
        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 90;
        CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280f, 720f);
        scaler.matchWidthOrHeight = 0.5f;

        // Panel in the top-left corner, a little below the debug text
        GameObject panel = new GameObject("StatsPanel", typeof(RectTransform));
        panel.transform.SetParent(canvasGO.transform, false);
        RectTransform panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0f, 1f);
        panelRect.anchorMax = new Vector2(0f, 1f);
        panelRect.pivot = new Vector2(0f, 1f);
        panelRect.sizeDelta = new Vector2(320f, 215f);
        panelRect.anchoredPosition = new Vector2(16f, -48f);
        Image bg = panel.AddComponent<Image>();
        bg.color = new Color(0f, 0f, 0f, 0.45f);
        bg.raycastTarget = false;

        infoText = MakeText("Info", panelRect, 24);
        Stretch(infoText.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(14f, -132f), new Vector2(-14f, -8f));

        loadoutText = MakeText("Loadout", panelRect, 20);
        Stretch(loadoutText.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f),
                new Vector2(14f, 8f), new Vector2(-14f, 80f));
    }

    private Text MakeText(string objectName, Transform parent, int size)
    {
        GameObject go = new GameObject(objectName, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        Text t = go.AddComponent<Text>();
        t.font = font;
        t.fontSize = size;
        t.alignment = TextAnchor.UpperLeft;
        t.color = Color.white;
        t.supportRichText = true;
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        t.raycastTarget = false;
        return t;
    }

    private static void Stretch(RectTransform rt, Vector2 anchorMin, Vector2 anchorMax,
                                Vector2 offsetMin, Vector2 offsetMax)
    {
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = offsetMin;
        rt.offsetMax = offsetMax;
    }
}