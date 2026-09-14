using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class PlaytestResetHotkey : MonoBehaviour
{
    void Update()
    {
        if (Keyboard.current != null && Keyboard.current.digit0Key.wasPressedThisFrame)
        {
            ResetToMainMenu();
        }
    }

    void ResetToMainMenu()
    {
        if (RunInventory.Instance != null)
            RunInventory.Instance.ClearRun();

        SceneManager.LoadScene("MainMenu");
    }
}