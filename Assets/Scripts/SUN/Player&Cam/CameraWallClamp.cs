using UnityEngine;
using Unity.Cinemachine;

[ExecuteAlways]
public class CameraWallClamp : CinemachineExtension
{
    [Header("อ้างอิง Anchor ของห้องนี้")]
    public Transform anchor;

    [Header("กันกล้องทะลุกำแพง")]
    public LayerMask wallLayer;
    public float minDistance = 1.5f;   // ระยะใกล้สุดที่กล้องเข้าหา Anchor ได้
    public float skin = 0.2f;          // เว้นระยะกันกล้องแปะติดกำแพงพอดีเป๊ะ

    protected override void PostPipelineStageCallback(
        CinemachineVirtualCameraBase vcam,
        CinemachineCore.Stage stage,
        ref CameraState state,
        float deltaTime)
    {
        // ทำงานทันทีหลัง Body (Position) คำนวณเสร็จ ก่อนจะส่งต่อไป Blend
        if (stage != CinemachineCore.Stage.Body) return;
        if (anchor == null) return;

        Vector3 origin = anchor.position;
        Vector3 currentPos = state.RawPosition;
        Vector3 dir = currentPos - origin;
        float dist = dir.magnitude;

        if (dist < 0.001f) return; // กันหารด้วยศูนย์
        dir /= dist;

        // ยิงเช็คตามเส้นเดิมเป๊ะ (Anchor → ตำแหน่งกล้องที่ตั้งใจไว้) ไม่เปลี่ยนทิศทางเลย
        if (Physics.Raycast(origin, dir, out RaycastHit hit, dist, wallLayer))
        {
            float safeDist = Mathf.Max(hit.distance - skin, minDistance);
            state.RawPosition = origin + dir * safeDist; // แค่ "ย่นระยะเข้ามา" บนเส้นเดิม
        }
    }
}