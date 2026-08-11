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

    private NavMeshAgent agent;
    private int currentWaypointIndex;
    private bool isWaiting;

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
        if(isWaiting) return;
        if(!agent.pathPending && agent.remainingDistance < stopAtDistance)
        {
            StartCoroutine(WaitPatrolPoint());
        }
    }

    private IEnumerator WaitPatrolPoint()
    {
        isWaiting = true;
        agent.isStopped = true;

        yield return new WaitForSeconds(patrolWaitsTime);

        agent.isStopped = false;
        GotoNextWaypoint();
        isWaiting = false;
    }

    private void GotoNextWaypoint()
    {
        if(Waypoints.Length == 0)
            return;
        agent.SetDestination(Waypoints[currentWaypointIndex].position);
        currentWaypointIndex = (currentWaypointIndex + 1) % Waypoints.Length;
    }
}
