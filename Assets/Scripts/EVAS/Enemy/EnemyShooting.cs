using System.Collections;
using UnityEngine;
using UnityEngine.Pool;

[RequireComponent(typeof(EnemyController))]
public class EnemyShooting : MonoBehaviour
{
    [Header("Shooting Settings")]
    [SerializeField] private PooledBullet bulletPrefab;
    [SerializeField] private Transform firePoint;
    [SerializeField] private float bulletSpeed = 25f;
    [SerializeField] private float shootingRange = 15f;

    [Header("Pooling Settings")]
    [SerializeField] private int defaultPoolCapacity = 10;
    [SerializeField] private int maxPoolSize = 30;

    [Header("Timing & Delay")]
    [SerializeField] private float aimDelayBeforeShoot = 0.4f;
    [SerializeField] private float postShootDelay = 0.3f;
    [SerializeField] private float fireCooldown = 2f;

    [Header("Aiming")]
    [SerializeField] private float aimHeightOffset = 1f;

    [Header("Raycast / Line of Sight")]
    [SerializeField] private LayerMask obstructionMask;
    [SerializeField] private LayerMask playerLayerMask;
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private float rotationSpeed = 10f;

    [Header("Target Search")]
    [SerializeField] private float playerSearchInterval = 0.5f;

    private EnemyController enemyController;
    private FieldOfView fieldOfView;
    private Transform playerTransform;
    private bool isShootingRoutineRunning;
    private float lastFireTime = -Mathf.Infinity;
    private float lastPlayerSearchTime = -Mathf.Infinity;
    private float shootingRangeSqr;
    private int combinedLineOfSightMask;
    private Coroutine shootRoutine;

    private IObjectPool<PooledBullet> bulletPool;

    private void Awake()
    {
        enemyController = GetComponent<EnemyController>();
        fieldOfView = GetComponent<FieldOfView>();

        if (firePoint == null) firePoint = transform;

        shootingRangeSqr = shootingRange * shootingRange;
        combinedLineOfSightMask = obstructionMask.value | playerLayerMask.value;

        if (bulletPrefab == null)
        {
            Debug.Log($"[{nameof(EnemyShooting)}] {name}: bulletPrefab is not assigned. Disabling component.", this);

            enabled = false;
            return;
        }

        InitializePool();
    }

    private void InitializePool()
    {
        bulletPool = new ObjectPool<PooledBullet>(
            createFunc: CreateBullet,
            actionOnGet: OnGetBullet,
            actionOnRelease: OnReleaseBullet,
            actionOnDestroy: OnDestroyBullet,
            collectionCheck: true,
            defaultCapacity: defaultPoolCapacity,
            maxSize: maxPoolSize
        );
    }

    private PooledBullet CreateBullet()
    {
        PooledBullet bullet = Instantiate(bulletPrefab);
        bullet.gameObject.SetActive(false);
        bullet.SetPool(bulletPool);
        return bullet;
    }

    private void OnGetBullet(PooledBullet bullet)
    {
    }

    private void OnReleaseBullet(PooledBullet bullet)
    {
        if (bullet != null)
            bullet.gameObject.SetActive(false);
    }

    private void OnDestroyBullet(PooledBullet bullet)
    {
        if (bullet != null)
            Destroy(bullet.gameObject);
    }

    private void Start()
    {
        FindPlayerTarget();
    }
    private void Update()
    {
        FindPlayerTarget();

        if (playerTransform == null || isShootingRoutineRunning)
            return;

        if(Time.time < lastFireTime + fireCooldown)
            return;
        
        float sqrDistanceToPlayer = (transform.position - playerTransform.position).sqrMagnitude;

        if (sqrDistanceToPlayer > shootingRangeSqr)
            return;

        if (enemyController == null || !enemyController.IsChasing)
            return;

        if (CanSeeTarget())
            shootRoutine = StartCoroutine(ShootSequenceRoutine());
    }
    private IEnumerator ShootSequenceRoutine()
    {
        isShootingRoutineRunning = true;

        enemyController.SetMovementLocked(true);

        float timer = 0f;
        while(timer < aimDelayBeforeShoot)
        {
            if (playerTransform != null && enemyController != null)
                enemyController.RotateYawTowards(playerTransform.position, rotationSpeed);
            timer += Time.deltaTime;
            yield return null;
        }
        if (HasClearLineOfSight())
        {
            SpawnBulletFromPool();
            lastFireTime = Time.time;
        }

        yield return new WaitForSeconds(postShootDelay);

        if (enemyController != null && isActiveAndEnabled)
            enemyController.SetMovementLocked(false);

        isShootingRoutineRunning = false;
        shootRoutine = null;
    }
    private bool HasClearLineOfSight()
    {
        if (!TryGetMuzzleAim(out Vector3 origin, out Vector3 direction, out Vector3 aimPoint))
            return false;

        float distance = Vector3.Distance(origin, aimPoint);
        RaycastHit[] hits = Physics.RaycastAll(origin, direction, distance, combinedLineOfSightMask, QueryTriggerInteraction.Ignore);
        if (hits.Length == 0)
            return true;

        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        for (int i = 0; i < hits.Length; i++)
        {
            if (hits[i].transform != null && hits[i].transform.IsChildOf(transform))
                continue;

            return hits[i].collider.CompareTag(playerTag);
        }

        return true;
    }

    private void SpawnBulletFromPool()
    {
        if (bulletPrefab == null || !TryGetMuzzleAim(out Vector3 spawnPos, out Vector3 fireDirection, out _))
            return;

        PooledBullet bullet = bulletPool.Get();
        bullet.Launch(spawnPos, Quaternion.LookRotation(fireDirection), fireDirection * bulletSpeed, transform);
    }

    private bool TryGetMuzzleAim(out Vector3 spawnPos, out Vector3 fireDirection, out Vector3 aimPoint)
    {
        spawnPos = Vector3.zero;
        fireDirection = Vector3.forward;
        aimPoint = Vector3.zero;

        if (playerTransform == null)
            return false;

        spawnPos = firePoint != null ? firePoint.position : transform.position;
        aimPoint = GetPlayerAimPoint();
        fireDirection = aimPoint - spawnPos;

        if (fireDirection.sqrMagnitude < 0.0001f)
            return false;

        fireDirection.Normalize();
        return true;
    }

    private Vector3 GetPlayerAimPoint()
    {
        Collider playerCollider = playerTransform.GetComponent<Collider>();
        if (playerCollider != null)
            return playerCollider.bounds.center;

        CharacterController characterController = playerTransform.GetComponent<CharacterController>();
        if (characterController != null)
            return characterController.bounds.center;

        return playerTransform.position + Vector3.up * aimHeightOffset;
    }
    private bool CanSeeTarget()
    {
        if (fieldOfView != null)
            return fieldOfView.canSeePlayer;

        return HasClearLineOfSight();
    }
    private void FindPlayerTarget()
    {
        if (playerTransform != null)
            return;

        if (fieldOfView != null && fieldOfView.playerRef != null)
        {
            playerTransform = fieldOfView.playerRef.transform;
            return;
        }

        if(Time.time < lastPlayerSearchTime + playerSearchInterval)
            return;

        lastPlayerSearchTime = Time.time;

        GameObject playerObj = GameObject.FindGameObjectWithTag(playerTag);
        if (playerObj != null)
            playerTransform = playerObj.transform;
    }
    private void OnDisable()
    {
        if (shootRoutine != null)
        {
            StopCoroutine(shootRoutine);
            shootRoutine = null;
        }

        isShootingRoutineRunning = false;

        if (enemyController != null && gameObject.activeInHierarchy)
            enemyController.SetMovementLocked(false);
    }
    private void OnDestroy()
    {
        bulletPool?.Clear();
    }
    private void OnValidate()
    {
        shootingRangeSqr = shootingRange * shootingRange;
        combinedLineOfSightMask = obstructionMask.value | playerLayerMask.value;
    }

    //private void ()
    //{
    //    Gizmos.color = Color.red;
    //    Gizmos.DrawWireSphere(transform.position, shootingRange);
    //}
}
