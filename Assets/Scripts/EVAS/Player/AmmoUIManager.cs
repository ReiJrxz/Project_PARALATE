using UnityEngine;
using TMPro;

public class AmmoUIManager : MonoBehaviour
{
    [Header("อ้างอิงสคริปต์ปืน")]
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
        if (playerGun == null)
            return;

        playerGun.OnAmmoChanged += HandleAmmoChanged;
        playerGun.OnReloadStatusChanged += HandleReloadChanged;
        playerGun.OnHeldChanged += HandleHeldChanged;

        SyncFromGun();
    }

    private void OnDisable()
    {
        if (playerGun == null)
            return;

        playerGun.OnAmmoChanged -= HandleAmmoChanged;
        playerGun.OnReloadStatusChanged -= HandleReloadChanged;
        playerGun.OnHeldChanged -= HandleHeldChanged;
    }

    private void SyncFromGun()
    {
        isGunHeld = playerGun.isHeld;
        isReloading = playerGun.IsReloading;
        lastCurrentAmmo = playerGun.CurrentAmmo;
        lastMaxAmmo = playerGun.MagazineSize;
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
