using System.Collections; // สำคัญ: ต้องใส่เพื่อใช้ Coroutine
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
    public float vaultDistance = 2f; // ระยะทางที่จะกระโดดข้ามไปตกอีกฝั่ง
    public float vaultHeight = 1.2f; // ความสูงของส่วนโค้งตอนกระโดด
    public float vaultDuration = 0.4f; // ระยะเวลาที่ลอยอยู่ในอากาศ (ค่าน้อย = กระโดดเร็ว)
    public LayerMask hurdleLayer; // กำหนดว่า Layer ไหนกระโดดข้ามได้บ้าง
    public float raycastDistance = 1f; // ระยะตรวจจับกำแพงด้านหน้า
    public float lowRayHeight = 0.2f;
    public float highRayHeight = 1.0f;

    private CharacterController controller;
    private Camera mainCamera;
    private PlayerInput playerInput;

    private InputAction moveAction;
    private InputAction sprintAction;
    private InputAction crouchAction;
    private InputAction pointerAction;
    private InputAction hurdleAction; // เพิ่ม Action สำหรับกระโดด

    private bool isVaulting = false; // ตัวแปรเช็คว่ากำลังกระโดดข้ามอยู่หรือไม่

    private bool isMovementLocked = false;
    private float movementSpeedMultiplier = 1f;
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
    }

    void Update()
    {
        // ถ้ากำลังปีนข้ามอยู่ จะไม่รับคำสั่งเดินและหันหน้า
        if (isVaulting || isMovementLocked) return;

        HandleMovement();
        HandleRotation();
        HandleVault();
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

        if (crouchAction.IsPressed())
        {
            currentSpeed = crouchSpeed;
        }
        else if (sprintAction.IsPressed())
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

    void HandleVault()
    {
        if (hurdleAction.WasPressedThisFrame())
        {
            Vector3 horizontalForward = new Vector3(transform.forward.x, 0f, transform.forward.z).normalized;

            Vector3 lowRayOrigin = transform.position + (Vector3.up * lowRayHeight);
            Vector3 highRayOrigin = transform.position + (Vector3.up * highRayHeight);

            bool hitLow = Physics.Raycast(lowRayOrigin, horizontalForward, out RaycastHit hit, raycastDistance, hurdleLayer);
            bool hitHigh = Physics.Raycast(highRayOrigin, horizontalForward, raycastDistance, hurdleLayer);

            // ถ้ายิงโดนกำแพงเตี้ย และไม่โดนกำแพงสูง (ความสูงข้ามได้)
            if (hitLow && !hitHigh)
            {
                Vector3 vaultDirection = -hit.normal;
                vaultDirection.y = 0f;
                vaultDirection.Normalize();

                // 1. คำนวณจุดที่จะไปตกก่อน
                Vector3 landingSpot = transform.position + (vaultDirection * vaultDistance);

                // 2. สร้างจุดยิง Raycast ที่ 3 (ยิงจากด้านบนของจุดตก ลงมาหาพื้น)
                // เราใช้ความสูงเท่ากับ highRayHeight เพื่อให้ยิงลงมาจากระดับหัว
                Vector3 landingRayOrigin = landingSpot + (Vector3.up * highRayHeight);

                // 3. ยิงลงมาข้างล่าง (Vector3.down) เช็คว่าข้างล่างเป็นกำแพง Hurdle หรือไม่
                bool hitThickWall = Physics.Raycast(landingRayOrigin, Vector3.down, highRayHeight, hurdleLayer);

                // 4. ถ้า "ไม่" ชนกำแพง (แสดงว่ากำแพงบาง ข้ามพ้น) ถึงจะยอมให้กระโดด
                if (!hitThickWall)
                {
                    transform.rotation = Quaternion.LookRotation(vaultDirection);
                    StartCoroutine(VaultRoutine(landingSpot));
                }
                else
                {
                    // คุณสามารถใส่เสียง "ตึ๊ด" หรือ UI แจ้งเตือนตรงนี้ได้ว่าข้ามไม่ได้
                    Debug.Log("ข้ามไม่ได้: กำแพงหนาเกินไป หรือไม่มีที่ลง!");
                }
            }
        }
    }

    // ฟังก์ชัน Coroutine ควบคุมการเคลื่อนที่แบบเส้นโค้ง (Parabola)
    IEnumerator VaultRoutine(Vector3 targetPosition)
    {
        isVaulting = true;
        controller.enabled = false; // ปิด CharacterController ชั่วคราวเพื่อไม่ให้ชนกำแพงตอนข้าม

        Vector3 startPos = transform.position;
        float timePassed = 0f;

        while (timePassed < 1f)
        {
            // เพิ่มเวลาตามความเร็วที่ตั้งไว้
            timePassed += Time.deltaTime / vaultDuration;

            // คำนวณตำแหน่ง X และ Z ให้เลื่อนไปข้างหน้าแบบเชิงเส้นตรง
            Vector3 currentPos = Vector3.Lerp(startPos, targetPosition, timePassed);

            // ใช้สูตร Math.Sin เพื่อสร้างความสูงแบบเส้นโค้งให้กับแกน Y
            currentPos.y += Mathf.Sin(timePassed * Mathf.PI) * vaultHeight;

            transform.position = currentPos;

            yield return null; // รอเฟรมถัดไป
        }

        // จับตัวละครวางให้ตรงตำแหน่งเป้าหมาย 100% เมื่อจบการทำงาน
        transform.position = targetPosition;

        controller.enabled = true; // เปิด CharacterController กลับมาใช้งาน
        isVaulting = false;
    }

    // (Optional) วาดเส้นสีแดงให้เห็นระยะ Raycast ในหน้าต่าง Scene เพื่อให้ตั้งค่าง่ายขึ้น
    private void OnDrawGizmos()
    {
        Vector3 horizontalForward = new Vector3(transform.forward.x, 0f, transform.forward.z).normalized;

        // 1. เส้นล่าง (สีเขียว)
        Gizmos.color = Color.green;
        Gizmos.DrawRay(transform.position + (Vector3.up * lowRayHeight), horizontalForward * raycastDistance);

        // 2. เส้นบน (สีแดง)
        Gizmos.color = Color.red;
        Gizmos.DrawRay(transform.position + (Vector3.up * highRayHeight), horizontalForward * raycastDistance);

        // 3. วาดเส้นจุดตก (สีเหลือง) เพื่อให้คุณเห็นภาพชัดๆ ใน Scene
        // จะวาดก็ต่อเมื่อเราอยู่ใน Play Mode และมีเป้าหมายเท่านั้น แต่เราสามารถจำลองคร่าวๆ ได้:
        if (Application.isPlaying)
        {
            Gizmos.color = Color.yellow;
            // จำลองการพุ่งไปข้างหน้าตาม vaultDistance
            Vector3 testLandingSpot = transform.position + (horizontalForward * vaultDistance);
            Vector3 testLandingOrigin = testLandingSpot + (Vector3.up * highRayHeight);

            // วาดเส้นยิงทิ่มลงพื้น
            Gizmos.DrawRay(testLandingOrigin, Vector3.down * highRayHeight);
            // วาดลูกกลมๆ ตรงจุดตก
            Gizmos.DrawSphere(testLandingSpot, 0.1f);
        }
    }
}
