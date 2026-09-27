using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class WeaponSlotUI : MonoBehaviour
{
    public Image iconImage;
    public TextMeshProUGUI nameText;
    public Image backgroundImage;

    [Header("Colors")]
    public Color normalColor = new Color(0f, 0f, 0f, 0.5f);
    public Color selectedColor = new Color(1f, 0.8f, 0.2f, 0.9f);

    public void Setup(string weaponName, Sprite icon)
    {
        if (nameText != null) nameText.text = weaponName;

        if (iconImage != null)
        {
            iconImage.sprite = icon;
            iconImage.enabled = icon != null; // ซ่อนไอคอนถ้าไม่มี sprite
        }
    }

    public void SetSelected(bool selected)
    {
        if (backgroundImage != null)
            backgroundImage.color = selected ? normalColor : selectedColor;
    }
}