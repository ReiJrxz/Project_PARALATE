using TMPro;
using UnityEngine;

public class AmmoUIManager : MonoBehaviour
{
    [Header("อ้างอิงสคริปต์ปืน (กระบอกที่กำลังถืออยู่)")]
    public GunAction playerGun;

    [Header("UI Elements")]
    public GameObject ammoPanel;
    public TextMeshProUGUI ammoText;

    private int lastCurrentAmmo;
    private int lastMaxAmmo;
    private bool isReloading;
    private bool isGunHeld;

    private void OnEnable()
    {
        SubscribeToGun(playerGun);
    }

    private void OnDisable()
    {
        UnsubscribeFromGun(playerGun);
    }

    // เรียกฟังก์ชันนี้จากสคริปต์สลับอาวุธ ตอนเปลี่ยนปืนที่ถือ
    public void SetActiveGun(GunAction newGun)
    {
        if (playerGun == newGun)
            return;

        UnsubscribeFromGun(playerGun);
        playerGun = newGun;

        if (playerGun != null)
            SubscribeToGun(playerGun);
        else
        {
            isGunHeld = false;
            RefreshDisplay(); // ซ่อน panel เมื่อไม่มีปืนถืออยู่
        }
    }

    private void SubscribeToGun(GunAction gun)
    {
        if (gun == null)
            return;

        gun.OnAmmoChanged += HandleAmmoChanged;
        gun.OnReloadStatusChanged += HandleReloadChanged;
        gun.OnHeldChanged += HandleHeldChanged;

        SyncFromGun(gun);
    }

    private void UnsubscribeFromGun(GunAction gun)
    {
        if (gun == null)
            return;

        gun.OnAmmoChanged -= HandleAmmoChanged;
        gun.OnReloadStatusChanged -= HandleReloadChanged;
        gun.OnHeldChanged -= HandleHeldChanged;
    }

    private void SyncFromGun(GunAction gun)
    {
        isGunHeld = gun.isHeld;
        isReloading = gun.IsReloading;
        lastCurrentAmmo = gun.CurrentAmmo;
        lastMaxAmmo = gun.MagazineSize;
        RefreshDisplay();
    }

    private void HandleHeldChanged(bool held)
    {
        isGunHeld = held;

        if (held)
        {
            lastCurrentAmmo = playerGun.CurrentAmmo;
            lastMaxAmmo = playerGun.MagazineSize;
            isReloading = playerGun.IsReloading;
        }

        RefreshDisplay();
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
        if (ammoPanel != null)
            ammoPanel.SetActive(isGunHeld);
        else if (ammoText != null)
            ammoText.gameObject.SetActive(isGunHeld);

        if (!isGunHeld || ammoText == null)
            return;

        ammoText.text = isReloading
            ? "Reloading..."
            : $"{lastCurrentAmmo} / {lastMaxAmmo}";
    }
}