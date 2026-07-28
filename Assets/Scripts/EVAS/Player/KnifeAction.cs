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
    public float backstabDelay = 2f;

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
                if (IsBehindEnemy(enemy.transform))
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
    bool IsBehindEnemy(Transform enemyTransform)
    {
        Transform attackTransform = playerTransform != null ? playerTransform : transform;
        Vector3 dirToPlayer = (attackTransform.position - enemyTransform.position).normalized;
        float positionDot = Vector3.Dot(enemyTransform.forward, dirToPlayer);
        return positionDot < -0.5f;
    }
    IEnumerator AssassinationSequence(EnemyHealth enemy)
    {
        isAssassinating = true;
        Debug.Log("กำลังลอบสังหาร... รอ 2 วินาที");

        yield return new WaitForSeconds(backstabDelay);

        if (enemy != null)
        {
            enemy.TakeDamage(9999f);
            Debug.Log("ลอบสังหารสำเร็จ!");
        }

        isAssassinating = false;
    }
}
