using UnityEngine;
using UnityEngine.InputSystem;
public class KnifeAction : MonoBehaviour
{
    [Header("Knife Settings")]
    public float damage = 25f;
    public float attackRange = 50f;
    public float attackRate = .5f;

    [Header("Raycast Settings")]
    public Vector3 raycastOffset = new Vector3(0f, 1f, 0f); // X=ซ้ายขวา, Y=ขึ้นลง, Z=หน้าหลัง

    [Header("State")]
    public bool isHeld = false;

    [Header("Input Actions")]
    public InputActionReference attackAction;

    [Header("Player Reference")]
    public Transform playerTransform;

    private float nextAttackTime;
    private PlayerInput playerInput;
    private InputAction resolvedAttackAction;
    void Update()
    {
        ResolveAttackAction();

        if (isHeld && resolvedAttackAction != null && resolvedAttackAction.WasPressedThisFrame() && Time.time > nextAttackTime)
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
                enemy.TakeDamage(damage);
                Debug.Log("ฟันปกติเข้าที่ศัตรู!");
            }
        }
        else
        {
            Debug.Log("Raycast ไม่ชนอะไรเลยในระยะ");
        }

    }
}
