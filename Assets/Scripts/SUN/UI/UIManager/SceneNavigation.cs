using UnityEngine;
using UnityEngine.SceneManagement;

namespace ZiddikJari
{
public class SceneNavigation : MonoBehaviour
    {
        [Header("Scene Settings")]
        [SerializeField] private string mainMenuSceneName = "MainMenu";

        // ปุ่ม Back to Main Menu
        public void BackToMainMenu()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(mainMenuSceneName);
        }

        // ปุ่ม Retry
        public void Retry()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(
                SceneManager.GetActiveScene().buildIndex
            );
        }
    }
}
