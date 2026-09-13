using UnityEngine;
using System.Collections;
using Unity.Cinemachine;
using UnityEngine.InputSystem;
using TMPro;
using System;

[RequireComponent(typeof(LineRenderer))]
[RequireComponent(typeof(AudioSource))]
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
    //public float scopeZoomSpeed = 12f; //ความเร็วในการซูม
    public bool hideCrosshairWhileScoped = false; //เปิด/ปิด crosshair

    [Header("First Person Scope Camera")]
    //public bool enableScopeCameraOffset = true; //เปิด/ปิด การขยับกล้อง
    //// เลื่อนซ้ายขวา (แกน X) + เลื่อนกล้องลง (ค่า Y ติดลบ) + เลื่อนหน้า (แกน Z)
    //public Vector3 scopedCameraOffset = new Vector3(0f, -2f, 0f);

    public bool useFirstPersonScope = true; // ตอนซูมให้กล้องขยับไปมุมมองบุคคลที่หนึ่ง
    public Vector3 firstPersonCameraOffset = new Vector3(0f, 1.55f, 0.15f);
    public int firstPersonCameraPriority = 2000;
    public bool snapScopeCameraTransition = true; // ตัดเข้ากล้อง First Person ทันที
    public bool centerCrosshairInFirstPersonScope = true;
    public bool lockCursorInFirstPersonScope = true;
    public float firstPersonMouseSensitivity = 0.12f;
    public float minFirstPersonPitch = -70f;
    public float maxFirstPersonPitch = 70f;

    //[Header("Legacy Top-Down Scope Camera Angle")]
    //public bool enableScopeCameraAngle = true; //เปิด/ปิด การขยับองศากล้อง
    //public float scopedTiltOffset = 10f; //ปรับองศาบนล่างของกล้องเมื่อซูม (ค่าบวก = เงยขึ้น, ค่าลบ = ก้มลง)

    //[Header("Dynamic Look Ahead (Scope)")]
    //public bool enableDynamicLook = true; // เปิด/ปิดให้กล้องขยับตามเมาส์เวลาซูม
    //public float maxLookOffsetX = 5f; // ระยะเลื่อนกล้องสูงสุดแกน X (ซ้าย-ขวา)
    //public float maxLookOffsetZ = 5f; // ระยะเลื่อนกล้องสูงสุดแกน Z (หน้า-หลัง สำหรับ Top-Down)

    private LineRenderer laserLine;
    private float nextFireTime;

    [Header("Audio")]
    public AudioClip fireSound;
    public AudioClip reloadStartSound;   // เสียงตอนเริ่มรีโหลด (ดึงแม็ก/เสียบแม็ก)
    public AudioClip reloadFinishSound;  // เสียงตอนรีโหลดเสร็จ (ปิดแม็ก/ขึ้นลำ) - ใส่หรือไม่ใส่ก็ได้
    public AudioClip equipSound;         // เสียงตอนชักปืนขึ้นมาถือ
    [Range(0f, 1f)] public float fireVolume = 1f;
    [Range(0f, 1f)] public float reloadVolume = 1f;
    [Range(0f, 1f)] public float equipVolume = 1f;
    public bool randomizePitch = true;
    [Range(0f, 0.2f)] public float pitchVariance = 0.05f;
    private AudioSource audioSource;

    [SerializeField] private int currentAmmo;
    private bool isReloading;
    private Coroutine reloadCoroutine;
    private TopDownPlayerController movementController;
    private InputAction resolvedReloadAction;

    public bool IsReloading => isReloading;
    public int CurrentAmmo => currentAmmo;
    public int MagazineSize => magazineSize;

    private Camera mainCam;
    // เปลี่ยนจาก Top Down เป็น First Person Scope Camera

    private CinemachineCamera[] virtualCameras;
    private CinemachinePositionComposer[] positionComposers;
    private CinemachineFollow[] followComponents;
    private Vector3[] baseOffsets;
    //private float[] baseCameraDistances;
    private CinemachinePanTilt[] panTiltComponents;
    private float[] baseTilts;

    private bool isScoped;
    private InputAction resolvedAimAction;
    private Transform ownerTransform;
    private CinemachineCamera firstPersonScopeCamera;
    private GameObject firstPersonScopeCameraObject;
    private CinemachineBrain cinemachineBrain;
    private CinemachineBlendDefinition savedBrainBlend;
    private Coroutine restoreBrainBlendCoroutine;
    private bool hasSavedBrainBlend;
    private bool isFirstPersonScopeCameraActive;
    private float firstPersonYaw;
    private float firstPersonPitch;
    private bool savedCursorVisible;
    private CursorLockMode savedCursorLockState;
    private bool hasSavedCursorState;

    private void Awake()
    {
        laserLine = GetComponent<LineRenderer>();
        laserLine.enabled = false;
        mainCam = Camera.main;
        if (mainCam != null)
            cinemachineBrain = mainCam.GetComponent<CinemachineBrain>();
        //virtualCameras = FindObjectsByType<CinemachineCamera>(FindObjectsInactive.Include);
        movementController = GetComponentInParent<TopDownPlayerController>();
        ownerTransform = movementController != null ? movementController.transform : transform.root;
        EnsureFirstPersonScopeCamera();

        audioSource = GetComponent<AudioSource>(); // เพิ่มบรรทัดนี้
        audioSource.playOnAwake = false;            // กันไม่ให้เล่นเองตอนเริ่มเกม

        if (mainCam != null)
            normalFieldOfView = mainCam.fieldOfView;

        currentAmmo = magazineSize;
        //CacheCameraOffsets();
    }
    private void OnDisable()
    {
        CancelReload();
        SetScoped(false, true);
        SetMovementAimCameraOverride(false);
        RestoreCinemachineBrainBlend();
        SetCrosshairVisible(false);
        //Cursor.visible = true;
    }

    private void OnDestroy()
    {
        RestoreCinemachineBrainBlend();

        if (firstPersonScopeCameraObject != null)
            Destroy(firstPersonScopeCameraObject);
    }

    public void SetHeld(bool held)
    {
        isHeld = held;
        movementController = GetComponentInParent<TopDownPlayerController>();
        ownerTransform = movementController != null ? movementController.transform : transform.root;
        EnsureFirstPersonScopeCamera();
        SetCrosshairVisible(held && isScoped && !hideCrosshairWhileScoped);
        OnHeldChanged?.Invoke(held);

        if (held)
        { 
        UpdateAmmoUI();
        PlaySound(equipSound, equipVolume);
        }

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

    private void LateUpdate()
    {
        HandleFirstPersonScopeLook();
        UpdateFirstPersonScopeCamera();
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
        PlaySound(reloadStartSound, reloadVolume); ;

        yield return new WaitForSeconds(reloadDuration);

        currentAmmo = magazineSize;
        isReloading = false;
        SetReloadMovementSpeed(false);
        reloadCoroutine = null;

        PlaySound(reloadFinishSound, reloadVolume);

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

        movementController.SetSprintLocked(reloading);
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
            // ตอนถือปืนปกติจะไม่มีเป้าเล็ง เป้าเล็งจะแสดงเฉพาะตอนซูมเท่านั้น
            SetCrosshairVisible(isHeld && isScoped && !hideCrosshairWhileScoped);

            if (isHeld && isScoped)
            {
                // ในโหมด FPS crosshair อยู่กลางจอ และเมาส์ใช้หมุนกล้อง
                crosshairUI.position = GetCrosshairScreenPosition();

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

    void SetScoped(bool scoped, bool instant)
    {
        if (useFirstPersonScope)
        {
            if (isScoped == scoped)
                return;

            isScoped = scoped;
            SetFirstPersonScopeCameraActive(scoped);
            return;
        }

        if (isFirstPersonScopeCameraActive)
            SetFirstPersonScopeCameraActive(false);

        SetMovementAimCameraOverride(false);
        RestoreFirstPersonCursorState();
        //SetTopDownScoped(scoped, instant);
    }

    void SetScoped(bool scoped)
    {
        SetScoped(scoped, false);
    }

    // ปิด First Person Scope ได้จาก Inspector ด้วย useFirstPersonScope = false
    // ถ้าปิดแล้วระบบจะกลับมาใช้มุมกล้อง Top-Down Scope เดิมด้านล่าง

    // ฟังก์ชันนี้ใช้ปรับมุมกล้องและตำแหน่งกล้องเมื่อเล็ง (Scope) หรือไม่เล็ง
    //void SetTopDownScoped(bool scoped, bool instant)
    //{
    //    isScoped = scoped;

    //    if (virtualCameras == null || baseOffsets == null || baseTilts == null)
    //        return;

    //    float targetFieldOfView = scoped ? scopedFieldOfView : normalFieldOfView;
    //    float lerpAmount = instant ? 1f : Time.deltaTime * scopeZoomSpeed;

    //    if (virtualCameras != null)
    //    {
    //        for (int i = 0; i < virtualCameras.Length; i++)
    //        {
    //            if (virtualCameras[i] == null)
    //                continue;

    //            if (enableScopeCameraOffset)
    //            {
    //                // --- ส่วนที่เพิ่มเข้ามาใหม่: คำนวณการขยับกล้องตามเมาส์ ---
    //                Vector3 dynamicLookOffset = Vector3.zero;

    //                if (scoped && enableDynamicLook)
    //                {
    //                    // 1. ดึงตำแหน่งเมาส์บนจอ
    //                    Vector2 mouseScreenPos = pointerAction.action.ReadValue<Vector2>();

    //                    // 2. หาจุดกึ่งกลางหน้าจอ
    //                    Vector2 screenCenter = new Vector2(Screen.width / 2f, Screen.height / 2f);

    //                    // 3. หาว่าเมาส์อยู่ห่างจากตรงกลางกี่เปอร์เซ็นต์ (ได้ค่า -1 ถึง 1)
    //                    float normalizedX = (mouseScreenPos.x - screenCenter.x) / screenCenter.x;
    //                    float normalizedY = (mouseScreenPos.y - screenCenter.y) / screenCenter.y;

    //                    // 4. จำกัดขอบเขตกันเมาส์หลุดจอ
    //                    normalizedX = Mathf.Clamp(normalizedX, -1f, 1f);
    //                    normalizedY = Mathf.Clamp(normalizedY, -1f, 1f);

    //                    // 5. แปลงเป็นระยะทางในโลก 3D (แกน X ซ้ายขวา, แกน Z หน้าหลัง)
    //                    dynamicLookOffset = new Vector3(normalizedX * maxLookOffsetX, 0f, normalizedY * maxLookOffsetZ);
    //                }

    //                Vector3 desiredOffset = baseOffsets[i] + (scoped ? scopedCameraOffset : Vector3.zero) + dynamicLookOffset;
    //                // --------------------------------------------------

    //                float desiredTilt = baseTilts[i] + (scoped ? scopedTiltOffset : 0f);

    //                if (positionComposers[i] != null)
    //                {
    //                    positionComposers[i].TargetOffset =
    //                        Vector3.Lerp(positionComposers[i].TargetOffset, desiredOffset, lerpAmount);
    //                }
    //                else if (followComponents[i] != null)
    //                {
    //                    followComponents[i].FollowOffset =
    //                        Vector3.Lerp(followComponents[i].FollowOffset, desiredOffset, lerpAmount);
    //                }
    //                else
    //                {
    //                    virtualCameras[i].transform.localPosition =
    //                        Vector3.Lerp(virtualCameras[i].transform.localPosition, desiredOffset, lerpAmount);
    //                }

    //                if (panTiltComponents[i] != null)
    //                {
    //                    var panTilt = panTiltComponents[i];
    //                    var tiltAxis = panTilt.TiltAxis;
    //                    tiltAxis.Value = Mathf.Lerp(tiltAxis.Value, desiredTilt, lerpAmount);
    //                    panTilt.TiltAxis = tiltAxis;
    //                }
    //                else
    //                {
    //                    Vector3 euler = virtualCameras[i].transform.localEulerAngles;
    //                    euler.x = Mathf.LerpAngle(euler.x, desiredTilt, lerpAmount);
    //                    virtualCameras[i].transform.localEulerAngles = euler;
    //                }
    //            }
    //        }
    //    }

    //    if (mainCam != null)
    //        mainCam.fieldOfView = Mathf.Lerp(mainCam.fieldOfView, targetFieldOfView, lerpAmount);
    //}

    // ฟังก์ชันนี้ใช้เก็บค่า offset และ tilt ของกล้องแต่ละตัวไว้เป็นฐาน เพื่อให้สามารถปรับเปลี่ยนได้โดยไม่ทับค่าที่ตั้งไว้ใน Inspector
    //void CacheCameraOffsets()
    //{
    //    if (virtualCameras == null) return;

    //    int count = virtualCameras.Length;
    //    positionComposers = new CinemachinePositionComposer[count];
    //    followComponents = new CinemachineFollow[count];
    //    baseOffsets = new Vector3[count];

    //    panTiltComponents = new CinemachinePanTilt[count];
    //    baseTilts = new float[count];

    //    for (int i = 0; i < count; i++)
    //    {
    //        if (virtualCameras[i] == null) continue;

    //        var composer = virtualCameras[i].GetComponent<CinemachinePositionComposer>();
    //        var follow = virtualCameras[i].GetComponent<CinemachineFollow>();
    //        var panTilt = virtualCameras[i].GetComponent<CinemachinePanTilt>();

    //        positionComposers[i] = composer;
    //        followComponents[i] = follow;
    //        panTiltComponents[i] = panTilt;

    //        // เก็บค่า offset เดิมไว้เป็นฐาน จะได้ไม่ทับค่าที่ตั้งไว้ใน Inspector
    //        if (composer != null)
    //            baseOffsets[i] = composer.TargetOffset;
    //        else if (follow != null)
    //            baseOffsets[i] = follow.FollowOffset;
    //        else
    //            baseOffsets[i] = virtualCameras[i].transform.localPosition;

    //        if (panTilt != null)
    //            baseTilts[i] = panTilt.TiltAxis.Value;
    //        else
    //            baseTilts[i] = virtualCameras[i].transform.localEulerAngles.x;
    //    }
    //}
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

        PlaySound(fireSound, fireVolume);

        if (showDebugLine) StartCoroutine(ShotEffect());

        laserLine.SetPosition(0, firePoint.position);

        Vector3 shootDirection = isScoped ? GetScopedShootDirection() : GetForwardShootDirection();

        RaycastHit hit;

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

    private Vector3 GetForwardShootDirection()
    {
        Transform facingTransform = ownerTransform != null ? ownerTransform : firePoint;
        Vector3 direction = facingTransform.forward;
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.001f)
            direction = firePoint.forward;

        return direction.normalized;
    }

    private Vector3 GetScopedShootDirection()
    {
        if (mainCam == null)
            mainCam = Camera.main;

        if (mainCam == null)
            return firePoint.forward;

        Ray aimRay = GetScopedAimRay();
        Vector3 aimPoint = aimRay.GetPoint(range);

        if (Physics.Raycast(aimRay, out RaycastHit cameraHit, range))
            aimPoint = cameraHit.point;

        Vector3 direction = aimPoint - firePoint.position;

        if (direction.sqrMagnitude < 0.001f)
            direction = aimRay.direction;

        return direction.normalized;
    }

    private Ray GetScopedAimRay()
    {
        if (useFirstPersonScope && firstPersonScopeCameraObject != null)
        {
            Vector2 screenPosition = GetCrosshairScreenPosition();
            float safeScreenHeight = Mathf.Max(1f, Screen.height);
            float safeScreenWidth = Mathf.Max(1f, Screen.width);
            float halfVerticalFov = scopedFieldOfView * Mathf.Deg2Rad * 0.5f;
            float halfHeight = Mathf.Tan(halfVerticalFov);
            float halfWidth = halfHeight * (safeScreenWidth / safeScreenHeight);

            float normalizedX = (screenPosition.x / safeScreenWidth - 0.5f) * 2f;
            float normalizedY = (screenPosition.y / safeScreenHeight - 0.5f) * 2f;
            Vector3 localDirection = new Vector3(normalizedX * halfWidth, normalizedY * halfHeight, 1f).normalized;
            Vector3 worldDirection = firstPersonScopeCameraObject.transform.TransformDirection(localDirection);

            return new Ray(firstPersonScopeCameraObject.transform.position, worldDirection);
        }

        if (mainCam == null)
            mainCam = Camera.main;

        if (mainCam != null)
            return mainCam.ScreenPointToRay(GetCrosshairScreenPosition());

        return new Ray(firePoint.position, firePoint.forward);
    }

    private Vector2 GetCrosshairScreenPosition()
    {
        if (isScoped && useFirstPersonScope && centerCrosshairInFirstPersonScope)
            return new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);

        if (pointerAction == null || pointerAction.action == null)
            return new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);

        Vector2 pointerPosition = pointerAction.action.ReadValue<Vector2>();
        return new Vector2(
            Mathf.Clamp(pointerPosition.x, 0f, Screen.width),
            Mathf.Clamp(pointerPosition.y, 0f, Screen.height));
    }

    private void EnsureFirstPersonScopeCamera()
    {
        if (!useFirstPersonScope || ownerTransform == null || firstPersonScopeCamera != null)
            return;

        firstPersonScopeCameraObject = new GameObject($"{name}_FirstPersonScopeCamera");
        firstPersonScopeCameraObject.transform.SetParent(ownerTransform, false);
        firstPersonScopeCamera = firstPersonScopeCameraObject.AddComponent<CinemachineCamera>();
        firstPersonScopeCamera.Priority.Value = 0;
        firstPersonScopeCamera.Lens.FieldOfView = scopedFieldOfView;
        UpdateFirstPersonScopeCamera();
    }

    private void SetFirstPersonScopeCameraActive(bool active)
    {
        if (isFirstPersonScopeCameraActive == active)
        {
            if (firstPersonScopeCamera != null)
                firstPersonScopeCamera.Priority.Value = active ? firstPersonCameraPriority : 0;

            return;
        }

        if (active)
        {
            EnsureFirstPersonScopeCamera();
            PrepareFirstPersonScopeCameraPose();
            ApplyInstantCinemachineCut();
        }
        else
        {
            ApplyInstantCinemachineCut();
        }

        if (firstPersonScopeCamera == null)
            return;

        isFirstPersonScopeCameraActive = active;
        firstPersonScopeCamera.Priority.Value = active ? firstPersonCameraPriority : 0;
        firstPersonScopeCamera.Lens.FieldOfView = scopedFieldOfView;
        SetMovementAimCameraOverride(active);

        if (!active)
        {
            RestoreFirstPersonCursorState();
            StartRestoreBrainBlendNextFrame();
        }
    }

    private void UpdateFirstPersonScopeCamera()
    {
        if (firstPersonScopeCamera == null || ownerTransform == null)
            return;

        Quaternion cameraRotation = Quaternion.Euler(firstPersonPitch, firstPersonYaw, 0f);

        firstPersonScopeCameraObject.transform.position = ownerTransform.position + cameraRotation * firstPersonCameraOffset;
        firstPersonScopeCameraObject.transform.rotation = cameraRotation;
        firstPersonScopeCamera.Lens.FieldOfView = scopedFieldOfView;
    }

    private void PrepareFirstPersonScopeCameraPose()
    {
        Quaternion startRotation = ownerTransform != null ? ownerTransform.rotation : transform.rotation;
        Vector3 euler = startRotation.eulerAngles;
        firstPersonYaw = euler.y;
        firstPersonPitch = NormalizePitch(euler.x);
        SaveAndApplyFirstPersonCursorState();
        UpdateFirstPersonScopeCamera();
    }

    private float NormalizePitch(float pitch)
    {
        return pitch > 180f ? pitch - 360f : pitch;
    }

    private void HandleFirstPersonScopeLook()
    {
        if (!isScoped || !useFirstPersonScope || firstPersonScopeCameraObject == null)
            return;

        Vector2 lookDelta = Mouse.current != null ? Mouse.current.delta.ReadValue() : Vector2.zero;
        firstPersonYaw += lookDelta.x * firstPersonMouseSensitivity;
        firstPersonPitch = Mathf.Clamp(
            firstPersonPitch - lookDelta.y * firstPersonMouseSensitivity,
            minFirstPersonPitch,
            maxFirstPersonPitch);

        if (ownerTransform != null)
            ownerTransform.rotation = Quaternion.Euler(0f, firstPersonYaw, 0f);
    }

    private void SaveAndApplyFirstPersonCursorState()
    {
        if (!lockCursorInFirstPersonScope || hasSavedCursorState)
            return;

        savedCursorVisible = Cursor.visible;
        savedCursorLockState = Cursor.lockState;
        hasSavedCursorState = true;
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }

    private void RestoreFirstPersonCursorState()
    {
        if (!hasSavedCursorState)
            return;

        Cursor.visible = savedCursorVisible;
        Cursor.lockState = savedCursorLockState;
        hasSavedCursorState = false;
    }

    private void ApplyInstantCinemachineCut()
    {
        if (!snapScopeCameraTransition)
            return;

        if (cinemachineBrain == null && mainCam != null)
            cinemachineBrain = mainCam.GetComponent<CinemachineBrain>();

        if (cinemachineBrain == null)
            return;

        if (restoreBrainBlendCoroutine != null)
        {
            StopCoroutine(restoreBrainBlendCoroutine);
            restoreBrainBlendCoroutine = null;
        }

        if (!hasSavedBrainBlend)
        {
            savedBrainBlend = cinemachineBrain.DefaultBlend;
            hasSavedBrainBlend = true;
        }

        cinemachineBrain.DefaultBlend = new CinemachineBlendDefinition(CinemachineBlendDefinition.Styles.Cut, 0f);
        cinemachineBrain.ActiveBlend = null;
    }

    private void StartRestoreBrainBlendNextFrame()
    {
        if (!snapScopeCameraTransition || !hasSavedBrainBlend || cinemachineBrain == null)
            return;

        if (restoreBrainBlendCoroutine != null)
            StopCoroutine(restoreBrainBlendCoroutine);

        restoreBrainBlendCoroutine = StartCoroutine(RestoreBrainBlendNextFrame());
    }

    private IEnumerator RestoreBrainBlendNextFrame()
    {
        yield return new WaitForEndOfFrame();
        RestoreCinemachineBrainBlend();
        restoreBrainBlendCoroutine = null;
    }

    private void RestoreCinemachineBrainBlend()
    {
        if (!hasSavedBrainBlend || cinemachineBrain == null)
            return;

        cinemachineBrain.DefaultBlend = savedBrainBlend;
        hasSavedBrainBlend = false;
    }

    private void SetMovementAimCameraOverride(bool active)
    {
        if (movementController == null)
            movementController = GetComponentInParent<TopDownPlayerController>();

        if (movementController == null)
            return;

        Transform aimCameraTransform = active && firstPersonScopeCameraObject != null
            ? firstPersonScopeCameraObject.transform
            : null;

        movementController.SetAimCameraOverride(aimCameraTransform, scopedFieldOfView, active);
    }

    // ฟังก์ชันนี้ใช้แสดงเอฟเฟกต์การยิงกระสุน (เปิด LineRenderer ชั่วคราว)
    private IEnumerator ShotEffect()
    {
        laserLine.enabled = true;
        yield return new WaitForSeconds(0.05f);
        laserLine.enabled = false;
    }

    private void PlaySound(AudioClip clip, float volume)
    {
        if (clip == null || audioSource == null)
            return;

        if (randomizePitch)
            audioSource.pitch = 1f + UnityEngine.Random.Range(-pitchVariance, pitchVariance);
        else
            audioSource.pitch = 1f;

        audioSource.PlayOneShot(clip, volume);
    }
}
