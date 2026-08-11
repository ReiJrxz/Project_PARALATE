using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class EnemyController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform[] Waypoints;

    [Header("Settings")]
    [SerializeField] private float patrolWaitsTime = 2f;
    [SerializeField] private float stopAtDistance = 0.5f;

    [Header("Look Around")]
    [SerializeField] private float lookAngle = 60f;
    [SerializeField] private float lookTurnSpeed = 180f;
    [SerializeField] private float lookHoldTime = 0.4f;

    private NavMeshAgent agent;
    private int currentWaypointIndex;
    private bool isWaiting;
    private bool isMovementLocked;
    private bool wasWaitingWhenMovementLocked;
    private bool updateRotationBeforePatrolWait;
    private bool updateRotationBeforeMovementLock;
    private Coroutine waitPatrolCoroutine;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
    }

    private void Start()
    {
        GotoNextWaypoint();
    }

    private void Update()
    {
        Patrol();
    }

    private void Patrol()
    {
        if(Waypoints.Length == 0) return;
        if(isMovementLocked) return;
        if(isWaiting) return;
        if(!agent.pathPending && agent.remainingDistance < stopAtDistance)
        {
            waitPatrolCoroutine = StartCoroutine(WaitPatrolPoint());
        }
    }

    public void SetMovementLocked(bool locked)
    {
        if (isMovementLocked == locked)
            return;

        isMovementLocked = locked;

        if (locked)
        {
            wasWaitingWhenMovementLocked = isWaiting;
            updateRotationBeforeMovementLock = isWaiting
                ? updateRotationBeforePatrolWait
                : agent.updateRotation;
            StopPatrolWait();
            agent.isStopped = true;
            agent.updateRotation = false;
            return;
        }

        agent.updateRotation = updateRotationBeforeMovementLock;
        agent.isStopped = false;

        if (wasWaitingWhenMovementLocked)
            GotoNextWaypoint();

        wasWaitingWhenMovementLocked = false;
    }

    private IEnumerator WaitPatrolPoint()
    {
        isWaiting = true;
        agent.isStopped = true;
        updateRotationBeforePatrolWait = agent.updateRotation;
        agent.updateRotation = false;

        yield return new WaitForSeconds(patrolWaitsTime);
        yield return LookAround();

        agent.updateRotation = updateRotationBeforePatrolWait;
        agent.isStopped = false;
        GotoNextWaypoint();
        isWaiting = false;
        waitPatrolCoroutine = null;
    }

    private void StopPatrolWait()
    {
        if (waitPatrolCoroutine != null)
        {
            StopCoroutine(waitPatrolCoroutine);
            waitPatrolCoroutine = null;
        }

        isWaiting = false;
    }

    private IEnumerator LookAround()
    {
        Quaternion originalRotation = transform.rotation;
        Quaternion leftRotation = originalRotation * Quaternion.Euler(0f, -lookAngle, 0f);
        Quaternion rightRotation = originalRotation * Quaternion.Euler(0f, lookAngle, 0f);

        yield return RotateTo(leftRotation);
        yield return new WaitForSeconds(lookHoldTime);
        yield return RotateTo(rightRotation);
        yield return new WaitForSeconds(lookHoldTime);
        yield return RotateTo(originalRotation);
    }

    private IEnumerator RotateTo(Quaternion targetRotation)
    {
        while(Quaternion.Angle(transform.rotation, targetRotation) > 0.1f)
        {
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation,
                targetRotation,
                lookTurnSpeed * Time.deltaTime);

            yield return null;
        }

        transform.rotation = targetRotation;
    }

    private void GotoNextWaypoint()
    {
        if(Waypoints.Length == 0)
            return;
        agent.SetDestination(Waypoints[currentWaypointIndex].position);
        currentWaypointIndex = (currentWaypointIndex + 1) % Waypoints.Length;
    }
}
