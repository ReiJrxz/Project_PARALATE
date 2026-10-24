using UnityEngine;

[RequireComponent(typeof(EnemyController))]
public class EnemyReflexSystem : MonoBehaviour
{
    [Header("Reflex Settings")]
    [SerializeField] private float reflexRadius = 5f;
    [SerializeField] private float reflexTurnSpeed = 720f;
    [SerializeField] private float fireAngleTolerance = 4f;
    [SerializeField] private float reflexCooldown = 1f;

    private EnemyController enemyController;
    private EnemyShooting enemyShooting;
    private AlertState alertState;
    private FieldOfView fieldOfView;
    private EnemyHealth enemyHealth;
    private Transform playerTransform;
    private bool isReflexing;
    private float nextReflexTime;

    private void Awake()
    {
        enemyController = GetComponent<EnemyController>();
        enemyShooting = GetComponent<EnemyShooting>();
        alertState = GetComponent<AlertState>();
        fieldOfView = GetComponent<FieldOfView>();
        enemyHealth = GetComponent<EnemyHealth>();
    }

    private void Update()
    {
        CachePlayer();

        if (isReflexing)
        {
            UpdateReflex();
            return;
        }

        if (!CanStartReflex())
            return;

        enemyController.StartChase();
        enemyController.SetMovementLocked(this, true);
        isReflexing = true;
    }

    private void UpdateReflex()
    {
        if (!IsReflexTargetValid())
        {
            EndReflex();
            return;
        }

        enemyController.RotateYawTowardsAtSpeed(playerTransform.position, reflexTurnSpeed);

        Vector3 directionToPlayer = playerTransform.position - transform.position;
        directionToPlayer.y = 0f;
        if (directionToPlayer.sqrMagnitude < 0.001f ||
            Vector3.Angle(transform.forward, directionToPlayer) > fireAngleTolerance)
            return;

        // EnemyShooting ล็อกการเคลื่อนไหวด้วย key ของตัวเองอยู่แล้ว ระบบนี้ปลดแค่ key ของตัวเอง
        enemyShooting.TryStartReflexShot();
        EndReflex();
    }

    private bool CanStartReflex()
    {
        // ถ้ามีระบบอื่นล็อกศัตรูอยู่ (ถูกรัดคอ/สลบ/กำลังยิง) ไม่ต้องทำ reflex
        return Time.time >= nextReflexTime &&
               enemyShooting != null &&
               !enemyController.IsMovementLocked &&
               IsReflexTargetValid();
    }

    private bool IsReflexTargetValid()
    {
        if (playerTransform == null || alertState == null || !alertState.IsAlert)
            return false;

        if (enemyHealth != null && (enemyHealth.IsDead || enemyHealth.IsStunned))
            return false;

        Vector3 offset = playerTransform.position - transform.position;
        offset.y = 0f;
        return offset.sqrMagnitude <= reflexRadius * reflexRadius;
    }

    private void EndReflex()
    {
        isReflexing = false;
        nextReflexTime = Time.time + reflexCooldown;

        if (enemyController != null)
            enemyController.SetMovementLocked(this, false);
    }

    private void OnDisable()
    {
        if (isReflexing)
            EndReflex();
    }

    private void CachePlayer()
    {
        if (playerTransform != null)
            return;

        if (fieldOfView != null && fieldOfView.playerRef != null)
        {
            playerTransform = fieldOfView.playerRef.transform;
            return;
        }

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
            playerTransform = player.transform;
    }

    private void OnValidate()
    {
        reflexRadius = Mathf.Max(0f, reflexRadius);
        reflexTurnSpeed = Mathf.Max(0f, reflexTurnSpeed);
        fireAngleTolerance = Mathf.Clamp(fireAngleTolerance, 0f, 45f);
        reflexCooldown = Mathf.Max(0f, reflexCooldown);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.3f, 0f, 0.8f);
        Gizmos.DrawWireSphere(transform.position, reflexRadius);
    }
}
