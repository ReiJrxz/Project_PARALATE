using UnityEngine;

/// <summary>
/// ให้ InteractableObject ถามข้อความ prompt ที่ควรแสดงตามสถานะปัจจุบัน
/// เช่น บันไดบอก "กด E เพื่อปีน" / "กด E เพื่อลงจากบันได", ประตูบอก "ต้องใช้ Keycard Lv.2"
/// </summary>
public interface IInteractPromptSource
{
    /// <summary>คืนข้อความที่จะโชว์ ถ้าคืน null หรือว่าง = ไม่โชว์ prompt</summary>
    string GetPromptMessage(GameObject player);
}
