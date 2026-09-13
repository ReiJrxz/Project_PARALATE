using UnityEngine;
using System;
using System.Collections.Generic;

[RequireComponent(typeof(AudioSource))]
public class PlayerAudioController : MonoBehaviour
{
    [Serializable]
    public class SurfaceSound
    {
        public string surfaceTag = "Untagged"; // Tag ของพื้นผิว เช่น "Grass", "Wood", "Stone", "Water"
        public AudioClip[] footstepClips;
    }

    [Header("Surface-Based Footsteps")]
    [Tooltip("ชุดเสียงเดินตามพื้นผิว ต่อ Tag ของ Collider พื้น")]
    public SurfaceSound[] surfaceSounds;
    [Tooltip("ชุดเสียงสำรอง ถ้า raycast ไม่เจอพื้น หรือไม่เจอ Tag ที่ตรงกัน")]
    public AudioClip[] defaultFootstepClips;

    [Header("Ground Detection")]
    public float groundCheckDistance = 1.2f;
    public LayerMask groundLayer = ~0; // default: ชนได้ทุก layer, ปรับใน Inspector ให้เหลือเฉพาะพื้น

    [Header("Audio Clips")]
    public AudioClip whistleClip;

    [Header("Footstep Variation")]
    [Range(0f, 0.2f)] public float pitchVariance = 0.08f;
    [Range(0f, 0.3f)] public float volumeVariance = 0.15f;
    [Range(0f, 1f)] public float footstepBaseVolume = 1f;

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
    private int lastFootstepIndex = -1;
    private readonly Collider[] noiseBuffer = new Collider[16];
    private Dictionary<string, AudioClip[]> surfaceLookup;

    void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        BuildSurfaceLookup();
    }

    void BuildSurfaceLookup()
    {
        surfaceLookup = new Dictionary<string, AudioClip[]>();
        if (surfaceSounds == null) return;

        foreach (var entry in surfaceSounds)
        {
            if (entry == null || string.IsNullOrEmpty(entry.surfaceTag))
                continue;

            // ถ้ามี tag ซ้ำ ใช้ตัวแรกที่เจอ ไม่ทับด้วยตัวหลัง
            if (!surfaceLookup.ContainsKey(entry.surfaceTag))
                surfaceLookup.Add(entry.surfaceTag, entry.footstepClips);
        }
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

            AudioClip[] clips = GetClipsForCurrentSurface();
            PlayFootstepSound(clips);
        }

        float interval = isSprinting ? footstepInterval * sprintIntervalMultiplier : footstepInterval;
        nextFootstepTime = Time.time + interval;
    }

    AudioClip[] GetClipsForCurrentSurface()
    {
        Vector3 origin = transform.position + Vector3.up * 0.1f;

        if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, groundCheckDistance, groundLayer, QueryTriggerInteraction.Ignore))
        {
            if (surfaceLookup.TryGetValue(hit.collider.tag, out AudioClip[] clips) && clips != null && clips.Length > 0)
                return clips;
        }

        return defaultFootstepClips;
    }

    void PlayFootstepSound(AudioClip[] clips)
    {
        if (audioSource == null || clips == null || clips.Length == 0)
            return;

        int index;
        if (clips.Length == 1)
        {
            index = 0;
        }
        else
        {
            do
            {
                index = UnityEngine.Random.Range(0, clips.Length);
            } while (index == lastFootstepIndex);
        }
        lastFootstepIndex = index;

        AudioClip clip = clips[index];
        if (clip == null) return;

        audioSource.pitch = 1f + UnityEngine.Random.Range(-pitchVariance, pitchVariance);
        float volume = footstepBaseVolume + UnityEngine.Random.Range(-volumeVariance, volumeVariance);
        audioSource.PlayOneShot(clip, Mathf.Clamp01(volume));
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