using UnityEngine;
using Unity.Cinemachine;

public class MultiFloorCameraZone : CameraZone
{
    [Header("มุมกล้องสำหรับชั้นนี้")]
    public float cameraPitch = 45f;      // มุมเงย/ก้ม (90 = มองตรงลงพื้นสนิท)
    public float cameraDistance = 12f;   // ระยะห่างจาก Anchor

    protected override void Start()
    {
        base.Start(); // ได้ logic Priority.Value + CheckIfPlayerAlreadyInside มาจาก CameraZone

        if (virtualCamera == null) return;

        var positionComposer = virtualCamera.GetComponent<CinemachinePositionComposer>();
        if (positionComposer != null)
        {
            positionComposer.TargetOffset = CalculateOffset(cameraPitch, cameraDistance);
        }

        // ตั้งมุมกล้องด้วยมือ ตาม Pitch ของชั้นนี้ (เหมือนหลักการ Do Nothing ที่ใช้กับ Room Camera ปกติ)
        virtualCamera.transform.rotation = Quaternion.Euler(cameraPitch, 0f, 0f);
    }

    private Vector3 CalculateOffset(float pitch, float distance)
    {
        float rad = pitch * Mathf.Deg2Rad;
        float height = Mathf.Sin(rad) * distance;
        float horizontal = Mathf.Cos(rad) * distance;
        return new Vector3(0, height, -horizontal);
    }
}