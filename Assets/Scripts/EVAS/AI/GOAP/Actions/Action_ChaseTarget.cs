using UnityEngine;

public sealed class Action_ChaseTarget : GoapAction
{
    private const float ArrivalDistance = 0.75f;
    private const float LostTargetTimeout = 5f;

    private Vector3 lastKnownPosition;
    private float pursuitStartedAt;
    private bool hasDestination;

    protected override void Configure()
    {
        Preconditions.Set("targetInSight", false);
        Effects.Set("targetInRange", true).Set("targetInSight", true);
    }

    public override bool Perform(GoapAgent agent)
    {
        AlertState alert = agent.GetComponent<AlertState>();
        if (alert == null || agent.Controller == null)
            return false;

        if (!hasDestination)
        {
            pursuitStartedAt = Time.time;
            hasDestination = true;
        }

        lastKnownPosition = alert.LastKnownPlayerPosition;
        agent.Controller.MoveToCombatPoint(lastKnownPosition);
        if (agent.FieldOfView != null && agent.FieldOfView.canSeePlayer)
        {
            IsDone = true;
            return true;
        }

        if (HasReachedLastKnownPosition(agent) || Time.time >= pursuitStartedAt + LostTargetTimeout)
        {
            alert.BeginLostTargetSearch();
            return false;
        }

        return true;
    }

    public override void ResetAction()
    {
        base.ResetAction();
        hasDestination = false;
    }

    private bool HasReachedLastKnownPosition(GoapAgent agent)
    {
        Vector3 offset = agent.transform.position - lastKnownPosition;
        offset.y = 0f;
        return offset.sqrMagnitude <= ArrivalDistance * ArrivalDistance;
    }
}
