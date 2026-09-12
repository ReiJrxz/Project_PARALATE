using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    public ActionCameraController actionCamera;

    [Header("Wall Lean Detection")]
    public float wallCheckDistance = 1f;
    public float wallExitDistance = 1.3f;
    public LayerMask wallLayer;

    [Header("Facing Check")]
    public float maxFacingAngle = 120f; // หันเกินมุมนี้ = ถือว่าหันหนีกำแพง

    private bool isWallLeaning = false;
    private Vector3 wallDirectionOnEnter; // เก็บทิศไปกำแพงตอนเข้า state

    void Update()
    {
        if (isWallLeaning)
        {
            CheckAutoExit();
        }
    }

    public void OnWallLean(InputAction.CallbackContext context)
    {
        if (!context.performed) return;

        if (!isWallLeaning)
            TryEnterWallLean();
        else
            ExitWallLean();
    }

    private void TryEnterWallLean()
    {
        RaycastHit hit;
        Vector3 checkOrigin = transform.position + Vector3.up * 1f;

        if (Physics.Raycast(checkOrigin, transform.forward, out hit, wallCheckDistance, wallLayer))
        {
            wallDirectionOnEnter = transform.forward;
            float side = Vector3.Dot(hit.normal, transform.right);
            bool wallNormalPointsRight = side > 0; // ← ตัวแปรนี้ "เกิด" อยู่แค่ในนี้

            actionCamera.OnWallLeanStart(transform.position, hit.normal, wallNormalPointsRight);
            EnterWallLean();
        }
    }

    private void CheckAutoExit()
    {
        // เช็คระยะ (เดิม)
        RaycastHit hit;
        Vector3 checkOrigin = transform.position + Vector3.up * 1f;
        bool stillNearWall = Physics.Raycast(checkOrigin, transform.forward, out hit, wallExitDistance, wallLayer);

        if (!stillNearWall)
        {
            ExitWallLean();

            Debug.Log("Exited Wall Lean due to distance");
            return; // ออกแล้ว ไม่ต้องเช็คมุมต่อ
        }

        // เช็คมุมหันหน้า (เพิ่มใหม่)
        float angleDiff = Vector3.Angle(transform.forward, wallDirectionOnEnter);
        if (angleDiff > maxFacingAngle)
        {
            ExitWallLean();
        }
    }

    private void EnterWallLean()
    {
        isWallLeaning = true;
    }

    private void ExitWallLean()
    {
        isWallLeaning = false;
        actionCamera.OnWallLeanEnd();
        Debug.Log("Exited Wall Lean");
    }
}