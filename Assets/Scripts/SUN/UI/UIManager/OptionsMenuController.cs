using UnityEngine;
using UnityEngine.SceneManagement;

public class OptionsMenuController : MonoBehaviour
{
    [Header("Tab Panels")]
    public GameObject gamePanel;
    public GameObject soundPanel;
    public GameObject videoPanel;
    public GameObject controllerPanel;

    private void Start()
    {
        // ตอนเปิดหน้า Option ขึ้นมา ให้โชว์หน้า Game (หรือ Video) เป็นหน้าแรก
        OpenTab(gamePanel);
    }

    // ฟังก์ชันสำหรับผูกกับปุ่ม Tab ด้านบน
    public void OnGameTabPressed() => OpenTab(gamePanel);
    public void OnSoundTabPressed() => OpenTab(soundPanel);
    public void OnVideoTabPressed() => OpenTab(videoPanel);
    public void OnControllerTabPressed() => OpenTab(controllerPanel);

    private void OpenTab(GameObject activePanel)
    {
        // ปิด Panel ทั้งหมดก่อน
        if (gamePanel != null) gamePanel.SetActive(false);
        if (soundPanel != null) soundPanel.SetActive(false);
        if (videoPanel != null) videoPanel.SetActive(false);
        if (controllerPanel != null) controllerPanel.SetActive(false);

        // เปิดเฉพาะ Panel ที่ถูกเลือก
        if (activePanel != null)
        {
            activePanel.SetActive(true);
        }
    }

    public void OnCloseOptionsPressed()
    {
        // สั่ง Unload Scene ตัวเองทิ้ง เพื่อกลับไปใช้ MainMenu (หรือหน้า Pause เกม)
        SceneManager.UnloadSceneAsync(gameObject.scene.name);
    }
}