using UnityEngine;
using TMPro; // สำคัญ: ต้องใส่เพื่อใช้ TextMeshPro

public class InteractUIManager : MonoBehaviour
{
    public static InteractUIManager Instance;

    [Header("UI Components")]
    public GameObject interactPanel; // ตัวก้อน UI (อาจจะเป็นภาพพื้นหลัง)
    public TextMeshProUGUI interactText; // ตัวหนังสือ TextMeshPro

    private void Awake()
    {
        // กำหนดให้ตัวนี้เป็นตัวหลัก
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);

        // ปิด UI ไว้ตอนเริ่มเกม
        HideInteract();
    }

    // ฟังก์ชันสำหรับให้สิ่งของต่างๆ สั่งให้โชว์ข้อความ
    public void ShowInteract(string message)
    {
        interactText.text = "[E] " + message;
        interactPanel.SetActive(true);
    }

    // ฟังก์ชันสั่งซ่อน
    public void HideInteract()
    {
        interactPanel.SetActive(false);
    }
}