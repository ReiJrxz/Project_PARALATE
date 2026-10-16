using UnityEngine;

public sealed class Action_AttackTarget : GoapAction
{
    private EnemyShooting enemyShooting;

    protected override void Configure()
    {
        Preconditions.Set("targetInSight", true).Set("targetInRange", true);
        Effects.Set("targetKilled", true);
    }

    public override bool CheckProceduralPrecondition(GoapAgent agent)
    {
        if (agent == null || agent.Target == null || agent.Controller == null || agent.FieldOfView == null || !agent.FieldOfView.canSeePlayer)
            return false;

        if (enemyShooting == null)
            enemyShooting = agent.GetComponent<EnemyShooting>();

        return enemyShooting != null && enemyShooting.isActiveAndEnabled;
    }

    public override bool Perform(GoapAgent agent)
    {
        agent.Controller.RotateYawTowards(agent.Target.position, 10f);
        IsDone = enemyShooting.TryStartReflexShot();
        return true;
    }
}
