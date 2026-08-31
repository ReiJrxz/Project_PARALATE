using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class EnemyRoaming : MonoBehaviour
{
    [Header("Roaming Settings")]
    public Transform[] waypoints; // จุดที่ต้องการให้เดินไป
    public float waitTimeAtWaypoint = 2f; // เวลายืนรอก่อนเดินไปจุดต่อไป

    private NavMeshAgent agent;
    private int currentWaypointIndex;
    private float waitTimer;
    private bool isWaiting;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();

        // ตรวจสอบว่ามีจุด Waypoint หรือไม่ ถ้ามีให้เริ่มเดินไปจุดแรก
        if (waypoints.Length > 0)
        {
            MoveToNextWaypoint();
        }
        else
        {
            Debug.LogWarning("ไม่มี Waypoint! กรุณาใส่จุดอ้างอิงใน Inspector");
        }
    }

    void Update()
    {
        // ถ้าไม่มี Waypoint เลยให้ออกจากการทำงาน
        if (waypoints.Length == 0) return;

        if (isWaiting)
        {
            // นับเวลารอ
            waitTimer -= Time.deltaTime;
            if (waitTimer <= 0f)
            {
                isWaiting = false;
                MoveToNextWaypoint();
            }
        }
        else
        {
            // เช็คว่าศัตรูเดินใกล้ถึงจุดหมายหรือยัง (ระยะทางเหลือน้อยกว่า 0.5)
            // !agent.pathPending คือเช็คว่าระบบคำนวณเส้นทางเสร็จแล้ว
            if (!agent.pathPending && agent.remainingDistance < 0.5f)
            {
                // ถึงจุดหมายแล้ว ให้เริ่มรอ
                isWaiting = true;
                waitTimer = waitTimeAtWaypoint;
            }
        }
    }

    void MoveToNextWaypoint()
    {
        // ถ้ามีแค่จุดเดียว ก็ให้เดินไปจุดนั้น
        if (waypoints.Length <= 1)
        {
            agent.SetDestination(waypoints[0].position);
            return;
        }

        // สุ่มตัวเลข Index ใหม่
        int newIndex = Random.Range(0, waypoints.Length);

        // วนลูปสุ่มใหม่ถัามันได้จุดเดิม (จะได้ไม่ยืนรอที่เดิมซ้ำสองรอบ)
        while (newIndex == currentWaypointIndex)
        {
            newIndex = Random.Range(0, waypoints.Length);
        }

        currentWaypointIndex = newIndex;
        agent.SetDestination(waypoints[currentWaypointIndex].position);
    }
}