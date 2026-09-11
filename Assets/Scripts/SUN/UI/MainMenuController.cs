using UnityEngine;

public class MainMenuController : MonoBehaviour
{
    [Header("Option Panel")]
    public GameObject optionPanel;

    public void OnNewGamePressed()
    {
        LevelManager.Instance.LoadLevel(0); // index 0 = Level1 ตามลำดับใน levelScenes
    }

    public void OnLoadGamePressed()
    {
        // ยังไม่มีระบบ save/load ตอนนี้ — ใส่ไว้เป็น placeholder ก่อน ค่อยต่อทีหลัง
    }

    public void OnOptionPressed()
    {
        optionPanel.SetActive(true);
    }

    public void OnOptionClosePressed()
    {
        optionPanel.SetActive(false);
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