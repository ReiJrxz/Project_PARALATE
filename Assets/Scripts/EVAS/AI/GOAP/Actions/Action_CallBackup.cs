using System.Collections.Generic;
using UnityEngine;

public sealed class Action_CallBackup : GoapAction
{
    private const int MaxNearbyColliders = 32;
    private static readonly Collider[] NearbyColliders = new Collider[MaxNearbyColliders];

    [SerializeField, Min(0f)] private float radioChannelDuration = 0.8f;
    [SerializeField, Min(0.1f)] private float fallbackRadius = 15f;

    private readonly HashSet<AlertState> notifiedEnemies = new HashSet<AlertState>();
    private EnemyController lockedController;
    private float channelStartTime;
    private bool isChanneling;

    protected override void Configure()
    {
        Preconditions.Set("targetAlerted", true).Set("backupCalled", false);
        Effects.Set("backupCalled", true);
    }

    public override bool CheckProceduralPrecondition(GoapAgent agent)
    {
        return agent != null && agent.GetComponent<AlertState>() != null;
    }

    public override bool Perform(GoapAgent agent)
    {
        AlertState alert = agent.GetComponent<AlertState>();
        if (alert == null)
            return false;

        if (!isChanneling)
        {
            isChanneling = true;
            channelStartTime = Time.time;
            lockedController = agent.Controller;
            lockedController?.SetMovementLocked(true);
        }

        if (Time.time < channelStartTime + radioChannelDuration)
            return true;

        RoomZone zone = RoomZone.GetFor(agent);
        if (zone != null)
            zone.BroadcastAlert(alert, alert.LastKnownPlayerPosition);
        else
            BroadcastByRadius(agent.transform.position, alert, alert.LastKnownPlayerPosition);

        IsDone = true;
        return true;
    }

    public override void ResetAction()
    {
        base.ResetAction();
        lockedController?.SetMovementLocked(false);
        lockedController = null;
        isChanneling = false;
        notifiedEnemies.Clear();
    }

    private void BroadcastByRadius(Vector3 origin, AlertState sender, Vector3 sourcePosition)
    {
        int count = Physics.OverlapSphereNonAlloc(origin, fallbackRadius, NearbyColliders, ~0, QueryTriggerInteraction.Collide);
        for (int i = 0; i < count; i++)
        {
            AlertState enemy = NearbyColliders[i].GetComponentInParent<AlertState>();
            if (enemy == null || enemy == sender || !notifiedEnemies.Add(enemy))
                continue;

            enemy.ReceiveReinforcement(sourcePosition);
        }
    }
}
