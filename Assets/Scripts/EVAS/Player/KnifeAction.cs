using UnityEngine;
using System.Collections;
using UnityEngine.InputSystem;

public class KnifeAction : MonoBehaviour
{
    [Header("Knife Settings")]
    public float damage = 25f;
    public float attackRange = 50f;
    public float attackRate = .5f;

    [Header("Raycast Settings")]
    public Vector3 raycastOffset = new Vector3(0f, 1f, 0f); // X=ซ้ายขวา, Y=ขึ้นลง, Z=หน้าหลัง

    [Header("Assassin Settings")]
    public float backstabDelay = 2f; // Delay ตอนฆ่าด้วยมือมีด (วินาที)

    [Header("State")]
    public bool isHeld = false;

    [Header("Input Actions")]
    public InputActionReference attackAction;

    [Header("Player Reference")]
    public Transform playerTransform;

    private float nextAttackTime;
    private bool isAssassinating = false;
    private PlayerInput playerInput;
    private InputAction resolvedAttackAction;
    private TopDownPlayerController movementController;

    void Update()
    {
        ResolveAttackAction();

        if (isHeld && resolvedAttackAction != null && resolvedAttackAction.WasPressedThisFrame() && Time.time > nextAttackTime && !isAssassinating)
        {
            nextAttackTime = Time.time + attackRate;
            Attack();
            Debug.Log("Atack");
        }
    }

    void ResolveAttackAction()
    {
        if (attackAction != null)
        {
            resolvedAttackAction = attackAction.action;
            return;
        }

        if (resolvedAttackAction != null)
            return;

        playerInput = GetComponentInParent<PlayerInput>();
        if (playerInput == null)
            return;

        resolvedAttackAction = playerInput.actions["Fire"];
    }
    // ฟังก์ชันสำหรับการโจมตีด้วยมีด
    void Attack()
    {
        Transform attackTransform = playerTransform != null ? playerTransform : transform;

        Vector3 attackOrigin = attackTransform.position
                               + (attackTransform.right * raycastOffset.x)
                               + (attackTransform.up * raycastOffset.y)
                               + (attackTransform.forward * raycastOffset.z);

        Vector3 attackDirection = attackTransform.forward;

        Debug.DrawRay(attackOrigin, attackDirection * attackRange, Color.red, 2f);

        RaycastHit hit;
        if(Physics.Raycast(attackOrigin, attackDirection, out hit, attackRange))
        {
            Debug.Log("<color=cyan>มีดฟันไปโดน: " + hit.collider.name + "</color>");

            Debug.Log("ชน: " + hit.collider.name);

            Debug.Log("Hit object: " + hit.collider.name);

            EnemyHealth enemy = hit.collider.GetComponentInParent<EnemyHealth>();
            if (enemy != null)
            {
                if (CanAssassinate(enemy))
                {
                    StartCoroutine(AssassinationSequence(enemy));
                }
                else
                {
                    enemy.TakeDamage(damage);
                    Debug.Log("ฟันปกติเข้าที่ศัตรู!");
                }
            }
        }
        else
        {
            Debug.Log("Raycast ไม่ชนอะไรเลยในระยะ");
        }

    }
    // ฟังก์ชันตรวจสอบว่าผู้เล่นสามารถลอบสังหารศัตรูได้หรือไม่
    bool CanAssassinate(EnemyHealth enemy)
    {
        FieldOfView fieldOfView = enemy.GetComponent<FieldOfView>();
        Transform attackTransform = playerTransform != null ? playerTransform : transform;

        return fieldOfView == null || !fieldOfView.IsTargetInVisionCone(attackTransform);
    }
    // ฟังก์ชันสำหรับลอบสังหารศัตรู
    IEnumerator AssassinationSequence(EnemyHealth enemy)
    {
        isAssassinating = true;
        SetPlayerMovementLocked(true);
        Debug.Log("กำลังลอบสังหารด้วยมีด... รอ 2 วินาที");

        yield return new WaitForSeconds(backstabDelay);

        if (enemy != null)
        {
            enemy.TakeDamage(9999f);
            Debug.Log("ลอบสังหารสำเร็จ!");
        }

        SetPlayerMovementLocked(false);
        isAssassinating = false;
    }
    // ฟังก์ชันสำหรับล็อกหรือปลดล็อกการเคลื่อนที่ของผู้เล่น
    void SetPlayerMovementLocked(bool locked)
    {
        if (movementController == null && playerTransform != null)
            movementController = playerTransform.GetComponent<TopDownPlayerController>();

        if (movementController != null)
            movementController.SetMovementLocked(locked);
    }
}
