using UnityEngine;
using Unity.Cinemachine;

public class CameraZone : MonoBehaviour
{
    [Header("กล้องตรง Section นี้")]
    public CinemachineCamera virtualCamera;

    private void Start()
    {
        // เริ่มเกมมา ให้ Priority ต่ำไว้ก่อน
        virtualCamera.Priority = 0;
    }

    private void OnTriggerEnter(Collider other)
    {
        // เมื่อ Player เดินเข้ามาในขอบเขต (Trigger) นี้
        if (other.CompareTag("Player"))
        {
            // ดันค่า Priority ให้สูงกว่ากล้องอื่น 
            // Cinemachine จะทำการ Blend ย้ายกล้องมาให้แบบเนียนๆ
            virtualCamera.Priority = 10;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        // เมื่อ Player เดินออกจากโซนนี้
        if (other.CompareTag("Player"))
        {
            // ลด Priority กลับลงไป
            virtualCamera.Priority = 0;
        }
    }
}