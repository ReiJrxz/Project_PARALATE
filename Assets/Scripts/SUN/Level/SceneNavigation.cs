using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>ผูกกับปุ่มของหน้า Game Over / Pause (ผ่าน LevelManager จึงมี Fade + Loading ให้)</summary>
public class SceneNavigation : MonoBehaviour
{
    // ปุ่ม Retry: เกิดใหม่ที่ Checkpoint ล่าสุดของด่านนี้
    public void Retry()
    {
        if (LevelManager.Instance != null)
            LevelManager.Instance.RetryFromCheckpoint();
        else
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    // ปุ่ม Back to Base
    public void BackToBase()
    {
        if (LevelManager.Instance != null)
        {
            LevelManager.Instance.AbandonRun();
            LevelManager.Instance.LoadBase();
        }
        else
        {
            SceneManager.LoadScene(SceneNames.Base);
        }
    }

    // ปุ่ม Back to Main Menu
    public void BackToMainMenu()
    {
        if (LevelManager.Instance != null)
            LevelManager.Instance.LoadMainMenu();
        else
            SceneManager.LoadScene(SceneNames.MainMenu);
    }
}
