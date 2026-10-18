using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Fades the screen to black, loads another scene, and fades back in.
/// It survives scene loads (that's how it can finish the fade in the new scene). Creates itself.
/// </summary>
public class ScreenFader : MonoBehaviour
{
    private static ScreenFader instance;

    public static ScreenFader Instance
    {
        get
        {
            if (instance == null)
            {
                GameObject go = new GameObject("ScreenFader");
                instance = go.AddComponent<ScreenFader>();
            }
            return instance;
        }
    }

    /// <summary>True from the start of the fade-out until the fade-in is finished. The player can't act meanwhile.</summary>
    public static bool IsBusy { get; private set; }

    private Image blackImage;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        instance = null;
        IsBusy = false;
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        DontDestroyOnLoad(gameObject);

        GameObject canvasGO = new GameObject("FadeCanvas");
        canvasGO.transform.SetParent(transform, false);
        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 200; // above everything

        GameObject imageGO = new GameObject("Black", typeof(RectTransform));
        imageGO.transform.SetParent(canvasGO.transform, false);
        RectTransform rect = imageGO.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        blackImage = imageGO.AddComponent<Image>();
        blackImage.color = new Color(0f, 0f, 0f, 0f);
        blackImage.raycastTarget = false;
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
            IsBusy = false;
        }
    }

    public void GoToScene(string sceneName, string entryId, float fadeOutTime, float fadeInTime)
    {
        if (IsBusy) return;
        StartCoroutine(Run(sceneName, entryId, fadeOutTime, fadeInTime));
    }

    private IEnumerator Run(string sceneName, string entryId, float fadeOutTime, float fadeInTime)
    {
        IsBusy = true;

        yield return Fade(0f, 1f, fadeOutTime);

        // Remember the player (health, money, items...) before this scene disappears
        PlayerCarryOver.Capture(entryId);

        AsyncOperation load = LoadAsync(sceneName);
        if (load == null)
        {
            Debug.LogError("Couldn't load the scene '" + sceneName + "'. Check the name, and that it's in the Build Settings.");
            PlayerCarryOver.Clear();
            yield return Fade(1f, 0f, fadeInTime);
            IsBusy = false;
            yield break;
        }

        while (!load.isDone) yield return null;

        // Give the new scene a couple of frames to start up (the player moves to the entry point)
        yield return null;
        yield return null;

        yield return Fade(1f, 0f, fadeInTime);
        IsBusy = false;
    }

    private IEnumerator Fade(float from, float to, float duration)
    {
        if (duration <= 0f)
        {
            SetAlpha(to);
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            SetAlpha(Mathf.Lerp(from, to, elapsed / duration));
            yield return null;
        }
        SetAlpha(to);
    }

    private void SetAlpha(float alpha)
    {
        blackImage.color = new Color(0f, 0f, 0f, alpha);
    }

    private static AsyncOperation LoadAsync(string sceneName)
    {
        if (!Application.CanStreamedLevelBeLoaded(sceneName))
        {
#if UNITY_EDITOR
            // In the editor, scenes that aren't in the Build Settings can still be loaded
            foreach (string guid in UnityEditor.AssetDatabase.FindAssets("t:Scene " + sceneName))
            {
                string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                if (System.IO.Path.GetFileNameWithoutExtension(path) == sceneName)
                {
                    return UnityEditor.SceneManagement.EditorSceneManager.LoadSceneAsyncInPlayMode(
                        path, new LoadSceneParameters(LoadSceneMode.Single));
                }
            }
#endif
            return null;
        }

        return SceneManager.LoadSceneAsync(sceneName);
    }
}