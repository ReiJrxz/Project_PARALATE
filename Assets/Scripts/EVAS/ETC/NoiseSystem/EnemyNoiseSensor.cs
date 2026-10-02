using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class EnemyNoiseSensor : MonoBehaviour, INoiseListener
{
    private NavMeshAgent agent;
    private AlertState alertState;

    [Header("Memory Settings")]
    [SerializeField] private float memoryDuration = 5f;
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

    private float GetAlertPoints(NoiseType type)
    {
        return type switch
        {
            NoiseType.Shooting => 6f,
            NoiseType.Sprinting => 2f,
            NoiseType.Whistling => 2f,
            NoiseType.Throwing => 2f,
            NoiseType.Walking => 1f,
            _ => 0f
        };
    }

    private void ApplyBehaviorByNoiseType(NoiseType type)
    {
        switch (type)
        {
            case NoiseType.Shooting:
                break;

            case NoiseType.Sprinting:
                break;

            case NoiseType.Whistling:
            case NoiseType.Throwing:
            case NoiseType.Walking:
            default:
                break;
        }
    }
    private int GetNoisePriority(NoiseType type)
    {
        return type switch
        {
            NoiseType.Shooting => 4,
            NoiseType.Sprinting => 3,
            NoiseType.Whistling => 2,
            NoiseType.Throwing => 2,
            NoiseType.Walking => 1,
            _ => 0
        };
    }

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
