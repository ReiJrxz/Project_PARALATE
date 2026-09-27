using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class AmmoUIManager : MonoBehaviour
{
    [Header("UI Elements - Panel")]
    public GameObject weaponHudPanel; // panel รวม โชว์ทุกครั้งที่ถืออาวุธ (ไม่ว่าปืนหรือมีด)

    [Header("UI Elements - Weapon Info")]
    public TextMeshProUGUI weaponNameText;
    public Image weaponIconImage;

    [Header("UI Elements - Ammo (โชว์เฉพาะปืน)")]
    public GameObject ammoGroup; // แยก GameObject ย่อยเฉพาะส่วนกระสุน จะได้ซ่อน/โชว์แยกจาก panel หลักได้
    public TextMeshProUGUI ammoText;

    private GunAction currentGun;
    private WeaponInfo currentWeaponInfo;
    private bool isWeaponHeld;

    private int lastCurrentAmmo;
    private int lastMaxAmmo;
    private bool isReloading;

    /// <summary>เรียกจาก PickupSystem ทุกครั้งที่ถือ/สลับ/ถอดอาวุธ ส่ง null ถ้าไม่ถืออะไรเลย</summary>
    public void SetActiveWeapon(GameObject item)
    {
        UnsubscribeFromGun(currentGun);
        currentGun = null;
        currentWeaponInfo = null;

        if (item == null)
        {
            isWeaponHeld = false;
            RefreshDisplay();
            return;
        }

        isWeaponHeld = true;
        currentWeaponInfo = item.GetComponent<WeaponInfo>();

        GunAction gun = item.GetComponent<GunAction>();
        if (gun != null)
        {
            currentGun = gun;
            SubscribeToGun(gun);
            return; // SubscribeToGun จะเรียก RefreshDisplay ให้เองผ่าน SyncFromGun
        }

        // ไม่ใช่ปืน (มีด หรืออาวุธชนิดอื่นที่ไม่มีกระสุน)
        isReloading = false;
        RefreshDisplay();
    }

    private void OnDisable()
    {
        UnsubscribeFromGun(currentGun);
    }

    private void SubscribeToGun(GunAction gun)
    {
        if (gun == null)
            return;

        gun.OnAmmoChanged += HandleAmmoChanged;
        gun.OnReloadStatusChanged += HandleReloadChanged;

        lastCurrentAmmo = gun.CurrentAmmo;
        lastMaxAmmo = gun.MagazineSize;
        isReloading = gun.IsReloading;
        RefreshDisplay();
    }

    private void UnsubscribeFromGun(GunAction gun)
    {
        if (gun == null)
            return;

        gun.OnAmmoChanged -= HandleAmmoChanged;
        gun.OnReloadStatusChanged -= HandleReloadChanged;
    }

    private void HandleAmmoChanged(int currentAmmo, int maxAmmo)
    {
        lastCurrentAmmo = currentAmmo;
        lastMaxAmmo = maxAmmo;
        RefreshDisplay();
    }

    private void HandleReloadChanged(bool reloading)
    {
        isReloading = reloading;
        RefreshDisplay();
    }

    private void RefreshDisplay()
    {
        if (weaponHudPanel != null)
            weaponHudPanel.SetActive(isWeaponHeld);

        if (!isWeaponHeld)
            return;

        // ชื่อ/ไอคอน โชว์เสมอไม่ว่าอาวุธชนิดไหน
        if (weaponNameText != null)
            weaponNameText.text = currentWeaponInfo != null ? currentWeaponInfo.DisplayName : "Weapon";

        if (weaponIconImage != null)
        {
            Sprite icon = currentWeaponInfo != null ? currentWeaponInfo.Icon : null;
            weaponIconImage.sprite = icon;
            weaponIconImage.enabled = icon != null;
        }

        // ส่วนกระสุน โชว์เฉพาะตอนถือปืน (currentGun != null)
        bool showAmmo = currentGun != null;

        if (ammoGroup != null)
            ammoGroup.SetActive(showAmmo);

        if (showAmmo && ammoText != null)
        {
            ammoText.text = isReloading
                ? "Reloading..."
                : $"{lastCurrentAmmo} / {lastMaxAmmo}";
        }
    }
}