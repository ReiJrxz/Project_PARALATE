using System.Collections.Generic;

public sealed class GoapPlanner
{
    private sealed class Node
    {
        public GoapState State;
        public Node Parent;
        public GoapAction Action;
        public float Cost;
    }

    public Queue<GoapAction> CreatePlan(GoapAgent agent, GoapState start, GoapGoal goal, IReadOnlyList<GoapAction> actions)
    {
        List<Node> open = new List<Node> { new Node { State = start, Cost = 0f } };
        Dictionary<GoapState, float> bestCosts = new Dictionary<GoapState, float> { [start] = 0f };

        while (open.Count > 0)
        {
            int cheapest = 0;
            for (int i = 1; i < open.Count; i++)
                if (open[i].Cost < open[cheapest].Cost) cheapest = i;

            Node current = open[cheapest];
            open.RemoveAt(cheapest);
            if (current.State.Satisfies(goal.DesiredState))
                return BuildQueue(current);

            foreach (GoapAction action in actions)
            {
                if (action == null || !action.isActiveAndEnabled || !current.State.Satisfies(action.Preconditions))
                    continue;

                GoapState nextState = current.State.Apply(action.Effects);
                float nextCost = current.Cost + action.Cost;
                if (bestCosts.TryGetValue(nextState, out float knownCost) && knownCost <= nextCost)
                    continue;

                bestCosts[nextState] = nextCost;
                open.Add(new Node { State = nextState, Parent = current, Action = action, Cost = nextCost });
            }
        }

        return null;
    }

    private static Queue<GoapAction> BuildQueue(Node node)
    {
        Stack<GoapAction> reversed = new Stack<GoapAction>();
        while (node.Parent != null)
        {
            reversed.Push(node.Action);
            node = node.Parent;
        }

        return new Queue<GoapAction>(reversed);
    }
}
