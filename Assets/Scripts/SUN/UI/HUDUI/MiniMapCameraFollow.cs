using UnityEngine;

public class MinimapCameraFollow : MonoBehaviour
{
    public Transform player;
    public float height = 30f; // ต้องสูงพอที่ Culling Mask/Far Clip Plane ของกล้องจะยังมองเห็นพื้น
    public bool rotateWithPlayer = false;

    void LateUpdate()
    {
        if (player == null) return;

        Vector3 pos = player.position;
        pos.y += height;
        transform.position = pos;

        if (rotateWithPlayer)
        {
            // หมุนกล้องตามทิศที่ตัวละครหันหน้า แต่ยังคงก้มลง 90 องศาเหมือนเดิม
            transform.rotation = Quaternion.Euler(90f, player.eulerAngles.y, 0f);
        }
        // ถ้า rotateWithPlayer = false กล้องจะคง rotation X=90,Y=0,Z=0 ตลอด (north-up)
    }
}