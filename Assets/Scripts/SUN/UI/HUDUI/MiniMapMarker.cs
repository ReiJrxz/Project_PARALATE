using UnityEngine;

public class MinimapMarker : MonoBehaviour
{
    [Tooltip("ตัวจริงที่ marker นี้ต้องตามตำแหน่ง")]
    public Transform target;

    [Tooltip("ความสูงที่ marker ลอยอยู่เหนือพื้น ต้องสูงกว่ากำแพงที่สุดในเลเวล")]
    public float heightOffset = 10f;

    void LateUpdate()
    {
        if (target == null) return;

        Vector3 pos = target.position;
        pos.y = heightOffset; // ใช้ค่าคงที่ ไม่ใช่ target.y + offset เพราะถ้าตัวละครกระโดด/ตกที่สูงต่างกัน dot จะไม่กระพริบเข้าออกจากมุมมองกล้อง
        transform.position = pos;
    }
}