using UnityEngine;
using Unity.Cinemachine;

public class CameraZone : MonoBehaviour
{
    [Header("กล้องประจำโซนนี้")]
    public CinemachineCamera virtualCamera;

    [Header("Priority")]
    protected static int globalPriorityCounter = 10;
    protected const int ROOM_TIER_MAX = 900; // เพดานกันชน Action Camera (1000+)
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
            if (globalPriorityCounter >= ROOM_TIER_MAX)
                globalPriorityCounter = 10; // wrap กลับ กันชน tier อื่น

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