using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelManager : MonoBehaviour
{
    public static LevelManager Instance { get; private set; }

    [Header("ชื่อ Scene ต้องตรงกับที่ตั้งใน Build Settings เป๊ะๆ")]
    public string mainMenuScene = "MainMenu";
    public string baseScene = "Base";
    public string[] levelScenes = { "Level1", "Level2", "Level3" };

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void LoadMainMenu() => SceneManager.LoadScene(mainMenuScene);

    public void LoadBase() => SceneManager.LoadScene(baseScene);

    public void LoadLevel(int levelIndex)
    {
        if (levelIndex < 0 || levelIndex >= levelScenes.Length)
        {
            Debug.LogWarning("Level index ไม่ถูกต้อง: " + levelIndex);
            return;
        }
        SceneManager.LoadScene(levelScenes[levelIndex]);
    }

    // เรียกตอนผู้เล่นถึงจุดจบด่านสำเร็จ
    public void CompleteLevel()
    {
        RunInventory.Instance.CommitToPersistent();
        LoadBase();
    }

    // เรียกตอนถูกจับ/ตาย/ด่านล้มเหลว
    public void FailLevel()
    {
        RunInventory.Instance.ClearRun();
        LoadBase(); // หรือจะ LoadLevel(index เดิม) ถ้าอยากให้เล่นด่านซ้ำทันทีแทนกลับ base
    }
}