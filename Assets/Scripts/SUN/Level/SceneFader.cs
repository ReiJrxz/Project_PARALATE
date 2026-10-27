using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// จอดำ Fade + หน้า Loading (ข้อความ + หลอดความคืบหน้า) ใช้ตอนเปลี่ยน Scene และวาร์ป
/// สร้าง UI เองด้วยโค้ด ไม่ต้องวางใน Scene และใช้เวลาแบบ unscaled (ใช้ได้ตอนเกม Pause)
///
/// ปรับหน้าตา: สร้างวัตถุเปล่า ใส่สคริปต์นี้ ปรับค่าใน Inspector แล้วทำเป็น Prefab ชื่อ "SceneFader"
/// เก็บไว้ที่ Assets/Resources/SceneFader.prefab ระบบจะใช้ Prefab นั้นแทนค่าเริ่มต้นเอง
/// (ไม่ต้องวางใน Scene) ขนาดทั้งหมดเป็นหน่วยที่ความละเอียดอ้างอิง 1920x1080 และย่อ/ขยายตามจอให้เอง
/// </summary>
public class SceneFader : MonoBehaviour
{
    public static SceneFader Instance { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        if (Instance != null)
            return;

        // ถ้ามี Prefab ที่ Resources/SceneFader ใช้ตัวนั้น (หน้าตาที่ปรับเอง) ไม่งั้นสร้างค่าเริ่มต้น
        SceneFader prefab = Resources.Load<SceneFader>("SceneFader");
        if (prefab != null)
        {
            Instantiate(prefab);
            return;
        }

        new GameObject("SceneFader").AddComponent<SceneFader>();
    }

    [Header("พื้นหลัง")]
    [SerializeField] private Color backgroundColor = Color.black;

    [Header("ข้อความ Loading")]
    [SerializeField] private bool showLoadingText = true;
    [SerializeField] private string loadingMessage = "Loading";
    [SerializeField] private float loadingFontSize = 22f;
    [SerializeField] private Color loadingTextColor = new Color(1f, 1f, 1f, 0.75f);
    [Tooltip("ให้มีจุด ... วิ่งต่อท้ายข้อความ")]
    [SerializeField] private bool animateDots = true;

    [Header("หลอดความคืบหน้า")]
    [SerializeField] private bool showProgressBar = true;
    [Tooltip("ความกว้างของหลอด")]
    [SerializeField] private float barWidth = 360f;
    [Tooltip("ความหนาของหลอด (เล็ก = เส้นบางๆ)")]
    [SerializeField] private float barHeight = 4f;
    [Tooltip("ระยะห่างจากขอบล่างของจอ")]
    [SerializeField] private float bottomOffset = 70f;
    [SerializeField] private Color barColor = new Color(1f, 1f, 1f, 0.9f);
    [SerializeField] private Color barBackColor = new Color(1f, 1f, 1f, 0.15f);

    [Header("Preview (ใช้ตอนกด Play เท่านั้น)")]
    [Tooltip("ติ๊กเพื่อให้หน้า Loading ค้างบนจอ แล้วปรับค่าด้านบนดูผลแบบ realtime " +
             "(ค่าที่ปรับตอน Play จะหายเมื่อหยุดเกม: คลิกขวาหัว Component → Copy Component " +
             "แล้วหยุดเกม คลิกขวาที่ Prefab → Paste Component Values)")]
    [SerializeField] private bool previewLoading;

    private CanvasGroup group;
    private RectTransform fadeRoot;
    private GameObject loadingRoot;
    private RectTransform progressFill;
    private TextMeshProUGUI loadingLabel;
    private bool refreshRequested;
    private bool wasPreviewing;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        BuildUI();
        SetAlpha(0f);
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    // แก้ค่าใน Inspector ตอนกำลัง Play: ขอให้สร้างหน้า Loading ใหม่ (ทำใน Update เพราะห้ามสร้างวัตถุใน OnValidate)
    private void OnValidate()
    {
        if (Application.isPlaying)
            refreshRequested = true;
    }

    private void Update()
    {
        if (refreshRequested && group != null)
        {
            refreshRequested = false;
            RebuildContent();
            ApplyPreview();
        }

        // จุด ... วิ่งต่อท้ายข้อความ (ใช้เวลา unscaled)
        if (animateDots && loadingLabel != null && loadingRoot.activeInHierarchy)
        {
            int dots = (int)(Time.unscaledTime * 2.5f) % 4;
            loadingLabel.text = loadingMessage + new string('.', dots);
        }
    }

    private void ApplyPreview()
    {
        if (previewLoading)
        {
            SetAlpha(1f);
            SetLoadingVisible(true);
            SetProgress(0.6f);
        }
        else if (wasPreviewing)
        {
            SetAlpha(0f);
        }

        wasPreviewing = previewLoading;
    }

    public IEnumerator FadeOut(float duration)
    {
        return Fade(1f, duration);
    }

    public IEnumerator FadeIn(float duration)
    {
        return Fade(0f, duration);
    }

    private IEnumerator Fade(float target, float duration)
    {
        float start = group.alpha;

        if (duration <= 0f)
        {
            SetAlpha(target);
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            SetAlpha(Mathf.Lerp(start, target, Mathf.Clamp01(elapsed / duration)));
            yield return null;
        }

        SetAlpha(target);
    }

    /// <summary>โหลด Scene แบบ Async พร้อมหลอด Loading (เรียกตอนจอดำสนิทแล้ว)</summary>
    public IEnumerator LoadSceneRoutine(string sceneName, float minLoadingTime)
    {
        AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName);
        if (operation == null)
            yield break; // ชื่อ Scene ไม่ถูกต้อง/ไม่อยู่ใน Build Settings (Unity แจ้ง error ให้แล้ว)

        operation.allowSceneActivation = false;
        SetLoadingVisible(true);
        SetProgress(0f);

        float elapsed = 0f;

        // progress หยุดที่ 0.9 จนกว่าจะอนุญาตให้เปลี่ยน Scene
        while (operation.progress < 0.9f || elapsed < minLoadingTime)
        {
            elapsed += Time.unscaledDeltaTime;
            SetProgress(Mathf.Clamp01(operation.progress / 0.9f));
            yield return null;
        }

        SetProgress(1f);
        operation.allowSceneActivation = true;

        while (!operation.isDone)
            yield return null;

        SetLoadingVisible(false);
    }

    public void SetLoadingVisible(bool visible)
    {
        if (loadingRoot != null)
            loadingRoot.SetActive(visible);
    }

    private void SetProgress(float value)
    {
        if (progressFill != null)
            progressFill.anchorMax = new Vector2(Mathf.Clamp01(value), 1f);
    }

    private void SetAlpha(float alpha)
    {
        group.alpha = alpha;
        group.blocksRaycasts = alpha > 0.001f; // จอดำบังอยู่ = กันคลิกปุ่มข้างใต้

        if (alpha <= 0.001f)
            SetLoadingVisible(false);
    }

    // ---------------------------------------------------------------- สร้าง UI ด้วยโค้ด

    private void BuildUI()
    {
        Canvas canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 32000;

        // ขนาดอ้างอิง 1920x1080: ตัวอักษรและหลอดย่อ/ขยายตามความละเอียดจอ ไม่ใหญ่เกินบนจอเล็ก
        CanvasScaler scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        gameObject.AddComponent<GraphicRaycaster>();
        group = gameObject.AddComponent<CanvasGroup>();

        BuildContent();
    }

    // สร้างหน้าตาจอดำ/หน้า Loading (แยกออกมาเพื่อให้สร้างใหม่ได้ตอนปรับค่าแบบ realtime)
    private void BuildContent()
    {
        RectTransform fade = CreateStretchedRect("Fade", transform);
        fadeRoot = fade;
        fade.gameObject.AddComponent<Image>().color = backgroundColor;

        loadingRoot = CreateStretchedRect("Loading", fade).gameObject;

        // หลอดความคืบหน้า: เส้นบางๆ กึ่งกลางล่างจอ
        if (showProgressBar)
        {
            RectTransform barBack = CreateBottomCenterRect("ProgressBar", loadingRoot.transform,
                new Vector2(barWidth, barHeight), bottomOffset);
            barBack.gameObject.AddComponent<Image>().color = barBackColor;

            progressFill = CreateStretchedRect("Fill", barBack);
            progressFill.gameObject.AddComponent<Image>().color = barColor;
        }

        // ข้อความ Loading อยู่เหนือหลอด (ข้ามถ้าโปรเจกต์ยังไม่ได้ Import TMP Essentials)
        if (showLoadingText && TMP_Settings.defaultFontAsset != null)
        {
            float textOffset = bottomOffset + (showProgressBar ? barHeight : 0f) + 12f;
            RectTransform textRect = CreateBottomCenterRect("LoadingText", loadingRoot.transform,
                new Vector2(barWidth, loadingFontSize * 1.6f), textOffset);

            loadingLabel = textRect.gameObject.AddComponent<TextMeshProUGUI>();
            loadingLabel.text = loadingMessage;
            loadingLabel.fontSize = loadingFontSize;
            loadingLabel.alignment = TextAlignmentOptions.Bottom;
            loadingLabel.color = loadingTextColor;
            loadingLabel.raycastTarget = false;
            loadingLabel.enableWordWrapping = false;
        }

        loadingRoot.SetActive(false);
    }

    private void RebuildContent()
    {
        if (fadeRoot != null)
        {
            fadeRoot.gameObject.SetActive(false);
            Destroy(fadeRoot.gameObject);
        }

        progressFill = null;
        loadingLabel = null;
        BuildContent();
    }

    private static RectTransform CreateStretchedRect(string objectName, Transform parent)
    {
        GameObject go = new GameObject(objectName, typeof(RectTransform));
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        return rect;
    }

    // กล่องขนาดคงที่ ยึดกึ่งกลางขอบล่างของจอ
    private static RectTransform CreateBottomCenterRect(string objectName, Transform parent, Vector2 size, float bottomOffset)
    {
        GameObject go = new GameObject(objectName, typeof(RectTransform));
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = new Vector2(0.5f, 0f);
        rect.anchorMax = new Vector2(0.5f, 0f);
        rect.pivot = new Vector2(0.5f, 0f);
        rect.sizeDelta = size;
        rect.anchoredPosition = new Vector2(0f, bottomOffset);
        return rect;
    }
}
