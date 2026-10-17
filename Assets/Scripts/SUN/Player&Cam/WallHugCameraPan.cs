using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Cinemachine;

public class WallHugCameraPan : MonoBehaviour
{
    [Header("Cinemachine camera")]
    [SerializeField] private CinemachineCamera wallHugCamera;
    [SerializeField] private CinemachineOrbitalFollow orbitalFollow;
    [SerializeField] private PlayerInput playerInput;

    [Header("Pan input")]
    [Tooltip("Input Action แบบ Value / Vector2 เช่น Right Stick หรือ Mouse Delta")]
    [SerializeField] private string cameraLookActionName = "CameraLook";
    [SerializeField, Min(1f)] private float panSpeed = 100f;
    [SerializeField, Range(10f, 85f)] private float panLimit = 65f;

    [Header("Starting angle")]
    [Tooltip("มุมเริ่มต้นเมื่อกำแพงอยู่ซ้าย ปรับตามทิศของ Tracking Target")]
    [SerializeField] private float leftWallCenterAngle = 0f;
    [Tooltip("มุมเริ่มต้นเมื่อกำแพงอยู่ขวา ปรับตามทิศของ Tracking Target")]
    [SerializeField] private float rightWallCenterAngle = 180f;

    [Header("Priority")]
    [SerializeField] private int wallHugPriority = 1000;

    private InputAction cameraLookAction;
    private bool isWallHugCameraActive;

    private void Awake()
    {
        if (wallHugCamera != null && orbitalFollow == null)
            orbitalFollow = wallHugCamera.GetComponent<CinemachineOrbitalFollow>();

        if (playerInput == null)
            playerInput = GetComponentInParent<PlayerInput>();

        if (playerInput != null && playerInput.actions != null)
            cameraLookAction = playerInput.actions.FindAction(cameraLookActionName, false);

        if (wallHugCamera != null)
            wallHugCamera.Priority.Value = 0;
    }

    public void EnterWallHug(int wallSide)
    {
        if (wallHugCamera == null || orbitalFollow == null)
        {
            Debug.LogWarning("WallHugCameraPan: ใส่ CinemachineCamera และ CinemachineOrbitalFollow ให้ครบ", this);
            return;
        }

        float center = wallSide < 0 ? leftWallCenterAngle : rightWallCenterAngle;

        InputAxis axis = orbitalFollow.HorizontalAxis;
        axis.Center = center;
        axis.Range = new Vector2(center - panLimit, center + panLimit);
        axis.Value = center;
        orbitalFollow.HorizontalAxis = axis;

        isWallHugCameraActive = true;
        wallHugCamera.Priority.Value = wallHugPriority;
    }

    public void ExitWallHug()
    {
        isWallHugCameraActive = false;

        if (wallHugCamera != null)
            wallHugCamera.Priority.Value = 0;
    }

    private void Update()
    {
        if (!isWallHugCameraActive || orbitalFollow == null || cameraLookAction == null)
            return;

        Vector2 look = cameraLookAction.ReadValue<Vector2>();
        bool mouseInput = cameraLookAction.activeControl != null &&
                          cameraLookAction.activeControl.device is Mouse;

        // Mouse Delta เป็นระยะการเลื่อนต่อเฟรม; จอยเป็นค่าความเร็วระหว่าง -1 ถึง 1
        float delta = look.x * panSpeed * (mouseInput ? 1f : Time.deltaTime);

        InputAxis axis = orbitalFollow.HorizontalAxis;
        axis.Value = Mathf.Clamp(
            axis.Value + delta,
            axis.Center - panLimit,
            axis.Center + panLimit);
        orbitalFollow.HorizontalAxis = axis;
    }

    private void OnDisable()
    {
        ExitWallHug();
    }
}
