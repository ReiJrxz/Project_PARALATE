using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class EnemyNoiseSensor : MonoBehaviour, INoiseListener
{
    private NavMeshAgent agent;
    private AlertState alertState;

    [Header("Memory Settings")]
    [SerializeField] private float memoryDuration = 5f;

    [Header("Noise Response Settings")]
    [SerializeField] private NoiseSetting walkingSetting = new NoiseSetting(1, 1f);
    [SerializeField] private NoiseSetting sprintingSetting = new NoiseSetting(3, 4f);
    [SerializeField] private NoiseSetting shootingSetting = new NoiseSetting(4, 6f);
    [SerializeField] private NoiseSetting whistlingSetting = new NoiseSetting(2, 2f);
    [SerializeField] private NoiseSetting throwingSetting = new NoiseSetting(2, 2f);
    [SerializeField] private NoiseSetting bulletHitWallSetting = new NoiseSetting(2, 4f);

    private float memoryTimer = 0f;
    private bool isInvestigating = false;
    private NoiseType currentTargetNoiseType;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        alertState = GetComponent<AlertState>();
    }

    private void OnEnable() => NoiseEmitterSystem.Register(this);

    private void OnDisable() => NoiseEmitterSystem.Unregister(this);

    private void Update()
    {
        if (isInvestigating)
        {
            memoryTimer -= Time.deltaTime;

            if (memoryTimer <= 0f || (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance))
            {
                isInvestigating = false;
            }
        }
    }

    public void OnNoiseHeard(in NoiseData noise)
    {
        if (!isActiveAndEnabled || agent == null || !agent.isOnNavMesh)
            return;

        // ตรวจสอบความสำคัญของเสียง
        if (isInvestigating && GetNoisePriority(noise.NoiseType) < GetNoisePriority(currentTargetNoiseType))
        {
            return;
        }

        // ใหั AI เดินไปตามเสียง
        if (NavMesh.SamplePosition(noise.Position, out NavMeshHit hit, 3.0f, NavMesh.AllAreas))
        {
            currentTargetNoiseType = noise.NoiseType;
            isInvestigating = true;
            memoryTimer = memoryDuration;

            if (alertState != null)
            {
                alertState.AddSuspiciousPoint(hit.position, GetAlertPoints(noise.NoiseType));
            }
            else
            {
                agent.SetDestination(hit.position);
            }
        }
    }

    [System.Serializable]
    public struct NoiseSetting
    {
        public int priority;
        public float alertPoints;
        public NoiseSetting(int priority, float alertPoints)
        {
            this.priority = priority;
            this.alertPoints = alertPoints;
        }
    }
    private NoiseSetting GetSetting(NoiseType type)
    {
        return type switch
        {
            NoiseType.Shooting => shootingSetting,
            NoiseType.Sprinting => sprintingSetting,
            NoiseType.BulletHitWall => bulletHitWallSetting,
            NoiseType.Whistling => whistlingSetting,
            NoiseType.Throwing => throwingSetting,
            NoiseType.Walking => walkingSetting,
            _ => default
        };
    }

    private float GetAlertPoints(NoiseType type) => GetSetting(type).alertPoints;
    private int GetNoisePriority(NoiseType type) => GetSetting(type).priority;

    private void OnDrawGizmosSelected()
    {
        if (isInvestigating && agent != null)
        {
            Gizmos.color = (currentTargetNoiseType == NoiseType.Shooting) ? Color.red : Color.yellow;
            Gizmos.DrawWireSphere(agent.destination, 0.6f);
            Gizmos.DrawLine(transform.position, agent.destination);
        }
    }
}
