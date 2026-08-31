using UnityEngine;

public class CameraFollowAnchor : MonoBehaviour
{
    public Transform player;

    void LateUpdate()
    {
        transform.position = player.position;
        // ไม่แตะ rotation เลย — ปล่อย identity ไว้ตลอด กันกล้องหมุนตามผู้เล่น
    }
}