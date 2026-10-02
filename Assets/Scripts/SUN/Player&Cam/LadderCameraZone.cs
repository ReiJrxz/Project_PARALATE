
using UnityEngine;
using Unity.Cinemachine;

public class LadderCameraZone : MonoBehaviour
{
    [SerializeField] private CinemachineCamera ladderCamera;
    [SerializeField] private CinemachineCamera roomCamera;

    [SerializeField] private int ladderPriority = 30;
    [SerializeField] private int inactivePriority = 0;

    private void Awake()
    {
        if (ladderCamera != null)
            ladderCamera.Priority.Value = inactivePriority;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;

        if (ladderCamera != null)
            ladderCamera.Priority.Value = ladderPriority;
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;

        if (ladderCamera != null)
            ladderCamera.Priority.Value = inactivePriority;
    }
}
