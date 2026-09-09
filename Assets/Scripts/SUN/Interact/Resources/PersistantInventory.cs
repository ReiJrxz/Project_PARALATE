using System.Collections.Generic;
using UnityEngine;

public class PersistentInventory : MonoBehaviour
{
    public static PersistentInventory Instance { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void EnsureExists()
    {
        if (Instance != null) return;
        GameObject go = new GameObject("RunInventory (Auto)");
        go.AddComponent<RunInventory>();
    }
    private Dictionary<ResourceData, int> owned = new Dictionary<ResourceData, int>();

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
        if (!owned.ContainsKey(data))
            owned[data] = 0;
        owned[data] += amount;
    }

    public int GetAmount(ResourceData data) =>
        owned.TryGetValue(data, out int value) ? value : 0;
}