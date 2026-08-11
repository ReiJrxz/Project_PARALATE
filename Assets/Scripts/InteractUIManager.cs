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
        {
            Destroy(gameObject);
            return;
        }

        // ปิด UI ไว้ตอนเริ่มเกม
        HideInteract();
    }

    // ฟังก์ชันสำหรับให้สิ่งของต่างๆ สั่งให้โชว์ข้อความ
    public void ShowInteract(string message)
    {
        if (interactText != null)
            interactText.text = "[E] " + message;
        else
            Debug.LogWarning("InteractUIManager is missing interactText.", this);

        if (interactPanel != null)
            interactPanel.SetActive(true);
        else
            Debug.LogWarning("InteractUIManager is missing interactPanel.", this);
    }

    // ฟังก์ชันสั่งซ่อน
    public void HideInteract()
    {
        if (interactPanel != null)
            interactPanel.SetActive(false);
    }
}
