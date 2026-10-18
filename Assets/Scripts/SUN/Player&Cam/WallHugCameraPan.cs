using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Cinemachine;

public class WallHugCameraPan : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private WallHugController wallHug;
    [SerializeField] private CinemachineCamera wallHugCamera;
    [SerializeField] private CinemachineFollow cameraFollow;
    [SerializeField] private Transform wallHugCameraAnchor;
    [SerializeField] private PlayerInput playerInput;

    [Header("Lateral camera position")]
    [SerializeField, Min(0f)] private float shoulderOffset = 0.65f;
    [SerializeField, Min(0f)] private float panLimit = 1.25f;
    [SerializeField, Min(0.1f)] private float panSpeed = 1.8f;
    [SerializeField, Min(0.01f)] private float cameraDamping = 0.18f;

    [Header("Input")]
    [SerializeField] private string cameraLookActionName = "CameraLook";

    [Header("Priority")]
    [SerializeField] private int activePriority = 1000;

    private InputAction cameraLookAction;
    private Vector3 baseFollowOffset;
    private Quaternion defaultAnchorLocalRotation;

    private float currentSideOffset;
    private float targetSideOffset;
    private float panInputOffset;
    private float dampingVelocity;
    private bool wasHugging;

    private void Awake()
    {
        if (wallHugCamera != null && cameraFollow == null)
            cameraFollow = wallHugCamera.GetComponent<CinemachineFollow>();

        if (wallHug == null)
            wallHug = GetComponentInParent<WallHugController>();

        if (playerInput == null)
            playerInput = GetComponentInParent<PlayerInput>();

        if (wallHugCameraAnchor != null)
            defaultAnchorLocalRotation = wallHugCameraAnchor.localRotation;

        if (playerInput != null && playerInput.actions != null)
            cameraLookAction = playerInput.actions.FindAction(cameraLookActionName, false);

        if (cameraFollow != null)
            baseFollowOffset = cameraFollow.FollowOffset;

        if (wallHugCamera != null)
            wallHugCamera.Priority.Value = 0;
    }

    private void LateUpdate()
    {
        if (wallHug == null || wallHugCamera == null || cameraFollow == null)
            return;

        bool hugging = wallHug.IsHugging;

        if (hugging)
        {
            // WallFacingDirection คือ normal ที่ชี้ออกจากกำแพง
            // หัน Anchor เข้าหากำแพง ทำให้ Follow Offset Z ติดลบ
            // พากล้องไปอยู่ฝั่งโล่งแทนที่จะจมเข้าไปในกำแพง
            Vector3 intoWall = -wallHug.WallFacingDirection;
            intoWall.y = 0f;

            if (wallHugCameraAnchor != null && intoWall.sqrMagnitude > 0.001f)
            {
                wallHugCameraAnchor.rotation =
                    Quaternion.LookRotation(intoWall.normalized, Vector3.up);
            }
        }

        if (hugging && !wasHugging)
        {
            panInputOffset = 0f;
            targetSideOffset = wallHug.CameraSide * shoulderOffset;
            wallHugCamera.Priority.Value = activePriority;
        }
        else if (!hugging && wasHugging)
        {
            targetSideOffset = 0f;
            panInputOffset = 0f;
            wallHugCamera.Priority.Value = 0;

            // คืน Anchor ให้กลับมาหันตามผู้เล่นตามปกติ
            if (wallHugCameraAnchor != null)
                wallHugCameraAnchor.localRotation = defaultAnchorLocalRotation;
        }

        if (hugging && cameraLookAction != null)
        {
            float horizontal = cameraLookAction.ReadValue<Vector2>().x;
            bool mouse = cameraLookAction.activeControl != null &&
                         cameraLookAction.activeControl.device is Mouse;

            panInputOffset = Mathf.Clamp(
                panInputOffset + horizontal * panSpeed * (mouse ? 0.01f : Time.deltaTime),
                -panLimit,
                panLimit);
        }

        float desiredX = baseFollowOffset.x + targetSideOffset + panInputOffset;

        currentSideOffset = Mathf.SmoothDamp(
            currentSideOffset,
            desiredX - baseFollowOffset.x,
            ref dampingVelocity,
            cameraDamping);

        Vector3 offset = baseFollowOffset;
        offset.x += currentSideOffset;
        cameraFollow.FollowOffset = offset;

        wasHugging = hugging;
    }

    private void OnDisable()
    {
        wasHugging = false;
        panInputOffset = 0f;
        currentSideOffset = 0f;
        dampingVelocity = 0f;

        if (wallHugCameraAnchor != null)
            wallHugCameraAnchor.localRotation = defaultAnchorLocalRotation;

        if (cameraFollow != null)
            cameraFollow.FollowOffset = baseFollowOffset;

        if (wallHugCamera != null)
            wallHugCamera.Priority.Value = 0;
    }
}