using UnityEngine;
using System.Collections;
using Unity.VisualScripting;

[RequireComponent(typeof(LineRenderer))]
public class GunAction : MonoBehaviour
{
    [Header("Gun Settings")]
    public Transform firePoint; // จุดยิงกระสุน
    public float range = 100f; // ระยะยิง
    public float damage = 10f; // ความเสียหายของกระสุน
    public float fireRate = 0.1f; // อัตราการยิง (ยิงต่อวินาที)

    [Header("Debug Settings")]
    public bool isHeld = false;
    public bool showDebugLine = true;

    private LineRenderer laserLine;
    private float nextFireTime;

    private void Awake()
    {
        laserLine = GetComponent<LineRenderer>();
        laserLine.enabled = false;
    }

    // Update is called once per frame
    void Update()
    {
        if (isHeld && Input.GetButton("Fire1") && Time.time > nextFireTime)
        {
            nextFireTime = Time.time + fireRate;
            Shoot();
        }
    }

    void Shoot()
    {
        if (showDebugLine) StartCoroutine(ShotEffect());

        laserLine.SetPosition(0, firePoint.position);

        RaycastHit hit;

        if(Physics.Raycast(firePoint.position, firePoint.forward, out hit, range))
        {
            Debug.Log("Hit: " + hit.collider.name);

            laserLine.SetPosition(1, hit.point);

            EnemyHealth enemy = hit.collider.GetComponent<EnemyHealth>();
            if (enemy != null)
            {
                enemy.TakeDamage(damage);
            }
        }
        else
        {
            laserLine.SetPosition(1, firePoint.position + (firePoint.forward * range));
        }
    }
    private IEnumerator ShotEffect()
    {
        laserLine.enabled = true;
        yield return new WaitForSeconds(0.05f); // แสดงเส้นนาน 0.05 วินาที
        laserLine.enabled = false;
    }
}
