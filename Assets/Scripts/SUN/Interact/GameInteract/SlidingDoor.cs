using System.Collections;
using UnityEngine;

public class SlidingDoor : MonoBehaviour, IInteractPromptSource
{
    [Header("การตั้งค่าบานเลื่อน")]
    public Vector3 slideOffset = new Vector3(2f, 0f, 0f); // เลื่อนไปตามแกน X 2 หน่วย
    public float slideSpeed = 5f; // ความเร็วในการเลื่อน
    public float holdTime = 3f; // เวลาที่จะเปิดค้างไว้ก่อนปิดเอง (หน่วยเป็นวินาที)

    [Header("การเข้าถึง (Keycard)")]
    [Tooltip("0 = ไม่ต้องใช้ Keycard, 1 = ต้องมี Keycard Lv.1 ขึ้นไป, 2 = Lv.2 ขึ้นไป ...")]
    [Min(0)] public int requiredKeycardLevel = 0;

    [Header("ข้อความ Prompt")]
    public string openPrompt = "กด E เพื่อเปิด";
    [Tooltip("{0} = ระดับ Keycard ที่ต้องใช้")]
    public string lockedPrompt = "ต้องใช้ Keycard Lv.{0}";

    [Header("เสียง (ไม่บังคับ)")]
    public AudioClip openSound;
    public AudioClip deniedSound;

    [Header("ปุ่มของประตูนี้ (ไม่บังคับ)")]
    [Tooltip("ลาก InteractableObject ของปุ่ม/โซนที่เปิดประตูนี้มาใส่ เพื่อให้ปุ่มแสดงข้อความตามสถานะบัตร " +
             "(ถ้าปุ่มเป็นลูกของประตูอยู่แล้วไม่ต้องใส่)")]
    public InteractableObject[] interactZones;

    private Vector3 closedPosition;
    private Vector3 openPosition;
    private Coroutine doorSequenceCoroutine;

    private string lockedPromptText;
    private int lockedPromptLevel = -1;

    void Awake()
    {
        if (interactZones == null)
            return;

        for (int i = 0; i < interactZones.Length; i++)
        {
            if (interactZones[i] != null)
                interactZones[i].SetPromptSource(this);
        }
    }

    void Start()
    {
        // จดจำตำแหน่งตอนปิด และคำนวณตำแหน่งตอนเปิดเตรียมไว้
        closedPosition = transform.position;
        openPosition = closedPosition + slideOffset;
    }

    public bool HasAccess()
    {
        return requiredKeycardLevel <= 0 || PlayerKeycards.CurrentLevel >= requiredKeycardLevel;
    }

    public string GetPromptMessage(GameObject player)
    {
        if (HasAccess())
            return openPrompt;

        // สร้างข้อความครั้งเดียวต่อระดับบัตร (ไม่สร้าง string ใหม่ทุกเฟรม)
        if (lockedPromptLevel != requiredKeycardLevel)
        {
            lockedPromptLevel = requiredKeycardLevel;
            lockedPromptText = string.Format(lockedPrompt, requiredKeycardLevel);
        }

        return lockedPromptText;
    }

    // ฟังก์ชันนี้จะถูกเรียกเมื่อผู้เล่นกดปุ่ม E ผ่าน UnityEvent ที่ InteractZone
    public void ActivateDoor()
    {
        // ไม่มี Keycard ตามที่ต้องการ: ไม่เปิด (เช็คตรงนี้ด้วย กัน UnityEvent เดิมเรียกข้ามการเช็ค)
        if (!HasAccess())
        {
            PlaySound(deniedSound);
            return;
        }

        PlaySound(openSound);

        // หากประตูกำลังทำงานอยู่ (เช่น กำลังเลื่อนปิด) ให้หยุดคำสั่งเดิมทันที
        if (doorSequenceCoroutine != null)
        {
            StopCoroutine(doorSequenceCoroutine);
        }

        // เริ่มลำดับการทำงานใหม่: เปิด -> ค้าง -> ปิด
        doorSequenceCoroutine = StartCoroutine(DoorSequenceRoutine());
    }

    private IEnumerator DoorSequenceRoutine()
    {
        // ลำดับที่ 1: สไลด์เปิด
        while (Vector3.Distance(transform.position, openPosition) > 0.01f)
        {
            transform.position = Vector3.Lerp(transform.position, openPosition, slideSpeed * Time.deltaTime);
            yield return null;
        }
        transform.position = openPosition; // วางให้เป๊ะ

        // ลำดับที่ 2: รอเวลา (Hold time)
        yield return new WaitForSeconds(holdTime);

        // ลำดับที่ 3: สไลด์ปิดกลับที่เดิม
        while (Vector3.Distance(transform.position, closedPosition) > 0.01f)
        {
            transform.position = Vector3.Lerp(transform.position, closedPosition, slideSpeed * Time.deltaTime);
            yield return null;
        }
        transform.position = closedPosition; // วางให้เป๊ะ
    }

    // เล่นผ่านกลุ่ม SFX ของ AudioManager (ถ้าไม่มี AudioManager ใช้ PlayClipAtPoint แทน)
    private void PlaySound(AudioClip clip)
    {
        AudioManager.PlayPickup(clip, transform.position);
    }
}
