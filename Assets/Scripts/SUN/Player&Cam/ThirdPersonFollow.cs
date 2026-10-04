using UnityEngine;

public class ThirdPersonFollowCamera : MonoBehaviour
{
    [Header("ผู้เล่นที่จะติดตาม")]
    public Transform player;

    [Header("มุมกล้อง (Third Person)")]
    [Range(0f, 60f)]
    public float pitch = 20f;
    public float distance = 8f;
    public float heightOffset = 1.6f;

    [Header("ความนุ่มนวล")]
    public float positionSmoothTime = 0.12f;

    [Header("ความหน่วงของการหมุนตาม (คล้าย Asset Store TPS Camera)")]
    [Tooltip("ยิ่งค่าน้อย ยิ่งหน่วง/ตามช้า ยิ่งค่ามาก ยิ่งตามไว")]
    public float yawSmoothTime = 0.25f;

    [Header("กันกล้องทะลุกำแพง")]
    public LayerMask wallLayer;
    public float minDistance = 2.5f;
    public float skin = 0.2f;

    private Vector3 positionVelocity;
    private float yawVelocity;
    private float currentYaw;

    void Start()
    {
        if (player != null) currentYaw = player.eulerAngles.y;
    }

    void LateUpdate()
    {
        if (player == null) return;

        // ★ จุดสำคัญ — ค่อยๆ ไล่ตามทิศทางผู้เล่น แทนที่จะอ่านค่าตรงๆ ทุกเฟรม
        float targetYaw = player.eulerAngles.y;
        currentYaw = Mathf.SmoothDampAngle(currentYaw, targetYaw, ref yawVelocity, yawSmoothTime);

        Vector3 offsetDir = CalculateOffsetDirection(pitch, currentYaw);
        Vector3 pivotPoint = player.position + Vector3.up * heightOffset;

        float safeDist = distance;
        if (Physics.Raycast(pivotPoint, offsetDir, out RaycastHit hit, distance, wallLayer))
        {
            safeDist = Mathf.Max(hit.distance - skin, minDistance);
        }

        Vector3 targetPos = pivotPoint + offsetDir * safeDist;
        transform.position = Vector3.SmoothDamp(transform.position, targetPos, ref positionVelocity, positionSmoothTime);

        transform.rotation = Quaternion.LookRotation(pivotPoint - transform.position);
    }

    Vector3 CalculateOffsetDirection(float pitch, float yaw)
    {
        float pitchRad = pitch * Mathf.Deg2Rad;
        float height = Mathf.Sin(pitchRad);
        float horizontal = Mathf.Cos(pitchRad);

        Quaternion yawRotation = Quaternion.Euler(0f, yaw, 0f);
        Vector3 horizontalDir = yawRotation * Vector3.back;

        return (horizontalDir * horizontal + Vector3.up * height).normalized;
    }
}