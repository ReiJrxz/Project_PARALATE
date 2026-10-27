using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// ตัวกลางระหว่าง Slider ในหน้า Option กับ AudioManager
/// ผูก Slider.OnValueChanged (Dynamic float) เข้ากับ SetXxxVolume และ (ไม่บังคับ) ลาก Slider มาใส่ช่องด้านล่าง
/// เพื่อให้ค่าเริ่มต้นของแถบตรงกับค่าที่บันทึกไว้
/// </summary>
public class AudioSettingsController : MonoBehaviour
{
    [Header("Sliders (ไม่บังคับ — ใช้ตั้งค่าเริ่มต้นตอนเปิดหน้า Option)")]
    [SerializeField] private Slider masterSlider;
    [SerializeField] private Slider musicSlider;
    [Tooltip("Master ของ SFX + UI")]
    [SerializeField] private Slider mastersfxSlider;
    [SerializeField] private Slider sfxSlider;
    [SerializeField] private Slider uiSlider;

    void OnEnable()
    {
        AudioManager audio = AudioManager.Instance;
        if (audio == null)
            return;

        SetSlider(masterSlider, audio.MasterVolume);
        SetSlider(musicSlider, audio.MusicVolume);
        SetSlider(mastersfxSlider, audio.MasterSfxVolume);
        SetSlider(sfxSlider, audio.SfxVolume);
        SetSlider(uiSlider, audio.UIVolume);
    }

    // ปิดหน้า Option = บันทึกค่า (ไม่ต้อง Save ทุกครั้งที่ลาก Slider)
    void OnDisable()
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.SaveSettings();
    }

    public void SetMasterVolume(float value)
    {
        if (AudioManager.Instance != null) AudioManager.Instance.SetMasterVolume(value);
    }

    public void SetMusicVolume(float value)
    {
        if (AudioManager.Instance != null) AudioManager.Instance.SetMusicVolume(value);
    }

    // ผูกกับ Slider "Master SFX" (คุม SFX + UI พร้อมกัน)
    public void SetMasterSFXVolume(float value)
    {
        if (AudioManager.Instance != null) AudioManager.Instance.SetMasterSfxVolume(value);
    }

    public void SetSFXVolume(float value)
    {
        if (AudioManager.Instance != null) AudioManager.Instance.SetSfxVolume(value);
    }

    public void SetUIVolume(float value)
    {
        if (AudioManager.Instance != null) AudioManager.Instance.SetUIVolume(value);
    }

    private static void SetSlider(Slider slider, float value)
    {
        if (slider != null)
            slider.SetValueWithoutNotify(value);
    }
}
