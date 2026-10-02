
using UnityEngine;

public class CameraFollowAnchor : MonoBehaviour
{
    [Header("Player to Follow")]
    [SerializeField] private Transform player;

    [Header("Follow Axes")]
    [SerializeField] private bool followX = true;
    [SerializeField] private bool followZ = true;

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

        if (followX)
            pos.x = player.position.x;

        if (followZ)
            pos.z = player.position.z;

        pos.y = fixedY;

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
