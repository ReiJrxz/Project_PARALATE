using UnityEngine;

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

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.E))
        {
            TryPickup();
        }

        if (Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Keypad1))
        {
            SwitchWeapon();
        }
    }

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
        if (gunItem == null || knifeItem == null)
            return;

        EquipWeapon(heldItem == gunItem ? knifeItem : gunItem);
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
            gun.isHeld = held;

        KnifeAction knife = item.GetComponent<KnifeAction>();
        if (knife != null)
            knife.isHeld = held;

        item.SetActive(held);
    }
}
