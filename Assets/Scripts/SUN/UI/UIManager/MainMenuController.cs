using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// ผูกกับปุ่มใน MainMenu ใช้ LevelManager (เปลี่ยน Scene) ส่วนหน้า Options โหลดแบบ Additive
/// ชื่อ Scene Options อยู่ใน SceneNames.Options (ต้องอยู่ใน Build Settings)
/// </summary>
public class MainMenuController : MonoBehaviour
{
    private bool isOptionsLoading;

    public void OnNewGamePressed()
    {
        // อนาคตถ้ามีการ New Game ควรจะเคลียร์ Save Data ก่อน
        //LoadLevel(0);
        UnityEngine.SceneManagement.SceneManager.LoadScene(SceneNames.Tutorial);
    }

    // สำหรับปุ่มเลือกด่าน: ใส่ index ที่ OnClick (0 = Level1, 1 = Level2, ...)
    public void OnSelectLevelPressed(int levelIndex)
    {
        LoadLevel(levelIndex);
    }

    public void OnLoadGamePressed()
    {
        // โครงสร้างเผื่อระบบ Save/Load ในอนาคต
        // เช่น: if (SaveSystem.HasSaveData()) LoadLevel(SaveSystem.GetSavedLevelIndex());
        Debug.Log("Load Game Button Pressed - Ready for SaveSystem hook.");
    }

    public void OnOptionPressed()
    {
        // กันผู้เล่นกดเบิ้ล ทั้งตอนกำลังโหลดและตอนโหลดเสร็จแล้ว
        if (isOptionsLoading || SceneManager.GetSceneByName(SceneNames.Options).isLoaded)
            return;

        // โหลด Scene ซ้อนขึ้นมา โดยไม่ลบ Scene MainMenu ทิ้ง
        AsyncOperation operation = SceneManager.LoadSceneAsync(SceneNames.Options, LoadSceneMode.Additive);

        // null = หา Scene ไม่เจอ (ยังไม่ได้ใส่ใน Build Settings) Unity จะแจ้ง error ให้เอง
        if (operation == null)
            return;

        isOptionsLoading = true;
        operation.completed += _ => isOptionsLoading = false;
    }

    public void OnExitPressed()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private static void LoadLevel(int levelIndex)
    {
        if (LevelManager.Instance == null)
        {
            Debug.LogError("ไม่พบ LevelManager");
            return;
        }

        LevelManager.Instance.LoadLevel(levelIndex);
    }
}