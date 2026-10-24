using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneNavigation : MonoBehaviour
{
    // ปุ่ม Back to Main Menu
    public void BackToMainMenu()
    {
        Time.timeScale = 1f;

        if (LevelManager.Instance != null)
            LevelManager.Instance.LoadMainMenu();
        else
            SceneManager.LoadScene(SceneNames.MainMenu);
    }

    // ปุ่ม Retry (โหลด Scene ปัจจุบันใหม่ และทิ้งของที่เก็บมาในรอบนี้)
    public void Retry()
    {
        Time.timeScale = 1f;

        if (LevelManager.Instance != null)
            LevelManager.Instance.AbandonRun();

        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}