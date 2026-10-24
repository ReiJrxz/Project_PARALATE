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
        if (data == null)
            return;

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

    /// <summary>ย้ายของที่เก็บในรอบนี้เข้าคลังถาวร (เรียกเมื่อจบด่านสำเร็จเท่านั้น)</summary>
    public void CommitToPersistent()
    {
        if (PersistentInventory.Instance == null)
        {
            // ไม่ล้างของในรอบนี้ เพื่อไม่ให้ของหายถ้าคลังถาวรยังไม่พร้อม
            Debug.LogError("PersistentInventory.Instance is null — cannot commit run inventory.");
            return;
        }

        foreach (var pair in collected)
            PersistentInventory.Instance.Add(pair.Key, pair.Value);

        ClearRun();
    }
}