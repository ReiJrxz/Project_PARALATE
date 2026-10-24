using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

public class UIButtonHoverEffect : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [Header("UI Components")]
    public Image buttonBackground;
    public TextMeshProUGUI buttonText;

    [Header("Colors (Hover)")]
    public Color bgHoverColor = new Color(1f, 1f, 1f, 0.5f); // สีพื้นหลังตอนชี้
    public Color textHoverColor = Color.black; // สีตัวอักษรตอนชี้

    [Header("Colors (Normal)")]
    public Color bgNormalColor = new Color(1f, 1f, 1f, 0f); // โปร่งใส
    public Color textNormalColor = Color.white; // สีตัวอักษรปกติ

    // ใช้ OnEnable/OnDisable แทน Start: ถ้าปุ่มถูกซ่อนตอนเมาส์ยังชี้อยู่ (เช่น สลับ Panel)
    // จะไม่ได้รับ OnPointerExit และปุ่มจะค้างสี Hover ตอนกลับมาแสดงอีกครั้ง
    private void OnEnable() => ApplyNormal();
    private void OnDisable() => ApplyNormal();

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (buttonBackground != null) buttonBackground.color = bgHoverColor;
        if (buttonText != null) buttonText.color = textHoverColor;

        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayUIHover();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        ApplyNormal();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayUIClick();
    }

    private void ApplyNormal()
    {
        if (buttonBackground != null) buttonBackground.color = bgNormalColor;
        if (buttonText != null) buttonText.color = textNormalColor;
    }
}