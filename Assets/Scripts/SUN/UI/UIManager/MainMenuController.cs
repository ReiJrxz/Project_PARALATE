using UnityEngine;
using UnityEngine.SceneManagement; // จำเป็นสำหรับการจัดการ Scene

public class MainMenuController : MonoBehaviour
{
    [Header("Scene Settings")]
    [Tooltip("ชื่อ Scene ที่ใช้สำหรับ Options")]
    public string optionsSceneName = "OptionsMenu";

    public void OnNewGamePressed()
    {
        // โหลดเกมด่านแรก (index 0) 
        // อนาคตถ้ามีการ New Game ควรจะเคลียร์ Save Data ก่อน
        LevelManager.Instance.LoadLevel(0);
    }

    public void OnLoadGamePressed()
    {
        // โครงสร้างเผื่อระบบ Save/Load ในอนาคต
        // สมมติว่าในอนาคตคุณสร้าง SaveSystem ขึ้นมาจัดการ
        /*
        if (SaveSystem.HasSaveData()) 
        {
            int savedLevelIndex = SaveSystem.GetSavedLevelIndex();
            // อาจจะมีการดึงข้อมูล Player Stats (เลือด, ปืน, อัปเกรด) มาด้วย
            LevelManager.Instance.LoadLevel(savedLevelIndex);
        }
        else
        {
            Debug.LogWarning("No save data found!");
            // อาจจะเด้ง UI แจ้งเตือนผู้เล่นว่าไม่มีเซฟ
        }
        */

        Debug.Log("Load Game Button Pressed - Ready for SaveSystem hook.");
    }

    public void OnOptionPressed()
    {
        // เช็คก่อนว่า Scene Option ถูกโหลดซ้อนขึ้นมาหรือยัง เพื่อกันผู้เล่นกดเบิ้ล
        if (!SceneManager.GetSceneByName(optionsSceneName).isLoaded)
        {
            // โหลด Scene ทับซ้อนขึ้นมา โดยไม่ลบ Scene MainMenu ทิ้ง
            SceneManager.LoadSceneAsync(optionsSceneName, LoadSceneMode.Additive);
        }
    }

    public void OnExitPressed()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}