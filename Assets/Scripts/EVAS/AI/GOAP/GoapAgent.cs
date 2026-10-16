using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Action_CallBackup))]
[RequireComponent(typeof(Action_AttackTarget))]
[RequireComponent(typeof(Action_ChaseTarget))]
public sealed class GoapAgent : MonoBehaviour
{
    [Header("Planning")]
    [SerializeField, Min(0.05f)] private float replanInterval = 0.2f;
    [SerializeField, Range(0f, 1f)] private float lowHealthThreshold = 0.35f;
    [SerializeField] private bool debugPlans;

    private readonly GoapPlanner planner = new GoapPlanner();
    private readonly List<GoapGoal> goals = new List<GoapGoal>();
    private readonly List<GoapAction> actions = new List<GoapAction>();
    private Queue<GoapAction> currentPlan;
    private float nextReplanTime;
    private bool combatActive;
    private bool backupCalled;

    public EnemyController Controller { get; private set; }
    public FieldOfView FieldOfView { get; private set; }
    public EnemyHealth Health { get; private set; }
    public Transform Target => FieldOfView != null && FieldOfView.playerRef != null ? FieldOfView.playerRef.transform : null;
    public bool IsCombatActive => combatActive;

    private void Awake()
    {
        Controller = GetComponent<EnemyController>();
        FieldOfView = GetComponent<FieldOfView>();
        Health = GetComponent<EnemyHealth>();
        GetComponents(actions);
        foreach (GoapAction action in actions) action.EnsureConfigured();
        goals.Add(new GoapGoal("Eliminate Player", 100, new GoapState().Set("targetKilled", true)));
        goals.Add(new GoapGoal("Call Backup", 80, new GoapState().Set("backupCalled", true)));
    }

    public void SetCombatActive(bool active)
    {
        if (combatActive == active)
        {
            if (!active) backupCalled = false;
            return;
        }

        combatActive = active;
        currentPlan = null;
        foreach (GoapAction action in actions) action?.ResetAction();
        if (!active) backupCalled = false;
        Controller?.SetGoapControl(active);
    }

    private void Update()
    {
        if (!combatActive) return;
        if (Time.time >= nextReplanTime && (currentPlan == null || currentPlan.Count == 0)) Replan();
        ExecuteCurrentAction();
    }

    private void Replan()
    {
        nextReplanTime = Time.time + replanInterval;
        GoapState world = BuildWorldState();
        foreach (GoapGoal goal in GetGoalsFor(world))
        {
            Queue<GoapAction> plan = planner.CreatePlan(this, world, goal, actions);
            if (plan == null || plan.Count == 0) continue;
            currentPlan = plan;
            if (debugPlans) Debug.Log($"{name} GOAP: {goal.Name} ({currentPlan.Peek().GetType().Name})", this);
            return;
        }
    }

    private void ExecuteCurrentAction()
    {
        if (currentPlan == null || currentPlan.Count == 0) return;
        GoapAction action = currentPlan.Peek();
        GoapState world = BuildWorldState();
        if (!world.Satisfies(action.Preconditions) || !action.CheckProceduralPrecondition(this))
        {
            action.ResetAction();
            currentPlan = null;
            return;
        }

        if (!action.Perform(this))
        {
            action.ResetAction();
            currentPlan = null;
            return;
        }

        if (!action.IsDone) return;
        if (action.Effects.Get("backupCalled")) backupCalled = true;
        action.ResetAction();
        currentPlan.Dequeue();
        if (currentPlan.Count == 0) currentPlan = null;
    }

    private GoapState BuildWorldState()
    {
        bool inSight = FieldOfView != null && FieldOfView.canSeePlayer;
        bool inRange = Target != null && Vector3.SqrMagnitude(transform.position - Target.position) <= 225f;
        bool lowHealth = Health != null && Health.HealthPercent <= lowHealthThreshold;
        return new GoapState()
            .Set("targetAlerted", true)
            .Set("targetInSight", inSight)
            .Set("targetInRange", inRange)
            .Set("targetKilled", false)
            .Set("backupCalled", backupCalled)
            .Set("isLowHealth", lowHealth);
    }

    private IEnumerable<GoapGoal> GetGoalsFor(GoapState world)
    {
        if (!world.Get("backupCalled")) yield return goals[1];
        yield return goals[0];
    }
}
