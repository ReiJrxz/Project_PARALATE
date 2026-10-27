using UnityEngine;
using TMPro; // สำคัญ: ต้องใส่เพื่อใช้ TextMeshPro

/// <summary>
/// ป้าย prompt กลางของสิ่งที่โต้ตอบได้ (บันได, ประตู ฯลฯ)
/// หลายสิ่งซ้อนกันได้: ใครแสดงก่อนถือป้ายไว้ จนกว่าจะซ่อนเอง แล้วตัวถัดไปค่อยเข้ามาแทน
/// </summary>
public class InteractUIManager : MonoBehaviour
{
    public static InteractUIManager Instance;

    [Header("UI Components")]
    public GameObject interactPanel; // ตัวก้อน UI (อาจจะเป็นภาพพื้นหลัง)
    public TextMeshProUGUI interactText; // ตัวหนังสือ TextMeshPro

    private Object currentOwner;
    private bool warnedMissingUI;

    private void Awake()
    {
        // กำหนดให้ตัวนี้เป็นตัวหลัก
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);

        // ลากแค่ช่องเดียวก็พอ: ถ้ามีแต่ Panel จะหา Text ในตัวมัน, ถ้ามีแต่ Text จะใช้วัตถุของ Text เป็น Panel
        if (interactText == null && interactPanel != null)
            interactText = interactPanel.GetComponentInChildren<TextMeshProUGUI>(true);

        if (interactPanel == null && interactText != null)
            interactPanel = interactText.gameObject;

        // ปิด UI ไว้ตอนเริ่มเกม
        HideInteract();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    /// <summary>ช่อง UI ครบหรือยัง (ใช้ตอน Debug)</summary>
    public bool HasUI => interactPanel != null && interactText != null;

    // เจ้าของ (owner) ขอแสดงข้อความ: เรียกซ้ำทุกเฟรมได้ ไม่เปลืองเพราะอัปเดตข้อความเฉพาะตอนเปลี่ยน
    public void ShowInteract(Object owner, string message)
    {
        // มีเจ้าของอื่นถือป้ายอยู่ (เจ้าของที่ถูก Destroy แล้วนับเป็น null) ให้รอก่อน
        if (currentOwner != null && currentOwner != owner)
            return;

        currentOwner = owner;

        if ((interactPanel == null || interactText == null) && !warnedMissingUI)
        {
            warnedMissingUI = true;
            Debug.LogWarning("InteractUIManager: ยังไม่ได้ลาก Interact Panel / Interact Text ใน Inspector จึงไม่มีอะไรแสดง", this);
        }

        if (interactText != null && interactText.text != message)
            interactText.text = message;

        SetPanelActive(true);
    }

    // เจ้าของขอซ่อน: ซ่อนได้เฉพาะป้ายที่ตัวเองเป็นคนแสดง
    public void HideInteract(Object owner)
    {
        if (currentOwner != null && currentOwner != owner)
            return;

        currentOwner = null;
        SetPanelActive(false);
    }

    // เวอร์ชันเก่า (ไม่ระบุเจ้าของ) ยังใช้ได้
    public void ShowInteract(string message) => ShowInteract(this, message);

    public void HideInteract()
    {
        currentOwner = null;
        SetPanelActive(false);
    }

    private void SetPanelActive(bool active)
    {
        if (interactPanel != null && interactPanel.activeSelf != active)
            interactPanel.SetActive(active);
    }
}
