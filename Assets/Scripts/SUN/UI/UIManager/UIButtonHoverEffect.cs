using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro; // ใช้สำหรับ TextMeshPro

namespace ZiddikJari
{
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

        private void Start()
        {
            // ตั้งค่าสีเริ่มต้นเป็นแบบ Normal
            if (buttonBackground != null) buttonBackground.color = bgNormalColor;
            if (buttonText != null) buttonText.color = textNormalColor;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            // เปลี่ยนสีและเล่นเสียง Hover
            if (buttonBackground != null) buttonBackground.color = bgHoverColor;
            if (buttonText != null) buttonText.color = textHoverColor;

            if (AudioManager.Instance != null)
                AudioManager.Instance.PlayUIHover();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            // กลับเป็นสีเดิม
            if (buttonBackground != null) buttonBackground.color = bgNormalColor;
            if (buttonText != null) buttonText.color = textNormalColor;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            // เล่นเสียงตอนกด
            if (AudioManager.Instance != null)
                AudioManager.Instance.PlayUIClick();
        }
    }
}