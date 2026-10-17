using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// The shop screen, in the style of Undertale: the shopkeeper's picture on top, a text box on the
/// left and a menu box on the right with Buy, Trade, Talk and Exit. The game is paused while it's open.
/// Controls: W / S choose, action key 1 (J) confirm, Esc or action key 2 (K) go back.
/// Created automatically the first time a Shop is opened.
/// </summary>
[DefaultExecutionOrder(-80)]
public class ShopMenu : MonoBehaviour
{
    private static ShopMenu instance;

    public static ShopMenu Instance
    {
        get
        {
            if (instance == null)
            {
                GameObject go = new GameObject("ShopMenu");
                instance = go.AddComponent<ShopMenu>();
            }
            return instance;
        }
    }

    public static bool IsOpen => instance != null && instance.isOpen;
    public static bool JustClosed => instance != null && instance.closedFrame == Time.frameCount;

    private const int VisibleRows = 6;

    private class Row
    {
        public string label;
        public string detail;   // shown in the text box while this row is highlighted
        public bool enabled = true;
        public Action onChoose;
    }

    private enum ShopState { Main, BuyList, BuyConfirm, TradeList, TradeConfirm, Talk }

    // --- UI ---
    private Font font;
    private GameObject root;
    private Image portraitImage;
    private Text portraitPlaceholder;
    private Text nameText;
    private Text moneyText;
    private Text dialogueText;
    private Text listText;
    private Text hintText;

    // --- State ---
    private Shop shop;
    private bool isOpen;
    private int openedFrame = -1;
    private int closedFrame = -1;
    private float previousTimeScale = 1f;

    private ShopState state;
    private readonly List<Row> rows = new List<Row>();
    private int index;
    private int scroll;
    private string say = "";
    private int lastListIndex;

    private ShopEntry pendingBuy;
    private ShopTrade pendingTrade;

    // =====================================================================

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;

        BuildUI();
        root.SetActive(false);
    }

    private void OnDestroy()
    {
        if (instance == this) instance = null;
        if (isOpen) Time.timeScale = previousTimeScale;
    }

    public void Open(Shop newShop)
    {
        if (isOpen || newShop == null) return;

        shop = newShop;
        isOpen = true;
        openedFrame = Time.frameCount;
        previousTimeScale = Time.timeScale > 0f ? Time.timeScale : 1f;
        Time.timeScale = 0f; // pause

        DialogueManager.SetPrompt(null);

        nameText.text = shop.ShopName;
        portraitImage.sprite = shop.Portrait;
        portraitImage.enabled = shop.Portrait != null;
        portraitPlaceholder.gameObject.SetActive(shop.Portrait == null);

        RefreshMoney();
        root.SetActive(true);
        ShowMain(shop.Greeting);
    }

    private void Close()
    {
        isOpen = false;
        closedFrame = Time.frameCount;
        Time.timeScale = previousTimeScale;
        root.SetActive(false);
    }

    private void Update()
    {
        if (!isOpen) return;
        if (openedFrame == Time.frameCount) return; // ignore the key press that opened it

        if (Input.GetKeyDown(KeyCode.Escape) || GameControls.Pressed(GameAction.Action2))
        {
            GoBack();
            return;
        }

        if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow)) Move(-1);
        else if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow)) Move(+1);

        if (GameControls.Pressed(GameAction.Action1) && rows.Count > 0)
        {
            Row row = rows[index];
            if (row.enabled && row.onChoose != null) row.onChoose();
        }
    }

    private void GoBack()
    {
        switch (state)
        {
            case ShopState.Main: Close(); break;
            case ShopState.BuyConfirm: ShowBuy(shop.BuyLine, lastListIndex); break;
            case ShopState.TradeConfirm: ShowTrade(shop.TradeLine, lastListIndex); break;
            default: ShowMain(shop.MenuLine); break;
        }
    }

    // =====================================================================
    //  Screens
    // =====================================================================

    private void ShowMain(string text)
    {
        say = text;
        SetRows(ShopState.Main, new List<Row>
        {
            new Row { label = "Buy",   onChoose = () => ShowBuy(shop.BuyLine, 0) },
            new Row { label = "Trade", onChoose = () => ShowTrade(shop.TradeLine, 0) },
            new Row { label = "Talk",  onChoose = () => ShowTalk(shop.TalkLine, 0) },
            new Row { label = "Exit",  onChoose = Close },
        }, 0);
    }

    // ---------------- Buy ----------------

    private void ShowBuy(string text, int keepIndex)
    {
        say = text;
        string currency = Currency();
        List<Row> list = new List<Row>();

        foreach (ShopEntry entry in shop.Stock)
        {
            if (entry == null || entry.item == null) continue;

            ShopEntry e = entry;
            bool soldOut = e.stock == 0;

            string detail = "<b>" + e.item.itemName + "</b>\n" + e.item.description;
            string effect = e.item.GetEffectSummary();
            if (!string.IsNullOrEmpty(effect)) detail += "\n<color=#C8B27A>" + effect + "</color>";
            detail += "\n\nPrice: " + e.price + " " + currency;
            if (e.stock > 0) detail += "   (in stock: " + e.stock + ")";

            list.Add(new Row
            {
                label = soldOut ? e.item.itemName + "  (sold out)" : e.item.itemName + "  -  " + e.price,
                detail = detail,
                enabled = !soldOut,
                onChoose = () => AskBuy(e)
            });
        }

        if (list.Count == 0) say = "I have nothing to sell right now.";
        list.Add(new Row { label = "Back", onChoose = () => ShowMain(shop.MenuLine) });

        SetRows(ShopState.BuyList, list, Mathf.Clamp(keepIndex, 0, list.Count - 1));
    }

    private void AskBuy(ShopEntry entry)
    {
        lastListIndex = index;
        pendingBuy = entry;
        say = "Buy the " + entry.item.itemName + " for " + entry.price + " " + Currency() + "?";

        SetRows(ShopState.BuyConfirm, new List<Row>
        {
            new Row { label = "Yes", onChoose = DoBuy },
            new Row { label = "No",  onChoose = () => ShowBuy(shop.BuyLine, lastListIndex) },
        }, 0);
    }

    private void DoBuy()
    {
        ShopEntry entry = pendingBuy;
        if (entry == null || entry.item == null) { ShowBuy(shop.BuyLine, lastListIndex); return; }

        if (entry.stock == 0)
        {
            ShowBuy("Sorry, that's sold out.", lastListIndex);
            return;
        }

        string failMessage;
        List<ItemStack> get = new List<ItemStack> { new ItemStack(entry.item, 1) };

        if (TradeUtility.TryExecute(entry.price, null, 0, get, out failMessage))
        {
            if (entry.stock > 0) entry.stock--;
            RefreshMoney();
            ShowBuy(shop.ThanksLine, lastListIndex);
        }
        else
        {
            ShowBuy(failMessage, lastListIndex);
        }
    }

    // ---------------- Trade ----------------

    private void ShowTrade(string text, int keepIndex)
    {
        say = text;
        string currency = Currency();
        List<Row> list = new List<Row>();

        foreach (ShopTrade t in shop.Trades)
        {
            if (t == null) continue;

            ShopTrade trade = t;
            string give = TradeUtility.Describe(trade.giveItems, trade.giveMoney, currency);
            string get = TradeUtility.Describe(trade.getItems, trade.getMoney, currency);
            bool used = trade.uses == 0;

            string detail = "You give:  " + give + "\nYou get:   " + get;
            if (trade.uses > 0) detail += "\n\n(" + trade.uses + " left)";

            list.Add(new Row
            {
                label = string.IsNullOrEmpty(trade.label) ? give + " > " + get : trade.label,
                detail = detail,
                enabled = !used,
                onChoose = () => AskTrade(trade)
            });
        }

        if (list.Count == 0) say = "I don't have any deals for you right now.";
        list.Add(new Row { label = "Back", onChoose = () => ShowMain(shop.MenuLine) });

        SetRows(ShopState.TradeList, list, Mathf.Clamp(keepIndex, 0, list.Count - 1));
    }

    private void AskTrade(ShopTrade trade)
    {
        lastListIndex = index;
        pendingTrade = trade;

        string currency = Currency();
        say = "Give " + TradeUtility.Describe(trade.giveItems, trade.giveMoney, currency) +
              " and get " + TradeUtility.Describe(trade.getItems, trade.getMoney, currency) + "?";

        SetRows(ShopState.TradeConfirm, new List<Row>
        {
            new Row { label = "Yes", onChoose = DoTrade },
            new Row { label = "No",  onChoose = () => ShowTrade(shop.TradeLine, lastListIndex) },
        }, 0);
    }

    private void DoTrade()
    {
        ShopTrade trade = pendingTrade;
        if (trade == null) { ShowTrade(shop.TradeLine, lastListIndex); return; }

        if (trade.uses == 0)
        {
            ShowTrade("That deal is done.", lastListIndex);
            return;
        }

        string failMessage;
        if (TradeUtility.TryExecute(trade.giveMoney, trade.giveItems, trade.getMoney, trade.getItems, out failMessage))
        {
            if (trade.uses > 0) trade.uses--;
            RefreshMoney();
            ShowTrade(shop.ThanksLine, lastListIndex);
        }
        else
        {
            ShowTrade(failMessage, lastListIndex);
        }
    }

    // ---------------- Talk ----------------

    private void ShowTalk(string text, int keepIndex)
    {
        say = text;
        List<Row> list = new List<Row>();

        foreach (ShopTalkTopic t in shop.Topics)
        {
            if (t == null || string.IsNullOrEmpty(t.topic)) continue;

            ShopTalkTopic topic = t;
            list.Add(new Row
            {
                label = topic.topic,
                onChoose = () =>
                {
                    say = topic.reply;
                    UpdateDialogue();
                }
            });
        }

        if (list.Count == 0) say = "I don't have much to say.";
        list.Add(new Row { label = "Back", onChoose = () => ShowMain(shop.MenuLine) });

        SetRows(ShopState.Talk, list, Mathf.Clamp(keepIndex, 0, list.Count - 1));
    }

    // =====================================================================
    //  List handling
    // =====================================================================

    private void SetRows(ShopState newState, List<Row> newRows, int startIndex)
    {
        state = newState;
        rows.Clear();
        rows.AddRange(newRows);
        index = Mathf.Clamp(startIndex, 0, rows.Count - 1);
        scroll = 0;
        ClampScroll();
        RenderList();
        UpdateDialogue();
        UpdateHint();
    }

    private void Move(int direction)
    {
        if (rows.Count == 0) return;
        index = ((index + direction) % rows.Count + rows.Count) % rows.Count;
        ClampScroll();
        RenderList();
        UpdateDialogue();
        UpdateHint();
    }

    private void ClampScroll()
    {
        if (index < scroll) scroll = index;
        if (index >= scroll + VisibleRows) scroll = index - VisibleRows + 1;
    }

    private void RenderList()
    {
        string result = "";
        int end = Mathf.Min(rows.Count, scroll + VisibleRows);

        for (int i = scroll; i < end; i++)
        {
            if (i > scroll) result += "\n";

            Row row = rows[i];
            if (!row.enabled) result += "<color=#6B675E>   " + row.label + "</color>";
            else if (i == index) result += "<color=#FFD966>> " + row.label + "</color>";
            else result += "   " + row.label;
        }
        listText.text = result;
    }

    private void UpdateDialogue()
    {
        string detail = rows.Count > 0 ? rows[index].detail : null;
        dialogueText.text = !string.IsNullOrEmpty(detail) ? detail : say;
    }

    private void UpdateHint()
    {
        string text = "W / S: choose        " + GameControls.GetKey(GameAction.Action1) + ": confirm        Esc / " +
                      GameControls.GetKey(GameAction.Action2) + ": back";
        if (rows.Count > VisibleRows) text += "        (" + (index + 1) + " / " + rows.Count + ")";
        hintText.text = text;
    }

    private void RefreshMoney()
    {
        PlayerStats stats = PlayerStats.Instance;
        moneyText.text = stats != null ? stats.CurrencyName + ":  " + stats.Money : "";
    }

    private static string Currency()
    {
        PlayerStats stats = PlayerStats.Instance;
        return stats != null ? stats.CurrencyName : "Aureus";
    }

    // =====================================================================
    //  UI (built from code so there's nothing to set up by hand)
    // =====================================================================

    private void BuildUI()
    {
        font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        GameObject canvasGO = new GameObject("ShopCanvas");
        canvasGO.transform.SetParent(transform, false);
        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 120; // above everything else
        CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280f, 720f);
        scaler.matchWidthOrHeight = 0.5f;
        root = canvasGO;

        // --- Black background ---
        RectTransform background = MakeRect("Background", canvasGO.transform);
        Stretch(background, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        AddImage(background.gameObject, new Color(0f, 0f, 0f, 0.97f));

        // --- The shopkeeper's picture ---
        RectTransform portraitBorder = MakeRect("PortraitBorder", canvasGO.transform);
        Center(portraitBorder, new Vector2(380f, 300f), new Vector2(0f, 140f));
        AddImage(portraitBorder.gameObject, Color.white);

        RectTransform portraitInner = MakeRect("PortraitInner", portraitBorder);
        Stretch(portraitInner, Vector2.zero, Vector2.one, new Vector2(4f, 4f), new Vector2(-4f, -4f));
        AddImage(portraitInner.gameObject, new Color(0.08f, 0.08f, 0.1f, 1f));

        RectTransform picture = MakeRect("Picture", portraitInner);
        Stretch(picture, Vector2.zero, Vector2.one, new Vector2(8f, 8f), new Vector2(-8f, -8f));
        portraitImage = picture.gameObject.AddComponent<Image>();
        portraitImage.preserveAspect = true;
        portraitImage.raycastTarget = false;

        portraitPlaceholder = MakeText("Placeholder", portraitInner, 24, TextAnchor.MiddleCenter, new Color(0.45f, 0.45f, 0.5f));
        portraitPlaceholder.text = "( picture )";
        Stretch(portraitPlaceholder.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        // --- Shop name (top-left) and money (top-right) ---
        nameText = MakeText("ShopName", canvasGO.transform, 32, TextAnchor.UpperLeft, Color.white);
        nameText.fontStyle = FontStyle.Bold;
        Anchor(nameText.rectTransform, new Vector2(0f, 1f), new Vector2(600f, 44f), new Vector2(30f, -22f));

        moneyText = MakeText("Money", canvasGO.transform, 28, TextAnchor.UpperRight, new Color(1f, 0.85f, 0.4f));
        Anchor(moneyText.rectTransform, new Vector2(1f, 1f), new Vector2(500f, 40f), new Vector2(-30f, -24f));

        // --- Text box (bottom-left) and menu box (bottom-right) ---
        RectTransform dialogueInner = MakeBox(canvasGO.transform, "DialogueBox", new Vector2(0f, 0f),
                                              new Vector2(760f, 250f), new Vector2(30f, 36f));
        dialogueText = MakeText("Dialogue", dialogueInner, 25, TextAnchor.UpperLeft, Color.white);
        dialogueText.verticalOverflow = VerticalWrapMode.Truncate;
        Stretch(dialogueText.rectTransform, Vector2.zero, Vector2.one, new Vector2(24f, 18f), new Vector2(-24f, -18f));

        RectTransform listInner = MakeBox(canvasGO.transform, "MenuBox", new Vector2(1f, 0f),
                                          new Vector2(450f, 250f), new Vector2(-30f, 36f));
        listText = MakeText("List", listInner, 26, TextAnchor.UpperLeft, Color.white);
        listText.verticalOverflow = VerticalWrapMode.Truncate;
        listText.lineSpacing = 1.1f;
        Stretch(listText.rectTransform, Vector2.zero, Vector2.one, new Vector2(24f, 18f), new Vector2(-24f, -18f));

        // --- Hint ---
        hintText = MakeText("Hint", canvasGO.transform, 16, TextAnchor.MiddleCenter, new Color(0.6f, 0.6f, 0.65f));
        Anchor(hintText.rectTransform, new Vector2(0.5f, 0f), new Vector2(1200f, 24f), new Vector2(0f, 8f));
    }

    // A white-bordered black box like in Undertale. Returns the inside, where the text goes.
    private RectTransform MakeBox(Transform parent, string objectName, Vector2 anchor, Vector2 size, Vector2 position)
    {
        RectTransform border = MakeRect(objectName, parent);
        Anchor(border, anchor, size, position);
        AddImage(border.gameObject, Color.white);

        RectTransform inner = MakeRect("Inner", border);
        Stretch(inner, Vector2.zero, Vector2.one, new Vector2(4f, 4f), new Vector2(-4f, -4f));
        AddImage(inner.gameObject, Color.black);
        return inner;
    }

    private RectTransform MakeRect(string objectName, Transform parent)
    {
        GameObject go = new GameObject(objectName, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go.GetComponent<RectTransform>();
    }

    private Image AddImage(GameObject go, Color color)
    {
        Image img = go.AddComponent<Image>();
        img.color = color;
        img.raycastTarget = false;
        return img;
    }

    private Text MakeText(string objectName, Transform parent, int size, TextAnchor alignment, Color color)
    {
        RectTransform rt = MakeRect(objectName, parent);
        Text t = rt.gameObject.AddComponent<Text>();
        t.font = font;
        t.fontSize = size;
        t.alignment = alignment;
        t.color = color;
        t.supportRichText = true;
        t.horizontalOverflow = HorizontalWrapMode.Wrap;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        t.raycastTarget = false;
        return t;
    }

    private static void Center(RectTransform rt, Vector2 size, Vector2 position)
    {
        Anchor(rt, new Vector2(0.5f, 0.5f), size, position);
    }

    // Anchors and pivots a rect to the same point (a corner, an edge middle or the center)
    private static void Anchor(RectTransform rt, Vector2 anchor, Vector2 size, Vector2 position)
    {
        rt.anchorMin = anchor;
        rt.anchorMax = anchor;
        rt.pivot = anchor;
        rt.sizeDelta = size;
        rt.anchoredPosition = position;
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