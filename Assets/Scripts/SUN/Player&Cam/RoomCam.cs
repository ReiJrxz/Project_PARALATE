using UnityEngine;

public class RoomCameraFollow : MonoBehaviour
{
    [Header("Anchor ของห้องนี้")]
    public Transform anchor;

    [Header("มุมกล้อง (ตั้งตามที่ต้องการเห็นในห้องนี้)")]
    public float pitch = 90f;   // 90 = มองตรงลงพื้น, ยิ่งน้อยยิ่งเอียงมองข้าง
    public float yaw = 0f;      // ทิศทางแนวราบ (หมุนรอบแกน Y)
    public float distance = 14f; // ระยะห่างจาก Anchor

    [Header("ความนุ่มนวลตอนตาม")]
    [Tooltip("0 = ตามทันทีไม่มีหน่วง, ค่าน้อย = นุ่ม, ค่ามาก = ไว")]
    public float followSpeed = 5f;

    void LateUpdate()
    {
        if (anchor == null) return;

        Vector3 offset = CalculateOffset(pitch, yaw, distance);
        Vector3 targetPos = anchor.position + offset;
        Quaternion targetRot = Quaternion.Euler(pitch, yaw, 0f);

        if (followSpeed <= 0f)
        {
            transform.position = targetPos;
            transform.rotation = targetRot;
        }
        else
        {
            transform.position = Vector3.Lerp(transform.position, targetPos, Time.deltaTime * followSpeed);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * followSpeed);
        }
    }

    Vector3 CalculateOffset(float pitch, float yaw, float distance)
    {
        float pitchRad = pitch * Mathf.Deg2Rad;
        float height = Mathf.Sin(pitchRad) * distance;
        float horizontalDist = Mathf.Cos(pitchRad) * distance;

        Quaternion yawRotation = Quaternion.Euler(0f, yaw, 0f);
        Vector3 horizontalDirection = yawRotation * Vector3.back;

        return horizontalDirection * horizontalDist + Vector3.up * height;
    }
}