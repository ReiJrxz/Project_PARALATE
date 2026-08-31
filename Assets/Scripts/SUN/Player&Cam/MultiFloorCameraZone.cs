using UnityEngine;
using Unity.Cinemachine;


// Preset 5 — สืบทอดมาจาก base แล้วเพิ่มแค่ส่วนคำนวณมุม/ความสูง
public class MultiFloorCameraZone : CameraZone
{
    [Header("ตั้งค่ามุมสำหรับชั้นนี้")]
    public float cameraPitch = 45f;
    public float cameraDistance = 12f;
    public Transform floorLookAtTarget;

    protected override void Start()
    {
        base.Start(); // เรียก logic priority เดิมจาก base ก่อน

        var transposer = virtualCamera.GetComponent<CinemachineFollow>();
        if (transposer != null)
            transposer.FollowOffset = CalculateOffset(cameraPitch, cameraDistance);

        if (floorLookAtTarget != null)
            virtualCamera.LookAt = floorLookAtTarget;
    }

    private Vector3 CalculateOffset(float pitch, float distance)
    {
        float rad = pitch * Mathf.Deg2Rad;
        float height = Mathf.Sin(rad) * distance;
        float horizontal = Mathf.Cos(rad) * distance;
        return new Vector3(0, height, -horizontal);
    }
}