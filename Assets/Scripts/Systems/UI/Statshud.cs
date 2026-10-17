using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// On-screen character info (top-left): name, health, DE, money, active effects, and what is equipped
/// on each action button. Created automatically by PlayerStats, so there is nothing to set up.
/// </summary>
public class StatsHUD : MonoBehaviour
{
    private PlayerStats stats;
    private PlayerCombat combat;
    private Font font;
    private Text infoText;
    private Text loadoutText;
    private Text messageText;
    private float messageTimer;

    public static StatsHUD Instance { get; private set; }

    public static StatsHUD Create(PlayerStats stats)
    {
        GameObject go = new GameObject("StatsHUD");
        StatsHUD hud = go.AddComponent<StatsHUD>();
        hud.Init(stats);
        return hud;
    }

    /// <summary>Shows a short message under the stats panel for a couple of seconds.</summary>
    public static void Show(string text)
    {
        if (Instance == null) return;
        Instance.messageText.text = text;
        Instance.messageTimer = 2.5f;
    }

    private void Init(PlayerStats owner)
    {
        Instance = this;
        stats = owner;
        combat = owner.GetComponent<PlayerCombat>();

        BuildUI();

        stats.Changed += Refresh;
        if (combat != null) combat.LoadoutChanged += Refresh;
        Refresh();
    }

    private void Update()
    {
        if (messageTimer > 0f)
        {
            messageTimer -= Time.unscaledDeltaTime;
            if (messageTimer <= 0f) messageText.text = "";
        }
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
        if (stats != null) stats.Changed -= Refresh;
        if (combat != null) combat.LoadoutChanged -= Refresh;
    }

    private void Refresh()
    {
        if (stats == null) return;

        string info =
            "<b><color=#FFD966>" + stats.CharacterName + "</color></b>\n" +
            "HP  " + stats.Health + " / " + stats.MaxHealth + "\n" +
            "DE  " + stats.DivineEnergy + " / " + stats.MaxDivineEnergy + "\n" +
            stats.CurrencyName + "  " + stats.Money;

        string status = stats.StatusText;
        if (!string.IsNullOrEmpty(status)) info += "\n<size=17><color=#9AD18B>" + status + "</color></size>";
        infoText.text = info;

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

        string color = ItemPages.Hex(ItemPages.ColorOf(item.category)); // red / yellow / blue by page
        return "[" + key + "]  " + item.itemName + "  <color=#" + color + ">" + item.category + "</color>";
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
        panelRect.sizeDelta = new Vector2(320f, 240f);
        panelRect.anchoredPosition = new Vector2(16f, -48f);
        Image bg = panel.AddComponent<Image>();
        bg.color = new Color(0f, 0f, 0f, 0.45f);
        bg.raycastTarget = false;

        infoText = MakeText("Info", panelRect, 24);
        Stretch(infoText.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(14f, -158f), new Vector2(-14f, -8f));

        loadoutText = MakeText("Loadout", panelRect, 20);
        Stretch(loadoutText.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f),
                new Vector2(14f, 8f), new Vector2(-14f, 80f));

        // Message line under the panel
        messageText = MakeText("Message", canvasGO.transform, 22);
        RectTransform messageRect = messageText.rectTransform;
        messageRect.anchorMin = new Vector2(0f, 1f);
        messageRect.anchorMax = new Vector2(0f, 1f);
        messageRect.pivot = new Vector2(0f, 1f);
        messageRect.sizeDelta = new Vector2(700f, 30f);
        messageRect.anchoredPosition = new Vector2(16f, -296f);
        Outline messageOutline = messageText.gameObject.AddComponent<Outline>();
        messageOutline.effectColor = new Color(0f, 0f, 0f, 0.9f);
        messageOutline.effectDistance = new Vector2(2f, -2f);
        messageText.text = "";
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