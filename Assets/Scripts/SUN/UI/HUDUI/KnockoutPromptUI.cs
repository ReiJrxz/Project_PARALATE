using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// UI ของ KnockoutSystem: ข้อความบอกปุ่ม (โชว์เมื่ออยู่ข้างหลังศัตรู) + หลอดนับถอยหลังตอน Kill/Mercy
/// หลอดเต็มตอนเริ่ม แล้วลดลงตามเวลาที่เหลือ ถ้าปล่อย Ctrl ก่อนเสร็จ หลอดจะค่อยๆ เพิ่มกลับจนเต็มแล้วซ่อน
///
/// โครง UI ที่ต้องสร้างใน Canvas:
///   Kill&Mercy                 (กลุ่มแม่ ห้ามใส่ในช่อง Root ใดๆ)
///   ├─ Mercy&KillText          ← promptRoot + promptText   (ข้อความปุ่ม)
///   └─ Mercy&KillBar           ← barRoot                   (กลุ่มของหลอดทั้งหมด)
///      ├─ Bar (Slider)         ← barSlider  (หรือใช้ barFill แทน ถ้าไม่ใช้ Slider)
///      ├─ ActionLabel          ← barLabel   (ไม่บังคับ)
///      └─ TimeLabel            ← barTimeText (ไม่บังคับ)
///
/// สำคัญ: promptRoot กับ barRoot ต้อง "ไม่ซ้อนกัน" (ตัวหนึ่งห้ามเป็นลูกของอีกตัว) ไม่งั้นซ่อนข้อความแล้วหลอดหายไปด้วย
/// </summary>
public class KnockoutPromptUI : MonoBehaviour
{
    [Header("Prompt (โชว์เมื่ออยู่ข้างหลังศัตรู)")]
    public GameObject promptRoot;
    public TMP_Text promptText;
    [TextArea] public string promptMessage = "LeftCtrl to mercy\nF to kill";

    [Header("Countdown Bar")]
    public GameObject barRoot;
    [Tooltip("ใช้ Slider ทำหลอด (ไม่บังคับ): ระบบจะตั้ง Min 0 / Max 1 และปิดการลากให้เอง ถ้าใส่ช่องนี้ไม่ต้องใส่ Bar Fill")]
    public Slider barSlider;
    [Tooltip("ทางเลือกแทน Slider: RectTransform ของแถบสีที่หดลง ระบบจะตั้ง Anchor ให้เอง")]
    public RectTransform barFill;
    public TMP_Text barLabel;
    public TMP_Text barTimeText;
    public string killLabel = "KILL";
    public string mercyLabel = "MERCY";

    [Tooltip("ความเร็วที่หลอดเพิ่มกลับตอนปล่อย Ctrl ก่อนเสร็จ (สัดส่วนของหลอดต่อวินาที)")]
    public float refillSpeed = 2f;

    private float totalSeconds = 1f;
    private float displayedFraction = 1f;
    private bool barActive;
    private bool refilling;

    private void Awake()
    {
        ValidateSetup();

        if (barSlider != null)
        {
            barSlider.minValue = 0f;
            barSlider.maxValue = 1f;
            barSlider.wholeNumbers = false;
            barSlider.interactable = false;                       // ผู้เล่นลากเองไม่ได้
            barSlider.transition = Selectable.Transition.None;    // ไม่ให้สีจางเพราะ interactable = false
        }

        if (barFill != null && barSlider == null)
        {
            // ยืดเต็มพื้นหลังก่อน แล้วค่อยลดความกว้างด้วย anchorMax.x (ไม่ต้องใช้ Sprite แบบ Filled)
            barFill.anchorMin = Vector2.zero;
            barFill.anchorMax = Vector2.one;
            barFill.offsetMin = Vector2.zero;
            barFill.offsetMax = Vector2.zero;
        }

        if (promptText != null)
            promptText.text = promptMessage;

        SetPromptVisible(false);
        EndCountdown();
    }

    private void Update()
    {
        if (!barActive || !refilling)
            return;

        displayedFraction = Mathf.MoveTowards(displayedFraction, 1f, refillSpeed * Time.deltaTime);
        ApplyFraction();

        if (displayedFraction >= 1f)
            EndCountdown();
    }

    public void SetPromptVisible(bool visible)
    {
        if (promptRoot != null && promptRoot.activeSelf != visible)
            promptRoot.SetActive(visible);
    }

    /// <summary>เริ่มหลอดนับถอยหลัง (หลอดเต็ม) isKill = true สำหรับ F, false สำหรับ Mercy (Ctrl)</summary>
    public void BeginCountdown(bool isKill, float seconds)
    {
        totalSeconds = Mathf.Max(0.01f, seconds);
        displayedFraction = 1f;
        barActive = true;
        refilling = false;

        if (barLabel != null)
            barLabel.text = isKill ? killLabel : mercyLabel;

        if (barRoot != null)
            barRoot.SetActive(true);

        ApplyFraction();
    }

    /// <summary>อัปเดตเวลาที่เหลือ (เรียกทุกเฟรมระหว่างนับถอยหลัง)</summary>
    public void SetRemaining(float remainingSeconds)
    {
        if (!barActive)
            return;

        refilling = false;
        displayedFraction = Mathf.Clamp01(remainingSeconds / totalSeconds);
        ApplyFraction();
    }

    /// <summary>ผู้เล่นปล่อยก่อนเสร็จ: หลอดค่อยๆ เพิ่มกลับจนเต็มแล้วซ่อน</summary>
    public void ReleaseEarly()
    {
        if (barActive)
            refilling = true;
    }

    /// <summary>เสร็จสิ้นหรือยกเลิกทันที: ซ่อนหลอด</summary>
    public void EndCountdown()
    {
        barActive = false;
        refilling = false;

        if (barRoot != null && barRoot.activeSelf)
            barRoot.SetActive(false);
    }

    // ตรวจการลากช่อง UI ผิดที่พบบ่อย: promptRoot กับ barRoot ซ้อนกัน
    private void ValidateSetup()
    {
        if (promptRoot != null && barRoot != null &&
            (barRoot.transform.IsChildOf(promptRoot.transform) || promptRoot.transform.IsChildOf(barRoot.transform)))
        {
            Debug.LogError("KnockoutPromptUI: Prompt Root และ Bar Root ต้องเป็นคนละวัตถุที่ไม่ซ้อนกัน " +
                           "(เช่น Prompt Root = ข้อความ, Bar Root = กลุ่มหลอด) ไม่งั้นซ่อนข้อความแล้วหลอดจะหายไปด้วย", this);
        }
    }

    private void ApplyFraction()
    {
        if (barSlider != null)
            barSlider.SetValueWithoutNotify(displayedFraction);
        else if (barFill != null)
            barFill.anchorMax = new Vector2(displayedFraction, 1f);

        if (barTimeText != null)
            barTimeText.SetText("{0:1}s", displayedFraction * totalSeconds);
    }
}
