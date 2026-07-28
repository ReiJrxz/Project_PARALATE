using UnityEngine;
using System.Collections;
using Unity.Cinemachine;
using UnityEngine.InputSystem; // ต้องใช้สำหรับ New Input System

[RequireComponent(typeof(LineRenderer))]
public class GunAction : MonoBehaviour
{
    [Header("Gun Settings")]
    public Transform firePoint;
    public float range = 100f; // ระยะยิงสูงสุดของปืน
    public float damage = 10f; // ความแรงของปืน
    public float fireRate = 0.1f; // ความเร็วในการยิง (วินาทีต่อการยิงหนึ่งครั้ง)

    [Header("Crosshair & UI")]
    public RectTransform crosshairUI; // ลาก UI เป้าเล็งมาใส่ช่องนี้

    [Header("Debug Settings")]
    public bool isHeld = false; // สถานะการถือปืน
    public bool showDebugLine = true; // เปิด/ปิด debug line ของกระสุน

    [Header("Input Actions")]
    public InputActionReference fireAction; // ลาก Action ของการยิงปืนมาใส่ช่องนี้
    public InputActionReference pointerAction; // ลาก Action ของ Pointer Position มาใส่ช่องนี้
    public InputActionReference aimAction; // ลาก Action ของการเล็ง (Scope) มาใส่ช่องนี้

    [Header("Scope Settings")]
    public bool enableScope = true; //เปิด/ปิด scope
    public float normalFieldOfView = 50f; //มุมกล้องตอนถือปืน
    public float scopedFieldOfView = 25f; //มุมกล้องตอน scope
    public float scopeZoomSpeed = 12f; //ความเร็วในการซูม
    public bool hideCrosshairWhileScoped = false; //เปิด/ปิด crosshair

    [Header("Scope Camera Offset")]
    public bool enableScopeCameraOffset = true; //เปิด/ปิด การขยับกล้อง
    // เลื่อนซ้ายขวา (แกน X) + เลื่อนกล้องลง (ค่า Y ติดลบ) + เลื่อนหน้า (แกน Z)
    public Vector3 scopedCameraOffset = new Vector3(0f, -2f, 0f);

    [Header("Scope Camera Angle")]
    public bool enableScopeCameraAngle = true; //เปิด/ปิด การขยับองศากล้อง
    public float scopedTiltOffset = 10f; //ปรับองศาบนล่างของกล้องเมื่อซูม (ค่าบวก = เงยขึ้น, ค่าลบ = ก้มลง)

    private LineRenderer laserLine; // ตัวแปรสำหรับ LineRenderer ของกระสุน
    private float nextFireTime; // ตัวแปรสำหรับควบคุมความเร็วในการยิง

    private Camera mainCam;
    private CinemachineCamera[] virtualCameras;
    private CinemachinePositionComposer[] positionComposers;
    private CinemachineFollow[] followComponents;
    private Vector3[] baseOffsets;
    private CinemachinePanTilt[] panTiltComponents;
    private float[] baseTilts;

    private bool isScoped;
    private InputAction resolvedAimAction;

    private void Awake()
    {
        laserLine = GetComponent<LineRenderer>();
        laserLine.enabled = false;
        mainCam = Camera.main;
        virtualCameras = FindObjectsByType<CinemachineCamera>(FindObjectsInactive.Include);

        if (mainCam != null)
            normalFieldOfView = mainCam.fieldOfView;

        CacheCameraOffsets();
    }
    // เมื่อปืนถูกปิดใช้งาน (เช่น เก็บปืน) ให้รีเซ็ตสถานะทั้งหมด
    private void OnDisable()
    {
        SetScoped(false, true);
        SetCrosshairVisible(false);
        Cursor.visible = true;
    }
    // ฟังก์ชันนี้ใช้เพื่อเปลี่ยนสถานะการถือปืนจากภายนอกสคริปต์
    public void SetHeld(bool held)
    {
        isHeld = held;
        SetCrosshairVisible(held);

        if (!held)
        {
            SetScoped(false, true);
            Cursor.visible = true;
        }
    }
    void Update()
    {
        HandleScope();
        HandleCrosshair();

        // ใช้ IsPressed() ของ New Input System แทน Input.GetButton เดิม
        if (isHeld && fireAction.action.IsPressed() && Time.time > nextFireTime)
        {
            nextFireTime = Time.time + fireRate;
            Shoot();
        }
    }
    // ฟังก์ชันนี้ใช้จัดการ UI เป้าเล็งและเคอร์เซอร์เมาส์
    void HandleCrosshair()
    {
        if (crosshairUI != null)
        {
            // เปิด/ปิด UI เป้าเล็งตามสถานะการถือปืน
            SetCrosshairVisible(isHeld && (!hideCrosshairWhileScoped || !isScoped));

            if (isHeld)
            {
                // เลื่อน UI เป้าเล็งให้ตรงกับตำแหน่งเมาส์บนจอ
                Vector2 mouseScreenPos = pointerAction.action.ReadValue<Vector2>();
                crosshairUI.position = mouseScreenPos;

                // ซ่อนเคอร์เซอร์เมาส์ของ Windows (เอาออกได้ถ้าไม่ชอบ)
                Cursor.visible = false;
            }
            else
            {
                Cursor.visible = true; // โชว์เมาส์ปกติเมื่อเก็บปืน
            }
        }
    }
    // ฟังก์ชันนี้ใช้จัดการการเล็ง (Scope) ของปืน
    void HandleScope()
    {
        ResolveAimAction();

        bool shouldScope = enableScope && isHeld && resolvedAimAction != null && resolvedAimAction.IsPressed();
        SetScoped(shouldScope, false);
    }
    // ฟังก์ชันนี้ใช้เพื่อหาค่า InputAction ของการเล็ง (Scope) จาก InputActionReference หรือจาก PlayerInput
    void ResolveAimAction()
    {
        if (aimAction != null)
        {
            resolvedAimAction = aimAction.action;
            return;
        }

        if (resolvedAimAction != null)
            return;

        PlayerInput playerInput = GetComponentInParent<PlayerInput>();
        if (playerInput == null)
            return;

        resolvedAimAction = playerInput.actions["Aiming"];
    }
    // ฟังก์ชันนี้ใช้ปรับมุมกล้องและตำแหน่งกล้องเมื่อเล็ง (Scope) หรือไม่เล็ง
    void SetScoped(bool scoped, bool instant)
    {
        isScoped = scoped;

        float targetFieldOfView = scoped ? scopedFieldOfView : normalFieldOfView;
        float lerpAmount = instant ? 1f : Time.deltaTime * scopeZoomSpeed;

        if (virtualCameras != null)
        {
            for (int i = 0; i < virtualCameras.Length; i++)
            {
                if (virtualCameras[i] == null)
                    continue;

                if (enableScopeCameraOffset)
                {
                    Vector3 desiredOffset = baseOffsets[i] + (scoped ? scopedCameraOffset : Vector3.zero);
                    float desiredTilt = baseTilts[i] + (scoped ? scopedTiltOffset : 0f);

                    if (positionComposers[i] != null)
                    {
                        positionComposers[i].TargetOffset =
                            Vector3.Lerp(positionComposers[i].TargetOffset, desiredOffset, lerpAmount);
                    }
                    else if (followComponents[i] != null)
                    {
                        followComponents[i].FollowOffset =
                            Vector3.Lerp(followComponents[i].FollowOffset, desiredOffset, lerpAmount);
                    }
                    else
                    {
                        virtualCameras[i].transform.localPosition =
                            Vector3.Lerp(virtualCameras[i].transform.localPosition, desiredOffset, lerpAmount);
                    }

                    if (panTiltComponents[i] != null)
                    {
                        var panTilt = panTiltComponents[i];
                        var tiltAxis = panTilt.TiltAxis;
                        tiltAxis.Value = Mathf.Lerp(tiltAxis.Value, desiredTilt, lerpAmount);
                        panTilt.TiltAxis = tiltAxis;
                    }
                    else
                    {
                        Vector3 euler = virtualCameras[i].transform.localEulerAngles;
                        euler.x = Mathf.LerpAngle(euler.x, desiredTilt, lerpAmount);
                        virtualCameras[i].transform.localEulerAngles = euler;
                    }
                }
            }
        }

        if (mainCam != null)
            mainCam.fieldOfView = Mathf.Lerp(mainCam.fieldOfView, targetFieldOfView, lerpAmount);
    }
    // ฟังก์ชันนี้ใช้เก็บค่า offset และ tilt ของกล้องแต่ละตัวไว้เป็นฐาน เพื่อให้สามารถปรับเปลี่ยนได้โดยไม่ทับค่าที่ตั้งไว้ใน Inspector
    void CacheCameraOffsets()
    {
        if (virtualCameras == null) return;

        int count = virtualCameras.Length;
        positionComposers = new CinemachinePositionComposer[count];
        followComponents = new CinemachineFollow[count];
        baseOffsets = new Vector3[count];

        panTiltComponents = new CinemachinePanTilt[count];
        baseTilts = new float[count];

        for (int i = 0; i < count; i++)
        {
            if (virtualCameras[i] == null) continue;

            var composer = virtualCameras[i].GetComponent<CinemachinePositionComposer>();
            var follow = virtualCameras[i].GetComponent<CinemachineFollow>();
            var panTilt = virtualCameras[i].GetComponent<CinemachinePanTilt>();

            positionComposers[i] = composer;
            followComponents[i] = follow;
            panTiltComponents[i] = panTilt;

            // เก็บค่า offset เดิมไว้เป็นฐาน จะได้ไม่ทับค่าที่ตั้งไว้ใน Inspector
            if (composer != null)
                baseOffsets[i] = composer.TargetOffset;
            else if (follow != null)
                baseOffsets[i] = follow.FollowOffset;
            else
                baseOffsets[i] = virtualCameras[i].transform.localPosition;

            if (panTilt != null)
                baseTilts[i] = panTilt.TiltAxis.Value;
            else
                baseTilts[i] = virtualCameras[i].transform.localEulerAngles.x;
        }
    }
    // ฟังก์ชันนี้ใช้เปิด/ปิด UI เป้าเล็ง
    void SetCrosshairVisible(bool visible)
    {
        if (crosshairUI != null && crosshairUI.gameObject.activeSelf != visible)
            crosshairUI.gameObject.SetActive(visible);
    }
    // ฟังก์ชันนี้ใช้ยิงกระสุนและตรวจสอบการชนของ Raycast
    void Shoot()
    {
        if (showDebugLine) StartCoroutine(ShotEffect());

        laserLine.SetPosition(0, firePoint.position);

        // 1. หาพิกัดเมาส์ 3D บนพื้นโลก
        Vector2 mouseScreenPos = pointerAction.action.ReadValue<Vector2>();
        Ray ray = mainCam.ScreenPointToRay(mouseScreenPos);
        Plane groundPlane = new Plane(Vector3.up, Vector3.zero);

        Vector3 shootDirection = firePoint.forward; // ค่าเริ่มต้น

        if (groundPlane.Raycast(ray, out float rayDistance))
        {
            Vector3 mouseWorldPoint = ray.GetPoint(rayDistance);

            // 2. สำคัญมาก! ปรับความสูงของเป้าหมายให้เท่ากับปากกระบอกปืน 
            // ไม่งั้นกระสุนจะยิงทิ่มลงพื้น (เพราะเมาส์อยู่บนพื้น Y=0)
            mouseWorldPoint.y = firePoint.position.y;

            // 3. คำนวณทิศทางจากปากกระบอกปืน พุ่งเฉียงไปหาเมาส์เป๊ะๆ
            shootDirection = (mouseWorldPoint - firePoint.position).normalized;
        }

        RaycastHit hit;

        // เปลี่ยนมายิงไปทาง shootDirection ที่คำนวณใหม่แทน firePoint.forward
        if (Physics.Raycast(firePoint.position, shootDirection, out hit, range))
        {
            Debug.Log("Hit: " + hit.collider.name);
            laserLine.SetPosition(1, hit.point);

            EnemyHealth enemy = hit.collider.GetComponent<EnemyHealth>();
            if (enemy != null) enemy.TakeDamage(damage);
        }
        else
        {
            laserLine.SetPosition(1, firePoint.position + (shootDirection * range));
        }
    }
    // ฟังก์ชันนี้ใช้แสดงเอฟเฟกต์การยิงกระสุน (เปิด LineRenderer ชั่วคราว)
    private IEnumerator ShotEffect()
    {
        laserLine.enabled = true;
        yield return new WaitForSeconds(0.05f);
        laserLine.enabled = false;
    }
}
