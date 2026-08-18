using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Cinemachine; // 1. เพิ่ม Library ของ Cinemachine

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(PlayerInput))]
public class TopDownPlayerController : MonoBehaviour
{
    [Header("Movement Speeds")]
    public float walkSpeed = 5f;
    public float sprintSpeed = 8f;
    public float crouchSpeed = 2.5f;

    [Header("Rotation Settings")]
    public float rotationSpeed = 15f;

    [Header("Aim & Armed Settings")]
    public bool isArmed = false;
    public float cameraZoomSpeed = 10f;
    private float normalFOV = 50f;
    private float aimFOV = 40f;

    // 2. เพิ่มช่องให้ใส่กล้อง Cinemachine
    [Header("Cinemachine Camera")]
    public CinemachineCamera virtualCamera;

    [Header("Vault Settings (ปีนข้ามสิ่งกีดขวาง)")]
    public float vaultDistance = 2f;
    public float vaultHeight = 1.2f;
    public float vaultDuration = 0.4f;
    public LayerMask hurdleLayer;
    public float raycastDistance = 1f;
    public float lowRayHeight = 0.2f;
    public float highRayHeight = 1.0f;

    [Header("Animation (อนิเมชั่น)")]
    public Animator animator;

    [Header("Ladder Settings (ปีนบันได)")]
    public float climbUpSpeed = 3f;
    public float climbDownSpeed = 6f;
    public float topExitOffset = 1.5f;
    public LayerMask ladderLayer;

    private CharacterController controller;
    private Camera mainCamera;
    private PlayerInput playerInput;

    private InputAction moveAction;
    private InputAction sprintAction;
    private InputAction crouchAction;
    private InputAction pointerAction;
    private InputAction hurdleAction;
    private InputAction aimAction;

    private bool isVaulting = false;
    private bool isMovementLocked = false;
    private bool isClimbing = false;
    private float movementSpeedMultiplier = 1f;

    private bool isCrouching = false;
    private bool isSprinting = false;
    private bool isAiming = false;

    private float climbCooldown = 0f;

    public bool IsMovementLocked => isMovementLocked;
    public bool IsClimbing => isClimbing;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        mainCamera = Camera.main;
        playerInput = GetComponent<PlayerInput>();

        moveAction = playerInput.actions["Movement"];
        sprintAction = playerInput.actions["Sprint"];
        crouchAction = playerInput.actions["Crouch"];
        pointerAction = playerInput.actions["PointerPosition"];
        hurdleAction = playerInput.actions["Hurdle"];
        aimAction = playerInput.actions["Aiming"];

        crouchAction.performed += OnCrouch;
        sprintAction.performed += OnSprintStart;
        sprintAction.canceled += OnSprintStop;
        hurdleAction.performed += OnVault;

        // 3. ดึงค่า FOV จาก Cinemachine แทน
        if (virtualCamera != null)
        {
            normalFOV = virtualCamera.Lens.FieldOfView;
            aimFOV = normalFOV * 0.8f;
        }

        // 4. บังคับซ่อนเมาส์ตั้งแต่เริ่ม และ "ขังเมาส์" ไว้ในกรอบหน้าต่างเกม
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Confined;
    }

    private void OnDestroy()
    {
        if (crouchAction != null) crouchAction.performed -= OnCrouch;
        if (sprintAction != null)
        {
            sprintAction.performed -= OnSprintStart;
            sprintAction.canceled -= OnSprintStop;
        }
        if (hurdleAction != null) hurdleAction.performed -= OnVault;
    }

    private void OnCrouch(InputAction.CallbackContext context)
    {
        isCrouching = !isCrouching;
    }

    private void OnSprintStart(InputAction.CallbackContext context)
    {
        isSprinting = true;
    }

    private void OnSprintStop(InputAction.CallbackContext context)
    {
        isSprinting = false;
    }

    private void OnVault(InputAction.CallbackContext context)
    {
        if (isVaulting || isMovementLocked || isClimbing) return;
        AttemptVault();
    }

    void Update()
    {
        if (climbCooldown > 0f) climbCooldown -= Time.deltaTime;

        if (isVaulting || isMovementLocked) return;

        if (isClimbing)
        {
            HandleClimbing();
            return;
        }

        if (aimAction != null)
        {
            isAiming = aimAction.IsPressed();
        }

        // 5. ปรับให้ซูมผ่านกล้อง Cinemachine แทน MainCamera
        if (!isArmed && virtualCamera != null)
        {
            float targetFOV = isAiming ? aimFOV : normalFOV;
            virtualCamera.Lens.FieldOfView = Mathf.Lerp(virtualCamera.Lens.FieldOfView, targetFOV, cameraZoomSpeed * Time.deltaTime);
        }

        HandleMovement();
        HandleRotation();
    }

    public void SetMovementLocked(bool locked)
    {
        isMovementLocked = locked;
    }

    public void SetMovementSpeedMultiplier(float multiplier)
    {
        movementSpeedMultiplier = Mathf.Max(0f, multiplier);
    }

    void HandleMovement()
    {
        Vector2 moveInput = moveAction.ReadValue<Vector2>();
        Vector3 direction = new Vector3(moveInput.x, 0f, moveInput.y).normalized;
        float currentSpeed = walkSpeed;

        if (isCrouching)
        {
            currentSpeed = crouchSpeed;
        }
        else if (isSprinting)
        {
            currentSpeed = sprintSpeed;
        }

        currentSpeed *= movementSpeedMultiplier;

        if (direction.magnitude >= 0.1f)
        {
            controller.Move(direction * currentSpeed * Time.deltaTime);
        }

        controller.Move(new Vector3(0, -9.81f * Time.deltaTime, 0));

        if (animator != null)
        {
            animator.SetFloat("Speed", direction.magnitude * walkSpeed);
        }
    }

    void HandleRotation()
    {
        if (isAiming || isArmed)
        {
            Vector2 mousePos = pointerAction.ReadValue<Vector2>();
            Ray ray = mainCamera.ScreenPointToRay(mousePos);
            Plane groundPlane = new Plane(Vector3.up, Vector3.zero);
            float rayDistance;

            if (groundPlane.Raycast(ray, out rayDistance))
            {
                Vector3 point = ray.GetPoint(rayDistance);
                Vector3 lookTarget = new Vector3(point.x, transform.position.y, point.z);

                Quaternion targetRotation = Quaternion.LookRotation(lookTarget - transform.position);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
            }
        }
        else
        {
            Vector2 moveInput = moveAction.ReadValue<Vector2>();
            Vector3 direction = new Vector3(moveInput.x, 0f, moveInput.y).normalized;

            if (direction.magnitude >= 0.1f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(direction);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
            }
        }
    }

    void AttemptVault()
    {
        Vector3 horizontalForward = new Vector3(transform.forward.x, 0f, transform.forward.z).normalized;
        Vector3 lowRayOrigin = transform.position + (Vector3.up * lowRayHeight);
        Vector3 highRayOrigin = transform.position + (Vector3.up * highRayHeight);

        bool hitLow = Physics.Raycast(lowRayOrigin, horizontalForward, out RaycastHit hit, raycastDistance, hurdleLayer);
        bool hitHigh = Physics.Raycast(highRayOrigin, horizontalForward, raycastDistance, hurdleLayer);

        if (hitLow && !hitHigh)
        {
            Vector3 vaultDirection = -hit.normal;
            vaultDirection.y = 0f;
            vaultDirection.Normalize();

            Vector3 landingSpot = transform.position + (vaultDirection * vaultDistance);
            Vector3 landingRayOrigin = landingSpot + (Vector3.up * highRayHeight);

            bool hitThickWall = Physics.Raycast(landingRayOrigin, Vector3.down, highRayHeight, hurdleLayer);

            if (!hitThickWall)
            {
                transform.rotation = Quaternion.LookRotation(vaultDirection);
                StartCoroutine(VaultRoutine(landingSpot));
            }
        }
    }

    IEnumerator VaultRoutine(Vector3 targetPosition)
    {
        isVaulting = true;
        controller.enabled = false;

        Vector3 startPos = transform.position;
        float timePassed = 0f;

        while (timePassed < 1f)
        {
            timePassed += Time.deltaTime / vaultDuration;
            Vector3 currentPos = Vector3.Lerp(startPos, targetPosition, timePassed);
            currentPos.y += Mathf.Sin(timePassed * Mathf.PI) * vaultHeight;

            transform.position = currentPos;
            yield return null;
        }

        transform.position = targetPosition;
        controller.enabled = true;
        isVaulting = false;
    }

    public void StartClimbing(Vector3 lookDirection, Transform startPoint)
    {
        if (isClimbing) { StopClimbing(); return; }
        if (climbCooldown > 0f) return;

        isClimbing = true;
        transform.rotation = Quaternion.LookRotation(lookDirection);

        if (animator != null) animator.SetBool("IsClimbing", true);
        if (startPoint != null) StartCoroutine(SmoothClimbEntryRoutine(startPoint.position));
    }

    IEnumerator SmoothClimbEntryRoutine(Vector3 targetPos)
    {
        isMovementLocked = true;
        controller.enabled = false;

        Vector3 startPos = transform.position;
        float timePassed = 0f;
        float duration = 0.35f;

        while (timePassed < 1f)
        {
            timePassed += Time.deltaTime / duration;
            transform.position = Vector3.Lerp(startPos, targetPos, timePassed);
            yield return null;
        }

        transform.position = targetPos;
        controller.enabled = true;
        isMovementLocked = false;
    }

    void HandleClimbing()
    {
        Vector2 moveInput = moveAction.ReadValue<Vector2>();
        float currentClimbSpeed = moveInput.y < 0 ? climbDownSpeed : climbUpSpeed;
        Vector3 climbDirection = new Vector3(0f, moveInput.y, 0f);

        controller.Move(climbDirection * currentClimbSpeed * Time.deltaTime);

        if (animator != null) animator.SetFloat("ClimbSpeed", moveInput.y);

        CheckLadderExit(moveInput.y, moveInput.x);
    }

    void CheckLadderExit(float verticalInput, float horizontalInput)
    {
        bool isAtBottom = controller.isGrounded || Physics.Raycast(transform.position + (Vector3.up * 0.1f), Vector3.down, 0.4f, ~ladderLayer, QueryTriggerInteraction.Ignore);

        if (isAtBottom)
        {
            if (verticalInput < 0 || Mathf.Abs(horizontalInput) > 0.1f)
            {
                StopClimbing();
                return;
            }
        }

        if (verticalInput > 0)
        {
            Vector3 rayOrigin = transform.position + (Vector3.up * highRayHeight);
            if (!Physics.Raycast(rayOrigin, transform.forward, 1.2f, ladderLayer, QueryTriggerInteraction.Ignore))
            {
                StartCoroutine(LadderTopExitRoutine());
            }
        }
    }

    void StopClimbing()
    {
        isClimbing = false;
        climbCooldown = 0.5f;
        if (animator != null)
        {
            animator.SetBool("IsClimbing", false);
            animator.SetFloat("ClimbSpeed", 0f);
        }
    }

    IEnumerator LadderTopExitRoutine()
    {
        isMovementLocked = true;
        if (animator != null) animator.SetTrigger("ClimbTopExit");
        controller.enabled = false;

        Vector3 startPos = transform.position;
        Vector3 targetPos = transform.position + (Vector3.up * 1.2f) + (transform.forward * topExitOffset);
        float timePassed = 0f;
        float duration = 0.5f;

        while (timePassed < 1f)
        {
            timePassed += Time.deltaTime / duration;
            transform.position = Vector3.Lerp(startPos, targetPos, timePassed);
            yield return null;
        }

        transform.position = targetPos;
        controller.enabled = true;
        StopClimbing();
        isMovementLocked = false;
    }
    private void OnDrawGizmos()
    {
        Vector3 horizontalForward = new Vector3(transform.forward.x, 0f, transform.forward.z).normalized;

        Gizmos.color = Color.green;
        Gizmos.DrawRay(transform.position + (Vector3.up * lowRayHeight), horizontalForward * raycastDistance);

        Gizmos.color = Color.red;
        Gizmos.DrawRay(transform.position + (Vector3.up * highRayHeight), horizontalForward * raycastDistance);

        if (Application.isPlaying)
        {
            Gizmos.color = Color.yellow;
            Vector3 testLandingSpot = transform.position + (horizontalForward * vaultDistance);
            Vector3 testLandingOrigin = testLandingSpot + (Vector3.up * highRayHeight);

            Gizmos.DrawRay(testLandingOrigin, Vector3.down * highRayHeight);
            Gizmos.DrawSphere(testLandingSpot, 0.1f);
        }
    }
}