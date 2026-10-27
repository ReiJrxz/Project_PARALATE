using UnityEngine;

/// <summary>
/// จุดใน Base ที่กด E เพื่อเปิดหน้าเลือกด่าน (ใช้ระบบเดียวกับ ItemPickup/KeycardPickup:
/// ต้องมี Collider และอยู่ใน Layer ที่ PickupDetector ตรวจ = itemLayer)
/// </summary>
[RequireComponent(typeof(Collider))]
public class LevelSelectTerminal : MonoBehaviour, IInteractable
{
    [Tooltip("ว่าง = หา LevelSelectUI ใน Scene เอง")]
    public LevelSelectUI ui;

    public string promptText = "กด E เพื่อเลือกด่าน";

    // ซ่อนป้าย "กด E" ระหว่างที่หน้าเลือกด่านเปิดอยู่ (ไม่ให้ข้อความทับหน้าจอ)
    public string PromptText => ui != null && ui.IsOpen ? string.Empty : promptText;

    public void Interact(GameObject interactor)
    {
        if (ui == null)
            ui = FindFirstObjectByType<LevelSelectUI>(FindObjectsInactive.Include);

        if (ui != null && !ui.IsOpen)
            ui.Open();
        else if (ui == null)
            Debug.LogWarning("LevelSelectTerminal: ไม่พบ LevelSelectUI ใน Scene", this);
    }
}
