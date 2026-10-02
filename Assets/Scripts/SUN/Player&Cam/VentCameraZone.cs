using UnityEngine;
using Unity.Cinemachine;

public class VentCameraZone : CameraZone
{
    [Header("Player Reference (ลาก Player ที่มี TopDownPlayerController)")]
    public TopDownPlayerController player;

    private const int VENT_PRIORITY = 1500; // สูงกว่า Room (10-900) และ Action Camera (1000) เสมอ

    protected override void ActivateCamera()
    {
        if (virtualCamera == null) return;

        virtualCamera.Priority.Value = VENT_PRIORITY;

        if (player != null)
            player.SetTankControlMode(true);

        Debug.Log($"{name}: เปิดกล้อง Vent {virtualCamera.name}, Priority = {VENT_PRIORITY}");
    }

    protected override void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        base.OnTriggerExit(other); // ให้ Base Class จัดการ HashSet + รีเซ็ต Priority เป็น inactive ให้

        if (player != null)
            player.SetTankControlMode(false);
    }
}