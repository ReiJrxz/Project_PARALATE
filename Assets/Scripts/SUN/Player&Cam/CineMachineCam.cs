using UnityEngine;
using Unity.Cinemachine;

public class CameraZone : MonoBehaviour
{
    [Header("กล้องประจำโซนนี้")]
    public CinemachineCamera virtualCamera;

    [Header("ตั้งค่า Priority")]
    public int activePriority = 100; // ค่าตอนเดินเข้าห้อง
    public int inactivePriority = 0; // ค่าตอนเดินออกห้อง

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            // เปลี่ยนจาก 10 เป็นตัวแปร activePriority
            virtualCamera.Priority = activePriority;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            // เปลี่ยนจาก 0 เป็นตัวแปร inactivePriority
            virtualCamera.Priority = inactivePriority;
        }
    }
}