using System.Collections;
using UnityEngine;

public class SlidingDoor : MonoBehaviour
{
    [Header("การตั้งค่าบานเลื่อน")]
    public Vector3 slideOffset = new Vector3(2f, 0f, 0f); // เลื่อนไปตามแกน X 2 หน่วย
    public float slideSpeed = 5f; // ความเร็วในการเลื่อน
    public float holdTime = 3f; // เวลาที่จะเปิดค้างไว้ก่อนปิดเอง (หน่วยเป็นวินาที)

    private Vector3 closedPosition;
    private Vector3 openPosition;
    private Coroutine doorSequenceCoroutine;

    void Start()
    {
        // จดจำตำแหน่งตอนปิด และคำนวณตำแหน่งตอนเปิดเตรียมไว้
        closedPosition = transform.position;
        openPosition = closedPosition + slideOffset;
    }

    // ฟังก์ชันนี้จะถูกเรียกเมื่อผู้เล่นกดปุ่ม E ผ่าน UnityEvent ที่ InteractZone
    public void ActivateDoor()
    {
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
}