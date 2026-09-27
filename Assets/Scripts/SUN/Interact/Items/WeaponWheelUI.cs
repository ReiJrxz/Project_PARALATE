using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;

public class WeaponWheelUI : MonoBehaviour
{
    [Header("References")]
    public PickupSystem pickupSystem;
    public PlayerInput playerInput;

    [Header("UI")]
    [Tooltip("Panel หลักของหน้าต่างอาวุธ เปิด/ปิดตอนกด Tab")]
    public GameObject wheelPanel;
    [Tooltip("Parent ที่จะ spawn ไอคอนอาวุธลงไป (ใส่ Horizontal/Vertical Layout Group ไว้)")]
    public Transform slotContainer;
    [Tooltip("Prefab ของช่องอาวุธ 1 ช่อง (มี Image + TextMeshProUGUI)")]
    public WeaponSlotUI slotPrefab;

    [Header("Behaviour")]
    [Tooltip("ชะลอเวลาตอนเปิดวงล้อ (1 = ปกติ, 0.2 = slow motion)")]
    [Range(0.05f, 1f)] public float timeScaleWhileOpen = 0.2f;
    [Tooltip("ให้เลือก 'ไม่ถืออาวุธ' ได้ในวงล้อ")]
    public bool allowEmptySlot = true;
    [Tooltip("ค่า scroll ขั้นต่ำที่นับเป็น 1 คลิก")]
    public float scrollThreshold = 0.1f;

    private InputAction wheelAction;
    private InputAction scrollAction;
    private List<GameObject> currentWeapons = new List<GameObject>();
    private List<WeaponSlotUI> spawnedSlots = new List<WeaponSlotUI>();
    private int selectedIndex = 0;
    private bool isOpen = false;
    private float cachedTimeScale = 1f;

    public bool IsOpen => isOpen;

    void Awake()
    {
        if (pickupSystem == null) pickupSystem = GetComponent<PickupSystem>();
        if (playerInput == null) playerInput = GetComponent<PlayerInput>();

        if (playerInput != null)
        {
            wheelAction = playerInput.actions["WeaponWheel"];
            scrollAction = playerInput.actions["ScrollWeapon"];
        }

        if (wheelPanel != null)
            wheelPanel.SetActive(false);
    }

    void Update()
    {
        if (wheelAction == null) return;

        if (wheelAction.WasPressedThisFrame())
            OpenWheel();

        if (isOpen)
        {
            HandleScroll();

            if (wheelAction.WasReleasedThisFrame())
                CloseWheelAndEquip();
        }
    }

    void OpenWheel()
    {
        currentWeapons = pickupSystem != null ? pickupSystem.GetOwnedWeapons() : new List<GameObject>();

        // ไม่มีอาวุธเลยก็ไม่ต้องเปิด
        if (currentWeapons.Count == 0 && !allowEmptySlot)
            return;

        isOpen = true;

        // ตั้ง index เริ่มต้นให้ตรงกับอาวุธที่ถืออยู่
        GameObject held = pickupSystem != null ? pickupSystem.HeldItem : null;
        selectedIndex = held != null ? currentWeapons.IndexOf(held) : GetEmptySlotIndex();
        if (selectedIndex < 0) selectedIndex = 0;

        BuildSlots();
        RefreshHighlight();

        if (wheelPanel != null)
            wheelPanel.SetActive(true);

        Cursor.visible = true; // โชว์เมาส์ตอนเลือก (เอาออกได้ถ้าไม่ชอบ)

        cachedTimeScale = Time.timeScale;
        Time.timeScale = timeScaleWhileOpen;
    }

    void HandleScroll()
    {
        if (scrollAction == null) return;

        float scroll = scrollAction.ReadValue<float>();
        if (Mathf.Abs(scroll) < scrollThreshold) return;

        int totalSlots = GetTotalSlotCount();
        if (totalSlots <= 1) return;

        int direction = scroll > 0 ? 1 : -1;
        selectedIndex = (selectedIndex + direction + totalSlots) % totalSlots; // วนลูปได้ทั้งสองทาง

        RefreshHighlight();
    }

    void CloseWheelAndEquip()
    {
        isOpen = false;

        if (wheelPanel != null)
            wheelPanel.SetActive(false);

        Time.timeScale = cachedTimeScale;
        Cursor.visible = false;

        if (pickupSystem == null) return;

        // ถ้าเลือกช่อง "ไม่ถืออาวุธ"
        if (allowEmptySlot && selectedIndex == GetEmptySlotIndex())
        {
            pickupSystem.UnequipWeaponPublic();
            return;
        }

        if (selectedIndex >= 0 && selectedIndex < currentWeapons.Count)
            pickupSystem.EquipWeaponPublic(currentWeapons[selectedIndex]);
    }

    int GetTotalSlotCount()
    {
        return currentWeapons.Count + (allowEmptySlot ? 1 : 0);
    }

    int GetEmptySlotIndex()
    {
        return allowEmptySlot ? currentWeapons.Count : -1; // ช่องว่างอยู่ท้ายสุดเสมอ
    }

    void BuildSlots()
    {
        // เคลียร์ของเก่า
        foreach (var slot in spawnedSlots)
        {
            if (slot != null) Destroy(slot.gameObject);
        }
        spawnedSlots.Clear();

        if (slotPrefab == null || slotContainer == null) return;

        for (int i = 0; i < currentWeapons.Count; i++)
        {
            WeaponSlotUI slot = Instantiate(slotPrefab, slotContainer);
            slot.Setup(GetWeaponDisplayName(currentWeapons[i]), GetWeaponIcon(currentWeapons[i]));
            spawnedSlots.Add(slot);
        }

        if (allowEmptySlot)
        {
            WeaponSlotUI emptySlot = Instantiate(slotPrefab, slotContainer);
            emptySlot.Setup("Unarmed", null);
            spawnedSlots.Add(emptySlot);
        }
    }

    void RefreshHighlight()
    {
        for (int i = 0; i < spawnedSlots.Count; i++)
        {
            if (spawnedSlots[i] != null)
                spawnedSlots[i].SetSelected(i == selectedIndex);
        }
    }

    string GetWeaponDisplayName(GameObject weapon)
    {
        if (weapon == null) return "Empty";

        WeaponInfo info = weapon.GetComponent<WeaponInfo>();
        return info != null && !string.IsNullOrEmpty(info.DisplayName) ? info.DisplayName : weapon.name;
    }

    Sprite GetWeaponIcon(GameObject weapon)
    {
        if (weapon == null) return null;

        WeaponInfo info = weapon.GetComponent<WeaponInfo>();
        return info != null ? info.Icon : null;
    }

    void OnDisable()
    {
        // กัน timeScale ค้างตอน disable กลางคัน
        if (isOpen)
        {
            Time.timeScale = cachedTimeScale;
            isOpen = false;
        }
    }
}