
using UnityEngine;
using Unity.Cinemachine;

public class MultiFloorCameraZone : CameraZone
{
    [Header("มุมกล้องสำหรับชั้นนี้")]
    [Range(0f, 180f)]
    public float cameraPitch = 45f;

    public float cameraDistance = 12f;
    public float cameraYaw = 0f;

    protected override void Start()
    {
        // ตั้งมุมก่อนเรียก Start ของคลาสแม่
        if (virtualCamera != null)
        {
            var composer =
                virtualCamera.GetComponent<CinemachinePositionComposer>();

            if (composer != null)
            {
                composer.TargetOffset =
                    CalculateOffset(
                        cameraPitch,
                        cameraYaw,
                        cameraDistance);
            }

            virtualCamera.transform.rotation =
                Quaternion.Euler(cameraPitch, cameraYaw, 0f);
        }

        base.Start();
    }

    private Vector3 CalculateOffset(
        float pitch, float yaw, float distance)
    {
        float pitchRad = pitch * Mathf.Deg2Rad;

        float height = Mathf.Sin(pitchRad) * distance;
        float horizontalDist = Mathf.Cos(pitchRad) * distance;

        Quaternion yawRotation =
            Quaternion.Euler(0f, yaw, 0f);

        Vector3 horizontalDirection =
            yawRotation * Vector3.back;

        return horizontalDirection * horizontalDist
             + Vector3.up * height;
    }
}
