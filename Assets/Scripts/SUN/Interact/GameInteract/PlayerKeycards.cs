using System;
using UnityEngine;

/// <summary>
/// เก็บระดับ Keycard สูงสุดที่ผู้เล่นถือ (0 = ไม่มีบัตร)
/// อยู่บนตัวผู้เล่นของแต่ละ Scene จึงเริ่มใหม่ทุกรอบ/ทุกครั้งที่โหลดด่าน
/// KeycardPickup จะเพิ่มคอมโพเนนต์นี้ให้ผู้เล่นเองถ้ายังไม่มี ไม่ต้องเพิ่มเอง
/// </summary>
public class PlayerKeycards : MonoBehaviour
{
    public static PlayerKeycards Instance { get; private set; }

    /// <summary>ระดับ Keycard สูงสุดที่มีตอนนี้ (0 ถ้าไม่มีผู้เล่น/ไม่มีบัตร)</summary>
    public static int CurrentLevel => Instance != null ? Instance.HighestLevel : 0;

    public int HighestLevel { get; private set; }

    public event Action<int> OnHighestLevelChanged;

    private void Awake()
    {
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    /// <summary>ได้รับ Keycard ระดับนี้ (บัตรสูงกว่าใช้แทนบัตรต่ำกว่าได้ จึงเก็บแค่ระดับสูงสุด)</summary>
    public void Grant(int level)
    {
        if (level <= HighestLevel)
            return;

        HighestLevel = level;
        OnHighestLevelChanged?.Invoke(HighestLevel);
    }

    public bool HasLevel(int requiredLevel)
    {
        return requiredLevel <= 0 || HighestLevel >= requiredLevel;
    }

    public static PlayerKeycards GetOrAdd(GameObject player)
    {
        PlayerKeycards cards = player.GetComponentInParent<PlayerKeycards>();
        return cards != null ? cards : player.AddComponent<PlayerKeycards>();
    }
}
