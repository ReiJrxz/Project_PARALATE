using UnityEngine;
using Unity.Cinemachine;

// Base class — ใช้กับ Preset 1-4 ตรงๆ
public class CameraZone : MonoBehaviour
{
    [Header("กล้องประจำโซนนี้")]
    public CinemachineCamera virtualCamera;

    [Header("Priority")]
    protected static int globalPriorityCounter = 10;
    public int inactivePriority = 0;

    protected virtual void Start()
    {
        if (virtualCamera == null)
        {
            Debug.LogWarning($"{name}: ยังไม่ได้ผูก virtualCamera");
            return;
        }
        virtualCamera.Priority = inactivePriority;
    }

    protected virtual void OnTriggerEnter(Collider other)
    {
        if (virtualCamera == null) return;
        if (other.CompareTag("Player"))
        {
            globalPriorityCounter++;
            virtualCamera.Priority = globalPriorityCounter;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (virtualCamera == null) return;
        if (other.CompareTag("Player"))
        {
            virtualCamera.Priority = inactivePriority;
        }
    }
}