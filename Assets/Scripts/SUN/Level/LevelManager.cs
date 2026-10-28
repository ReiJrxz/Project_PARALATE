using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// ตัวคุมการเปลี่ยน Scene / วาร์ป / Checkpoint / จบด่าน / ล้มเหลว (Singleton ข้าม Scene)
/// ทุกการเปลี่ยน Scene ผ่าน SceneFader (จอดำ Fade + Loading)
/// วางไว้ใน MainMenu ได้ตามเดิม ถ้าเริ่ม Play จาก Scene อื่นระบบจะสร้างให้เองอัตโนมัติ
/// ชื่อ Scene อยู่ใน SceneNames
/// </summary>
public class LevelManager : MonoBehaviour
{
    private static LevelManager instance;
    private static bool applicationQuitting;

    /// <summary>
    /// LevelManager ตัวเดียวของเกม ถ้าเรียกตอนไม่มี (เช่น ถูกทำลายตอนเปลี่ยน Scene) จะสร้างใหม่ให้อัตโนมัติ
    /// จึงไม่เป็น null ระหว่างเล่น (ตัวที่สร้างใหม่จะเริ่มใหม่ เช่น Checkpoint ล่าสุดหาย)
    /// </summary>
    public static LevelManager Instance
    {
        get
        {
            if (instance == null && Application.isPlaying && !applicationQuitting)
            {
                Debug.LogWarning("LevelManager: ไม่พบตัวที่ใช้งานอยู่ (อาจถูกทำลายตอนเปลี่ยน Scene) จึงสร้างใหม่อัตโนมัติ " +
                                 "ตรวจว่า LevelManager ใน MainMenu เป็นวัตถุระดับบนสุดของ Hierarchy ไม่ได้อยู่ใต้วัตถุอื่น");
                CreateAuto();
            }

            return instance;
        }
    }

    /// <summary>ตอนนี้มี LevelManager อยู่ไหม (ไม่สร้างใหม่ให้)</summary>
    public static bool Exists => instance != null;

    /// <summary>กำลังเปลี่ยน Scene/วาร์ปอยู่ไหม (ไม่สร้าง LevelManager ใหม่ให้)</summary>
    public static bool IsTransitionRunning => instance != null && instance.IsTransitioning;

    private static void CreateAuto()
    {
        new GameObject("LevelManager (Auto)").AddComponent<LevelManager>();
    }

    // ล้างค่า static ตอนเริ่มเล่น (กรณีปิด Domain Reload ใน Enter Play Mode Options)
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        instance = null;
        applicationQuitting = false;
    }

    [Header("Transition")]
    [SerializeField] private float fadeOutDuration = 0.4f;
    [SerializeField] private float fadeInDuration = 0.5f;
    [Tooltip("เวลา Loading ขั้นต่ำ เพื่อไม่ให้หน้า Loading แวบหายเร็วเกินไป")]
    [SerializeField] private float minLoadingTime = 0.5f;
    [Tooltip("ค้างจอดำหลังวาร์ป เพื่อให้กล้อง/ห้องใหม่พร้อมก่อนเปิดจอ")]
    [SerializeField] private float teleportBlackHoldTime = 0.2f;

    public int LevelCount => SceneNames.Levels.Length;

    /// <summary>จุดเกิดที่ผู้เล่นจะกลับมาเกิดเมื่อ Retry (null = ใช้จุดเริ่มต้นของด่าน)</summary>
    public string CheckpointSpawnId { get; private set; }

    [Tooltip("ถ้าการเปลี่ยน Scene/วาร์ปค้างเกินเวลานี้ (วินาที) จะปลดล็อกให้อัตโนมัติ กันวาร์ปใช้ไม่ได้ตลอดไป")]
    [SerializeField] private float transitionTimeout = 20f;

    private bool transitioning;
    private float transitionStartedAt;

    public bool IsTransitioning
    {
        get
        {
            if (transitioning && Time.realtimeSinceStartup - transitionStartedAt > transitionTimeout)
            {
                Debug.LogWarning("LevelManager: การเปลี่ยน Scene/วาร์ปค้างนานเกินกำหนด จึงปลดล็อกให้อัตโนมัติ", this);
                transitioning = false;
                CameraZone.ForceCut = false;
            }

            return transitioning;
        }
    }

    private void SetTransitioning(bool value)
    {
        transitioning = value;

        if (value)
            transitionStartedAt = Time.realtimeSinceStartup;
    }

    private string pendingSpawnId;

    // ทำงานหลัง Awake ของ Scene แรก: ถ้า Scene ไม่มี LevelManager (เช่น กด Play จากด่านโดยตรง) ค่อยสร้างให้
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void EnsureExists()
    {
        if (instance != null)
            return;

        CreateAuto();
    }

    void Awake()
    {
        if (instance != null && instance != this)
        {
            // ซ้ำกับตัวที่ค้างมาจาก Scene ก่อนหน้า (เช่น MainMenu) ลบเฉพาะคอมโพเนนต์นี้
            // ถ้ายังมีคอมโพเนนต์อื่น/ลูกอยู่บนวัตถุเดียวกัน จะไม่ลบทิ้งทั้งวัตถุ
            if (GetComponents<Component>().Length <= 2 && transform.childCount == 0)
                Destroy(gameObject);
            else
                Destroy(this);

            return;
        }

        instance = this;

        // DontDestroyOnLoad ใช้ได้เฉพาะวัตถุระดับบนสุด ถ้าซ้อนอยู่ใต้วัตถุอื่นให้แยกออกมาก่อน
        if (transform.parent != null)
            transform.SetParent(null);

        DontDestroyOnLoad(gameObject);

        SceneManager.sceneLoaded += OnSceneLoaded;
        ApplySceneState(SceneManager.GetActiveScene());
        RegisterStartingCheckpoint(SceneManager.GetActiveScene());
    }

    void OnApplicationQuit()
    {
        applicationQuitting = true;
    }

    void OnDestroy()
    {
        if (instance != this)
            return;

        SceneManager.sceneLoaded -= OnSceneLoaded;
        instance = null;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Scene ที่โหลดซ้อน (เช่น OptionsMenu) ต้องไม่รีเซ็ต timeScale / ไม่ย้ายผู้เล่น
        if (mode == LoadSceneMode.Additive)
            return;

        ApplySceneState(scene);
        ArriveAtSpawn(scene);
    }

    // กันเวลาค้าง (เช่น วงล้ออาวุธ slow-mo) และคืนเมาส์ให้หน้าเมนู
    // ส่วนในด่าน TopDownPlayerController จัดการเมาส์เองตอน Start
    private static void ApplySceneState(Scene scene)
    {
        Time.timeScale = 1f;

        if (scene.name == SceneNames.MainMenu)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    // ------------------------------------------------------------ เปลี่ยน Scene

    public void LoadMainMenu()
    {
        LoadScene(SceneNames.MainMenu);
    }

    public void LoadBase()
    {
        LoadScene(SceneNames.Base, null, false);
    }

    public void LoadLevel(int levelIndex)
    {
        LoadLevel(levelIndex, null);
    }

    public void LoadLevel(int levelIndex, string spawnId)
    {
        if (levelIndex < 0 || levelIndex >= SceneNames.Levels.Length)
        {
            Debug.LogWarning("Level index ไม่ถูกต้อง: " + levelIndex);
            return;
        }

        LoadScene(SceneNames.Levels[levelIndex], spawnId);
    }

    /// <summary>
    /// โหลด Scene ใดก็ได้ (พร้อม Fade + Loading) แล้ววางผู้เล่นที่ spawnId (null = จุดเริ่มต้นของด่าน)
    /// abandonRun = true: ทิ้งของที่เก็บมาในรอบก่อนหน้า (เริ่มรอบใหม่)
    /// </summary>
    public void LoadScene(string sceneName, string spawnId = null, bool abandonRun = true)
    {
        if (IsTransitioning)
            return;

        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogError("LevelManager.LoadScene: ชื่อ Scene ว่าง (ตรวจช่อง Scene Name ของด่านใน LevelSelectUI หรือของตัววาร์ป)", this);
            return;
        }

        if (!Application.CanStreamedLevelBeLoaded(sceneName))
        {
            Debug.LogError($"LevelManager.LoadScene: ไม่พบ Scene '{sceneName}' ใน Build Settings " +
                           "(ตรวจตัวสะกด/ตัวพิมพ์ใหญ่เล็ก และเพิ่ม Scene ใน File > Build Profiles)", this);
            return;
        }

        if (abandonRun)
            AbandonRun();

        StartCoroutine(SceneTransitionRoutine(sceneName, spawnId));
    }

    private IEnumerator SceneTransitionRoutine(string sceneName, string spawnId)
    {
        SetTransitioning(true);
        pendingSpawnId = spawnId;

        SceneFader fader = SceneFader.Instance;

        if (fader != null)
        {
            yield return fader.FadeOut(fadeOutDuration);

            Time.timeScale = 1f;
            yield return fader.LoadSceneRoutine(sceneName, minLoadingTime);

            yield return null; // รอ 1 เฟรมให้ Start() ของ Scene ใหม่ทำงานก่อนเปิดจอ
            yield return fader.FadeIn(fadeInDuration);
        }
        else
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(sceneName);
        }

        SetTransitioning(false);
    }

    // ------------------------------------------------------------ จุดเกิด / Checkpoint

    // Scene ใหม่โหลดเสร็จ: วางผู้เล่นที่จุดเกิดที่ขอมา (หรือจุดเริ่มต้นของด่าน)
    private void ArriveAtSpawn(Scene scene)
    {
        string requestedId = pendingSpawnId;
        pendingSpawnId = null;
        CheckpointSpawnId = null;

        TopDownPlayerController player = FindFirstObjectByType<TopDownPlayerController>();
        if (player == null)
            return;

        SpawnPoint spawn = SpawnPoint.Find(requestedId, scene) ?? SpawnPoint.GetDefault(scene);
        if (spawn == null)
            return;

        PlacePlayer(player, spawn);
        RegisterArrival(spawn, scene.name);
    }

    // กด Play จาก Scene ด่านโดยตรง (ตอนทดสอบ): ไม่ย้ายผู้เล่น แต่ใช้จุดที่ใกล้ที่สุดเป็น Checkpoint
    private void RegisterStartingCheckpoint(Scene scene)
    {
        TopDownPlayerController player = FindFirstObjectByType<TopDownPlayerController>();
        if (player == null)
            return;

        SpawnPoint nearest = SpawnPoint.FindNearest(scene, player.transform.position);
        if (nearest != null)
            RegisterArrival(nearest, scene.name);
    }

    private void RegisterArrival(SpawnPoint spawn, string sceneName)
    {
        if (spawn.isCheckpoint)
            CheckpointSpawnId = spawn.spawnId;

        if (spawn.unlockForSelection)
            SpawnProgress.Unlock(sceneName, spawn.spawnId);
    }

    private static void PlacePlayer(TopDownPlayerController player, SpawnPoint spawn)
    {
        // CharacterController ต้องปิดก่อนย้ายตำแหน่ง ไม่งั้นมันจะดึงตัวละครกลับที่เดิม
        CharacterController controller = player.GetComponent<CharacterController>();
        bool wasEnabled = controller != null && controller.enabled;

        if (wasEnabled)
            controller.enabled = false;

        player.transform.SetPositionAndRotation(spawn.transform.position, spawn.transform.rotation);

        if (wasEnabled)
            controller.enabled = true;

        Physics.SyncTransforms(); // ให้ Trigger (เช่น CameraZone) รู้ตำแหน่งใหม่ทันที
    }

    /// <summary>วาร์ปผู้เล่นไปจุดเกิดใน Scene เดียวกัน (Fade ดำ → ย้าย → เปิดจอ) และตั้งเป็น Checkpoint ใหม่</summary>
    public void TeleportPlayerTo(SpawnPoint target, Action onFinished = null)
    {
        if (target == null || IsTransitioning)
            return;

        StartCoroutine(TeleportRoutine(target, onFinished));
    }

    private IEnumerator TeleportRoutine(SpawnPoint target, Action onFinished)
    {
        SetTransitioning(true);

        TopDownPlayerController player = FindFirstObjectByType<TopDownPlayerController>();
        if (player != null)
            player.SetMovementLocked(this, true);

        SceneFader fader = SceneFader.Instance;

        if (fader != null)
            yield return fader.FadeOut(fadeOutDuration);

        // จอดำแล้ว: ให้กล้องตัดทันที (ไม่ Blend) ตอนสลับไปห้องใหม่
        CameraZone.ForceCut = true;

        if (player != null)
        {
            PlacePlayer(player, target);
            RegisterArrival(target, target.gameObject.scene.name);
        }

        // ให้ฟิสิกส์ทำงาน 1 รอบ (CameraZone ห้องใหม่จับผู้เล่นได้) แล้วค้างจอดำสั้นๆ ก่อนเปิดจอ
        yield return new WaitForFixedUpdate();

        // ตอนวาร์ป CharacterController ถูกปิด/เปิดชั่วคราว Unity อาจไม่ส่ง OnTriggerExit ให้ CameraZone เดิม
        // จึงให้ทุกโซนเช็กตำแหน่งผู้เล่นใหม่ กล้องห้องเก่าจะไม่ค้าง
        if (player != null)
            CameraZone.RefreshForPlayer(player.GetComponent<Collider>());

        yield return new WaitForSecondsRealtime(teleportBlackHoldTime);

        CameraZone.ForceCut = false;

        if (fader != null)
            yield return fader.FadeIn(fadeInDuration);

        if (player != null)
            player.SetMovementLocked(this, false);

        SetTransitioning(false);
        onFinished?.Invoke();
    }

    // ------------------------------------------------------------ จบด่าน / ล้มเหลว / Retry

    // เรียกตอนผู้เล่นถึงจุดจบด่านสำเร็จ: เก็บของถาวร แล้วกลับ Base
    public void CompleteLevel()
    {
        if (IsTransitioning)
            return;

        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayVictorySfx();

        if (RunInventory.Instance != null)
            RunInventory.Instance.CommitToPersistent();

        LoadBase();
    }

    // รอบนี้ล้มเหลว (ตาย/ถูกจับ): ทิ้งของที่เก็บมาในรอบนี้ แต่ยังอยู่ใน Scene เดิม
    // (PlayerHealth.Die เรียกใช้ เพื่อให้หน้าจอ Game Over + ปุ่ม Retry / Back to Base ทำงานต่อได้)
    public void OnRunFailed()
    {
        AbandonRun();

        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayLoseSfx();
    }

    // ล้มเหลวแล้วกลับฮับทันที (ใช้ถ้าไม่ต้องการหน้าจอ Game Over)
    public void FailLevel()
    {
        OnRunFailed();
        LoadBase();
    }

    /// <summary>โหลด Scene ปัจจุบันใหม่ แล้วให้ผู้เล่นเกิดที่ Checkpoint ล่าสุด (ศัตรู/ของในด่านรีเซ็ตด้วย)</summary>
    public void RetryFromCheckpoint()
    {
        LoadScene(SceneManager.GetActiveScene().name, CheckpointSpawnId);
    }

    // ออกจากรอบกลางคัน (Retry / กลับเมนู / เริ่มด่านใหม่) ของที่เก็บมาในรอบนี้จึงไม่ถูกเก็บถาวร
    public void AbandonRun()
    {
        if (RunInventory.Instance != null)
            RunInventory.Instance.ClearRun();
    }
}
