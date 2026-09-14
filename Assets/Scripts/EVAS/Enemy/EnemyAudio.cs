using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class EnemyAudioController : MonoBehaviour
{
    [Header("Footstep Audio")]
    public AudioClip[] footstepClips;
    [Range(0f, 0.2f)] public float footstepPitchVariance = 0.08f;
    [Range(0f, 0.3f)] public float footstepVolumeVariance = 0.15f;
    [Range(0f, 1f)] public float footstepBaseVolume = 0.8f;
    public float baseFootstepInterval = 0.45f;
    [Tooltip("ความเร็วอ้างอิงที่ interval เท่ากับ baseFootstepInterval พอดี (ควรใกล้เคียง patrol speed)")]
    public float referenceSpeed = 3.5f;
    public float minFootstepInterval = 0.2f;
    public float movementThreshold = 0.1f;

    [Header("Gunfire Audio")]
    public AudioClip[] fireClips;
    [Range(0f, 1f)] public float fireVolume = 1f;

    [Header("Alert Audio")]
    public AudioClip alertSound;
    [Range(0f, 1f)] public float alertVolume = 1f;

    private AudioSource audioSource;
    private float nextFootstepTime;
    private int lastFootstepIndex = -1;
    private int lastFireIndex = -1;

    void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        audioSource.playOnAwake = false;
    }

    /// <summary>เรียกทุกเฟรมพร้อมความเร็วปัจจุบันของ agent จัดการ cooldown ให้เอง</summary>
    public void HandleFootstep(float currentSpeed)
    {
        if (currentSpeed < movementThreshold || Time.time < nextFootstepTime)
            return;

        PlayFootstepSound();

        float speedRatio = Mathf.Max(currentSpeed, 0.1f) / referenceSpeed;
        float interval = baseFootstepInterval / speedRatio;
        nextFootstepTime = Time.time + Mathf.Max(interval, minFootstepInterval);
    }

    void PlayFootstepSound()
    {
        if (audioSource == null || footstepClips == null || footstepClips.Length == 0)
            return;

        int index = PickIndex(footstepClips.Length, ref lastFootstepIndex);
        AudioClip clip = footstepClips[index];
        if (clip == null) return;

        audioSource.pitch = 1f + Random.Range(-footstepPitchVariance, footstepPitchVariance);
        float volume = footstepBaseVolume + Random.Range(-footstepVolumeVariance, footstepVolumeVariance);
        audioSource.PlayOneShot(clip, Mathf.Clamp01(volume));
    }

    public void PlayFireSound()
    {
        if (audioSource == null || fireClips == null || fireClips.Length == 0)
            return;

        int index = PickIndex(fireClips.Length, ref lastFireIndex);
        AudioClip clip = fireClips[index];
        if (clip == null) return;

        audioSource.pitch = 1f; // เสียงยิงมักไม่อยากให้ pitch เพี้ยนมาก
        audioSource.PlayOneShot(clip, fireVolume);
    }

    public void PlayAlertSound()
    {
        if (audioSource == null || alertSound == null)
            return;

        audioSource.pitch = 1f;
        audioSource.PlayOneShot(alertSound, alertVolume);
    }

    int PickIndex(int length, ref int lastIndex)
    {
        if (length == 1) return 0;

        int index;
        do
        {
            index = Random.Range(0, length);
        } while (index == lastIndex);

        lastIndex = index;
        return index;
    }
}