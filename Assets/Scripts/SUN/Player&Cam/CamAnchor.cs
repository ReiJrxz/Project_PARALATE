
using UnityEngine;

public class CameraFollowAnchor : MonoBehaviour
{
    [Header("Player to Follow")]
    [SerializeField] private Transform player;

    [Header("Follow Axes")]
    [SerializeField] private bool followX = true;
    [SerializeField] private bool followY = false;
    [SerializeField] private bool followZ = true;

    [Header("Follow Offset")]
    [SerializeField] private Vector3 followOffset = Vector3.zero;

    [Header("Fixed World Height")]
    [SerializeField] private float fixedY = 2.5f;

    [Header("Optional World Bounds")]
    [SerializeField] private bool useBounds = false;
    [SerializeField] private Vector2 xBounds = new Vector2(-10f, 10f);
    [SerializeField] private Vector2 zBounds = new Vector2(-10f, 10f);

    private void LateUpdate()
    {
        if (player == null)
            return;

        Vector3 pos = transform.position;

        // เลือกว่าจะตามตำแหน่ง Player ในแต่ละแกนหรือไม่
        if (followX)
            pos.x = player.position.x + followOffset.x;

        if (followY)
            pos.y = player.position.y + followOffset.y;
        else
            pos.y = fixedY;

        if (followZ)
            pos.z = player.position.z + followOffset.z;

        // จำกัดพื้นที่ที่ Anchor สามารถเคลื่อนที่ได้
        if (useBounds)
        {
            pos.x = Mathf.Clamp(pos.x, xBounds.x, xBounds.y);
            pos.z = Mathf.Clamp(pos.z, zBounds.x, zBounds.y);
        }

        transform.position = pos;
    }

    public void SetPlayer(Transform newPlayer)
    {
        player = newPlayer;
    }
}
