using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// จุดเกิด/จุด Checkpoint ของผู้เล่นใน Scene (เช่น จุดเริ่มของแต่ละห้อง 1-1, 1-2, ...)
/// - โหลด Scene แล้ว LevelManager จะวางผู้เล่นที่จุดนี้ตาม spawnId ที่ขอมา (ถ้าไม่ขอ ใช้ตัวที่ order น้อยสุด)
/// - ผู้เล่นมาถึงจุดนี้ (วาร์ปหรือโหลด) = เป็น Checkpoint ปัจจุบัน + ปลดล็อกให้เลือกเป็นจุดเกิดที่ Base
/// </summary>
public class SpawnPoint : MonoBehaviour
{
    public static readonly List<SpawnPoint> All = new List<SpawnPoint>();

    [Tooltip("รหัสจุดเกิด เช่น 1-1, 1-2 ต้องไม่ซ้ำกันใน Scene และห้ามมีอักขระ |")]
    public string spawnId = "1-1";

    public string displayName = "1-1";

    [Tooltip("ใช้เรียงลำดับ ตัวที่น้อยที่สุดคือจุดเริ่มต้นของด่าน")]
    public int order = 0;

    [Tooltip("ผู้เล่นตายแล้ว Retry จะกลับมาเกิดที่จุดนี้ (หลังจากเคยมาถึงจุดนี้แล้ว)")]
    public bool isCheckpoint = true;

    [Tooltip("มาถึงแล้วปลดล็อกให้เลือกเป็นจุดเกิดจากหน้าเลือกด่านที่ Base")]
    public bool unlockForSelection = true;

    private void OnEnable()
    {
        if (!All.Contains(this))
            All.Add(this);
    }

    private void OnDisable()
    {
        All.Remove(this);
    }

    public static SpawnPoint Find(string id, Scene scene)
    {
        if (string.IsNullOrEmpty(id))
            return null;

        for (int i = 0; i < All.Count; i++)
        {
            if (All[i].gameObject.scene == scene && All[i].spawnId == id)
                return All[i];
        }

        return null;
    }

    /// <summary>จุดเริ่มต้นของด่าน = ตัวที่ order น้อยที่สุดใน Scene นั้น</summary>
    public static SpawnPoint GetDefault(Scene scene)
    {
        SpawnPoint best = null;

        for (int i = 0; i < All.Count; i++)
        {
            if (All[i].gameObject.scene != scene)
                continue;

            if (best == null || All[i].order < best.order)
                best = All[i];
        }

        return best;
    }

    public static SpawnPoint FindNearest(Scene scene, Vector3 position)
    {
        SpawnPoint best = null;
        float bestSqrDistance = float.MaxValue;

        for (int i = 0; i < All.Count; i++)
        {
            if (All[i].gameObject.scene != scene)
                continue;

            float sqrDistance = (All[i].transform.position - position).sqrMagnitude;
            if (sqrDistance < bestSqrDistance)
            {
                bestSqrDistance = sqrDistance;
                best = All[i];
            }
        }

        return best;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = isCheckpoint ? Color.green : Color.cyan;
        Gizmos.DrawWireSphere(transform.position + Vector3.up * 0.5f, 0.5f);
        Gizmos.DrawRay(transform.position + Vector3.up * 0.5f, transform.forward * 1.2f);
    }
}
