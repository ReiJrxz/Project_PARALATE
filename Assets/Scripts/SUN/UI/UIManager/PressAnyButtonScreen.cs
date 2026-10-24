using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

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
        if (pressAnyButtonPanel != null) pressAnyButtonPanel.SetActive(true);
        if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
    }

    private void Update()
    {
        if (!isWaitingForInput)
            return;

        // 1. ระบบกะพริบ (Fade In / Out): PingPong วิ่งสลับ 0-1 ทำให้ Alpha โปร่งใสสลับทึบ
        if (pressAnyKeyText != null)
        {
            Color textColor = pressAnyKeyText.color;
            textColor.a = Mathf.PingPong(Time.time * blinkSpeed, 1f);
            pressAnyKeyText.color = textColor;
        }

        // 2. เช็คการกดปุ่มใดๆ
        if (!WasAnyInputPressedThisFrame())
            return;

        isWaitingForInput = false;

        // คืนค่า Alpha ให้เต็ม 100%
        if (pressAnyKeyText != null)
        {
            Color finalColor = pressAnyKeyText.color;
            finalColor.a = 1f;
            pressAnyKeyText.color = finalColor;
        }

        // สลับหน้า Panel
        if (pressAnyButtonPanel != null) pressAnyButtonPanel.SetActive(false);
        if (mainMenuPanel != null) mainMenuPanel.SetActive(true);

        // เสียงตอนกดปุ่มเข้า Main Menu
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayUIClick();
    }

    // ใช้ Input System ใหม่ (เหมือนส่วนอื่นของโปรเจกต์) แทน Input.anyKeyDown ของระบบเก่า
    // ซึ่งจะ error ถ้า Player Settings ตั้ง Active Input Handling เป็น "Input System Package" อย่างเดียว
    private static bool WasAnyInputPressedThisFrame()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard != null && keyboard.anyKey.wasPressedThisFrame)
            return true;

        Mouse mouse = Mouse.current;
        if (mouse != null &&
            (mouse.leftButton.wasPressedThisFrame ||
             mouse.rightButton.wasPressedThisFrame ||
             mouse.middleButton.wasPressedThisFrame))
            return true;

        foreach (Gamepad pad in Gamepad.all)
        {
            if (pad.buttonSouth.wasPressedThisFrame || pad.buttonEast.wasPressedThisFrame ||
                pad.buttonWest.wasPressedThisFrame || pad.buttonNorth.wasPressedThisFrame ||
                pad.startButton.wasPressedThisFrame)
                return true;
        }

        return false;
    }
}