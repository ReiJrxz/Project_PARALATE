using System;
using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

[RequireComponent(typeof(PlayerInput))]
public class PickupSystem : MonoBehaviour
{

    [Header("Pickup Settings")]
    public Transform PickUp;

    public Vector3 holdPositionOffset;
    public Vector3 holdRotationOffset;

    [Header("UI")]
    public AmmoUIManager ammoUIManager;

    [Header("Weapon Wheel")]
    public WeaponWheelUI weaponWheel;

    private GameObject heldItem;
    private GameObject gunItem;
    private GameObject knifeItem;
    private PlayerInput playerInput;
    private InputAction interactAction;
    private InputAction switchWeaponAction;
    private InputAction unequipWeaponAction;
    private TopDownPlayerController movementController;
    private KnockoutSystem knockoutSystem;
    private PickupDetector pickupDetector;

    public bool HasEquippedWeapon => heldItem != null;
    public bool HasKnifeEquipped => heldItem != null && heldItem.GetComponent<KnifeAction>() != null;

    /// <summary>เปลี่ยนอาวุธได้หรือไม่ (ห้ามตอนถูกล็อก/กำลังรัดคอ)</summary>
    public bool CanChangeWeapon =>
        (movementController == null || !movementController.IsMovementLocked) &&
        (knockoutSystem == null || !knockoutSystem.IsKnockout);

    void Awake()
    {
        playerInput = GetComponent<PlayerInput>();
        interactAction = playerInput.actions["Interact"];
        switchWeaponAction = playerInput.actions["SwitchWeapon"];
        unequipWeaponAction = playerInput.actions["UnequipWeapon"];
        movementController = GetComponent<TopDownPlayerController>();
        knockoutSystem = GetComponent<KnockoutSystem>();
        pickupDetector = GetComponent<PickupDetector>();
    }

    void Update()
    {
        if (knockoutSystem != null && knockoutSystem.IsKnockout)
            return;

        if (pickupDetector != null && interactAction.WasPressedThisFrame())
            pickupDetector.TryInteract();

        if (movementController != null && movementController.IsMovementLocked)
            return;

        // ถ้าวงล้อเปิดอยู่ ไม่ต้องรับ input สลับ/ถอดแบบเดิม
        if (weaponWheel != null && weaponWheel.IsOpen)
            return;

        if (switchWeaponAction.WasPressedThisFrame())
            SwitchWeapon();

        if (unequipWeaponAction.WasPressedThisFrame())
            UnequipWeapon();
    }

    /// <summary>คืน true ถ้าเก็บสำเร็จ (false = เก็บไม่ได้ เช่น มีอาวุธชนิดนี้อยู่แล้ว)</summary>
    public bool PickUpObject(GameObject item)
    {
        if (item == null)
            return false;

        GunAction gun = item.GetComponent<GunAction>();
        KnifeAction knife = item.GetComponent<KnifeAction>();

        if (gun == null && knife == null)
            return false;

        if (gun != null && gunItem != null)
            return false;

        if (knife != null && knifeItem != null)
            return false;

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
        return true;
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

        // ถือชิ้นนี้อยู่แล้ว ไม่ต้องปิด/เปิดใหม่ (กัน reload ถูกยกเลิกและเสียง equip เล่นซ้ำ)
        if (item == heldItem && item.activeSelf)
            return;

        SetWeaponHeld(gunItem, false);
        SetWeaponHeld(knifeItem, false);

        heldItem = item;
        heldItem.SetActive(true);
        SetWeaponHeld(heldItem, true);

        if (ammoUIManager != null)
            ammoUIManager.SetActiveWeapon(heldItem);
    }
    void UnequipWeapon()
    {
        SetWeaponHeld(gunItem, false);
        SetWeaponHeld(knifeItem, false);
        heldItem = null;

        if (ammoUIManager != null)
            ammoUIManager.SetActiveWeapon(null);
    }

    void SetWeaponHeld(GameObject item, bool held)
    {
        if (item == null)
            return;

        item.SetActive(held);

        GunAction gun = item.GetComponent<GunAction>();
        if (gun != null)
            gun.SetHeld(held);

        KnifeAction knife = item.GetComponent<KnifeAction>();
        if (knife != null)
            knife.isHeld = held;
    }

    public GameObject HeldItem => heldItem;

    /// <summary>คืนรายการอาวุธที่ถืออยู่ทั้งหมด เรียงตามลำดับคงที่ (ปืนก่อน มีดหลัง)</summary>
    public List<GameObject> GetOwnedWeapons()
    {
        List<GameObject> weapons = new List<GameObject>();

        if (gunItem != null) weapons.Add(gunItem);
        if (knifeItem != null) weapons.Add(knifeItem);

        return weapons;
    }

    /// <summary>ให้ UI เรียกเพื่อสลับอาวุธ (เปลี่ยนจาก private เป็น public)</summary>
    public void EquipWeaponPublic(GameObject item)
    {
        EquipWeapon(item);
    }

    /// <summary>ถอดอาวุธจากภายนอก (เผื่อเลือก "ไม่ถืออะไร" ในวงล้อ)</summary>
    public void UnequipWeaponPublic()
    {
        UnequipWeapon();
    }

}
