using UnityEngine;
using System.Collections;
using Unity.Cinemachine;
using UnityEngine.InputSystem;
using TMPro;
using System;

[RequireComponent(typeof(LineRenderer))]
public class GunAction : MonoBehaviour
{
    [Header("Gun Settings")]
    public Transform firePoint;
    public float range = 100f;
    public float damage = 10f;
    public float fireRate = 0.1f;

    [Header("Ammo & Reload")]
    public int magazineSize = 30;
    public float reloadDuration = 5f;
    [Range(0f, 1f)]
    public float reloadMovementSpeedMultiplier = 0.5f;
    public bool autoReloadWhenEmpty = true;

    [Header("Crosshair & UI")]
    public RectTransform crosshairUI;
    public event Action<int, int> OnAmmoChanged;
    public event Action<bool> OnReloadStatusChanged;
    public event Action<bool> OnHeldChanged;

    [Header("Debug Settings")]
    public bool isHeld = false; // สถานะการถือปืน
    public bool showDebugLine = true; // เปิด/ปิด debug line ของกระสุน

    [Header("Input Actions")]
    public InputActionReference fireAction;
    public InputActionReference pointerAction;
    public InputActionReference aimAction;
    public InputActionReference reloadAction;

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

    [Header("Dynamic Look Ahead (Scope)")]
    public bool enableDynamicLook = true; // เปิด/ปิดให้กล้องขยับตามเมาส์เวลาซูม
    public float maxLookOffsetX = 5f; // ระยะเลื่อนกล้องสูงสุดแกน X (ซ้าย-ขวา)
    public float maxLookOffsetZ = 5f; // ระยะเลื่อนกล้องสูงสุดแกน Z (หน้า-หลัง สำหรับ Top-Down)

    private LineRenderer laserLine;
    private float nextFireTime;

    [SerializeField] private int currentAmmo;
    private bool isReloading;
    private Coroutine reloadCoroutine;
    private TopDownPlayerController movementController;
    private InputAction resolvedReloadAction;

    public bool IsReloading => isReloading;
    public int CurrentAmmo => currentAmmo;
    public int MagazineSize => magazineSize;

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
        movementController = GetComponentInParent<TopDownPlayerController>();

        if (mainCam != null)
            normalFieldOfView = mainCam.fieldOfView;

        currentAmmo = magazineSize;
        CacheCameraOffsets();
    }
    private void OnDisable()
    {
        CancelReload();
        SetScoped(false, true);
        SetCrosshairVisible(false);
        //Cursor.visible = true;
    }
    public void SetHeld(bool held)
    {
        isHeld = held;
        SetCrosshairVisible(held);
        OnHeldChanged?.Invoke(held);

        if (held)
            UpdateAmmoUI();

        if (!held)
        {
            CancelReload();
            SetScoped(false, true);
            //Cursor.visible = true;
        }
    }
    void Update()
    {
        HandleScope();
        HandleCrosshair();
        HandleReload();

        if (isHeld && !isReloading && currentAmmo > 0
            && fireAction.action.IsPressed() && Time.time > nextFireTime)
        {
            nextFireTime = Time.time + fireRate;
            Shoot();
        }
    }

    void HandleReload()
    {
        if (!isHeld || isReloading)
            return;

        if (currentAmmo >= magazineSize)
            return;

        ResolveReloadAction();

        if (resolvedReloadAction != null && resolvedReloadAction.WasPressedThisFrame())
            StartReload();
    }

    void ResolveReloadAction()
    {
        if (reloadAction != null)
        {
            resolvedReloadAction = reloadAction.action;
            return;
        }

        if (resolvedReloadAction != null)
            return;

        PlayerInput playerInput = GetComponentInParent<PlayerInput>();
        if (playerInput == null)
            return;

        resolvedReloadAction = playerInput.actions["Reload"];
    }

    public void StartReload()
    {
        if (!isHeld || isReloading || currentAmmo >= magazineSize)
            return;

        if (reloadCoroutine != null)
            StopCoroutine(reloadCoroutine);

        reloadCoroutine = StartCoroutine(ReloadRoutine());
    }

    void CancelReload()
    {
        if (reloadCoroutine != null)
        {
            StopCoroutine(reloadCoroutine);
            reloadCoroutine = null;
        }

        if (!isReloading)
            return;

        isReloading = false;
        SetReloadMovementSpeed(false);
        OnReloadStatusChanged?.Invoke(false);
    }

    IEnumerator ReloadRoutine()
    {
        isReloading = true;
        OnReloadStatusChanged?.Invoke(true);
        SetReloadMovementSpeed(true);

        yield return new WaitForSeconds(reloadDuration);

        currentAmmo = magazineSize;
        isReloading = false;
        SetReloadMovementSpeed(false);
        reloadCoroutine = null;

        OnReloadStatusChanged?.Invoke(false);
        UpdateAmmoUI();
    }

    void SetReloadMovementSpeed(bool reloading)
    {
        if (movementController == null)
            movementController = GetComponentInParent<TopDownPlayerController>();

        if (movementController == null)
            return;

        movementController.SetMovementSpeedMultiplier(
            reloading ? reloadMovementSpeedMultiplier : 1f);
    }

    void UpdateAmmoUI()
    {
        OnAmmoChanged?.Invoke(currentAmmo, magazineSize);
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
                //Cursor.visible = true; // โชว์เมาส์ปกติเมื่อเก็บปืน
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
                    // --- ส่วนที่เพิ่มเข้ามาใหม่: คำนวณการขยับกล้องตามเมาส์ ---
                    Vector3 dynamicLookOffset = Vector3.zero;

                    if (scoped && enableDynamicLook)
                    {
                        // 1. ดึงตำแหน่งเมาส์บนจอ
                        Vector2 mouseScreenPos = pointerAction.action.ReadValue<Vector2>();

                        // 2. หาจุดกึ่งกลางหน้าจอ
                        Vector2 screenCenter = new Vector2(Screen.width / 2f, Screen.height / 2f);

                        // 3. หาว่าเมาส์อยู่ห่างจากตรงกลางกี่เปอร์เซ็นต์ (ได้ค่า -1 ถึง 1)
                        float normalizedX = (mouseScreenPos.x - screenCenter.x) / screenCenter.x;
                        float normalizedY = (mouseScreenPos.y - screenCenter.y) / screenCenter.y;

                        // 4. จำกัดขอบเขตกันเมาส์หลุดจอ
                        normalizedX = Mathf.Clamp(normalizedX, -1f, 1f);
                        normalizedY = Mathf.Clamp(normalizedY, -1f, 1f);

                        // 5. แปลงเป็นระยะทางในโลก 3D (แกน X ซ้ายขวา, แกน Z หน้าหลัง)
                        dynamicLookOffset = new Vector3(normalizedX * maxLookOffsetX, 0f, normalizedY * maxLookOffsetZ);
                    }

                    // เอาค่า Base + Offset ตอนซูม + Offset จากเมาส์
                    Vector3 desiredOffset = baseOffsets[i] + (scoped ? scopedCameraOffset : Vector3.zero) + dynamicLookOffset;
                    // --------------------------------------------------

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
    void Shoot()
    {
        if (currentAmmo <= 0 || isReloading)
            return;

        currentAmmo--;
        UpdateAmmoUI();

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

        if (currentAmmo <= 0 && autoReloadWhenEmpty)
            StartReload();
    }
    // ฟังก์ชันนี้ใช้แสดงเอฟเฟกต์การยิงกระสุน (เปิด LineRenderer ชั่วคราว)
    private IEnumerator ShotEffect()
    {
        laserLine.enabled = true;
        yield return new WaitForSeconds(0.05f);
        laserLine.enabled = false;
    }
}
