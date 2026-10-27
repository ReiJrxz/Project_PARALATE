using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;
using UnityEngine.UI;

/// <summary>
/// หน้าเลือกด่านที่ Base (เปิดจาก LevelSelectTerminal)
///
/// วิธีใช้: กดแท็บซ้ายเพื่อเลือกด่าน → กดปุ่มจุดเกิด "เพื่อดูรายละเอียด" (ชื่อ, คำอธิบาย, Minimap, ไฮไลต์จุดบนแผนที่)
/// → กดปุ่ม Start เพื่อเข้าด่าน (กดปุ่มจุดเกิดไม่ได้เข้าด่านทันที)
///
/// รูปแบบหน้าต่อด่าน:
///   MapWithList : แถบปุ่มจุดเกิดทางซ้าย + แผนที่ตรงกลางที่มีปุ่มแดง (เลือกจากทั้งสองที่ได้)
///   ButtonGrid  : เรียงเป็นหัวข้อ + แถบยาว (เช่น "ด่าน 1" + แถบปุ่ม, "ด่าน 2" + แถบปุ่ม) เหมาะกับ Tutorial
///                 กำหนดหัวข้อที่ Group Title ของแต่ละจุดเกิด จุดที่ติดกันและชื่อกลุ่มเดียวกันจะอยู่แถบเดียวกัน
///
/// กฎจุดเกิด: จุดแรกของแต่ละด่านเลือกได้เสมอ จุดอื่นต้องเคยไปถึงแล้ว (SpawnProgress)
/// สคริปต์นี้ต้องอยู่บนวัตถุที่เปิดอยู่เสมอ (เช่น GameSystem/LevelSelectUI) ไม่ใช่บน Panel ที่ถูกซ่อน
/// </summary>
public class LevelSelectUI : MonoBehaviour
{
    public enum PageLayout
    {
        MapWithList = 0, // แถบปุ่มซ้าย + แผนที่ที่มีปุ่มแดง
        ButtonGrid = 1   // หัวข้อ + แถบยาวของปุ่ม
    }

    [System.Serializable]
    public class SpawnOption
    {
        [Tooltip("ต้องตรงกับ Spawn Id ของ SpawnPoint ใน Scene ของด่านนั้น")]
        public string spawnId = "1-1";
        public string displayName = "1-1";
        [TextArea] public string description;
        [Tooltip("ภาพ Minimap/ตัวอย่างของจุดนี้ (โชว์ในช่อง Preview ฝั่งขวา)")]
        public Sprite preview;
        [Tooltip("ตำแหน่งบนแผนที่ (0-1 ทั้งแกน X, Y) ใช้กับรูปแบบ MapWithList")]
        public Vector2 mapPosition = new Vector2(0.5f, 0.5f);
        [Tooltip("ใช้กับรูปแบบ ButtonGrid: หัวข้อของแถบที่ปุ่มนี้อยู่ เช่น \"ด่าน 1\" (ปุ่มที่ติดกันและชื่อเดียวกันจะอยู่แถบเดียวกัน)")]
        public string groupTitle;
    }

    [System.Serializable]
    public class LevelOption
    {
        public string displayName = "1";
        [Tooltip("ชื่อ Scene (ต้องอยู่ใน Build Settings และสะกดตรงทุกตัวอักษร)")]
        public string sceneName;
        [Tooltip("MapWithList = ด่านปกติ (แถบซ้าย + แผนที่) / ButtonGrid = หัวข้อ + แถบยาว (Tutorial)")]
        public PageLayout layout = PageLayout.MapWithList;
        [Tooltip("ปิด = โชว์แค่จุดแรก (เข้าจุดเดียวเสมอ)")]
        public bool allowSpawnSelection = true;
        [Tooltip("วางแท็บนี้ที่มุมล่างซ้าย (Bottom Tab Root) เช่น Tutorial")]
        public bool bottomTab;
        public Sprite mapImage;
        [TextArea] public string description;
        [Tooltip("เรียงตามลำดับ ตัวแรกคือจุดเริ่มต้น (เลือกได้เสมอ) ตัวอื่นต้องเคยไปถึงก่อน")]
        public SpawnOption[] spawns;
    }

    private class SpawnEntry
    {
        public SpawnOption spawn;
        public Button listButton;   // ปุ่มในแถบซ้าย หรือปุ่มในแถบยาว (Tutorial)
        public Button marker;       // ปุ่มแดงบนแผนที่
        public Color listNormal = Color.white;
        public Color markerNormal = Color.white;
    }

    [Header("UI หลัก")]
    public GameObject panel;
    [FormerlySerializedAs("backButton")]
    public Button closeButton;

    [Header("แท็บเลือกด่าน (ซ้าย)")]
    public Transform tabRoot;
    [Tooltip("ไม่บังคับ: ที่วางแท็บที่ติ๊ก Bottom Tab (ถ้าว่างจะใช้ Tab Root)")]
    public Transform bottomTabRoot;
    public Button tabButtonPrefab;
    public Color tabSelectedColor = Color.white;
    public Color tabNormalColor = new Color(0.65f, 0.65f, 0.65f, 1f);

    [Header("หน้าแบบ MapWithList: แถบซ้าย + แผนที่")]
    [Tooltip("ไม่บังคับ: กลุ่มที่รวมแถบซ้าย + แผนที่ (จะเปิด/ปิดตามรูปแบบหน้า)")]
    public GameObject mapPage;
    [Tooltip("แถบปุ่มจุดเกิดทางซ้าย (ใส่ Vertical Layout Group)")]
    [FormerlySerializedAs("listRoot")]
    public Transform spawnListRoot;
    public Image mapImage;
    [Tooltip("ที่วางปุ่มแดงบนแผนที่ (ห้ามมี Layout Group ให้ยืดเต็มพื้นที่แผนที่)")]
    public RectTransform spawnMapRoot;
    [Tooltip("ไม่บังคับ: ปุ่มวงกลมแดง (ถ้าว่างจะใช้ Spawn Button Prefab)")]
    public Button spawnMarkerPrefab;
    public Color markerSelectedColor = Color.white;
    public float markerSelectedScale = 1.35f;

    [Header("หน้าแบบ ButtonGrid: หัวข้อ + แถบยาว")]
    [Tooltip("ไม่บังคับ: กลุ่มของหน้านี้ (จะเปิด/ปิดตามรูปแบบหน้า)")]
    public GameObject gridPage;
    [Tooltip("ที่วางหัวข้อและแถบ ต้องเป็น Vertical Layout Group (Control Child Size ทั้งสองแกน, Force Expand เฉพาะ Width)")]
    public Transform spawnGridRoot;
    [Tooltip("ไม่บังคับ: Prefab ข้อความหัวข้อ (ถ้าว่างจะสร้างข้อความมาตรฐานให้)")]
    public TMP_Text sectionTitlePrefab;
    [Tooltip("ไม่บังคับ: Prefab แถบยาว (ต้องมี Horizontal Layout Group) ถ้าว่างจะสร้างแถบมาตรฐานให้")]
    public RectTransform sectionBarPrefab;
    public float sectionBarHeight = 96f;
    public Color sectionBarColor = new Color(1f, 1f, 1f, 0.12f);

    [Header("Prefab ปุ่มจุดเกิด (ใช้ทั้งแถบซ้ายและแถบยาว)")]
    [FormerlySerializedAs("optionButtonPrefab")]
    public Button spawnButtonPrefab;
    public Color spawnSelectedColor = Color.white;

    [Header("ขนาดปุ่ม (ตั้งที่นี่ ไม่ต้องแก้ใน Prefab)")]
    public Vector2 tabButtonSize = new Vector2(160f, 56f);
    public Vector2 listButtonSize = new Vector2(240f, 56f);
    public Vector2 gridButtonSize = new Vector2(200f, 64f);
    public Vector2 markerSize = new Vector2(40f, 40f);

    [Header("ฝั่งขวา: รายละเอียด")]
    [Tooltip("ภาพ Minimap/Preview ของจุดที่เลือก")]
    public Image previewImage;
    [FormerlySerializedAs("titleText")]
    public TMP_Text detailTitle;
    public TMP_Text detailDescription;
    [Tooltip("ปุ่ม Start เข้าด่านที่จุดที่เลือก (ใช้ได้เมื่อเลือกจุดแล้วเท่านั้น) " +
             "ถ้าไม่ใส่ จะกดจุดเดิมซ้ำเพื่อเข้าด่านแทน")]
    public Button startButton;

    [Header("พฤติกรรม")]
    [Tooltip("เมาส์ชี้ปุ่มแล้วโชว์รายละเอียดชั่วคราว (ออกจากปุ่มแล้วกลับไปที่จุดที่เลือก)")]
    public bool previewOnHover = true;
    [Tooltip("เปิดหน้าด่านแล้วเลือกจุดแรกที่ปลดล็อกให้เลย")]
    public bool autoSelectFirstSpawn = false;
    public string lockedSuffix = " (ยังไม่ปลดล็อก)";

    [Header("ด่านที่เลือกได้")]
    public LevelOption[] levels;

    private int currentLevelIndex = -1;
    private Button[] tabButtons;
    private SpawnOption selectedSpawn;
    private readonly List<SpawnEntry> entries = new List<SpawnEntry>();
    private TopDownPlayerController lockedPlayer;

    public bool IsOpen => panel != null && panel.activeSelf;

    private void Awake()
    {
        if (panel != null)
            panel.SetActive(false);

        if (closeButton != null)
            closeButton.onClick.AddListener(Close);

        if (startButton != null)
            startButton.onClick.AddListener(StartSelected);
    }

    private void Update()
    {
        if (!IsOpen)
            return;

        bool cancel = (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) ||
                      (Gamepad.current != null && Gamepad.current.buttonEast.wasPressedThisFrame);

        if (cancel)
            Close();
    }

    private void OnDisable()
    {
        if (IsOpen)
            Close();
    }

    public void Open()
    {
        if (IsOpen)
            return;

        if (panel == null || tabRoot == null || tabButtonPrefab == null || spawnButtonPrefab == null ||
            levels == null || levels.Length == 0)
        {
            Debug.LogError("LevelSelectUI: ยังตั้งค่าไม่ครบ (Panel / Tab Root / Tab Button Prefab / Spawn Button Prefab / Levels)", this);
            return;
        }

        panel.SetActive(true);

        lockedPlayer = FindFirstObjectByType<TopDownPlayerController>();
        if (lockedPlayer != null)
            lockedPlayer.SetMovementLocked(this, true);

        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        BuildTabs();
        SelectLevel(GetDefaultLevelIndex());
    }

    public void Close()
    {
        if (panel != null)
            panel.SetActive(false);

        if (lockedPlayer != null)
            lockedPlayer.SetMovementLocked(this, false);

        lockedPlayer = null;

        // กลับสู่โหมดเมาส์ของตอนเล่น (เหมือน TopDownPlayerController.Start)
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Confined;
    }

    // ------------------------------------------------------------ แท็บ

    private int GetDefaultLevelIndex()
    {
        // แท็บแรกที่ไม่ใช่แท็บล่าง (ถ้าไม่มี ใช้ตัวแรกสุด)
        for (int i = 0; i < levels.Length; i++)
        {
            if (!levels[i].bottomTab)
                return i;
        }

        return 0;
    }

    private void BuildTabs()
    {
        ClearChildren(tabRoot);
        ClearChildren(bottomTabRoot);

        tabButtons = new Button[levels.Length];

        for (int i = 0; i < levels.Length; i++)
        {
            int index = i;
            Transform parent = levels[i].bottomTab && bottomTabRoot != null ? bottomTabRoot : tabRoot;

            tabButtons[i] = CreateButton(parent, tabButtonPrefab, tabButtonSize, levels[i].displayName, true,
                () => SelectLevel(index));
        }
    }

    private void SelectLevel(int index)
    {
        if (index < 0 || index >= levels.Length)
            return;

        currentLevelIndex = index;
        selectedSpawn = null;

        LevelOption level = levels[index];

        for (int i = 0; i < tabButtons.Length; i++)
        {
            if (tabButtons[i] != null && tabButtons[i].targetGraphic != null)
                tabButtons[i].targetGraphic.color = i == index ? tabSelectedColor : tabNormalColor;
        }

        if (mapImage != null)
        {
            mapImage.sprite = level.mapImage;
            mapImage.enabled = level.mapImage != null;
        }

        BuildSpawnButtons(level);
        ShowLevelDetail();
        UpdateStartButton();

        if (autoSelectFirstSpawn)
            SelectFirstUnlocked(level);
    }

    // ------------------------------------------------------------ จุดเกิด

    private void BuildSpawnButtons(LevelOption level)
    {
        bool grid = level.layout == PageLayout.ButtonGrid && spawnGridRoot != null;

        // เปิดหน้าที่ตรงรูปแบบ (ถ้าไม่ได้ตั้งกลุ่มหน้าไว้ จะเปิด/ปิดตัว Root โดยตรง)
        SetActive(mapPage, !grid);
        SetActive(gridPage, grid);

        if (mapPage == null)
        {
            SetActive(spawnListRoot != null ? spawnListRoot.gameObject : null, !grid);
            SetActive(spawnMapRoot != null ? spawnMapRoot.gameObject : null, !grid);
        }

        if (gridPage == null)
            SetActive(spawnGridRoot != null ? spawnGridRoot.gameObject : null, grid);

        ClearChildren(spawnListRoot);
        ClearChildren(spawnMapRoot);
        ClearChildren(spawnGridRoot);
        entries.Clear();

        if (level.spawns == null)
            return;

        // ปิด Allow Spawn Selection = โชว์แค่จุดแรก
        int visibleCount = level.allowSpawnSelection ? level.spawns.Length : Mathf.Min(1, level.spawns.Length);

        RectTransform currentBar = null;
        string currentGroup = null;
        Button firstSelectable = null;

        for (int i = 0; i < visibleCount; i++)
        {
            SpawnOption spawn = level.spawns[i];
            bool unlocked = IsSpawnUnlocked(level, i);
            string label = unlocked ? spawn.displayName : spawn.displayName + lockedSuffix;

            SpawnEntry entry = new SpawnEntry { spawn = spawn };
            entries.Add(entry);

            if (grid)
            {
                // หัวข้อ + แถบยาว: เปลี่ยนชื่อกลุ่มเมื่อไหร่ เริ่มหัวข้อและแถบใหม่
                if (currentBar == null || spawn.groupTitle != currentGroup)
                {
                    currentGroup = spawn.groupTitle;

                    if (!string.IsNullOrEmpty(currentGroup))
                        CreateSectionTitle(currentGroup);

                    currentBar = CreateSectionBar();
                }

                entry.listButton = CreateButton(currentBar, spawnButtonPrefab, gridButtonSize, label, unlocked,
                    () => SelectSpawn(level, spawn));
            }
            else if (spawnListRoot != null)
            {
                entry.listButton = CreateButton(spawnListRoot, spawnButtonPrefab, listButtonSize, label, unlocked,
                    () => SelectSpawn(level, spawn));
            }

            if (entry.listButton != null)
            {
                entry.listNormal = GetGraphicColor(entry.listButton);
                AddHover(entry.listButton, spawn, unlocked);

                if (firstSelectable == null && unlocked)
                    firstSelectable = entry.listButton;
            }

            // ปุ่มแดงบนแผนที่ (เฉพาะรูปแบบ MapWithList)
            if (!grid && spawnMapRoot != null)
            {
                Button prefab = spawnMarkerPrefab != null ? spawnMarkerPrefab : spawnButtonPrefab;
                string markerLabel = spawnMarkerPrefab != null ? string.Empty : label;

                entry.marker = CreateButton(spawnMapRoot, prefab, markerSize, markerLabel, unlocked,
                    () => SelectSpawn(level, spawn));

                RectTransform rect = (RectTransform)entry.marker.transform;
                rect.anchorMin = spawn.mapPosition;
                rect.anchorMax = spawn.mapPosition;
                rect.anchoredPosition = Vector2.zero;

                entry.markerNormal = GetGraphicColor(entry.marker);
                AddHover(entry.marker, spawn, unlocked);
            }
        }

        // จอยเท่านั้นที่ต้องมีปุ่มเลือกไว้ก่อนเพื่อเลื่อนด้วยปุ่มทิศทาง (เมาส์ไม่ต้อง)
        if (firstSelectable != null && Gamepad.current != null && EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(firstSelectable.gameObject);
    }

    private void SelectFirstUnlocked(LevelOption level)
    {
        if (level.spawns == null)
            return;

        for (int i = 0; i < entries.Count; i++)
        {
            if (IsSpawnUnlocked(level, i))
            {
                SelectSpawn(level, entries[i].spawn);
                return;
            }
        }
    }

    // กดปุ่มจุดเกิด/ปุ่มแดง: เลือกจุดนี้และโชว์รายละเอียด (ยังไม่เข้าด่าน ต้องกด Start)
    private void SelectSpawn(LevelOption level, SpawnOption spawn)
    {
        // ไม่มีปุ่ม Start: กดจุดเดิมซ้ำเพื่อเข้าด่าน
        if (startButton == null && selectedSpawn == spawn)
        {
            StartLevel(level, spawn.spawnId);
            return;
        }

        selectedSpawn = spawn;
        RefreshSelectionVisuals();
        ShowSpawnDetail(spawn, true);
        UpdateStartButton();
    }

    // ไฮไลต์ปุ่มที่เลือก (ทั้งปุ่มในรายการและปุ่มแดงบนแผนที่ของจุดเดียวกัน)
    private void RefreshSelectionVisuals()
    {
        for (int i = 0; i < entries.Count; i++)
        {
            SpawnEntry entry = entries[i];
            bool selected = entry.spawn == selectedSpawn;

            if (entry.listButton != null)
                SetGraphicColor(entry.listButton, selected ? spawnSelectedColor : entry.listNormal);

            if (entry.marker != null)
            {
                SetGraphicColor(entry.marker, selected ? markerSelectedColor : entry.markerNormal);
                entry.marker.transform.localScale = selected ? Vector3.one * markerSelectedScale : Vector3.one;
            }
        }
    }

    private void StartSelected()
    {
        if (selectedSpawn == null || currentLevelIndex < 0)
            return;

        StartLevel(levels[currentLevelIndex], selectedSpawn.spawnId);
    }

    private void UpdateStartButton()
    {
        if (startButton != null)
            startButton.interactable = selectedSpawn != null;
    }

    private void StartLevel(LevelOption level, string spawnId)
    {
        // ตรวจก่อนปิดหน้า: ถ้า Scene ผิด จะได้ไม่ปิดหน้าไปเฉยๆ แล้วไม่เกิดอะไร
        if (string.IsNullOrEmpty(level.sceneName))
        {
            Debug.LogError($"LevelSelectUI: ด่าน '{level.displayName}' ยังไม่ได้ใส่ Scene Name", this);
            return;
        }

        if (!Application.CanStreamedLevelBeLoaded(level.sceneName))
        {
            Debug.LogError($"LevelSelectUI: ไม่พบ Scene '{level.sceneName}' ใน Build Settings (ตรวจตัวสะกด/ตัวพิมพ์ใหญ่เล็ก)", this);
            return;
        }

        Close();

        if (LevelManager.Instance != null)
            LevelManager.Instance.LoadScene(level.sceneName, spawnId);
    }

    // จุดแรกเลือกได้เสมอ ตัวอื่นต้องเคยไปถึงแล้ว
    private static bool IsSpawnUnlocked(LevelOption level, int index)
    {
        return index == 0 || SpawnProgress.IsUnlocked(level.sceneName, level.spawns[index].spawnId);
    }

    // ------------------------------------------------------------ หัวข้อ + แถบยาว (Tutorial)

    private void CreateSectionTitle(string title)
    {
        TMP_Text text;

        if (sectionTitlePrefab != null)
        {
            text = Instantiate(sectionTitlePrefab, spawnGridRoot);
        }
        else
        {
            if (TMP_Settings.defaultFontAsset == null)
                return;

            GameObject go = new GameObject("SectionTitle", typeof(RectTransform));
            go.transform.SetParent(spawnGridRoot, false);

            TextMeshProUGUI created = go.AddComponent<TextMeshProUGUI>();
            created.fontSize = 28f;
            created.alignment = TextAlignmentOptions.MidlineLeft;
            created.color = Color.white;
            created.raycastTarget = false;
            text = created;
        }

        text.gameObject.SetActive(true);
        text.text = title;

        LayoutElement element = text.GetComponent<LayoutElement>();
        if (element == null)
            element = text.gameObject.AddComponent<LayoutElement>();

        element.minHeight = 44f;
        element.preferredHeight = 44f;
    }

    private RectTransform CreateSectionBar()
    {
        RectTransform bar;

        if (sectionBarPrefab != null)
        {
            bar = Instantiate(sectionBarPrefab, spawnGridRoot);
            bar.gameObject.SetActive(true);
        }
        else
        {
            GameObject go = new GameObject("SectionBar", typeof(RectTransform), typeof(Image));
            bar = (RectTransform)go.transform;
            bar.SetParent(spawnGridRoot, false);
            go.GetComponent<Image>().color = sectionBarColor;
        }

        HorizontalLayoutGroup row = bar.GetComponent<HorizontalLayoutGroup>();
        if (row == null)
        {
            row = bar.gameObject.AddComponent<HorizontalLayoutGroup>();
            row.padding = new RectOffset(16, 16, 12, 12);
            row.spacing = 12f;
            row.childAlignment = TextAnchor.MiddleLeft;
            row.childControlWidth = true;
            row.childControlHeight = true;
            row.childForceExpandWidth = false;
            row.childForceExpandHeight = false;
        }

        LayoutElement element = bar.GetComponent<LayoutElement>();
        if (element == null)
            element = bar.gameObject.AddComponent<LayoutElement>();

        element.minHeight = sectionBarHeight;
        element.preferredHeight = sectionBarHeight;
        element.flexibleWidth = 1f;

        return bar;
    }

    // ------------------------------------------------------------ ฝั่งขวา

    private void AddHover(Button button, SpawnOption spawn, bool unlocked)
    {
        if (!previewOnHover)
            return;

        // LevelSelectHoverRelay ถูกเพิ่มให้ปุ่มตอนรันไทม์ที่นี่ ไม่ต้องใส่เองใน Prefab
        LevelSelectHoverRelay relay = button.gameObject.AddComponent<LevelSelectHoverRelay>();
        relay.Init(() => ShowSpawnDetail(spawn, unlocked), RestoreDetail);
    }

    private void RestoreDetail()
    {
        if (selectedSpawn != null)
            ShowSpawnDetail(selectedSpawn, true);
        else
            ShowLevelDetail();
    }

    private void ShowLevelDetail()
    {
        if (currentLevelIndex < 0)
            return;

        LevelOption level = levels[currentLevelIndex];
        SetDetail(level.displayName, level.description, null);
    }

    private void ShowSpawnDetail(SpawnOption spawn, bool unlocked)
    {
        SetDetail(
            unlocked ? spawn.displayName : spawn.displayName + lockedSuffix,
            spawn.description,
            unlocked ? spawn.preview : null);
    }

    private void SetDetail(string title, string description, Sprite preview)
    {
        if (detailTitle != null)
            detailTitle.text = title;

        if (detailDescription != null)
            detailDescription.text = description;

        if (previewImage != null)
        {
            previewImage.sprite = preview;
            previewImage.enabled = preview != null;
        }
    }

    // ------------------------------------------------------------ UI helper

    private static Button CreateButton(Transform parent, Button prefab, Vector2 size, string label, bool interactable,
        UnityEngine.Events.UnityAction onClick)
    {
        Button button = Instantiate(prefab, parent);
        button.gameObject.SetActive(true);
        button.interactable = interactable;

        ApplySize(button, size);

        TMP_Text text = button.GetComponentInChildren<TMP_Text>();
        if (text != null)
            text.text = label;

        if (onClick != null)
            button.onClick.AddListener(onClick);

        return button;
    }

    // ตั้งขนาดปุ่มจากโค้ด: ทั้งขนาดตรงๆ (ใช้เมื่อพ่อไม่มี Layout Group เช่น ปุ่มแดงบนแผนที่)
    // และ Layout Element (ใช้เมื่อพ่อเป็น Layout Group) ปุ่มจึงไม่หดเหลือเส้นบางๆ
    private static void ApplySize(Button button, Vector2 size)
    {
        if (size.x <= 0f || size.y <= 0f)
            return;

        ((RectTransform)button.transform).sizeDelta = size;

        LayoutElement element = button.GetComponent<LayoutElement>();
        if (element == null)
            element = button.gameObject.AddComponent<LayoutElement>();

        element.minWidth = size.x;
        element.minHeight = size.y;
        element.preferredWidth = size.x;
        element.preferredHeight = size.y;
    }

    private static Color GetGraphicColor(Button button)
    {
        return button.targetGraphic != null ? button.targetGraphic.color : Color.white;
    }

    private static void SetGraphicColor(Button button, Color color)
    {
        if (button.targetGraphic != null)
            button.targetGraphic.color = color;
    }

    private static void SetActive(GameObject target, bool active)
    {
        if (target != null && target.activeSelf != active)
            target.SetActive(active);
    }

    private static void ClearChildren(Transform root)
    {
        if (root == null)
            return;

        for (int i = root.childCount - 1; i >= 0; i--)
        {
            GameObject child = root.GetChild(i).gameObject;
            child.SetActive(false); // ออกจาก Layout ทันที (Destroy จริงตอนท้ายเฟรม)
            Destroy(child);
        }
    }
}
