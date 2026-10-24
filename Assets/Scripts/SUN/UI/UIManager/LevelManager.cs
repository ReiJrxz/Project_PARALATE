using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// ตัวคุมการเปลี่ยน Scene / จบด่าน / ล้มเหลว (Singleton ข้าม Scene)
/// วางไว้ใน MainMenu ได้ตามเดิม ถ้าเริ่ม Play จาก Scene อื่นระบบจะสร้างให้เองอัตโนมัติ
/// ชื่อ Scene อยู่ใน SceneNames
/// </summary>
public class LevelManager : MonoBehaviour
{
    public static LevelManager Instance { get; private set; }

    public int LevelCount => SceneNames.Levels.Length;

    // ทำงานหลัง Awake ของ Scene แรก: ถ้า Scene ไม่มี LevelManager (เช่น กด Play จากด่านโดยตรง) ค่อยสร้างให้
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void EnsureExists()
    {
        if (Instance != null)
            return;

        new GameObject("LevelManager (Auto)").AddComponent<LevelManager>();
    }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        SceneManager.sceneLoaded += OnSceneLoaded;
        ApplySceneState(SceneManager.GetActiveScene());
    }

    void OnDestroy()
    {
        if (Instance != this)
            return;

        SceneManager.sceneLoaded -= OnSceneLoaded;
        Instance = null;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Scene ที่โหลดซ้อน (เช่น OptionsMenu) ต้องไม่รีเซ็ต timeScale ของ Scene หลัก (เช่น ตอน Pause)
        if (mode == LoadSceneMode.Additive)
            return;

        ApplySceneState(scene);
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

    public void LoadMainMenu()
    {
        AbandonRun();
        SceneManager.LoadScene(SceneNames.MainMenu);
    }

    public void LoadBase() => SceneManager.LoadScene(SceneNames.Base);

    public void LoadLevel(int levelIndex)
    {
        if (levelIndex < 0 || levelIndex >= SceneNames.Levels.Length)
        {
            Debug.LogWarning("Level index ไม่ถูกต้อง: " + levelIndex);
            return;
        }

        // เริ่มด่านใหม่ = เริ่มรอบใหม่ ไม่เอาของค้างจากรอบก่อนมาด้วย
        AbandonRun();
        SceneManager.LoadScene(SceneNames.Levels[levelIndex]);
    }

    // เรียกตอนผู้เล่นถึงจุดจบด่านสำเร็จ
    public void CompleteLevel()
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayVictorySfx();

        if (RunInventory.Instance != null)
            RunInventory.Instance.CommitToPersistent();

        LoadBase();
    }

    // รอบนี้ล้มเหลว (ตาย/ถูกจับ): ทิ้งของที่เก็บมาในรอบนี้ แต่ยังอยู่ใน Scene เดิม
    // (PlayerHealth.Die เรียกใช้ เพื่อให้หน้าจอ Dead + ปุ่ม Retry ทำงานต่อได้)
    public void OnRunFailed()
    {
        AbandonRun();

        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayLoseSfx();
    }

    // ล้มเหลวแล้วกลับฮับทันที (ใช้ถ้าไม่ต้องการหน้าจอ Dead)
    public void FailLevel()
    {
        OnRunFailed();
        LoadBase();
    }

    // ออกจากรอบกลางคัน (Retry / กลับเมนู / เริ่มด่านใหม่) ของที่เก็บมาในรอบนี้จึงไม่ถูกเก็บถาวร
    public void AbandonRun()
    {
        if (RunInventory.Instance != null)
            RunInventory.Instance.ClearRun();
    }
}