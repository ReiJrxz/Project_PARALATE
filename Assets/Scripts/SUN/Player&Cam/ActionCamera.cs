using UnityEngine;
using Unity.Cinemachine;

public class ActionCameraController : MonoBehaviour
{
    public CinemachineCamera wallLeanCamera;

    [Header("มุมกล้องเทียบกับผู้เล่นตอนพิง")]
    public float cameraDistance = 3.5f;
    public float cameraHeight = 1.6f;
    public float lookAtHeight = 1.5f;
    public float sideOffsetAngle = 30f;
    public float screenSideOffset = 0.5f; // ระยะเลื่อนกล้องไปด้านข้าง (เพื่อไม่ให้ชนกำแพง)

    [Header("กันกล้องทะลุกำแพง")]
    public LayerMask wallLayer; // เพิ่มบรรทัดนี้ — ประกาศแยกต่างหากในคลาสนี้

    [Header("Damping")]
    public float transitionSpeed = 5f;

    private const int ACTION_PRIORITY = 1000;
    private const int INACTIVE = 0;

    private Vector3 targetPosition;
    private Quaternion targetRotation;
    private bool isTransitioning = false;

    void Update()
    {
        if (isTransitioning && wallLeanCamera != null)
        {
            wallLeanCamera.transform.position = Vector3.Lerp(
                wallLeanCamera.transform.position, targetPosition, Time.deltaTime * transitionSpeed);

            wallLeanCamera.transform.rotation = Quaternion.Slerp(
                wallLeanCamera.transform.rotation, targetRotation, Time.deltaTime * transitionSpeed);
        }
    }

    public void OnWallLeanStart(Vector3 playerPosition, Vector3 wallNormal, bool wallNormalPointsRight)
    {
        if (wallLeanCamera == null) return;

        wallLeanCamera.Priority.Value = ACTION_PRIORITY;

        // ถ้ากำแพงเบี้ยวขวา ให้กล้องอ้อมไปฝั่งซ้ายแทน (เพื่อเปิดมุมมองฝั่งขวาที่ไม่มีอะไรบัง) และกลับกัน
        float dynamicAngle = wallNormalPointsRight ? -sideOffsetAngle : sideOffsetAngle;
        float dynamicScreenOffset = wallNormalPointsRight ? -screenSideOffset : screenSideOffset;

        Vector3 facingDirection = wallNormal;
        Quaternion sideRotation = Quaternion.AngleAxis(dynamicAngle, Vector3.up);
        Vector3 cameraDirection = sideRotation * facingDirection;

        Vector3 lookTarget = playerPosition + Vector3.up * lookAtHeight;
        Vector3 rayOrigin = playerPosition + Vector3.up * cameraHeight;

        float actualDistance = cameraDistance;
        RaycastHit hit;
        if (Physics.Raycast(rayOrigin, cameraDirection, out hit, cameraDistance, wallLayer))
        {
            actualDistance = Mathf.Max(hit.distance - 0.2f, 1.5f);
        }

        targetPosition = rayOrigin + cameraDirection * actualDistance;

        Vector3 rightAxis = Vector3.Cross(Vector3.up, cameraDirection).normalized;
        Vector3 framedLookTarget = lookTarget + rightAxis * dynamicScreenOffset;

        targetRotation = Quaternion.LookRotation(framedLookTarget - targetPosition);

        isTransitioning = true;
    }

    public void OnWallLeanEnd()
    {
        if (wallLeanCamera == null) return;
        wallLeanCamera.Priority.Value = INACTIVE;
        isTransitioning = false;
    }
}