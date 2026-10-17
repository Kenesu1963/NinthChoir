using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Shown when the player dies: Restart Level, Respawn at Last Checkpoint, Exit to Title Screen.
/// Created automatically by PlayerStats, so there is nothing to set up.
/// Controls: W / S choose, action key 1 (J) or Enter confirms.
/// </summary>
[DefaultExecutionOrder(-70)]
public class DeathScreen : MonoBehaviour
{
    // =====================================================================
    //  Settings
    // =====================================================================

    private const float ShowDelay = 1.2f; // seconds between dying and the screen appearing
    private const float FadeTime = 0.8f;  // seconds the screen takes to fade in (the menu waits for it)

    // Name of your title screen scene. Leave it empty for now: "Exit" then just ends the simulation.
    // Later, put the scene's name here (and add the scene to the Build Settings).
    private const string TitleSceneName = "";

    private const string TitleText = "YOU DIED";
    private const string RestartLabel = "Restart Level";
    private const string RespawnCheckpointLabel = "Respawn at Last Checkpoint";
    private const string RespawnStartLabel = "Respawn at the Start"; // when no checkpoint was saved yet
    private const string ExitLabel = "Exit to Title Screen";

    // =====================================================================

    public static bool IsOpen { get; private set; }
    public static bool JustClosed => closedFrame == Time.frameCount;
    private static int closedFrame = -1;

    private PlayerStats stats;
    private Font font;
    private GameObject root;
    private CanvasGroup group;
    private Text subtitleText;
    private Text menuText;
    private Text hintText;

    private bool waiting;
    private float waitTimer;
    private float fadeTimer;
    private int index = 1; // starts on "Respawn"
    private float previousTimeScale = 1f;
    private readonly string[] labels = new string[3];

    public static DeathScreen Create(PlayerStats stats)
    {
        GameObject go = new GameObject("DeathScreen");
        DeathScreen screen = go.AddComponent<DeathScreen>();
        screen.Init(stats);
        return screen;
    }

    private void Awake()
    {
        IsOpen = false;
    }

    private void Init(PlayerStats owner)
    {
        stats = owner;
        BuildUI();
        root.SetActive(false);
        stats.Died += OnDied;
    }

    private void OnDestroy()
    {
        if (stats != null) stats.Died -= OnDied;
        if (IsOpen)
        {
            IsOpen = false;
            Time.timeScale = previousTimeScale;
        }
    }

    private void OnDied()
    {
        waiting = true;
        waitTimer = ShowDelay;
        DialogueManager.ForceClose(); // a conversation can't stay open over the death screen
        DialogueManager.SetPrompt(null);
    }

    // =====================================================================

    private void Update()
    {
        if (waiting)
        {
            if (stats == null || !stats.IsDead) { waiting = false; return; } // revived by something else

            waitTimer -= Time.unscaledDeltaTime;
            if (waitTimer <= 0f)
            {
                waiting = false;
                Open();
            }
            return;
        }

        if (!IsOpen) return;

        // Fade in. The menu can't be used until it's done, so no key press is wasted by accident.
        if (fadeTimer < FadeTime)
        {
            fadeTimer += Time.unscaledDeltaTime;
            group.alpha = Mathf.Clamp01(fadeTimer / FadeTime);
            return;
        }

        if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow)) Move(-1);
        else if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow)) Move(+1);

        if (GameControls.Pressed(GameAction.Action1) || Input.GetKeyDown(KeyCode.Return)) Choose();
    }

    private void Open()
    {
        IsOpen = true;
        previousTimeScale = Time.timeScale > 0f ? Time.timeScale : 1f;
        Time.timeScale = 0f; // pause

        labels[0] = RestartLabel;
        labels[1] = RespawnManager.Instance.HasCheckpoint ? RespawnCheckpointLabel : RespawnStartLabel;
        labels[2] = ExitLabel;

        subtitleText.text = stats.CharacterName + " has fallen.";
        hintText.text = "W / S: choose        " + GameControls.GetKey(GameAction.Action1) + ": confirm";

        index = 1;
        fadeTimer = 0f;
        group.alpha = 0f;
        Render();
        root.SetActive(true);
    }

    private void Close()
    {
        IsOpen = false;
        closedFrame = Time.frameCount;
        Time.timeScale = previousTimeScale;
        root.SetActive(false);
    }

    private void Move(int direction)
    {
        index = ((index + direction) % labels.Length + labels.Length) % labels.Length;
        Render();
    }

    private void Choose()
    {
        switch (index)
        {
            case 0: RestartLevel(); break;
            case 1: RespawnAtCheckpoint(); break;
            default: ExitToTitle(); break;
        }
    }

    private void Render()
    {
        string result = "";
        for (int i = 0; i < labels.Length; i++)
        {
            if (i > 0) result += "\n";
            result += i == index
                ? "<color=#FFD966>\u00BB  " + labels[i] + "  \u00AB</color>"
                : "<color=#B8B8B8>" + labels[i] + "</color>";
        }
        menuText.text = result;
    }

    // =====================================================================
    //  The three options
    // =====================================================================

    private void RespawnAtCheckpoint()
    {
        Close();
        RespawnManager.Instance.RespawnPlayer();
    }

    private void RestartLevel()
    {
        Close(); // resumes time before the scene reloads

        Scene scene = SceneManager.GetActiveScene();

#if UNITY_EDITOR
        // Works even if the scene isn't in the Build Settings yet
        if (SceneUtility.GetBuildIndexByScenePath(scene.path) < 0)
        {
            UnityEditor.SceneManagement.EditorSceneManager.LoadSceneInPlayMode(
                scene.path, new LoadSceneParameters(LoadSceneMode.Single));
            return;
        }
#endif
        SceneManager.LoadScene(scene.buildIndex);
    }

    private void ExitToTitle()
    {
        Close();

        if (!string.IsNullOrEmpty(TitleSceneName))
        {
            SceneManager.LoadScene(TitleSceneName);
            return;
        }

        // No title screen yet: end the simulation
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // =====================================================================
    //  UI (built from code so there's nothing to set up by hand)
    // =====================================================================

    private void BuildUI()
    {
        font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        GameObject canvasGO = new GameObject("DeathCanvas");
        canvasGO.transform.SetParent(transform, false);
        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 130; // above everything
        CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280f, 720f);
        scaler.matchWidthOrHeight = 0.5f;
        group = canvasGO.AddComponent<CanvasGroup>();
        root = canvasGO;

        // Dark, slightly red backdrop
        RectTransform backdrop = MakeRect("Backdrop", canvasGO.transform);
        Stretch(backdrop, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        AddImage(backdrop.gameObject, new Color(0.06f, 0f, 0f, 0.9f));

        Text title = MakeText("Title", canvasGO.transform, 96, TextAnchor.MiddleCenter, new Color(0.78f, 0.1f, 0.1f));
        title.fontStyle = FontStyle.Bold;
        title.text = TitleText;
        Center(title.rectTransform, new Vector2(1000f, 120f), new Vector2(0f, 170f));
        AddOutline(title.gameObject);

        subtitleText = MakeText("Subtitle", canvasGO.transform, 30, TextAnchor.MiddleCenter, new Color(0.7f, 0.7f, 0.72f));
        Center(subtitleText.rectTransform, new Vector2(1000f, 44f), new Vector2(0f, 90f));

        menuText = MakeText("Menu", canvasGO.transform, 36, TextAnchor.UpperCenter, Color.white);
        menuText.lineSpacing = 1.5f;
        Center(menuText.rectTransform, new Vector2(1000f, 200f), new Vector2(0f, -40f));

        hintText = MakeText("Hint", canvasGO.transform, 18, TextAnchor.MiddleCenter, new Color(0.6f, 0.6f, 0.65f));
        Center(hintText.rectTransform, new Vector2(1000f, 30f), new Vector2(0f, -320f));
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

    private void AddOutline(GameObject go)
    {
        Outline outline = go.AddComponent<Outline>();
        outline.effectColor = new Color(0f, 0f, 0f, 0.9f);
        outline.effectDistance = new Vector2(3f, -3f);
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