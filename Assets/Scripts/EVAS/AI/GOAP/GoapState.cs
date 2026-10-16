using System;
using System.Collections.Generic;

/// <summary>A small immutable-by-convention bag of facts used by the GOAP planner.</summary>
[Serializable]
public sealed class GoapState : IEquatable<GoapState>
{
    private readonly Dictionary<string, bool> facts;

    public GoapState()
    {
        facts = new Dictionary<string, bool>(StringComparer.Ordinal);
    }

    private GoapState(Dictionary<string, bool> source)
    {
        facts = new Dictionary<string, bool>(source, StringComparer.Ordinal);
    }

    public bool Get(string key, bool defaultValue = false) => facts.TryGetValue(key, out bool value) ? value : defaultValue;

    public GoapState Set(string key, bool value)
    {
        facts[key] = value;
        return this;
    }

    public GoapState Clone() => new GoapState(facts);

    public bool Satisfies(GoapState required)
    {
        foreach (KeyValuePair<string, bool> pair in required.facts)
        {
            if (!facts.TryGetValue(pair.Key, out bool value) || value != pair.Value)
                return false;
        }

        return true;
    }

    public GoapState Apply(GoapState effects)
    {
        GoapState result = Clone();
        foreach (KeyValuePair<string, bool> pair in effects.facts)
            result.facts[pair.Key] = pair.Value;
        return result;
    }

    public bool Equals(GoapState other)
    {
        if (ReferenceEquals(this, other)) return true;
        if (other == null || facts.Count != other.facts.Count) return false;
        foreach (KeyValuePair<string, bool> pair in facts)
            if (!other.facts.TryGetValue(pair.Key, out bool value) || value != pair.Value) return false;
        return true;
    }

    public override bool Equals(object obj) => Equals(obj as GoapState);

    public override int GetHashCode()
    {
        int hash = 17;
        foreach (KeyValuePair<string, bool> pair in facts)
            hash = (hash * 31) ^ pair.Key.GetHashCode() ^ pair.Value.GetHashCode();
        return hash;
    }
}
