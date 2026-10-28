using System;

[Serializable]
public sealed class GoapGoal
{
    public string Name { get; }
    public int Priority { get; }
    public GoapState DesiredState { get; }

    public GoapGoal(string name, int priority, GoapState desiredState)
    {
        Name = name;
        Priority = priority;
        DesiredState = desiredState;
    }
}
