using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// The inventory screen. Press the inventory key (F by default) to pause the game and open it.
///
/// A rectangular frame shows a zoomed-in part of a huge, rotating diagram of the godhead.
/// The center of the diagram sits exactly on the TOP EDGE of the frame, and the whole diagram
/// (with the slot pages on it) turns around that point like a wheel.
/// Three pages sit on the wheel, each a triangle of slots pointing down (rows of 4, 3, 2, 1):
///     Body = weapons,   Mind = tools,   Soul = incantations.
///
/// Controls:  arrows / WASD move the selector.  Pushing against the left or right limit of a row
/// spins the wheel to the next page.  Action key 1 (J) opens the options for the selected item.
///
/// The diagram image is loaded from  Assets/Resources/GodheadDiagram  (any .png / .jpg).
/// Created automatically by PlayerInventory, so there is nothing to set up in the scene.
/// </summary>
[DefaultExecutionOrder(-90)]
public class InventoryMenu : MonoBehaviour
{
    // =====================================================================
    //  Tuning (UI units on a 1280 x 720 screen)
    // =====================================================================

    private const string DiagramResourceName = "GodheadDiagram";

    // The diagram image must have the diagram centered in it. The wheel then turns around the
    // exact middle of the image: half of the width and half of the height.
    // DiagramWidth is how wide the image is drawn. A bigger number zooms in more.
    private const float DiagramWidth = 3350f;
    private static readonly Vector2 DiagramCenter = new Vector2(0.5f, 0.5f);
    private static readonly Color DiagramTint = new Color(0.86f, 0.80f, 0.66f, 1f); // makes the white paper look aged

    private const float FrameWidth = 1000f;
    private const float FrameHeight = 580f;
    private const float WheelRadius = FrameHeight / 2f; // the active page sits in the middle of the frame
    private const float SlotSize = 88f;
    private const float SlotGap = 10f;
    private const float TurnSmoothTime = 0.25f;          // seconds, how fast the wheel spins

    private static readonly string[] PageNames = { "BODY", "MIND", "SOUL" };
    private static readonly string[] PageSubtitles = { "Weapons", "Tools", "Incantations" };

    private static readonly int[] RowCounts = { 4, 3, 2, 1 }; // a triangle pointing down
    private static readonly int[] RowStart = { 0, 4, 7, 9 };  // index of the first slot of each row
    private static readonly GameAction[] ActionButtons = { GameAction.Action1, GameAction.Action2, GameAction.Action3 };

    private static readonly Color Gold = new Color(0.85f, 0.72f, 0.35f, 1f);
    private static readonly Color Parchment = new Color(0.95f, 0.90f, 0.78f, 1f);

    // =====================================================================

    public static bool IsOpen { get; private set; }

    private class SlotView
    {
        public RectTransform rect;
        public Image background;
        public Outline outline;
        public Text nameText;
        public Image icon;
        public GameObject equippedTag;
    }

    private class MenuEntry
    {
        public string label;
        public string note;
        public bool enabled = true;
        public Action onChoose;
    }

    private enum State { Browse, Actions, Picker, Info }

    private PlayerInventory inventory;
    private Font font;
    private GameObject root;
    private RectTransform wheel;
    private Text headerText;
    private Text hintText;
    private SlotView[][] slots;
    private CanvasGroup[] pageGroups;

    // selector
    private RectTransform selector;
    private Image[] selectorBars;
    private int selRow;
    private int selCol;
    private float desiredX; // keeps your column when moving up and down

    // side panel (options / check)
    private GameObject panelRoot;
    private Text panelTitle;
    private Text panelBody;
    private readonly List<MenuEntry> entries = new List<MenuEntry>();
    private int entryIndex;
    private ItemData selectedItem;
    private State state;

    // wheel
    private float previousTimeScale = 1f;
    private int steps;               // how many pages the wheel has turned (can be negative)
    private float wheelAngle;
    private float targetAngle;
    private float angleVelocity;

    private int PageIndex => ((steps % PageNames.Length) + PageNames.Length) % PageNames.Length;

    // =====================================================================
    //  Creation
    // =====================================================================

    public static InventoryMenu Create(PlayerInventory inventory)
    {
        GameObject go = new GameObject("InventoryMenu");
        InventoryMenu menu = go.AddComponent<InventoryMenu>();
        menu.Init(inventory);
        return menu;
    }

    private void Awake()
    {
        IsOpen = false;
    }

    private void Init(PlayerInventory owner)
    {
        inventory = owner;
        BuildUI();
        ApplyWheel();
        root.SetActive(false);
    }

    private void OnDestroy()
    {
        if (IsOpen)
        {
            IsOpen = false;
            Time.timeScale = previousTimeScale;
        }
    }

    // =====================================================================
    //  Update (runs even while the game is paused)
    // =====================================================================

    private void Update()
    {
        if (!IsOpen)
        {
            if (GameControls.Pressed(GameAction.Inventory) && !DialogueManager.IsOpen) Open();
            return;
        }

        if (GameControls.Pressed(GameAction.Inventory))
        {
            Close();
            return;
        }

        switch (state)
        {
            case State.Browse: UpdateBrowse(); break;
            case State.Actions:
            case State.Picker: UpdateList(); break;
            case State.Info: UpdateInfo(); break;
        }

        // Spin the wheel (unscaled time, because the game is paused)
        wheelAngle = Mathf.SmoothDamp(wheelAngle, targetAngle, ref angleVelocity,
                                      TurnSmoothTime, Mathf.Infinity, Time.unscaledDeltaTime);
        ApplyWheel();
        PulseSelector();
    }

    private static bool LeftPressed => Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow);
    private static bool RightPressed => Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow);
    private static bool UpPressed => Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow);
    private static bool DownPressed => Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow);

    private void Open()
    {
        IsOpen = true;
        previousTimeScale = Time.timeScale > 0f ? Time.timeScale : 1f;
        Time.timeScale = 0f; // pause

        state = State.Browse;
        panelRoot.SetActive(false);
        RefreshSlots();
        UpdateHeader();
        MoveSelector();
        ApplyWheel();
        SetHint(BrowseHint());
        root.SetActive(true);
    }

    private void Close()
    {
        IsOpen = false;
        Time.timeScale = previousTimeScale;
        panelRoot.SetActive(false);
        root.SetActive(false);
    }

    // =====================================================================
    //  Browsing (moving the selector)
    // =====================================================================

    private void UpdateBrowse()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Close();
            return;
        }

        if (LeftPressed) MoveHorizontal(-1);
        else if (RightPressed) MoveHorizontal(+1);

        if (UpPressed) MoveVertical(-1);
        else if (DownPressed) MoveVertical(+1);

        if (GameControls.Pressed(GameAction.Action1)) OpenActions();
    }

    private static float SlotOffsetX(int row, int col)
    {
        return col - (RowCounts[row] - 1) / 2f; // in slots, 0 = middle of the row
    }

    private void MoveHorizontal(int direction)
    {
        int newCol = selCol + direction;

        if (newCol < 0)
        {
            // Pushed against the left limit: turn to the previous page, arriving at the right end of the row
            Turn(-1);
            selCol = RowCounts[selRow] - 1;
        }
        else if (newCol >= RowCounts[selRow])
        {
            // Pushed against the right limit: turn to the next page, arriving at the left end of the row
            Turn(+1);
            selCol = 0;
        }
        else
        {
            selCol = newCol;
        }

        desiredX = SlotOffsetX(selRow, selCol);
        MoveSelector();
    }

    private void MoveVertical(int direction)
    {
        int newRow = Mathf.Clamp(selRow + direction, 0, RowCounts.Length - 1);
        if (newRow == selRow) return;

        selRow = newRow;

        // Pick the slot in the new row that is closest to where we were
        int best = 0;
        float bestDistance = float.MaxValue;
        for (int c = 0; c < RowCounts[selRow]; c++)
        {
            float distance = Mathf.Abs(SlotOffsetX(selRow, c) - desiredX);
            if (distance < bestDistance - 0.001f)
            {
                best = c;
                bestDistance = distance;
            }
        }
        selCol = best;
        MoveSelector();
    }

    private void Turn(int direction)
    {
        steps += direction;
        targetAngle = -120f * steps; // negative = clockwise: the next page swings in from the top right
        UpdateHeader();
    }

    private ItemData SelectedItem()
    {
        IReadOnlyList<ItemData> items = inventory.GetItems((ItemCategory)PageIndex);
        int index = RowStart[selRow] + selCol;
        return index < items.Count ? items[index] : null;
    }

    // =====================================================================
    //  Option menus (side panel)
    // =====================================================================

    private void OpenActions()
    {
        ItemData item = SelectedItem();
        if (item == null) return; // empty slot: nothing to do
        selectedItem = item;
        ShowActions();
    }

    private void ShowActions()
    {
        bool equipped = inventory.IsEquipped(selectedItem);

        entries.Clear();
        if (equipped) entries.Add(new MenuEntry { label = "Unequip", onChoose = DoUnequip });
        else entries.Add(new MenuEntry { label = "Equip", onChoose = ShowEquipPicker });

        entries.Add(new MenuEntry { label = "Check", onChoose = ShowInfo });

        entries.Add(new MenuEntry
        {
            label = "Swap",
            enabled = equipped,
            note = equipped ? "" : "equipped items only",
            onChoose = ShowSwapPicker
        });

        entryIndex = 0;
        state = State.Actions;
        panelTitle.text = selectedItem.itemName;
        panelBody.fontSize = 22;
        panelRoot.SetActive(true);
        RenderList();
        SetHint(ListHint());
    }

    // "Equip": choose which action button gets the item. An occupied button is replaced.
    private void ShowEquipPicker()
    {
        PlayerCombat combat = inventory.Combat;
        if (combat == null) return;

        entries.Clear();
        foreach (GameAction action in ActionButtons)
        {
            GameAction target = action; // captured by the lambda below
            ItemData current = combat.GetEquipped(target);
            entries.Add(new MenuEntry
            {
                label = "[" + GameControls.GetKey(target) + "]  " + (current != null ? current.itemName : "(empty)"),
                note = current != null ? "replaces" : "",
                onChoose = () =>
                {
                    combat.Equip(target, selectedItem); // whatever was there gets unequipped
                    FinishChange();
                }
            });
        }

        entryIndex = 0;
        state = State.Picker;
        panelTitle.text = "Equip to which button?";
        panelBody.fontSize = 22;
        RenderList();
        SetHint(ListHint());
    }

    // "Swap": exchange the action button of this equipped item with another button.
    private void ShowSwapPicker()
    {
        PlayerCombat combat = inventory.Combat;
        if (combat == null || !combat.TryGetEquippedAction(selectedItem, out GameAction currentAction)) return;

        entries.Clear();
        foreach (GameAction action in ActionButtons)
        {
            if (action == currentAction) continue;

            GameAction target = action;
            ItemData other = combat.GetEquipped(target);
            entries.Add(new MenuEntry
            {
                label = "[" + GameControls.GetKey(target) + "]  " + (other != null ? other.itemName : "(empty)"),
                note = other != null ? "swap" : "move here",
                onChoose = () =>
                {
                    combat.SwapSlots(currentAction, target);
                    FinishChange();
                }
            });
        }

        entryIndex = 0;
        state = State.Picker;
        panelTitle.text = "Swap with which button?";
        panelBody.fontSize = 22;
        RenderList();
        SetHint(ListHint());
    }

    private void DoUnequip()
    {
        PlayerCombat combat = inventory.Combat;
        if (combat != null && combat.TryGetEquippedAction(selectedItem, out GameAction action))
        {
            combat.Unequip(action);
        }
        FinishChange();
    }

    private void ShowInfo()
    {
        state = State.Info;
        panelTitle.text = selectedItem.itemName;
        panelBody.fontSize = 18;
        string description = string.IsNullOrEmpty(selectedItem.description) ? "No description." : selectedItem.description;
        panelBody.text = description + "\n\n<color=#C8B27A>" + selectedItem.GetStatsText() + "</color>";
        SetHint("J / Esc: back");
    }

    private void FinishChange()
    {
        RefreshSlots();
        state = State.Browse;
        panelRoot.SetActive(false);
        SetHint(BrowseHint());
    }

    private void UpdateList()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (state == State.Picker)
            {
                ShowActions();
            }
            else
            {
                state = State.Browse;
                panelRoot.SetActive(false);
                SetHint(BrowseHint());
            }
            return;
        }

        if (UpPressed) MoveEntry(-1);
        else if (DownPressed) MoveEntry(+1);

        if (GameControls.Pressed(GameAction.Action1))
        {
            MenuEntry entry = entries[entryIndex];
            if (entry.enabled && entry.onChoose != null) entry.onChoose();
        }
    }

    private void UpdateInfo()
    {
        if (Input.GetKeyDown(KeyCode.Escape) || GameControls.Pressed(GameAction.Action1))
        {
            ShowActions();
        }
    }

    private void MoveEntry(int direction)
    {
        int count = entries.Count;
        for (int step = 1; step <= count; step++)
        {
            int index = (((entryIndex + direction * step) % count) + count) % count;
            if (entries[index].enabled)
            {
                entryIndex = index;
                break;
            }
        }
        RenderList();
    }

    private void RenderList()
    {
        string result = "";
        for (int i = 0; i < entries.Count; i++)
        {
            if (i > 0) result += "\n";

            MenuEntry entry = entries[i];
            string text = entry.label;
            if (!string.IsNullOrEmpty(entry.note)) text += "  <size=15>" + entry.note + "</size>";

            if (!entry.enabled) result += "<color=#6B675E>   " + text + "</color>";
            else if (i == entryIndex) result += "<color=#FFD966>> " + text + "</color>";
            else result += "   " + text;
        }
        panelBody.text = result;
    }

    // =====================================================================
    //  Content
    // =====================================================================

    private string BrowseHint()
    {
        return "Arrows / WASD: move  (push against the edge to turn the wheel)        " +
               GameControls.GetKey(GameAction.Action1) + ": options        Esc / " +
               GameControls.GetKey(GameAction.Inventory) + ": close";
    }

    private string ListHint()
    {
        return "W / S: choose        " + GameControls.GetKey(GameAction.Action1) + ": confirm        Esc: back";
    }

    private void SetHint(string text)
    {
        hintText.text = text;
    }

    private void UpdateHeader()
    {
        string result = "<color=#8E8A80>\u00AB</color>     ";
        for (int i = 0; i < PageNames.Length; i++)
        {
            if (i > 0) result += "     ";
            result += i == PageIndex
                ? "<color=#FFD966>" + PageNames[i] + "</color>"
                : "<color=#8E8A80>" + PageNames[i] + "</color>";
        }
        result += "     <color=#8E8A80>\u00BB</color>";
        headerText.text = result;
    }

    private void RefreshSlots()
    {
        for (int page = 0; page < PageNames.Length; page++)
        {
            IReadOnlyList<ItemData> items = inventory.GetItems((ItemCategory)page);

            for (int i = 0; i < PlayerInventory.SlotsPerPage; i++)
            {
                SlotView view = slots[page][i];
                ItemData item = i < items.Count ? items[i] : null;
                bool hasItem = item != null;
                bool equipped = hasItem && inventory.IsEquipped(item);

                view.background.color = hasItem ? new Color(0.06f, 0.05f, 0.04f, 0.88f)
                                                : new Color(0f, 0f, 0f, 0.35f);
                view.outline.effectColor = equipped ? Gold
                                         : hasItem ? new Color(0.85f, 0.72f, 0.35f, 0.55f)
                                                   : new Color(0.85f, 0.72f, 0.35f, 0.25f);

                view.nameText.text = hasItem ? item.itemName : "";
                bool hasIcon = hasItem && item.icon != null;
                view.icon.enabled = hasIcon;
                if (hasIcon) view.icon.sprite = item.icon;
                view.nameText.alignment = hasIcon ? TextAnchor.UpperCenter : TextAnchor.MiddleCenter;

                view.equippedTag.SetActive(equipped);
            }
        }
    }

    // Turns the wheel and fades pages in and out so only the page near the bottom is visible.
    private void ApplyWheel()
    {
        wheel.localRotation = Quaternion.Euler(0f, 0f, wheelAngle);

        for (int k = 0; k < pageGroups.Length; k++)
        {
            float pageAngle = -90f + 120f * k + wheelAngle;
            float away = Mathf.Abs(Mathf.DeltaAngle(-90f, pageAngle)); // 0 = in the middle of the frame
            pageGroups[k].alpha = Mathf.Clamp01((90f - away) / 60f);
        }
    }

    private void MoveSelector()
    {
        SlotView view = slots[PageIndex][RowStart[selRow] + selCol];
        selector.SetParent(view.rect, false);
        selector.anchorMin = Vector2.zero;
        selector.anchorMax = Vector2.one;
        selector.offsetMin = new Vector2(-7f, -7f);
        selector.offsetMax = new Vector2(7f, 7f);
        selector.localScale = Vector3.one;
        selector.SetAsLastSibling();
    }

    private void PulseSelector()
    {
        float alpha = 0.7f + 0.3f * Mathf.Sin(Time.unscaledTime * 6f);
        Color color = new Color(1f, 0.88f, 0.4f, alpha);
        foreach (Image bar in selectorBars) bar.color = color;
    }

    // =====================================================================
    //  UI (built from code so there's nothing to set up by hand)
    // =====================================================================

    private void BuildUI()
    {
        font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        GameObject canvasGO = new GameObject("InventoryCanvas");
        canvasGO.transform.SetParent(transform, false);
        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 110; // above the dialogue box and the HUD
        CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280f, 720f);
        scaler.matchWidthOrHeight = 0.5f;
        root = canvasGO;

        // --- Dark backdrop ---
        RectTransform dimmer = MakeRect("Dimmer", canvasGO.transform);
        Stretch(dimmer, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        AddImage(dimmer.gameObject, new Color(0f, 0f, 0f, 0.82f));

        // --- Header (which page you're on) ---
        headerText = MakeText("Header", canvasGO.transform, 30, TextAnchor.MiddleCenter, FontStyle.Bold, Parchment);
        Center(headerText.rectTransform, new Vector2(FrameWidth, 40f), new Vector2(0f, 314f));

        // --- Frame: gold border + clipped viewport ---
        RectTransform border = MakeRect("FrameBorder", canvasGO.transform);
        Center(border, new Vector2(FrameWidth + 8f, FrameHeight + 8f), new Vector2(0f, -22f));
        AddImage(border.gameObject, Gold);

        RectTransform viewport = MakeRect("Viewport", border);
        Center(viewport, new Vector2(FrameWidth, FrameHeight), Vector2.zero);
        AddImage(viewport.gameObject, new Color(0.05f, 0.04f, 0.03f, 1f));
        viewport.gameObject.AddComponent<RectMask2D>(); // everything outside the frame is cut off

        // --- Hint ---
        hintText = MakeText("Hint", canvasGO.transform, 17, TextAnchor.MiddleCenter, FontStyle.Normal,
                            new Color(0.7f, 0.7f, 0.72f));
        Center(hintText.rectTransform, new Vector2(1200f, 28f), new Vector2(0f, -340f));

        // --- The wheel. Its center is the TOP EDGE of the frame. ---
        wheel = MakeRect("Wheel", viewport);
        Center(wheel, Vector2.zero, new Vector2(0f, FrameHeight / 2f));

        BuildDiagram();
        BuildPages();
        BuildSelector();
        BuildSidePanel(viewport);
    }

    private void BuildDiagram()
    {
        Sprite diagram = Resources.Load<Sprite>(DiagramResourceName);
        if (diagram == null)
        {
            Texture2D tex = Resources.Load<Texture2D>(DiagramResourceName);
            if (tex != null)
            {
                diagram = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
            }
        }

        if (diagram == null)
        {
            Debug.LogWarning("InventoryMenu: couldn't find the diagram image. Put it in Assets/Resources and name it '" +
                             DiagramResourceName + "'. The menu works without it, but the background will be plain.");
            return;
        }

        RectTransform rt = MakeRect("Diagram", wheel);
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = DiagramCenter; // the wheel turns around the diagram's own center
        float aspect = diagram.rect.height / diagram.rect.width;
        rt.sizeDelta = new Vector2(DiagramWidth, DiagramWidth * aspect);
        rt.anchoredPosition = Vector2.zero;

        Image img = rt.gameObject.AddComponent<Image>();
        img.sprite = diagram;
        img.color = DiagramTint;
        img.raycastTarget = false;
    }

    private void BuildPages()
    {
        slots = new SlotView[PageNames.Length][];
        pageGroups = new CanvasGroup[PageNames.Length];
        float pitch = SlotSize + SlotGap;

        for (int k = 0; k < PageNames.Length; k++)
        {
            // Pages sit on the wheel 120 degrees apart. Page 0 starts at the bottom, in the middle of the frame.
            float angle = (-90f + 120f * k) * Mathf.Deg2Rad;

            RectTransform page = MakeRect("Page_" + PageNames[k], wheel);
            Center(page, Vector2.zero, new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * WheelRadius);
            page.localRotation = Quaternion.Euler(0f, 0f, 120f * k); // so it ends up upright when it reaches the bottom
            pageGroups[k] = page.gameObject.AddComponent<CanvasGroup>();

            // Page title
            Text title = MakeText("Title", page, 32, TextAnchor.MiddleCenter, FontStyle.Bold, Gold);
            title.text = PageNames[k] + "   <size=20><color=#C8B27A>" + PageSubtitles[k] + "</color></size>";
            Center(title.rectTransform, new Vector2(600f, 44f), new Vector2(0f, 226f));
            AddOutline(title.gameObject);

            // Slots: a triangle pointing down, rows of 4, 3, 2 and 1
            slots[k] = new SlotView[PlayerInventory.SlotsPerPage];
            for (int row = 0; row < RowCounts.Length; row++)
            {
                float y = (1.5f - row) * pitch;
                for (int col = 0; col < RowCounts[row]; col++)
                {
                    float x = SlotOffsetX(row, col) * pitch;
                    slots[k][RowStart[row] + col] = BuildSlot(page, new Vector2(x, y));
                }
            }
        }
    }

    private SlotView BuildSlot(RectTransform page, Vector2 position)
    {
        RectTransform rt = MakeRect("Slot", page);
        Center(rt, new Vector2(SlotSize, SlotSize), position);

        SlotView view = new SlotView();
        view.rect = rt;
        view.background = AddImage(rt.gameObject, new Color(0f, 0f, 0f, 0.35f));
        view.outline = rt.gameObject.AddComponent<Outline>();
        view.outline.effectDistance = new Vector2(2f, -2f);

        // Optional item icon
        RectTransform iconRt = MakeRect("Icon", rt);
        Center(iconRt, new Vector2(46f, 46f), new Vector2(0f, -6f));
        view.icon = iconRt.gameObject.AddComponent<Image>();
        view.icon.preserveAspect = true;
        view.icon.raycastTarget = false;
        view.icon.enabled = false;

        // Item name
        view.nameText = MakeText("Name", rt, 15, TextAnchor.MiddleCenter, FontStyle.Bold, Parchment);
        view.nameText.verticalOverflow = VerticalWrapMode.Truncate;
        Stretch(view.nameText.rectTransform, Vector2.zero, Vector2.one, new Vector2(4f, 22f), new Vector2(-4f, -4f));

        // "EQUIPPED" tag
        RectTransform tagRt = MakeRect("EquippedTag", rt);
        tagRt.anchorMin = new Vector2(0.5f, 0f);
        tagRt.anchorMax = new Vector2(0.5f, 0f);
        tagRt.pivot = new Vector2(0.5f, 0f);
        tagRt.sizeDelta = new Vector2(70f, 16f);
        tagRt.anchoredPosition = new Vector2(0f, 4f);
        AddImage(tagRt.gameObject, Gold);

        Text tagText = MakeText("Text", tagRt, 11, TextAnchor.MiddleCenter, FontStyle.Bold, new Color(0.1f, 0.07f, 0.02f));
        tagText.horizontalOverflow = HorizontalWrapMode.Overflow;
        tagText.text = "EQUIPPED";
        Stretch(tagText.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        view.equippedTag = tagRt.gameObject;
        view.equippedTag.SetActive(false);
        return view;
    }

    // A hollow gold frame that moves from slot to slot.
    private void BuildSelector()
    {
        selector = MakeRect("Selector", wheel);
        selectorBars = new Image[4];
        const float t = 4f; // thickness

        selectorBars[0] = MakeBar(selector, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -t), new Vector2(0f, 0f)); // top
        selectorBars[1] = MakeBar(selector, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 0f), new Vector2(0f, t));  // bottom
        selectorBars[2] = MakeBar(selector, new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f, 0f), new Vector2(t, 0f));  // left
        selectorBars[3] = MakeBar(selector, new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(-t, 0f), new Vector2(0f, 0f)); // right
    }

    private Image MakeBar(RectTransform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
    {
        RectTransform rt = MakeRect("Bar", parent);
        Stretch(rt, anchorMin, anchorMax, offsetMin, offsetMax);
        return AddImage(rt.gameObject, new Color(1f, 0.88f, 0.4f, 1f));
    }

    // Fixed panel on the right side of the frame, used for the options, pickers and the "Check" info.
    private void BuildSidePanel(RectTransform viewport)
    {
        RectTransform rt = MakeRect("SidePanel", viewport);
        Center(rt, new Vector2(290f, 440f), new Vector2(345f, 0f));
        AddImage(rt.gameObject, new Color(0.04f, 0.03f, 0.02f, 0.95f));
        Outline outline = rt.gameObject.AddComponent<Outline>();
        outline.effectColor = Gold;
        outline.effectDistance = new Vector2(2f, -2f);
        panelRoot = rt.gameObject;

        panelTitle = MakeText("Title", rt, 24, TextAnchor.UpperLeft, FontStyle.Bold, Gold);
        Stretch(panelTitle.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(16f, -76f), new Vector2(-16f, -12f));

        panelBody = MakeText("Body", rt, 22, TextAnchor.UpperLeft, FontStyle.Normal, Parchment);
        panelBody.verticalOverflow = VerticalWrapMode.Truncate;
        panelBody.lineSpacing = 1.15f;
        Stretch(panelBody.rectTransform, Vector2.zero, Vector2.one,
                new Vector2(16f, 16f), new Vector2(-16f, -90f));

        panelRoot.SetActive(false);
    }

    // =====================================================================
    //  UI helpers
    // =====================================================================

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

    private void AddOutline(GameObject go)
    {
        Outline outline = go.AddComponent<Outline>();
        outline.effectColor = new Color(0f, 0f, 0f, 0.9f);
        outline.effectDistance = new Vector2(2f, -2f);
    }

    private Text MakeText(string objectName, Transform parent, int size, TextAnchor alignment,
                          FontStyle style, Color color)
    {
        RectTransform rt = MakeRect(objectName, parent);
        Text t = rt.gameObject.AddComponent<Text>();
        t.font = font;
        t.fontSize = size;
        t.alignment = alignment;
        t.fontStyle = style;
        t.color = color;
        t.supportRichText = true;
        t.horizontalOverflow = HorizontalWrapMode.Wrap;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        t.raycastTarget = false;
        return t;
    }

    private static void Center(RectTransform rt, Vector2 size, Vector2 position)
    {
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
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