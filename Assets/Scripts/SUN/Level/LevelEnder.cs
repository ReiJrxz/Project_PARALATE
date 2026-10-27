using UnityEngine;

/// <summary>
/// วาร์ปอเนกประสงค์ (ชื่อคลาสเดิมคือ LevelExitTrigger จึงใช้กับ Trigger เก่าใน Scene ได้เลย
/// ค่าเริ่มต้น = จบด่านสำเร็จแล้วกลับ Base เหมือนเดิม)
/// </summary>
[RequireComponent(typeof(Collider))]
public class LevelExitTrigger : MonoBehaviour
{
    public enum WarpMode
    {
        CompleteLevel = 0,   // จบด่านสำเร็จ: เก็บของถาวร แล้วกลับ Base (พฤติกรรมเดิม)
        TeleportToSpawn = 1, // วาร์ปไปจุดเกิดใน Scene เดียวกัน (เช่น 1-1 → 1-2) และตั้งเป็น Checkpoint ใหม่
        LoadScene = 2,       // โหลด Scene อื่นแล้วเกิดที่จุดเกิดที่ระบุ
        ReturnToBase = 3     // กลับ Base ทันที โดยไม่นับว่าจบด่าน (ของที่เก็บมาไม่ถูกบันทึก)
    }

    public WarpMode mode = WarpMode.CompleteLevel;

    [Header("TeleportToSpawn")]
    public SpawnPoint targetSpawn;

    [Header("LoadScene")]
    [Tooltip("ชื่อ Scene (ต้องอยู่ใน Build Settings)")]
    public string targetScene;
    [Tooltip("ว่าง = จุดเริ่มต้นของด่าน")]
    public string targetSpawnId;

    [Tooltip("ใช้ได้ครั้งเดียว (ใช้กับโหมดวาร์ปในด่านเท่านั้น โหมดโหลด Scene จะใช้ครั้งเดียวอยู่แล้ว)")]
    public bool oneShot = true;

    private bool busy;

    void OnTriggerEnter(Collider other)
    {
        if (busy || !other.CompareTag("Player"))
            return;

        LevelManager manager = LevelManager.Instance;
        if (manager == null || manager.IsTransitioning)
            return;

        switch (mode)
        {
            case WarpMode.CompleteLevel:
                busy = true;
                manager.CompleteLevel();
                break;

            case WarpMode.ReturnToBase:
                busy = true;
                manager.AbandonRun();
                manager.LoadBase();
                break;

            case WarpMode.LoadScene:
                if (string.IsNullOrEmpty(targetScene))
                {
                    Debug.LogWarning($"[{name}] โหมด LoadScene แต่ยังไม่ได้ใส่ Target Scene", this);
                    return;
                }

                busy = true;
                manager.LoadScene(targetScene, targetSpawnId);
                break;

            case WarpMode.TeleportToSpawn:
                if (targetSpawn == null)
                {
                    Debug.LogWarning($"[{name}] โหมด TeleportToSpawn แต่ยังไม่ได้ใส่ Target Spawn", this);
                    return;
                }

                busy = true;
                manager.TeleportPlayerTo(targetSpawn, () =>
                {
                    if (!oneShot)
                        busy = false;
                });
                break;
        }
    }
}
