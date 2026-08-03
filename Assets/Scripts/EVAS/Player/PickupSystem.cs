using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerInput))]
public class PickupSystem : MonoBehaviour
{
    [Header("Pickup Settings")]
    public Transform PickUp;
    public float pickupRange = 3f;
    public LayerMask itemLayer;

    public Vector3 holdPositionOffset;
    public Vector3 holdRotationOffset;

    private GameObject heldItem;
    private GameObject gunItem;
    private GameObject knifeItem;
    private PlayerInput playerInput;
    private InputAction interactAction;
    private InputAction switchWeaponAction;
    private InputAction unequipWeaponAction;
    private TopDownPlayerController movementController;
    private KnockoutSystem knockoutSystem;

    public bool HasEquippedWeapon => heldItem != null;
    public bool HasKnifeEquipped => heldItem != null && heldItem.GetComponent<KnifeAction>() != null;

    void Awake()
    {
        playerInput = GetComponent<PlayerInput>();
        interactAction = playerInput.actions["Interact"];
        switchWeaponAction = playerInput.actions["SwitchWeapon"];
        unequipWeaponAction = playerInput.actions["UnequipWeapon"];
        movementController = GetComponent<TopDownPlayerController>();
        knockoutSystem = GetComponent<KnockoutSystem>();
    }

    void Update()
    {
        if (movementController != null && movementController.IsMovementLocked)
            return;

        if (knockoutSystem != null && knockoutSystem.IsKnockout)
            return;

        if (interactAction.WasPressedThisFrame())
            TryPickup();

        if (switchWeaponAction.WasPressedThisFrame())
            SwitchWeapon();

        if (unequipWeaponAction.WasPressedThisFrame())
            UnequipWeapon();
    }

    void TryPickup()
    {
        Ray ray = new Ray(transform.position, transform.forward);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, pickupRange))
        {
            GameObject weaponRoot = GetWeaponRoot(hit.collider.gameObject);

            if (hit.collider.CompareTag("Pickup") || weaponRoot.CompareTag("Pickup"))
                PickUpObject(weaponRoot);
        }
    }

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

    void UnequipWeapon()
    {
        SetWeaponHeld(gunItem, false);
        SetWeaponHeld(knifeItem, false);
        heldItem = null;
    }

    GameObject GetPreferredWeapon()
    {
        if (gunItem != null)
            return gunItem;

        return knifeItem;
    }

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
}
