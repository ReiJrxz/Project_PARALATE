using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

[RequireComponent(typeof(PlayerInput))]
public class KnockoutSystem : MonoBehaviour
{
    public enum StealthActionType
    {
        KillWithoutKnife,
        KillWithKnife,
        KnockoutWithoutKnife,
        KnockoutWithKnife
    }

    [Header("Range")]
    public float actionRange = 2f;
    public Vector3 actionRaycastOffset = new Vector3(0f, 1f, 0f);

    [Header("Kill Delays (F)")]
    public float killDelayWithoutKnife = 5f;
    public float killDelayWithKnife = 2f;

    [Header("Knockout Hold (Ctrl)")]
    public float knockoutHoldDuration = 3f;

    [Header("Knockout Stun Duration")]
    public float knockoutStunDuration = 10f;

    private PlayerInput playerInput;
    private InputAction killAction;
    private InputAction knockOutAction;
    private TopDownPlayerController movementController;
    private PickupSystem pickupSystem;
    private bool isPerformingAction;
    private bool isHoldingKnockout;
    private float knockoutHoldTimer;
    private EnemyHealth knockoutTarget;
    private StealthActionType currentKnockoutType;

    public bool IsKnockout => isPerformingAction || isHoldingKnockout;

    void Awake()
    {
        playerInput = GetComponent<PlayerInput>();
        killAction = playerInput.actions["Kill"];
        knockOutAction = playerInput.actions["KnockOut"];
        movementController = GetComponent<TopDownPlayerController>();
        pickupSystem = GetComponent<PickupSystem>();
    }

    void Update()
    {
        if (movementController != null && movementController.IsMovementLocked && !isHoldingKnockout)
            return;

        if (isPerformingAction)
            return;

        if (killAction.WasPressedThisFrame())
            TryKill();

        HandleKnockoutHold();
    }

    void TryKill()
    {
        if (!TryGetTarget(out EnemyHealth enemy))
            return;

        bool hasKnife = pickupSystem != null && pickupSystem.HasKnifeEquipped;
        StealthActionType actionType = hasKnife
            ? StealthActionType.KillWithKnife
            : StealthActionType.KillWithoutKnife;

        StartCoroutine(KillSequence(enemy, actionType));
    }

    void HandleKnockoutHold()
    {
        if (knockOutAction.WasPressedThisFrame() && !isHoldingKnockout)
        {
            if (!TryGetTarget(out EnemyHealth enemy))
                return;

            bool hasKnife = pickupSystem != null && pickupSystem.HasKnifeEquipped;
            isHoldingKnockout = true;
            knockoutHoldTimer = 0f;
            knockoutTarget = enemy;
            currentKnockoutType = hasKnife
                ? StealthActionType.KnockoutWithKnife
                : StealthActionType.KnockoutWithoutKnife;

            SetEnemyMovementLocked(knockoutTarget, true);
            SetPlayerMovementLocked(true);
            Debug.Log(GetKnockoutHoldStartMessage(currentKnockoutType, knockoutHoldDuration));
        }

        if (!isHoldingKnockout)
            return;

        if (!knockOutAction.IsPressed())
        {
            CancelKnockoutHold("ปล่อย Ctrl ก่อนครบเวลา — ศัตรูไม่สลบ");
            return;
        }

        if (!IsTargetStillValid(knockoutTarget))
        {
            CancelKnockoutHold("เป้าหมายไม่ถูกต้อง — ยกเลิกการรัดคอ");
            return;
        }

        knockoutHoldTimer += Time.deltaTime;

        if (knockoutHoldTimer >= knockoutHoldDuration)
            CompleteKnockout();
    }

    void CompleteKnockout()
    {
        if (knockoutTarget != null)
        {
            knockoutTarget.KnockOut(knockoutStunDuration);
            Debug.Log(GetKnockoutSuccessMessage(currentKnockoutType));
        }

        ResetKnockoutHold(false);
    }

    void CancelKnockoutHold(string reason)
    {
        Debug.Log(reason);
        ResetKnockoutHold(true);
    }

    void ResetKnockoutHold(bool unlockTarget = true)
    {
        if (unlockTarget)
            SetEnemyMovementLocked(knockoutTarget, false);

        isHoldingKnockout = false;
        knockoutHoldTimer = 0f;
        knockoutTarget = null;
        SetPlayerMovementLocked(false);
    }

    bool TryGetTarget(out EnemyHealth enemy)
    {
        enemy = null;

        Vector3 attackOrigin = transform.position
                               + (transform.right * actionRaycastOffset.x)
                               + (transform.up * actionRaycastOffset.y)
                               + (transform.forward * actionRaycastOffset.z);

        Debug.DrawRay(attackOrigin, transform.forward * actionRange, Color.magenta, 2f);

        if (!Physics.Raycast(attackOrigin, transform.forward, out RaycastHit hit, actionRange))
            return false;

        enemy = hit.collider.GetComponentInParent<EnemyHealth>();
        return enemy != null && CanTarget(enemy);
    }

    bool IsTargetStillValid(EnemyHealth enemy)
    {
        if (enemy == null || !CanTarget(enemy))
            return false;

        return TryGetTarget(out EnemyHealth currentTarget) && currentTarget == enemy;
    }

    bool CanTarget(EnemyHealth enemy)
    {
        if (enemy.IsDead || enemy.IsStunned)
            return false;

        FieldOfView fieldOfView = enemy.GetComponent<FieldOfView>();
        return fieldOfView == null || !fieldOfView.IsTargetInVisionCone(transform);
    }

    IEnumerator KillSequence(EnemyHealth enemy, StealthActionType actionType)
    {
        isPerformingAction = true;
        SetPlayerMovementLocked(true);
        SetEnemyMovementLocked(enemy, true);

        float delay = actionType == StealthActionType.KillWithKnife
            ? killDelayWithKnife
            : killDelayWithoutKnife;

        Debug.Log(GetKillStartMessage(actionType, delay));

        yield return new WaitForSeconds(delay);

        if (enemy != null)
        {
            enemy.TakeDamage(9999f);
            Debug.Log(GetKillSuccessMessage(actionType));
        }

        if (enemy != null && !enemy.IsDead)
            SetEnemyMovementLocked(enemy, false);

        SetPlayerMovementLocked(false);
        isPerformingAction = false;
    }

    static string GetKillStartMessage(StealthActionType actionType, float delay)
    {
        return actionType == StealthActionType.KillWithKnife
            ? $"กำลังฆ่าด้วยมีด... รอ {delay} วินาที"
            : $"กำลังฆ่าโดยไม่ใช้มีด... รอ {delay} วินาที";
    }

    static string GetKnockoutHoldStartMessage(StealthActionType actionType, float holdDuration)
    {
        return actionType == StealthActionType.KnockoutWithKnife
            ? $"กำลังรัดคอด้วยมีด... กด Ctrl ค้าง {holdDuration} วินาที"
            : $"กำลังรัดคอ... กด Ctrl ค้าง {holdDuration} วินาที";
    }

    static string GetKillSuccessMessage(StealthActionType actionType)
    {
        return actionType == StealthActionType.KillWithKnife
            ? "ฆ่าด้วยมีดสำเร็จ!"
            : "ฆ่าโดยไม่ใช้มีดสำเร็จ!";
    }

    static string GetKnockoutSuccessMessage(StealthActionType actionType)
    {
        return actionType == StealthActionType.KnockoutWithKnife
            ? "รัดคอด้วยมีดสำเร็จ! ศัตรูสลบ"
            : "รัดคอสำเร็จ! ศัตรูสลบ";
    }

    void SetPlayerMovementLocked(bool locked)
    {
        if (movementController == null)
            movementController = GetComponent<TopDownPlayerController>();

        if (movementController != null)
            movementController.SetMovementLocked(locked);
    }

    static void SetEnemyMovementLocked(EnemyHealth enemy, bool locked)
    {
        if (enemy == null)
            return;

        EnemyController enemyController = enemy.GetComponent<EnemyController>();
        if (enemyController != null)
            enemyController.SetMovementLocked(locked);
    }
}
