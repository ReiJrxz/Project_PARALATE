using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class PlaytestResetHotkey : MonoBehaviour
{
    public static PlaytestResetHotkey Instance { get; private set; }

    const string MainMenuScene = "MainMenu";
    const string Level0Scene = "Level0(Tutorial)";

    [Header("Playtest Keys")]
    public Key resetToMainMenuKey = Key.L;
    public Key resetToLevel0Key = Key.LeftBracket;
    public Key toggleInvincibleKey = Key.RightBracket;

    bool isInvincible;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void EnsureExists()
    {
        if (Instance != null)
            return;

        GameObject go = new GameObject("PlaytestResetHotkey (Auto)");
        go.AddComponent<PlaytestResetHotkey>();
    }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDestroy()
    {
        if (Instance == this)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            Instance = null;
        }
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ApplyInvincible();
    }

    void Update()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
            return;

        if (keyboard[resetToMainMenuKey].wasPressedThisFrame && IsInLevel0())
        {
            ResetAndLoad(MainMenuScene);
            Cursor.visible = true;
        }

        if (keyboard[resetToLevel0Key].wasPressedThisFrame)
            ResetAndLoad(Level0Scene);

        if (keyboard[toggleInvincibleKey].wasPressedThisFrame)
            ToggleInvincible();
    }

    static bool IsInLevel0()
    {
        return SceneManager.GetActiveScene().name == Level0Scene;
    }

    static void ResetAndLoad(string sceneName)
    {
        if (RunInventory.Instance != null)
            RunInventory.Instance.ClearRun();

        SceneManager.LoadScene(sceneName);
    }

    void ToggleInvincible()
    {
        isInvincible = !isInvincible;
        ApplyInvincible();
        Debug.Log($"Player Invincible: {isInvincible}", this);
    }

    void ApplyInvincible()
    {
        PlayerHealth playerHealth = FindAnyObjectByType<PlayerHealth>();
        if (playerHealth != null)
            playerHealth.isInvincible = isInvincible;
    }
}
