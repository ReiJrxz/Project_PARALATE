using UnityEngine;
using System.Collections.Generic;

public class RunInventory : MonoBehaviour
{
    public static RunInventory Instance { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void EnsureExists()
    {
        if (Instance != null) return;
        GameObject go = new GameObject("RunInventory (Auto)");
        go.AddComponent<RunInventory>();
    }

    private Dictionary<ResourceData, int> collected = new Dictionary<ResourceData, int>();

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void Add(ResourceData data, int amount)
    {
        if (!collected.ContainsKey(data))
            collected[data] = 0;
        collected[data] += amount;
    }

    public int GetAmount(ResourceData data) =>
        collected.TryGetValue(data, out int value) ? value : 0;

    public void ClearRun()
    {
        collected.Clear();
    }

    public void CommitToPersistent()
    {
        foreach (var pair in collected)
            PersistentInventory.Instance.Add(pair.Key, pair.Value);

        ClearRun();
    }
}