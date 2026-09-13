using UnityEngine;
using UnityEngine.Pool;

[RequireComponent(typeof(Rigidbody))]
public class PooledBullet : MonoBehaviour
{
    private static readonly RaycastHit[] sweepHits = new RaycastHit[16];

    [SerializeField] private float lifeTime = 3f;
    [SerializeField] private float damage = 10f;
    [SerializeField] private LayerMask hitMask = ~0;
    [SerializeField] private bool meshDisable = true;

    private Rigidbody rb;
    private SphereCollider bulletCollider;
    private IObjectPool<PooledBullet> pool;
    private Transform owner;
    private Vector3 previousPosition;
    private float lifeTimer;
    private bool isReturned;

    public PlayerHealth pHealth;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        bulletCollider = GetComponent<SphereCollider>();
        rb.useGravity = false;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
    }

    public void SetPool(IObjectPool<PooledBullet> objectPool)
    {
        pool = objectPool;
    }

    public void Launch(Vector3 position, Quaternion rotation, Vector3 velocity, Transform bulletOwner = null)
    {
        isReturned = false;
        owner = bulletOwner;
        lifeTimer = lifeTime;

        IgnoreOwnerCollisions(true);
        transform.SetPositionAndRotation(position, rotation);

        if (!gameObject.activeSelf)
            gameObject.SetActive(true);

        rb.position = position;
        rb.rotation = rotation;
        rb.linearVelocity = velocity;
        rb.angularVelocity = Vector3.zero;
        previousPosition = position;
    }

    private void Update()
    {
        if (isReturned)
            return;

        lifeTimer -= Time.deltaTime;
        if (lifeTimer <= 0f)
            ReturnToPool();

        DisableMesh();
    }

    private void FixedUpdate()
    {
        if (isReturned)
            return;

        Vector3 currentPosition = rb.position;
        CheckTravelHits(previousPosition, currentPosition);
        previousPosition = currentPosition;
    }

    private void CheckTravelHits(Vector3 from, Vector3 to)
    {
        Vector3 delta = to - from;
        float distance = delta.magnitude;
        if (distance <= 0.0001f)
            return;

        float radius = GetBulletRadius();
        int hitCount = Physics.SphereCastNonAlloc(
            from,
            radius,
            delta / distance,
            sweepHits,
            distance,
            hitMask,
            QueryTriggerInteraction.Collide);

        Collider closestCollider = null;
        float closestDistance = float.MaxValue;

        for (int i = 0; i < hitCount; i++)
        {
            Collider hitCollider = sweepHits[i].collider;
            if (!IsValidHit(hitCollider))
                continue;

            if (sweepHits[i].distance >= closestDistance)
                continue;

            closestDistance = sweepHits[i].distance;
            closestCollider = hitCollider;
        }

        if (closestCollider != null)
            ProcessHit(closestCollider);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!IsValidHit(other))
            return;

        ProcessHit(other);
    }

    private bool IsValidHit(Collider other)
    {
        if (isReturned || other == null)
            return false;

        if (IsOwnerCollider(other))
            return false;

        if (other.isTrigger && !other.CompareTag("Player"))
            return false;

        return true;
    }

    private void ProcessHit(Collider other)
    {
        if (isReturned || other == null)
            return;

        if (other.CompareTag("Player"))
        {
            Debug.Log($"[{nameof(PooledBullet)}] {name}: Hit player. Dealing {damage} damage.", this);
            PlayerHealth playerHealth = other.GetComponentInParent<PlayerHealth>();
            if (playerHealth != null)
                playerHealth.TakeDamage(damage);
        }
            
        else
            Debug.Log($"[{nameof(PooledBullet)}] {name}: Hit {other.name}.", this);

        ReturnToPool();
    }

    private float GetBulletRadius()
    {
        float radius = bulletCollider != null ? bulletCollider.radius : 0.5f;
        Vector3 scale = transform.lossyScale;
        return radius * Mathf.Max(scale.x, Mathf.Max(scale.y, scale.z));
    }

    private void ReturnToPool()
    {
        if (isReturned)
            return;

        isReturned = true;
        IgnoreOwnerCollisions(false);
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        if (pool != null)
            pool.Release(this);
        else
            gameObject.SetActive(false);
    }

    private bool IsOwnerCollider(Collider other)
    {
        if (owner == null || other == null)
            return false;

        return other.transform == owner || other.transform.IsChildOf(owner);
    }

    private void IgnoreOwnerCollisions(bool ignore)
    {
        if (owner == null || bulletCollider == null)
            return;

        Collider[] ownerColliders = owner.GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < ownerColliders.Length; i++)
        {
            if (ownerColliders[i] == null || ownerColliders[i] == bulletCollider)
                continue;

            Physics.IgnoreCollision(bulletCollider, ownerColliders[i], ignore);
        }
    }
    private void DisableMesh()
    {
        if (meshDisable)
            GetComponent<MeshRenderer>().enabled = false;
        else
            GetComponent<MeshRenderer>().enabled = true;
    }
}
