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

    [Header("Knockout Delays (Ctrl)")]
    public float knockoutDelayWithoutKnife = 5f;
    public float knockoutDelayWithKnife = 3f;

    [Header("Knockout Stun Duration")]
    public float knockoutStunDuration = 10f;

    private PlayerInput playerInput;
    private InputAction killAction;
    private InputAction knockOutAction;
    private TopDownPlayerController movementController;
    private PickupSystem pickupSystem;
    private bool isPerformingAction;

    public bool IsKnockout => isPerformingAction;

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
        if (movementController != null && movementController.IsMovementLocked)
            return;

        if (isPerformingAction)
            return;

        if (killAction.WasPressedThisFrame())
            TryStealthAction(StealthActionType.KillWithoutKnife, StealthActionType.KillWithKnife);

        if (knockOutAction.WasPressedThisFrame())
            TryStealthAction(StealthActionType.KnockoutWithoutKnife, StealthActionType.KnockoutWithKnife);
    }

    void TryStealthAction(StealthActionType withoutKnifeType, StealthActionType withKnifeType)
    {
        bool hasKnife = pickupSystem != null && pickupSystem.HasKnifeEquipped;
        StealthActionType actionType = hasKnife ? withKnifeType : withoutKnifeType;

        Vector3 attackOrigin = transform.position
                               + (transform.right * actionRaycastOffset.x)
                               + (transform.up * actionRaycastOffset.y)
                               + (transform.forward * actionRaycastOffset.z);

        Debug.DrawRay(attackOrigin, transform.forward * actionRange, Color.magenta, 2f);

        if (!Physics.Raycast(attackOrigin, transform.forward, out RaycastHit hit, actionRange))
            return;

        EnemyHealth enemy = hit.collider.GetComponentInParent<EnemyHealth>();
        if (enemy == null || !CanTarget(enemy))
            return;

        StartCoroutine(StealthActionSequence(enemy, actionType));
    }

    bool CanTarget(EnemyHealth enemy)
    {
        if (enemy.IsDead || enemy.IsStunned)
            return false;

        FieldOfView fieldOfView = enemy.GetComponent<FieldOfView>();
        return fieldOfView == null || !fieldOfView.IsTargetInVisionCone(transform);
    }

    IEnumerator StealthActionSequence(EnemyHealth enemy, StealthActionType actionType)
    {
        isPerformingAction = true;
        SetPlayerMovementLocked(true);

        float delay = GetActionDelay(actionType);
        Debug.Log(GetActionStartMessage(actionType, delay));

        yield return new WaitForSeconds(delay);

        if (enemy != null)
        {
            if (IsKillAction(actionType))
            {
                enemy.TakeDamage(9999f);
                Debug.Log(GetKillSuccessMessage(actionType));
            }
            else
            {
                enemy.KnockOut(knockoutStunDuration);
                Debug.Log(GetChokeSuccessMessage(actionType));
            }
        }

        SetPlayerMovementLocked(false);
        isPerformingAction = false;
    }

    static bool IsKillAction(StealthActionType actionType)
    {
        return actionType == StealthActionType.KillWithoutKnife
               || actionType == StealthActionType.KillWithKnife;
    }

    float GetActionDelay(StealthActionType actionType)
    {
        switch (actionType)
        {
            case StealthActionType.KillWithKnife:
                return killDelayWithKnife;
            case StealthActionType.KillWithoutKnife:
                return killDelayWithoutKnife;
            case StealthActionType.KnockoutWithKnife:
                return knockoutDelayWithKnife;
            default:
                return knockoutDelayWithoutKnife;
        }
    }

    static string GetActionStartMessage(StealthActionType actionType, float delay)
    {
        switch (actionType)
        {
            case StealthActionType.KillWithKnife:
                return $"กำลังฆ่าด้วยมีด... รอ {delay} วินาที";
            case StealthActionType.KillWithoutKnife:
                return $"กำลังฆ่าโดยไม่ใช้มีด... รอ {delay} วินาที";
            case StealthActionType.KnockoutWithKnife:
                return $"กำลังรัดคอด้วยมีด... รอ {delay} วินาที";
            default:
                return $"กำลังรัดคอ... รอ {delay} วินาที";
        }
    }

    static string GetKillSuccessMessage(StealthActionType actionType)
    {
        return actionType == StealthActionType.KillWithKnife
            ? "ฆ่าด้วยมีดสำเร็จ!"
            : "ฆ่าโดยไม่ใช้มีดสำเร็จ!";
    }

    static string GetChokeSuccessMessage(StealthActionType actionType)
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
}
