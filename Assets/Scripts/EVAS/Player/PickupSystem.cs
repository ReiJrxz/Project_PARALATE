using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

[RequireComponent(typeof(PlayerInput))]
public class PickupSystem : MonoBehaviour
{
    [Header("Pickup Settings")]
    public Transform PickUp;
    public float pickupRange = 3f;
    public LayerMask itemLayer;

    public Vector3 holdPositionOffset;
    public Vector3 holdRotationOffset;

    [Header("Knockout")]
    public float KnockoutRange = 2f;
    public float KnockoutKillDelay = 5f; // Delay ตอนฆ่าด้วยมือเปล่า (วินาที)
    public Vector3 KnockoutRaycastOffset = new Vector3(0f, 1f, 0f);

    private GameObject heldItem;
    private GameObject gunItem;
    private GameObject knifeItem;
    private PlayerInput playerInput;
    private InputAction interactAction;
    private InputAction switchWeaponAction;
    private InputAction unequipWeaponAction;
    private InputAction attackAction;
    private TopDownPlayerController movementController;
    private bool isKnockout = false;

    void Awake()
    {
        playerInput = GetComponent<PlayerInput>();
        interactAction = playerInput.actions["Interact"];
        switchWeaponAction = playerInput.actions["SwitchWeapon"];
        unequipWeaponAction = playerInput.actions["UnequipWeapon"];
        attackAction = playerInput.actions["Fire"];
        movementController = GetComponent<TopDownPlayerController>();
    }
    void Update()
    {
        if (movementController != null && movementController.IsMovementLocked)
            return;

        if (isKnockout)
            return;

        if (interactAction.WasPressedThisFrame())
        {
            TryPickup();
        }

        if (switchWeaponAction.WasPressedThisFrame())
        {
            SwitchWeapon();
        }

        if (unequipWeaponAction.WasPressedThisFrame())
        {
            UnequipWeapon();
        }

        if (heldItem == null && attackAction.WasPressedThisFrame())
        {
            TryUnarmedAssassination();
        }
    }
    // ฟังก์ชันสำหรับตรวจสอบและเก็บไอเท็ม
    void TryPickup()
    {
        Ray ray = new Ray(transform.position, transform.forward);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, pickupRange))
        {
            GameObject weaponRoot = GetWeaponRoot(hit.collider.gameObject);

            if (hit.collider.CompareTag("Pickup") || weaponRoot.CompareTag("Pickup"))
            {
                PickUpObject(weaponRoot);
            }
        }
    }
    // ฟังก์ชันสำหรับเก็บไอเท็ม
    void PickUpObject(GameObject item)
    {
        if (item == null)
            return;

        GunAction gun = item.GetComponent<GunAction>();
        KnifeAction knife = item.GetComponent<KnifeAction>();

        if (gun == null && knife == null)
            return;

        if (gun != null && gunItem != null)
        {
            Debug.Log("Already have gun");
            return;
        }

        if (knife != null && knifeItem != null)
        {
            Debug.Log("Already have knife");
            return;
        }

        Rigidbody rb = item.GetComponent<Rigidbody>();
        if (rb != null)
            rb.isKinematic = true;

        Collider[] colliders = item.GetComponentsInChildren<Collider>();
        foreach (Collider coll in colliders)
            coll.enabled = false;

        item.transform.SetParent(PickUp);
        item.transform.localPosition = holdPositionOffset;
        item.transform.localEulerAngles = holdRotationOffset;

        if (gun != null)
            gunItem = item;

        if (knife != null)
        {
            knifeItem = item;
            knife.playerTransform = transform;
        }

        EquipWeapon(item);
        Debug.Log("Equipped: " + item.name);
    }
    // ฟังก์ชันสำหรับหาตัว root ของอาวุธ (Gun หรือ Knife)
    GameObject GetWeaponRoot(GameObject item)
    {
        GunAction gun = item.GetComponentInParent<GunAction>();
        if (gun != null)
            return gun.gameObject;

        KnifeAction knife = item.GetComponentInParent<KnifeAction>();
        if (knife != null)
            return knife.gameObject;

        return item;
    }
    // ฟังก์ชันสำหรับสลับอาวุธ
    void SwitchWeapon()
    {
        if (gunItem == null && knifeItem == null)
            return;

        if (heldItem == null)
        {
            EquipWeapon(GetPreferredWeapon());
            return;
        }

        if (gunItem != null && knifeItem != null)
        {
            EquipWeapon(heldItem == gunItem ? knifeItem : gunItem);
            return;
        }

        EquipWeapon(GetPreferredWeapon());
    }
    // ฟังก์ชันสำหรับปลดอาวุธ
    void UnequipWeapon()
    {
        SetWeaponHeld(gunItem, false);
        SetWeaponHeld(knifeItem, false);
        heldItem = null;
    }
    // ฟังก์ชันสำหรับหาว่าอาวุธที่ควรถือคืออะไร (Gun > Knife)
    GameObject GetPreferredWeapon()
    {
        if (gunItem != null)
            return gunItem;

        return knifeItem;
    }
    // ฟังก์ชันสำหรับสวมอาวุธ
    void EquipWeapon(GameObject item)
    {
        if (item == null)
            return;

        SetWeaponHeld(gunItem, false);
        SetWeaponHeld(knifeItem, false);

        heldItem = item;
        heldItem.SetActive(true);
        SetWeaponHeld(heldItem, true);
    }
    // ฟังก์ชันสำหรับตั้งค่าอาวุธว่าเป็น held หรือไม่
    void SetWeaponHeld(GameObject item, bool held)
    {
        if (item == null)
            return;

        GunAction gun = item.GetComponent<GunAction>();
        if (gun != null)
            gun.SetHeld(held);

        KnifeAction knife = item.GetComponent<KnifeAction>();
        if (knife != null)
            knife.isHeld = held;

        item.SetActive(held);
    }
    // ฟังก์ชันสำหรับพยายามลอบสังหารด้วยมือเปล่า
    void TryUnarmedAssassination()
    {
        Vector3 attackOrigin = transform.position
                               + (transform.right * KnockoutRaycastOffset.x)
                               + (transform.up * KnockoutRaycastOffset.y)
                               + (transform.forward * KnockoutRaycastOffset.z);

        Debug.DrawRay(attackOrigin, transform.forward * KnockoutRange, Color.magenta, 2f);

        if (!Physics.Raycast(attackOrigin, transform.forward, out RaycastHit hit, KnockoutRange))
            return;

        EnemyHealth enemy = hit.collider.GetComponentInParent<EnemyHealth>();
        if (enemy == null || !CanAssassinate(enemy))
            return;

        StartCoroutine(UnarmedAssassinationSequence(enemy));
    }
    // ฟังก์ชันสำหรับตรวจสอบว่าผู้เล่นสามารถลอบสังหารศัตรูได้หรือไม่
    bool CanAssassinate(EnemyHealth enemy)
    {
        FieldOfView fieldOfView = enemy.GetComponent<FieldOfView>();
        return fieldOfView == null || !fieldOfView.IsTargetInVisionCone(transform);
    }
    // Coroutine สำหรับลอบสังหารด้วยมือเปล่า
    IEnumerator UnarmedAssassinationSequence(EnemyHealth enemy)
    {
        isKnockout = true;
        SetPlayerMovementLocked(true);

        Debug.Log("กำลังลอบสังหารด้วยมีด... รอ 5 วินาที");

        yield return new WaitForSeconds(KnockoutKillDelay);

        if (enemy != null)
        {
            enemy.TakeDamage(9999f);
            Debug.Log("ลอบสังหารสำเร็จ!");
        }
        
        SetPlayerMovementLocked(false);
        isKnockout = false;
    }
    // ฟังก์ชันสำหรับล็อกหรือปลดล็อกการเคลื่อนที่ของผู้เล่น
    void SetPlayerMovementLocked(bool locked)
    {
        if (movementController == null)
            movementController = GetComponent<TopDownPlayerController>();

        if (movementController != null)
            movementController.SetMovementLocked(locked);
    }
}
