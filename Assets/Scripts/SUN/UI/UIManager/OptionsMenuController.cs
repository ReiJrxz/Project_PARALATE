using UnityEngine;
using UnityEngine.SceneManagement;

public class OptionsMenuController : MonoBehaviour
{
    [Header("Tab Panels")]
    public GameObject gamePanel;
    public GameObject soundPanel;
    public GameObject videoPanel;
    public GameObject controllerPanel;

    private bool isClosing;

    private void Start()
    {
        // ตอนเปิดหน้า Option ขึ้นมา ให้โชว์หน้า Game เป็นหน้าแรก
        OpenTab(gamePanel);
    }

    // ฟังก์ชันสำหรับผูกกับปุ่ม Tab ด้านบน
    public void OnGameTabPressed() => OpenTab(gamePanel);
    public void OnSoundTabPressed() => OpenTab(soundPanel);
    public void OnVideoTabPressed() => OpenTab(videoPanel);
    public void OnControllerTabPressed() => OpenTab(controllerPanel);

    private void OpenTab(GameObject activePanel)
    {
        if (gamePanel != null) gamePanel.SetActive(false);
        if (soundPanel != null) soundPanel.SetActive(false);
        if (videoPanel != null) videoPanel.SetActive(false);
        if (controllerPanel != null) controllerPanel.SetActive(false);

        if (activePanel != null)
            activePanel.SetActive(true);
    }

    public void OnCloseOptionsPressed()
    {
        // กันกดปุ่มปิดซ้ำระหว่างที่ Scene กำลัง Unload
        if (isClosing)
            return;

        isClosing = true;

        // Unload Scene ตัวเองทิ้ง เพื่อกลับไปใช้ MainMenu (หรือหน้า Pause เกม)
        // ตอน Scene ถูกทำลาย AudioSettingsController.OnDisable จะบันทึกค่าเสียงให้เอง
        SceneManager.UnloadSceneAsync(gameObject.scene);
    }
}