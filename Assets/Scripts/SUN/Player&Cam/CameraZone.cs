
using System.Collections.Generic;
using UnityEngine;
using Unity.Cinemachine;

[RequireComponent(typeof(BoxCollider))]
public class CameraZone : MonoBehaviour
{
    [Header("กล้องประจำโซนนี้")]
    public CinemachineCamera virtualCamera;

    [Header("Priority")]
    protected static int globalPriorityCounter = 10;
    protected const int ROOM_TIER_MAX = 900;

    public int inactivePriority = 0;
    public int activePriority = 20;

    protected BoxCollider zoneCollider;

    private readonly HashSet<Collider> playerColliders =
        new HashSet<Collider>();

    protected virtual void Start()
    {
        zoneCollider = GetComponent<BoxCollider>();

        if (zoneCollider != null)
            zoneCollider.isTrigger = true;

        if (virtualCamera == null)
        {
            Debug.LogWarning($"{name}: ยังไม่ได้ผูก virtualCamera");
            return;
        }

        virtualCamera.Priority.Value = inactivePriority;

        CheckIfPlayerAlreadyInside();
    }

    protected virtual void CheckIfPlayerAlreadyInside()
    {
        if (zoneCollider == null)
            return;

        Vector3 center =
            transform.TransformPoint(zoneCollider.center);

        Vector3 halfExtents = Vector3.Scale(
            zoneCollider.size * 0.5f,
            transform.lossyScale);

        Collider[] overlaps = Physics.OverlapBox(
            center, halfExtents, transform.rotation);

        foreach (Collider col in overlaps)
        {
            if (col.CompareTag("Player"))
                playerColliders.Add(col);
        }

        if (playerColliders.Count > 0)
            ActivateCamera();
    }

    protected virtual void ActivateCamera()
    {
        if (virtualCamera == null)
            return;

        globalPriorityCounter++;

        if (globalPriorityCounter >= ROOM_TIER_MAX)
            globalPriorityCounter = 10;

        virtualCamera.Priority.Value =
            Mathf.Max(activePriority, globalPriorityCounter);

        Debug.Log(
            $"{name}: เปิดกล้อง {virtualCamera.name}, " +
            $"Priority = {virtualCamera.Priority.Value}");
    }

    protected virtual void OnTriggerEnter(Collider other)
    {
        if (virtualCamera == null ||
            !other.CompareTag("Player"))
            return;

        bool wasEmpty = playerColliders.Count == 0;
        playerColliders.Add(other);

        if (wasEmpty)
            ActivateCamera();
    }

    protected virtual void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;

        playerColliders.Remove(other);

        if (playerColliders.Count == 0 &&
            virtualCamera != null)
        {
            virtualCamera.Priority.Value = inactivePriority;
        }
    }

    protected virtual void OnDisable()
    {
        playerColliders.Clear();

        if (virtualCamera != null)
            virtualCamera.Priority.Value = inactivePriority;
    }
}
