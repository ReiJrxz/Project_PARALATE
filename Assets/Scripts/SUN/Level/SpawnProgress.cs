using UnityEngine;

/// <summary>
/// จำว่าจุดเกิดไหนของแต่ละด่านที่ผู้เล่นเคยไปถึงแล้ว (เก็บใน PlayerPrefs)
/// ใช้ตอนเลือกจุดเริ่มที่ Base: จุดที่ยังไม่เคยไปจะเลือกไม่ได้
/// </summary>
public static class SpawnProgress
{
    private const string KeyPrefix = "SpawnUnlocked_";
    private const char Separator = '|';

    public static bool IsUnlocked(string sceneName, string spawnId)
    {
        if (string.IsNullOrEmpty(sceneName) || string.IsNullOrEmpty(spawnId))
            return false;

        string saved = PlayerPrefs.GetString(KeyPrefix + sceneName, "");
        if (saved.Length == 0)
            return false;

        string[] ids = saved.Split(Separator);
        for (int i = 0; i < ids.Length; i++)
        {
            if (ids[i] == spawnId)
                return true;
        }

        return false;
    }

    public static void Unlock(string sceneName, string spawnId)
    {
        if (string.IsNullOrEmpty(sceneName) || string.IsNullOrEmpty(spawnId) || IsUnlocked(sceneName, spawnId))
            return;

        string key = KeyPrefix + sceneName;
        string saved = PlayerPrefs.GetString(key, "");

        PlayerPrefs.SetString(key, saved.Length == 0 ? spawnId : saved + Separator + spawnId);
        PlayerPrefs.Save();
    }

    public static void ResetScene(string sceneName)
    {
        PlayerPrefs.DeleteKey(KeyPrefix + sceneName);
    }

#if UNITY_EDITOR
    // เมนูสำหรับทดสอบ: Tools > Reset Spawn Progress
    [UnityEditor.MenuItem("Tools/Reset Spawn Progress")]
    private static void EditorResetAll()
    {
        ResetScene(SceneNames.Tutorial);

        for (int i = 0; i < SceneNames.Levels.Length; i++)
            ResetScene(SceneNames.Levels[i]);

        Debug.Log("Reset spawn progress แล้ว");
    }
#endif
}
