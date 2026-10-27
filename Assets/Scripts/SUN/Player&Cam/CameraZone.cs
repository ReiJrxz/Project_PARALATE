
using System.Collections.Generic;
using UnityEngine;
using Unity.Cinemachine;

[RequireComponent(typeof(BoxCollider))]
public class CameraZone : MonoBehaviour
{
    [Header("กล้องประจำโซนนี้")]
    public CinemachineCamera virtualCamera;

    [Header("Priority")]
    // ตัวนับการเปิดกล้อง: โซนที่เปิดทีหลังจะได้ priority สูงกว่าโซนที่ทับซ้อนกันเสมอ
    // priority ของกล้องห้อง = activePriority + ตัวนับ (วนกลับเมื่อถึง ROOM_TIER_MAX)
    // ต้องต่ำกว่ากล้องพิเศษ เช่น ActionCamera (1000) และ First Person Scope (2000)
    protected static int activationCounter = 0;
    protected const int ROOM_TIER_MAX = 900;

    public int inactivePriority = 0;
    public int activePriority = 20;

    [Header("การเปลี่ยนกล้องเมื่อเข้าห้องนี้ (Blend)")]
    [Tooltip("ปิด = ใช้ Default Blend ของ CinemachineBrain / เปิด = ห้องนี้ใช้ Blend ตามด้านล่าง " +
             "(เช่น ห้องนี้ Cut ห้องอื่น Ease In Out) ถ้าตั้ง Custom Blends ใน Brain ไว้ ค่าใน Custom Blends จะมาก่อน")]
    public bool overrideBlend = false;
    public CinemachineBlendDefinition.Styles blendStyle = CinemachineBlendDefinition.Styles.EaseInOut;
    [Min(0f)] public float blendTime = 1.5f;

    /// <summary>LevelManager เปิดไว้ตอนวาร์ป เพื่อให้กล้องตัดทันทีตอนจอดำ ไม่ต้อง Blend</summary>
    public static bool ForceCut;

    [Tooltip("เปิดเพื่อดู Console ว่าโซนนี้เปิด/ปิดกล้องตอนไหน (ใช้ไล่ปัญหากล้องค้าง)")]
    public bool debugLog = false;

    // เก็บค่า Default Blend เดิมของ Brain ไว้ ให้ห้องที่ไม่ได้ Override กลับมาใช้ค่าเดิม
    private static CinemachineBrain trackedBrain;
    private static CinemachineBlendDefinition originalBlend;

    // ทุกโซนที่เปิดอยู่ ใช้ตอนผู้เล่นถูกวาร์ป เพื่อเช็กใหม่ว่าตอนนี้ควรใช้กล้องโซนไหน
    private static readonly List<CameraZone> activeZones = new List<CameraZone>();

    protected BoxCollider zoneCollider;

    private readonly HashSet<Collider> playerColliders =
        new HashSet<Collider>();

    protected virtual void OnEnable()
    {
        if (!activeZones.Contains(this))
            activeZones.Add(this);
    }

    protected virtual void Start()
    {
        zoneCollider = GetComponent<BoxCollider>();

        if (zoneCollider != null)
            zoneCollider.isTrigger = true;

        if (virtualCamera == null)
        {
            Debug.LogWarning($"{name}: ยังไม่ได้ผูก virtualCamera");
            return;
        }

        virtualCamera.Priority.Value = inactivePriority;

        CheckIfPlayerAlreadyInside();
    }

    protected virtual void CheckIfPlayerAlreadyInside()
    {
        if (zoneCollider == null)
            return;

        Vector3 center =
            transform.TransformPoint(zoneCollider.center);

        Vector3 halfExtents = Vector3.Scale(
            zoneCollider.size * 0.5f,
            transform.lossyScale);

        Collider[] overlaps = Physics.OverlapBox(
            center, halfExtents, transform.rotation);

        foreach (Collider col in overlaps)
        {
            if (col.CompareTag("Player"))
                playerColliders.Add(col);
        }

        if (playerColliders.Count > 0)
            ActivateCamera();
    }

    // ตั้ง Blend ของ Brain ก่อนเปลี่ยนกล้อง (Brain จะอ่านค่าตอนเห็นว่ากล้องที่ Priority สูงสุดเปลี่ยน)
    protected void ApplyBlend()
    {
        if (!ForceCut && !overrideBlend && trackedBrain == null)
            return; // ยังไม่เคยแตะ Blend เลย ไม่ต้องยุ่งกับ Brain

        if (CinemachineBrain.ActiveBrainCount == 0)
            return;

        CinemachineBrain brain = CinemachineBrain.GetActiveBrain(0);
        if (brain == null)
            return;

        // Brain ใหม่ (เช่น โหลด Scene ใหม่): จำค่า Default Blend เดิมของตัวนั้นไว้ก่อนเปลี่ยน
        if (brain != trackedBrain)
        {
            trackedBrain = brain;
            originalBlend = brain.DefaultBlend;
        }

        if (ForceCut)
            brain.DefaultBlend = new CinemachineBlendDefinition(CinemachineBlendDefinition.Styles.Cut, 0f);
        else if (overrideBlend)
            brain.DefaultBlend = new CinemachineBlendDefinition(blendStyle, blendTime);
        else
            brain.DefaultBlend = originalBlend;
    }

    protected virtual void ActivateCamera()
    {
        if (virtualCamera == null)
            return;

        ApplyBlend();

        int range = Mathf.Max(1, ROOM_TIER_MAX - activePriority);
        activationCounter = activationCounter % range + 1;

        virtualCamera.Priority.Value = activePriority + activationCounter;

        if (debugLog)
            Debug.Log($"{name}: เปิดกล้อง {virtualCamera.name}, Priority = {virtualCamera.Priority.Value}", this);
    }

    protected virtual void OnTriggerEnter(Collider other)
    {
        if (virtualCamera == null ||
            !other.CompareTag("Player"))
            return;

        bool wasEmpty = playerColliders.Count == 0;
        playerColliders.Add(other);

        if (wasEmpty)
            ActivateCamera();
    }

    protected virtual void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;

        playerColliders.Remove(other);

        if (playerColliders.Count == 0 &&
            virtualCamera != null)
        {
            virtualCamera.Priority.Value = inactivePriority;

            if (debugLog)
                Debug.Log($"{name}: ปิดกล้อง {virtualCamera.name} (ผู้เล่นออกจากโซน)", this);
        }
    }

    /// <summary>
    /// เรียกหลังวาร์ปผู้เล่น: เช็กใหม่ทุกโซนว่าผู้เล่นอยู่โซนไหนจริงๆ
    /// ตอนวาร์ป CharacterController ถูกปิด/เปิดชั่วคราว Unity อาจไม่ส่ง OnTriggerExit ให้โซนเดิม
    /// ทำให้โซนเดิมเข้าใจว่าผู้เล่นยังอยู่ แล้วกล้องห้องเก่าค้างอยู่
    /// </summary>
    public static void RefreshForPlayer(Collider player)
    {
        if (player == null)
            return;

        for (int i = 0; i < activeZones.Count; i++)
            activeZones[i].RefreshZone(player);
    }

    private void RefreshZone(Collider player)
    {
        if (zoneCollider == null || virtualCamera == null)
            return;

        if (playerColliders.Count > 0)
        {
            // โซนนี้คิดว่าผู้เล่นอยู่ แต่ตัวผู้เล่นไม่ทับกล่องแล้ว = พลาด Exit ไป ให้ปิดกล้อง
            if (!zoneCollider.bounds.Intersects(player.bounds))
                ForceExit();

            return;
        }

        // โซนนี้ยังไม่รู้ว่าผู้เล่นเข้ามา: ถ้ากึ่งกลางตัวผู้เล่นอยู่ในกล่องจริง ให้เปิดกล้อง
        Vector3 center = player.bounds.center;
        Vector3 closest = zoneCollider.ClosestPoint(center);

        if ((closest - center).sqrMagnitude < 0.0001f)
        {
            playerColliders.Add(player);
            ActivateCamera();
        }
    }

    // ปิดกล้องของโซนนี้ทันที โดยไม่ต้องรอ OnTriggerExit
    protected virtual void ForceExit()
    {
        playerColliders.Clear();

        if (virtualCamera != null)
            virtualCamera.Priority.Value = inactivePriority;

        if (debugLog)
            Debug.Log($"{name}: ปิดกล้องโดยบังคับ (ผู้เล่นถูกวาร์ปออกไป)", this);
    }

    protected virtual void OnDisable()
    {
        activeZones.Remove(this);
        playerColliders.Clear();

        if (virtualCamera != null)
            virtualCamera.Priority.Value = inactivePriority;
    }
}
