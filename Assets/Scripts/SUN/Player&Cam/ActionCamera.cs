using UnityEngine;
using Unity.Cinemachine;

public class ActionCameraController : MonoBehaviour
{
    public CinemachineCamera wallLeanCamera;

    private const int ACTION_PRIORITY = 1000;
    private const int INACTIVE = 0;

    public void OnWallLeanStart() => wallLeanCamera.Priority.Value = ACTION_PRIORITY;
    public void OnWallLeanEnd() => wallLeanCamera.Priority.Value = INACTIVE;
}