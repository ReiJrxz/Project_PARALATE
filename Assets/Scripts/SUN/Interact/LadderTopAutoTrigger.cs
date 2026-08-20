using UnityEngine;

public class LadderTopAutoTrigger : MonoBehaviour
{
    [Header("จุดเริ่มปีนด้านบนสุด")]
    public Transform topStartPoint;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            TopDownPlayerController player = other.GetComponent<TopDownPlayerController>();

            // ถ้าดึงสคริปต์ผู้เล่นมาได้ และไม่ได้กำลังเกาะบันไดอยู่
            if (player != null && !player.IsMovementLocked)
            {
                // สั่งให้เกาะบันไดทันที (เกาะนิ่งๆ รอกดปุ่มลง)
                Vector3 lookDirection = topStartPoint.forward;
                player.StartClimbing(lookDirection, topStartPoint);
            }
        }
    }
}