using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Serialization;

public class EnemyController : MonoBehaviour
{
    private enum EnemyState
    {
        Patrol,
        LookAround,
        Chase,
        Locked
    }

    [Header("References")]
    [FormerlySerializedAs("Waypoints")]
    [SerializeField] private Transform[] waypoints;

    [Header("Settings")]
    [FormerlySerializedAs("patrolWaitsTime")]
    [SerializeField] private float patrolWaitTime = 2f;
    [SerializeField] private float stopAtDistance = 0.5f;

    [Header("Look Around")]
    [SerializeField] private float lookAngle = 60f;
    [SerializeField] private float lookTurnSpeed = 180f;
    [SerializeField] private float lookHoldTime = 0.4f;

    [Header("Chase")]
    [SerializeField] private float chaseSpeed = 5f;
    [SerializeField] private float returnToPatrolAfterLostSightTime = 3f;

    private NavMeshAgent agent;
    private FieldOfView fieldOfView;
    private Transform chaseTarget;
    private Coroutine lookAroundCoroutine;
    private EnemyState currentState;
    private EnemyState stateBeforeLock;
    private int currentWaypointIndex;
    private float patrolSpeed;
    private float lostSightTimer;
    private float minimumChaseTimer;
    private bool updateRotationBeforeManualTurn;

    private bool HasWaypoints => waypoints != null && waypoints.Length > 0;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        fieldOfView = GetComponent<FieldOfView>();
        patrolSpeed = agent.speed;
    }

    private void Start()
    {
        EnsureChaseTarget();
        EnterPatrol(false);
    }

    private void Update()
    {
        if (currentState == EnemyState.Locked)
            return;

        if (currentState != EnemyState.Chase && CanSeeChaseTarget())
        {
            StartChase();
            return;
        }

        switch (currentState)
        {
            case EnemyState.Patrol:
                UpdatePatrol();
                break;

            case EnemyState.Chase:
                UpdateChase();
                break;
        }
    }

    public void StartChase(float minimumChaseTime = 0f)
    {
        EnsureChaseTarget();

        if (chaseTarget == null || currentState == EnemyState.Locked)
            return;

        StopLookAround();
        currentState = EnemyState.Chase;
        lostSightTimer = 0f;
        minimumChaseTimer = Mathf.Max(minimumChaseTimer, minimumChaseTime);

        agent.speed = chaseSpeed;
        agent.isStopped = false;
        agent.updateRotation = true;
        agent.SetDestination(chaseTarget.position);
    }

    public void SetMovementLocked(bool locked)
    {
        if (locked)
        {
            EnterLocked();
            return;
        }

        ExitLocked();
    }

    private void UpdatePatrol()
    {
        if (!HasWaypoints)
            return;

        if (!agent.pathPending && agent.remainingDistance <= stopAtDistance)
            EnterLookAround();
    }

    private void UpdateChase()
    {
        EnsureChaseTarget();

        if (chaseTarget == null)
        {
            EnterPatrolFromNearestWaypoint();
            return;
        }

        agent.SetDestination(chaseTarget.position);

        if (CanSeeChaseTarget())
            lostSightTimer = 0f;
        else
            lostSightTimer += Time.deltaTime;

        if (minimumChaseTimer > 0f)
            minimumChaseTimer -= Time.deltaTime;

        if (minimumChaseTimer <= 0f && lostSightTimer >= returnToPatrolAfterLostSightTime)
            EnterPatrolFromNearestWaypoint();
    }

    private void EnterPatrol(bool useNearestWaypoint)
    {
        StopLookAround();
        currentState = EnemyState.Patrol;
        lostSightTimer = 0f;
        minimumChaseTimer = 0f;

        agent.speed = patrolSpeed;
        agent.isStopped = false;
        agent.updateRotation = true;

        if (!HasWaypoints)
            return;

        if (useNearestWaypoint)
            currentWaypointIndex = GetNearestWaypointIndex();

        GoToCurrentWaypoint();
    }

    private void EnterPatrolFromNearestWaypoint()
    {
        EnterPatrol(true);
    }

    private void EnterLookAround()
    {
        if (currentState == EnemyState.LookAround)
            return;

        StopLookAround();
        currentState = EnemyState.LookAround;
        lookAroundCoroutine = StartCoroutine(LookAroundRoutine());
    }

    private void EnterLocked()
    {
        if (currentState == EnemyState.Locked)
            return;

        stateBeforeLock = currentState;
        StopLookAround();
        currentState = EnemyState.Locked;
        agent.isStopped = true;
        agent.updateRotation = false;
    }

    private void ExitLocked()
    {
        if (currentState != EnemyState.Locked)
            return;

        EnemyState resumeState = stateBeforeLock;
        currentState = resumeState;
        agent.isStopped = false;
        agent.updateRotation = true;

        if (resumeState == EnemyState.Chase)
            StartChase(minimumChaseTimer);
        else if (resumeState == EnemyState.LookAround)
            AdvanceToNextWaypoint();
        else
            EnterPatrol(false);
    }

    private IEnumerator LookAroundRoutine()
    {
        agent.isStopped = true;
        updateRotationBeforeManualTurn = agent.updateRotation;
        agent.updateRotation = false;

        yield return new WaitForSeconds(patrolWaitTime);
        yield return LookAround();

        agent.updateRotation = updateRotationBeforeManualTurn;
        agent.isStopped = false;
        lookAroundCoroutine = null;
        AdvanceToNextWaypoint();
    }

    private void StopLookAround()
    {
        if (lookAroundCoroutine != null)
        {
            StopCoroutine(lookAroundCoroutine);
            lookAroundCoroutine = null;
        }

        if (currentState == EnemyState.LookAround)
        {
            agent.updateRotation = updateRotationBeforeManualTurn;
            agent.isStopped = false;
        }
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
        while (Quaternion.Angle(transform.rotation, targetRotation) > 0.1f)
        {
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation,
                targetRotation,
                lookTurnSpeed * Time.deltaTime);

            yield return null;
        }

        transform.rotation = targetRotation;
    }

    private void AdvanceToNextWaypoint()
    {
        currentState = EnemyState.Patrol;

        if (!HasWaypoints)
            return;

        currentWaypointIndex = (currentWaypointIndex + 1) % waypoints.Length;
        GoToCurrentWaypoint();
    }

    private void GoToCurrentWaypoint()
    {
        if (!HasWaypoints || waypoints[currentWaypointIndex] == null)
            return;

        agent.SetDestination(waypoints[currentWaypointIndex].position);
    }

    private int GetNearestWaypointIndex()
    {
        int nearestIndex = 0;
        float nearestDistance = float.MaxValue;

        for (int i = 0; i < waypoints.Length; i++)
        {
            if (waypoints[i] == null)
                continue;

            float distance = Vector3.SqrMagnitude(transform.position - waypoints[i].position);
            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                nearestIndex = i;
            }
        }

        return nearestIndex;
    }

    private bool CanSeeChaseTarget()
    {
        if (fieldOfView == null)
            return false;

        return fieldOfView.canSeePlayer;
    }

    private void EnsureChaseTarget()
    {
        if (chaseTarget != null)
            return;

        if (fieldOfView != null && fieldOfView.playerRef != null)
        {
            chaseTarget = fieldOfView.playerRef.transform;
            return;
        }

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
            chaseTarget = player.transform;
    }
}
