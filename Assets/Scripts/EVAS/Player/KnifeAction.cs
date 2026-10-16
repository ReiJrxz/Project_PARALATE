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
    private void Awake()
    {
        ResolveAttackAction();
    }

    private void OnEnable()
    {
        // ผูก Event เมื่อเปิดใช้งาน Script หรือ Object
        if (resolvedAttackAction != null)
        {
            resolvedAttackAction.performed += OnAttackPerformed;
            resolvedAttackAction.Enable();
        }
    }

    private void OnDisable()
    {
        // ถอด Event ออกเสมอเมื่อปิดใช้งาน เพื่อป้องกัน Memory Leak
        if (resolvedAttackAction != null)
        {
            resolvedAttackAction.performed -= OnAttackPerformed;
        }
    }

    // ฟังก์ชันนี้จะถูกเรียกเฉพาะตอนกดปุ่มโจมตีเท่านั้น ไม่ได้วนลูปทุกเฟรม
    private void OnAttackPerformed(InputAction.CallbackContext context)
    {
        if (isHeld && Time.time >= nextAttackTime)
        {
            nextAttackTime = Time.time + attackRate;
            Attack();
            Debug.Log("Attack");
        }
    }

    void ResolveAttackAction()
    {
        if (attackAction != null)
        {
            resolvedAttackAction = attackAction.action;
            return;
        }

        playerInput = GetComponentInParent<PlayerInput>();
        if (playerInput != null)
        {
            resolvedAttackAction = playerInput.actions["Fire"];
        }
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
        if(Physics.Raycast(attackOrigin, attackDirection, out hit, attackRange, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
        {
            Debug.Log("<color=cyan>มีดฟันไปโดน: " + hit.collider.name + "</color>");

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
