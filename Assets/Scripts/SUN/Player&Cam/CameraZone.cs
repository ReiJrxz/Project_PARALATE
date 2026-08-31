using UnityEngine;
using Unity.Cinemachine;

public class CameraZone : MonoBehaviour
{
    [Header("กล้องประจำโซนนี้")]
    public CinemachineCamera virtualCamera;

    [Header("Priority")]
    protected static int globalPriorityCounter = 10;
    protected const int ROOM_TIER_MAX = 900;
    public int inactivePriority = 0;

    private BoxCollider zoneCollider;

    protected virtual void Start()
    {
        if (virtualCamera == null)
        {
            Debug.LogWarning($"{name}: ยังไม่ได้ผูก virtualCamera");
            return;
        }

        virtualCamera.Priority.Value = inactivePriority;
        zoneCollider = GetComponent<BoxCollider>();

        CheckIfPlayerAlreadyInside();
    }

    private void CheckIfPlayerAlreadyInside()
    {
        if (zoneCollider == null)
        {
            Debug.LogWarning($"{name}: ไม่พบ BoxCollider บน GameObject นี้!");
            return;
        }

        Vector3 center = transform.TransformPoint(zoneCollider.center);
        Vector3 halfExtents = Vector3.Scale(zoneCollider.size * 0.5f, transform.lossyScale);

        Collider[] overlaps = Physics.OverlapBox(center, halfExtents, transform.rotation);
        Debug.Log($"{name}: เจอ {overlaps.Length} colliders ในโซน"); // เพิ่มบรรทัดนี้

        foreach (var col in overlaps)
        {
            Debug.Log($"{name}: เจอ collider ชื่อ {col.name} tag = {col.tag}"); // เพิ่มบรรทัดนี้
            if (col.CompareTag("Player"))
            {
                ActivateCamera();
                break;
            }
        }
    }

    private void ActivateCamera()
    {
        globalPriorityCounter++;
        if (globalPriorityCounter >= ROOM_TIER_MAX)
            globalPriorityCounter = 10;

        virtualCamera.Priority.Value = globalPriorityCounter;
        Debug.Log($"{name}: เปิดกล้อง {virtualCamera.name} priority = {virtualCamera.Priority}"); // เพิ่มบรรทัดนี้
    }
    protected virtual void OnTriggerEnter(Collider other)
    {
        if (virtualCamera == null) return;
        if (other.CompareTag("Player"))
        {
            ActivateCamera();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (virtualCamera == null) return;
        if (other.CompareTag("Player"))
        {
            virtualCamera.Priority.Value = inactivePriority;
        }
    }
}