using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Runs signs and NPC conversations and draws the dialogue box.
/// It creates itself (and its UI) the first time it's needed, so there is nothing to set up in the scene.
/// Controls: action key 1 (J by default) to advance / confirm, W/S or Up/Down to choose an answer.
/// </summary>
[DefaultExecutionOrder(-100)] // runs before the player scripts, so a key press is never used twice
public class DialogueManager : MonoBehaviour
{
    private static DialogueManager instance;

    public static DialogueManager Instance
    {
        get
        {
            if (instance == null)
            {
                GameObject go = new GameObject("DialogueManager");
                instance = go.AddComponent<DialogueManager>();
            }
            return instance;
        }
    }

    public static bool IsOpen => instance != null && instance.isOpen;
    public static bool JustClosed => instance != null && instance.closedFrame == Time.frameCount;

    /// <summary>Closes the conversation right now (e.g. when the player dies).</summary>
    public static void ForceClose()
    {
        if (instance != null && instance.isOpen) instance.Close();
    }

    public float charsPerSecond = 45f; // typewriter speed

    private enum Mode { Sign, Conversation }

    // --- UI ---
    private Font font;
    private GameObject panel;
    private RectTransform bodyRect;
    private Text speakerText;
    private Text bodyText;
    private Text choicesText;
    private Text hintText;
    private GameObject promptRoot;
    private Text promptText;
    private string currentPrompt;

    // --- State ---
    private bool isOpen;
    private int openedFrame = -1;
    private int closedFrame = -1;
    private Mode mode;

    private string fullText = "";
    private float visibleChars;
    private bool lineDone;
    private bool currentIsLast;
    private DialogueChoice[] currentChoices;
    private int selected;

    private string signTitle;
    private string[] signPages;
    private int pageIndex;

    private string defaultSpeaker;
    private readonly Dictionary<string, DialogueNode> nodeMap = new Dictionary<string, DialogueNode>();
    private DialogueNode currentNode;

    private bool HasChoices => currentChoices != null && currentChoices.Length > 0;

    // =====================================================================
    //  Public API
    // =====================================================================

    public void ShowSign(string title, string[] pages)
    {
        if (isOpen || pages == null || pages.Length == 0) return;

        mode = Mode.Sign;
        signTitle = title;
        signPages = pages;
        pageIndex = 0;

        Open();
        DisplayLine(signTitle, signPages[0], null, signPages.Length == 1);
    }

    public void ShowConversation(string npcName, List<DialogueNode> nodes)
    {
        if (isOpen || nodes == null || nodes.Count == 0) return;

        mode = Mode.Conversation;
        defaultSpeaker = npcName;

        nodeMap.Clear();
        foreach (DialogueNode node in nodes)
        {
            if (!string.IsNullOrEmpty(node.id)) nodeMap[node.id] = node;
        }

        Open();
        ShowNode(nodes[0]);
    }

    /// <summary>Shows a small "[J] Talk" hint at the bottom of the screen. Pass null to hide it.</summary>
    public static void SetPrompt(string text)
    {
        if (instance == null && text == null) return;
        Instance.ApplyPrompt(Instance.isOpen ? null : text);
    }

    // =====================================================================
    //  Unity
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
        panel.SetActive(false);
        promptRoot.SetActive(false);
    }

    private void OnDestroy()
    {
        if (instance == this) instance = null;
    }

    private void Update()
    {
        if (!isOpen) return;
        if (openedFrame == Time.frameCount) return; // ignore the key press that opened it
        if (ShopMenu.IsOpen) return;                // a shop was opened from this conversation: wait for it to close

        bool confirm = GameControls.Pressed(GameAction.Action1); // action key 1

        // Typewriter effect. Pressing the key while it types shows the full line instantly.
        if (!lineDone)
        {
            visibleChars += charsPerSecond * Time.unscaledDeltaTime;
            if (confirm || visibleChars >= fullText.Length)
            {
                FinishLine();
            }
            else
            {
                int count = Mathf.Clamp(Mathf.FloorToInt(visibleChars), 0, fullText.Length);
                bodyText.text = fullText.Substring(0, count);
            }
            return;
        }

        // Choosing an answer
        if (HasChoices)
        {
            int n = currentChoices.Length;
            if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow))
            {
                selected = (selected - 1 + n) % n;
                RefreshChoices();
            }
            if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow))
            {
                selected = (selected + 1) % n;
                RefreshChoices();
            }
        }

        if (confirm) Advance();
    }

    // =====================================================================
    //  Flow
    // =====================================================================

    private void Open()
    {
        isOpen = true;
        openedFrame = Time.frameCount;
        panel.SetActive(true);
        ApplyPrompt(null);
    }

    private void Close()
    {
        isOpen = false;
        closedFrame = Time.frameCount;
        panel.SetActive(false);
    }

    private void ShowNode(DialogueNode node)
    {
        currentNode = node;
        string speaker = string.IsNullOrEmpty(node.speaker) ? defaultSpeaker : node.speaker;
        bool hasChoices = node.choices != null && node.choices.Length > 0;
        bool isLast = !hasChoices && string.IsNullOrEmpty(node.next);
        DisplayLine(speaker, node.text, hasChoices ? node.choices : null, isLast);
    }

    private void GoToNode(string id)
    {
        if (string.IsNullOrEmpty(id))
        {
            Close();
            return;
        }

        if (!nodeMap.TryGetValue(id, out DialogueNode node))
        {
            Debug.LogWarning("Dialogue: there is no node with the id '" + id + "'. Ending the conversation.");
            Close();
            return;
        }

        ShowNode(node);
    }

    private void Advance()
    {
        if (mode == Mode.Sign)
        {
            pageIndex++;
            if (pageIndex >= signPages.Length) Close();
            else DisplayLine(signTitle, signPages[pageIndex], null, pageIndex == signPages.Length - 1);
            return;
        }

        // Conversation
        if (HasChoices)
        {
            DialogueChoice choice = currentChoices[selected];
            string target = choice.goTo;
            if (choice.onSelected != null) choice.onSelected.Invoke();
            GoToNode(target);
        }
        else
        {
            GoToNode(currentNode.next);
        }
    }

    private void DisplayLine(string speaker, string text, DialogueChoice[] choices, bool isLast)
    {
        currentChoices = choices;
        currentIsLast = isLast;
        selected = 0;
        fullText = text ?? "";
        visibleChars = 0f;
        lineDone = false;

        speakerText.text = speaker ?? "";
        speakerText.gameObject.SetActive(!string.IsNullOrEmpty(speaker));
        bodyText.text = "";
        choicesText.text = "";
        hintText.text = "";

        // More room for the text when there are no answer options
        bodyRect.offsetMin = new Vector2(28f, HasChoices ? 140f : 44f);
    }

    private void FinishLine()
    {
        lineDone = true;
        bodyText.text = fullText;

        string key = GameControls.GetKey(GameAction.Action1).ToString();
        if (HasChoices)
        {
            RefreshChoices();
            hintText.text = "W/S choose     [" + key + "] confirm";
        }
        else
        {
            hintText.text = "[" + key + "] " + (currentIsLast ? "close" : "continue");
        }
    }

    private void RefreshChoices()
    {
        string result = "";
        for (int i = 0; i < currentChoices.Length; i++)
        {
            if (i > 0) result += "\n";
            if (i == selected) result += "<color=#FFD966>> " + currentChoices[i].text + "</color>";
            else result += "   " + currentChoices[i].text;
        }
        choicesText.text = result;
    }

    private void ApplyPrompt(string text)
    {
        if (text == currentPrompt) return;
        currentPrompt = text;
        promptRoot.SetActive(text != null);
        if (text != null) promptText.text = text;
    }

    // =====================================================================
    //  UI (built from code so there's nothing to set up by hand)
    // =====================================================================

    private void BuildUI()
    {
        font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        GameObject canvasGO = new GameObject("DialogueCanvas");
        canvasGO.transform.SetParent(transform, false);
        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280f, 720f);
        scaler.matchWidthOrHeight = 0.5f;

        // --- Dialogue box ---
        RectTransform panelRect = MakeRect("DialoguePanel", canvasGO.transform);
        panelRect.anchorMin = new Vector2(0.5f, 0f);
        panelRect.anchorMax = new Vector2(0.5f, 0f);
        panelRect.pivot = new Vector2(0.5f, 0f);
        panelRect.sizeDelta = new Vector2(1000f, 280f);
        panelRect.anchoredPosition = new Vector2(0f, 30f);
        Image bg = panelRect.gameObject.AddComponent<Image>();
        bg.color = new Color(0.05f, 0.05f, 0.08f, 0.93f);
        bg.raycastTarget = false;
        Outline outline = panelRect.gameObject.AddComponent<Outline>();
        outline.effectColor = new Color(1f, 1f, 1f, 0.3f);
        outline.effectDistance = new Vector2(2f, -2f);
        panel = panelRect.gameObject;

        speakerText = MakeText("Speaker", panelRect, 26, TextAnchor.MiddleLeft, FontStyle.Bold);
        speakerText.color = new Color(1f, 0.85f, 0.4f);
        Stretch(speakerText.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(28f, -48f), new Vector2(-28f, -12f));

        bodyText = MakeText("Body", panelRect, 24, TextAnchor.UpperLeft, FontStyle.Normal);
        bodyRect = bodyText.rectTransform;
        Stretch(bodyRect, new Vector2(0f, 0f), new Vector2(1f, 1f),
                new Vector2(28f, 44f), new Vector2(-28f, -54f));

        choicesText = MakeText("Choices", panelRect, 24, TextAnchor.UpperLeft, FontStyle.Normal);
        choicesText.supportRichText = true;
        Stretch(choicesText.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f),
                new Vector2(28f, 16f), new Vector2(-28f, 128f));

        hintText = MakeText("Hint", panelRect, 16, TextAnchor.LowerRight, FontStyle.Normal);
        hintText.color = new Color(0.7f, 0.7f, 0.75f);
        Stretch(hintText.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f),
                new Vector2(28f, 8f), new Vector2(-24f, 32f));

        // --- Small "[J] Talk" prompt ---
        RectTransform promptRect = MakeRect("InteractPrompt", canvasGO.transform);
        promptRect.anchorMin = new Vector2(0.5f, 0f);
        promptRect.anchorMax = new Vector2(0.5f, 0f);
        promptRect.pivot = new Vector2(0.5f, 0f);
        promptRect.sizeDelta = new Vector2(320f, 44f);
        promptRect.anchoredPosition = new Vector2(0f, 40f);
        Image promptBg = promptRect.gameObject.AddComponent<Image>();
        promptBg.color = new Color(0f, 0f, 0f, 0.65f);
        promptBg.raycastTarget = false;
        promptRoot = promptRect.gameObject;

        promptText = MakeText("PromptText", promptRect, 22, TextAnchor.MiddleCenter, FontStyle.Normal);
        Stretch(promptText.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
    }

    private RectTransform MakeRect(string objectName, Transform parent)
    {
        GameObject go = new GameObject(objectName, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go.GetComponent<RectTransform>();
    }

    private Text MakeText(string objectName, Transform parent, int size, TextAnchor alignment, FontStyle style)
    {
        RectTransform rt = MakeRect(objectName, parent);
        Text t = rt.gameObject.AddComponent<Text>();
        t.font = font;
        t.fontSize = size;
        t.alignment = alignment;
        t.fontStyle = style;
        t.color = Color.white;
        t.horizontalOverflow = HorizontalWrapMode.Wrap;
        t.verticalOverflow = VerticalWrapMode.Truncate;
        t.supportRichText = false; // so typing a line character by character never shows half a tag
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