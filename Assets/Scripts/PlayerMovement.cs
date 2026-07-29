using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

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

    [Header("Vault Settings (ปีนข้ามสิ่งกีดขวาง)")]
    public float vaultDistance = 2f;
    public float vaultHeight = 1.2f;
    public float vaultDuration = 0.4f;
    public LayerMask hurdleLayer;
    public float raycastDistance = 1f;
    public float lowRayHeight = 0.2f;
    public float highRayHeight = 1.0f;

    private CharacterController controller;
    private Camera mainCamera;
    private PlayerInput playerInput;

    private InputAction moveAction;
    private InputAction sprintAction;
    private InputAction crouchAction;
    private InputAction pointerAction;
    private InputAction hurdleAction;

    private bool isVaulting = false;
    private bool isMovementLocked = false;
    private float movementSpeedMultiplier = 1f;

    // สวิตช์ความจำสำหรับสถานะต่างๆ
    private bool isCrouching = false;
    private bool isSprinting = false;

    public bool IsMovementLocked => isMovementLocked;

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

        // 1. ผูก Event เข้ากับ Action ต่างๆ (Subscribe)
        crouchAction.performed += OnCrouch;

        sprintAction.performed += OnSprintStart;
        sprintAction.canceled += OnSprintStop;

        hurdleAction.performed += OnVault;
    }

    // 2. ยกเลิก Event เมื่อ Object ถูกทำลาย ป้องกันอาการ Memory Leak 
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

    // ==========================================
    // กลุ่มฟังก์ชันรับ Event จาก Input System
    // ==========================================

    private void OnCrouch(InputAction.CallbackContext context)
    {
        isCrouching = !isCrouching; // ทำงานแบบ Tap สลับสวิตช์ย่อ/ยืน
    }

    private void OnSprintStart(InputAction.CallbackContext context)
    {
        isSprinting = true; // ผู้เล่นเริ่มกดปุ่มวิ่งค้างไว้ (Hold)
    }

    private void OnSprintStop(InputAction.CallbackContext context)
    {
        isSprinting = false; // ผู้เล่นปล่อยปุ่มวิ่ง (Release)
    }

    private void OnVault(InputAction.CallbackContext context)
    {
        // ถ้ากำลังปีนอยู่ หรือโดนล็อกห้ามขยับ จะไม่ให้กระโดดซ้ำ
        if (isVaulting || isMovementLocked) return;
        AttemptVault();
    }

    // ==========================================
    // ลอจิกหลักของเกม
    // ==========================================

    void Update()
    {
        if (isVaulting || isMovementLocked) return;

        // ในนี้จะเหลือแค่อะไรที่ต้องขยับตามเฟรมตลอดเวลา
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

        // 3. ใช้ค่าจากตัวแปรสวิตช์โดยตรง โดยไม่ต้องสนใจเรื่องปุ่มกดแล้ว
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

        // แรงโน้มถ่วง
        controller.Move(new Vector3(0, -9.81f * Time.deltaTime, 0));
    }

    void HandleRotation()
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
            else
            {
                Debug.Log("ข้ามไม่ได้: กำแพงหนาเกินไป หรือไม่มีที่ลง!");
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