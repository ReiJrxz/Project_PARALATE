using UnityEngine;
using Unity.Cinemachine;

public class MultiFloorCameraZone : CameraZone
{
    [Header("มุมกล้องสำหรับชั้นนี้")]
    public float cameraPitch = 45f;      // มุมเงย/ก้ม (90 = มองตรงลงพื้นสนิท)
    public float cameraDistance = 12f;   // ระยะห่างจาก Anchor
    public float cameraYaw = 0f;

    protected override void Start()
    {
        base.Start(); // ได้ logic Priority.Value + CheckIfPlayerAlreadyInside มาจาก CameraZone

        if (virtualCamera == null) return;

        var positionComposer = virtualCamera.GetComponent<CinemachinePositionComposer>();
        if (positionComposer != null)
        {
            positionComposer.TargetOffset = CalculateOffset(cameraPitch,cameraYaw, cameraDistance);
        }

        // ตั้งมุมกล้องด้วยมือ ตาม Pitch ของชั้นนี้ (เหมือนหลักการ Do Nothing ที่ใช้กับ Room Camera ปกติ)
        virtualCamera.transform.rotation = Quaternion.Euler(cameraPitch, cameraYaw, 0f);
    }

    private Vector3 CalculateOffset(float pitch,float yaw , float distance )
    {
        float pitchRad = pitch * Mathf.Deg2Rad;
        float height = Mathf.Sin(pitchRad) * distance;
        float horizontalDist = Mathf.Cos(pitchRad) * distance;

        Quaternion yawRoataion = Quaternion.Euler(0f, cameraYaw, 0f);
        Vector3 horizontalDirection = yawRoataion * Vector3.back;
        return horizontalDirection * horizontalDist + Vector3.up * height;
    }
}