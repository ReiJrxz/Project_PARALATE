using UnityEngine;
using System.Collections;
using UnityEngine.InputSystem; // ต้องใช้สำหรับ New Input System

[RequireComponent(typeof(LineRenderer))]
public class GunAction : MonoBehaviour
{
    [Header("Gun Settings")]
    public Transform firePoint;
    public float range = 100f;
    public float damage = 10f;
    public float fireRate = 0.1f;

    [Header("Crosshair & UI")]
    public RectTransform crosshairUI; // ลาก UI เป้าเล็งมาใส่ช่องนี้

    [Header("Debug Settings")]
    public bool isHeld = false;
    public bool showDebugLine = true;

    [Header("Input Actions")]
    public InputActionReference fireAction;
    public InputActionReference pointerAction;

    private LineRenderer laserLine;
    private float nextFireTime;
    private Camera mainCam;

    private void Awake()
    {
        laserLine = GetComponent<LineRenderer>();
        laserLine.enabled = false;
        mainCam = Camera.main;
    }

    private void OnDisable()
    {
        SetCrosshairVisible(false);
        Cursor.visible = true;
    }

    public void SetHeld(bool held)
    {
        isHeld = held;
        SetCrosshairVisible(held);

        if (!held)
            Cursor.visible = true;
    }

    void Update()
    {
        HandleCrosshair();

        // ใช้ IsPressed() ของ New Input System แทน Input.GetButton เดิม
        if (isHeld && fireAction.action.IsPressed() && Time.time > nextFireTime)
        {
            nextFireTime = Time.time + fireRate;
            Shoot();
        }
    }

    void HandleCrosshair()
    {
        if (crosshairUI != null)
        {
            // เปิด/ปิด UI เป้าเล็งตามสถานะการถือปืน
            SetCrosshairVisible(isHeld);

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

    void SetCrosshairVisible(bool visible)
    {
        if (crosshairUI != null && crosshairUI.gameObject.activeSelf != visible)
            crosshairUI.gameObject.SetActive(visible);
    }

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

    private IEnumerator ShotEffect()
    {
        laserLine.enabled = true;
        yield return new WaitForSeconds(0.05f);
        laserLine.enabled = false;
    }
}
