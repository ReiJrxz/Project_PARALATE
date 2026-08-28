using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    public ActionCameraController actionCamera;

    [Header("Wall Lean Detection")]
    public float wallCheckDistance = 1f;
    public LayerMask wallLayer;

    private bool isWallLeaning = false;

    public void OnWallLean(InputAction.CallbackContext context)
    {
        if (!context.performed) return; // ทำงานแค่ตอนกดลง (ไม่ทำงานตอนปล่อย)

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
            EnterWallLean();
        }
    }

    private void EnterWallLean()
    {
        isWallLeaning = true;
        actionCamera.OnWallLeanStart();
    }

    private void ExitWallLean()
    {
        isWallLeaning = false;
        actionCamera.OnWallLeanEnd();
    }
}