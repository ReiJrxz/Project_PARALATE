using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Cinemachine;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(PlayerInput))]
[RequireComponent(typeof(AudioSource))]
public class TopDownPlayerController : MonoBehaviour
{
    [Header("Movement Type")]
    [Tooltip("ติ๊กถูกสำหรับ Isometric/Third-Person, เอาออกสำหรับ Top-Down เดิม")]
    public bool useCameraRelativeMovement = false;

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

    [Header("Crouch Physical Settings (การย่อตัว)")]
    [Tooltip("เปิดเพื่อหดขนาดแคปซูลฟิสิกส์ตอนนั่ง (ถ้าเปิดแล้วเดินไม่ไปให้ปิดไว้)")]
    public bool enablePhysicalCrouch = false;
    public float crouchHeight = 1f;
    public float crouchTransitionSpeed = 10f;
    private float originalHeight;
    private Vector3 originalCenter;

   
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
    private InputAction whistleAction;

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

    [Header("Player Audio")]
    public PlayerAudioController playerAudio;
    void Start()
    {
        controller = GetComponent<CharacterController>();
        mainCamera = Camera.main;
        playerInput = GetComponent<PlayerInput>();

        if (playerAudio == null) playerAudio = GetComponent<PlayerAudioController>();

        // จำขนาดแคปซูลดั้งเดิมไว้ ป้องกันปัญหาจมพื้น
        if (controller != null)
        {
            originalHeight = controller.height;
            originalCenter = controller.center;
        }

        moveAction = playerInput.actions["Movement"];
        sprintAction = playerInput.actions["Sprint"];
        crouchAction = playerInput.actions["Crouch"];
        pointerAction = playerInput.actions["PointerPosition"];
        hurdleAction = playerInput.actions["Hurdle"];
        aimAction = playerInput.actions["Aiming"];
        whistleAction = playerInput.actions["Whistle"];

        if (crouchAction != null) crouchAction.performed += OnCrouch;
        if (sprintAction != null)
        {
            sprintAction.performed += OnSprintStart;
            sprintAction.canceled += OnSprintStop;
        }
        if (hurdleAction != null) hurdleAction.performed += OnVault;
        if (whistleAction != null) whistleAction.performed += OnWhistle;

        if (virtualCamera != null)
        {
            normalFOV = virtualCamera.Lens.FieldOfView;
            aimFOV = normalFOV * 0.8f;
        }

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
        if (whistleAction != null) whistleAction.performed -= OnWhistle;
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

    private void OnWhistle(InputAction.CallbackContext context)
    {
        if (isVaulting || isMovementLocked) return;
        if (playerAudio != null) playerAudio.PlayWhistle();
    }

    void Update()
    {
        if (climbCooldown > 0f) climbCooldown -= Time.deltaTime;
        if (isVaulting || isMovementLocked) return;
        if (isClimbing) { HandleClimbing(); return; }

        if (aimAction != null) isAiming = aimAction.IsPressed();

        if (!isArmed && virtualCamera != null)
        {
            float targetFOV = isAiming ? aimFOV : normalFOV;
            virtualCamera.Lens.FieldOfView = Mathf.Lerp(virtualCamera.Lens.FieldOfView, targetFOV, cameraZoomSpeed * Time.deltaTime);
        }

        HandleCrouchPhysicality();
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

    void HandleCrouchPhysicality()
    {
        if (!enablePhysicalCrouch) return;

        Vector3 targetScale = transform.localScale;

        float targetYScale = isCrouching ? (crouchHeight / originalHeight) : 1f;
        targetScale.y = targetYScale;

        transform.localScale = Vector3.Lerp(transform.localScale, targetScale, crouchTransitionSpeed * Time.deltaTime);
    }


    void HandleMovement()
    {
        Vector2 moveInput = Vector2.zero;
        if (moveAction != null) moveInput = moveAction.ReadValue<Vector2>();

        Vector3 direction = Vector3.zero;

        if (useCameraRelativeMovement)
        {
            Vector3 cameraForward = mainCamera.transform.forward;
            Vector3 cameraRight = mainCamera.transform.right;
            cameraForward.y = 0f;
            cameraRight.y = 0f;
            direction = (cameraForward.normalized * moveInput.y + cameraRight.normalized * moveInput.x).normalized;
        }
        else
        {
            direction = new Vector3(moveInput.x, 0f, moveInput.y).normalized;
        }

        float currentSpeed = walkSpeed;

        if (isCrouching) currentSpeed = crouchSpeed;
        else if (isSprinting) currentSpeed = sprintSpeed;

        currentSpeed *= movementSpeedMultiplier;

        if (direction.magnitude >= 0.1f)
        {
            controller.Move(direction * currentSpeed * Time.deltaTime);

            if (playerAudio != null)
            {
                playerAudio.HandleFootstep(controller.isGrounded, isSprinting, isCrouching);
            }
        }

        controller.Move(new Vector3(0, -9.81f * Time.deltaTime, 0));

        if (animator != null)
        {
            animator.SetFloat("Speed", direction.magnitude * currentSpeed);
            animator.SetBool("IsCrouching", isCrouching);
        }
    }

    void HandleRotation()
    {
        if (isAiming || isArmed)
        {
            Vector2 mousePos = Vector2.zero;
            if (pointerAction != null) mousePos = pointerAction.ReadValue<Vector2>();

            Ray ray = mainCamera.ScreenPointToRay(mousePos);
            Plane groundPlane = new Plane(Vector3.up, Vector3.zero);

            if (groundPlane.Raycast(ray, out float rayDistance))
            {
                Vector3 point = ray.GetPoint(rayDistance);
                Vector3 lookTarget = new Vector3(point.x, transform.position.y, point.z);

                Quaternion targetRotation = Quaternion.LookRotation(lookTarget - transform.position);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
            }
        }
        else
        {
            Vector2 moveInput = Vector2.zero;
            if (moveAction != null) moveInput = moveAction.ReadValue<Vector2>();

            Vector3 direction = Vector3.zero;

            if (useCameraRelativeMovement)
            {
                Vector3 cameraForward = mainCamera.transform.forward;
                Vector3 cameraRight = mainCamera.transform.right;
                cameraForward.y = 0f;
                cameraRight.y = 0f;
                direction = (cameraForward.normalized * moveInput.y + cameraRight.normalized * moveInput.x).normalized;
            }
            else
            {
                direction = new Vector3(moveInput.x, 0f, moveInput.y).normalized;
            }

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
        Debug.Log($"hitLow: {hitLow}, hitHigh: {hitHigh}");

        if (hitLow && !hitHigh)
        {
            Vector3 vaultDirection = -hit.normal;
            vaultDirection.y = 0f;
            vaultDirection.Normalize();

            Vector3 landingSpot = transform.position + (vaultDirection * vaultDistance);
            Vector3 landingRayOrigin = landingSpot + (Vector3.up * highRayHeight);

            bool hitThickWall = Physics.Raycast(landingRayOrigin, Vector3.down, highRayHeight, hurdleLayer);
            Debug.Log($"hitThickWall: {hitThickWall}");
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
        // ไม่ปิด controller.enabled แล้ว เพื่อให้ยังชนกำแพง/สิ่งกีดขวางได้ระหว่างกระโดดข้าม

        Vector3 startPos = transform.position;
        float timePassed = 0f;
        float previousHeightOffset = 0f;

        while (timePassed < 1f)
        {
            timePassed += Time.deltaTime / vaultDuration;

            Vector3 lerpedPos = Vector3.Lerp(startPos, targetPosition, timePassed);
            float heightOffset = Mathf.Sin(Mathf.Clamp01(timePassed) * Mathf.PI) * vaultHeight;

            // คำนวณตำแหน่งเป้าหมายของเฟรมนี้ (รวมส่วนโค้งกระโดดด้วย)
            Vector3 targetFramePos = lerpedPos;
            targetFramePos.y += heightOffset;

            Vector3 delta = targetFramePos - transform.position;
            controller.Move(delta);   // ใช้ Move แทน set position ตรงๆ ให้เช็คชนกำแพงระหว่างทาง

            previousHeightOffset = heightOffset;
            yield return null;
        }

        // Snap ตำแหน่งสุดท้ายให้แม่นยำ (จุดยืนจริง ไม่มี height offset)
        Vector3 finalDelta = targetPosition - transform.position;
        controller.Move(finalDelta);

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
        // ไม่ปิด controller.enabled แล้ว เพื่อให้ยังชนกำแพงได้ระหว่างเข้าสู่บันได

        Vector3 startPos = transform.position;
        float timePassed = 0f;
        float duration = 0.35f;

        while (timePassed < 1f)
        {
            timePassed += Time.deltaTime / duration;
            Vector3 nextPos = Vector3.Lerp(startPos, targetPos, timePassed);
            Vector3 delta = nextPos - transform.position;
            controller.Move(delta);   // ใช้ Move แทน set position ตรงๆ
            yield return null;
        }

        Vector3 finalDelta = targetPos - transform.position;
        controller.Move(finalDelta);

        isMovementLocked = false;   // <-- สำคัญมาก! ปลดล็อคตอนจบ ไม่งั้นค้างตลอดไป
    }

    void HandleClimbing()
    {
        Vector2 moveInput = Vector2.zero;
        if (moveAction != null) moveInput = moveAction.ReadValue<Vector2>();

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

        Vector3 startPos = transform.position;
        Vector3 targetPos = transform.position + (Vector3.up * 1.2f) + (transform.forward * topExitOffset);
        float timePassed = 0f;
        float duration = 0.5f;

        while (timePassed < 1f)
        {
            timePassed += Time.deltaTime / duration;
            Vector3 nextPos = Vector3.Lerp(startPos, targetPos, timePassed);
            Vector3 delta = nextPos - transform.position;
            controller.Move(delta); 
            yield return null;
        }

        Vector3 finalDelta = targetPos - transform.position;
        controller.Move(finalDelta);

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