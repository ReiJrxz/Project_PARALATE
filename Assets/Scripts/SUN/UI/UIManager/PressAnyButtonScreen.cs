using UnityEngine;
using TMPro; // อย่าลืมใส่บรรทัดนี้เพื่อเรียกใช้ TextMeshPro

public class PressAnyButtonScreen : MonoBehaviour
{
    [Header("UI References")]
    public GameObject pressAnyButtonPanel;
    public GameObject mainMenuPanel;

    [Tooltip("ลาก Text หรือออบเจกต์ข้อความ Press Any Button มาใส่ตรงนี้")]
    public TextMeshProUGUI pressAnyKeyText;

    [Header("Blink Settings")]
    [Tooltip("ความเร็วในการกะพริบ ยิ่งเยอะยิ่งเร็ว")]
    public float blinkSpeed = 2f;

    private bool isWaitingForInput = true;

    private void Start()
    {
        pressAnyButtonPanel.SetActive(true);
        mainMenuPanel.SetActive(false);
    }

    private void Update()
    {
        if (isWaitingForInput)
        {
            // 1. ระบบกะพริบ (Fade In / Out)
            if (pressAnyKeyText != null)
            {
                Color textColor = pressAnyKeyText.color;
                // Mathf.PingPong จะวิ่งสลับไปมาระหว่าง 0 ถึง 1 ตามเวลา ทำให้ค่า Alpha โปร่งใสสลับทึบ
                textColor.a = Mathf.PingPong(Time.time * blinkSpeed, 1f);
                pressAnyKeyText.color = textColor;
            }

            // 2. ระบบเช็คการกดปุ่มใดๆ และเล่นเสียง
            if (Input.anyKeyDown)
            {
                isWaitingForInput = false;

                // คืนค่า Alpha ให้กลับมาเต็ม 100% (เผื่ออยากให้มันสว่างค้างตอนกดเสร็จ ก่อนเปลี่ยนหน้า)
                if (pressAnyKeyText != null)
                {
                    Color finalColor = pressAnyKeyText.color;
                    finalColor.a = 1f;
                    pressAnyKeyText.color = finalColor;
                }

                // สลับหน้า Panel
                pressAnyButtonPanel.SetActive(false);
                mainMenuPanel.SetActive(true);

                // เรียกใช้เสียงตอนกดปุ่มเข้า Main Menu
                if (ZiddikJari.AudioManager.Instance != null)
                {
                    ZiddikJari.AudioManager.Instance.PlayUIClick();
                }
            }
        }
    }
}