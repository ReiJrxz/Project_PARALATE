using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(BoxCollider), typeof(Rigidbody))]
public sealed class RoomZone : MonoBehaviour
{
    private readonly HashSet<AlertState> enemies = new HashSet<AlertState>();
    private readonly HashSet<Transform> players = new HashSet<Transform>();
    private static readonly Dictionary<AlertState, RoomZone> EnemyZones = new Dictionary<AlertState, RoomZone>();

    public static RoomZone GetFor(Component member)
    {
        AlertState enemy = member == null ? null : member.GetComponent<AlertState>();
        return enemy != null && EnemyZones.TryGetValue(enemy, out RoomZone zone) ? zone : null;
    }

    private void Awake()
    {
        BoxCollider box = GetComponent<BoxCollider>();
        box.isTrigger = true;
        Rigidbody body = GetComponent<Rigidbody>();
        body.isKinematic = true;
        body.useGravity = false;
    }

    private void Start()
    {
        BoxCollider box = GetComponent<BoxCollider>();
        Vector3 center = transform.TransformPoint(box.center);
        Vector3 halfExtents = Vector3.Scale(box.size * 0.5f, transform.lossyScale);
        foreach (Collider occupant in Physics.OverlapBox(center, halfExtents, transform.rotation, ~0, QueryTriggerInteraction.Collide))
        {
            Register(occupant.GetComponentInParent<AlertState>());
            if (occupant.CompareTag("Player")) players.Add(occupant.transform);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        Register(other.GetComponentInParent<AlertState>());
        if (other.CompareTag("Player")) players.Add(other.transform);
    }

    private void OnTriggerExit(Collider other)
    {
        AlertState enemy = other.GetComponentInParent<AlertState>();
        if (enemy != null)
        {
            enemies.Remove(enemy);
            if (EnemyZones.TryGetValue(enemy, out RoomZone zone) && zone == this) EnemyZones.Remove(enemy);
        }
        if (other.CompareTag("Player")) players.Remove(other.transform);
    }

    public void BroadcastAlert(AlertState sender, Vector3 sourcePosition)
    {
        foreach (AlertState enemy in enemies)
            if (enemy != null && enemy != sender) enemy.ReceiveReinforcement(sourcePosition);
    }

    private void Register(AlertState enemy)
    {
        if (enemy != null) enemies.Add(enemy);
        if (enemy != null) EnemyZones[enemy] = this;
    }
}
