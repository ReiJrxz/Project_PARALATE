using UnityEngine;

public abstract class GoapAction : MonoBehaviour
{
    [SerializeField, Min(0f)] private float baseCost = 1f;

    public GoapState Preconditions { get; } = new GoapState();
    public GoapState Effects { get; } = new GoapState();
    public virtual float Cost => baseCost;
    public virtual bool IsDone { get; protected set; }

    private bool configured;

    protected virtual void Awake() => EnsureConfigured();

    protected abstract void Configure();
    public void EnsureConfigured()
    {
        if (configured) return;
        Configure();
        configured = true;
    }
    public virtual bool CheckProceduralPrecondition(GoapAgent agent) => true;
    public abstract bool Perform(GoapAgent agent);
    public virtual void ResetAction() => IsDone = false;
}
