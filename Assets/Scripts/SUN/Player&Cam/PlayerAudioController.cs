using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class PlayerAudioController : MonoBehaviour
{
    [Header("Audio Clips")]
    public AudioClip footstepClip;
    public AudioClip whistleClip;

    [Header("Footstep Timing")]
    public float footstepInterval = 0.5f;
    [Tooltip("ตัวคูณ interval ตอนวิ่ง (ยิ่งน้อยยิ่งถี่)")]
    public float sprintIntervalMultiplier = 0.7f;

    [Header("Noise Radius (สำหรับ Stealth AI)")]
    public float walkNoiseRadius = 7f;
    public float sprintNoiseRadius = 15f;
    public float whistleNoiseRadius = 20f;
    public LayerMask enemyLayer;

    private AudioSource audioSource;
    private float nextFootstepTime = 0f;
    private readonly Collider[] noiseBuffer = new Collider[16];

    void Awake()
    {
        audioSource = GetComponent<AudioSource>();
    }

    /// <summary>
    /// เรียกทุกเฟรมที่ตัวละครกำลังเดิน/วิ่งบนพื้น
    /// จัดการ cooldown ระหว่างฝีเท้าให้เอง
    /// </summary>
    public void HandleFootstep(bool isGrounded, bool isSprinting, bool isCrouching)
    {
        if (!isGrounded || Time.time < nextFootstepTime) return;

        if (!isCrouching)
        {
            float radius = isSprinting ? sprintNoiseRadius : walkNoiseRadius;
            EmitNoise(radius);

            if (audioSource != null && footstepClip != null)
            {
                audioSource.PlayOneShot(footstepClip);
            }
        }

        float interval = isSprinting ? footstepInterval * sprintIntervalMultiplier : footstepInterval;
        nextFootstepTime = Time.time + interval;
    }

    public void PlayWhistle()
    {
        if (audioSource != null && whistleClip != null)
        {
            audioSource.PlayOneShot(whistleClip);
        }
        EmitNoise(whistleNoiseRadius);
        Debug.Log("เป่าปากล่อศัตรู! รัศมี: " + whistleNoiseRadius);
    }

    void EmitNoise(float radius)
    {
        int count = Physics.OverlapSphereNonAlloc(transform.position, radius, noiseBuffer, enemyLayer);
        for (int i = 0; i < count; i++)
        {
            // ส่งสัญญาณไปปลุก AI ในอนาคตได้จากตรงนี้ เช่น noiseBuffer[i].GetComponent<EnemyAI>()?.OnHearNoise(transform.position);
        }
    }
}