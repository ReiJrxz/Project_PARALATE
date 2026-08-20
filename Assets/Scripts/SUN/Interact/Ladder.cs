using UnityEngine;

[RequireComponent(typeof(InteractableObject))]
public class Ladder : MonoBehaviour
{
    [Header("การตั้งค่าบันได")]
    public Transform climbStartPoint;

    private InteractableObject interactable;

    void Start()
    {
        interactable = GetComponent<InteractableObject>();
    }

    public void ActivateLadder()
    {
        if (interactable.interactingPlayer != null)
        {
            TopDownPlayerController player = interactable.interactingPlayer.GetComponent<TopDownPlayerController>();

            if (player != null && climbStartPoint != null)
            {
                // เปลี่ยนมาใช้ทิศทางแกน Z ของ climbStartPoint แทน 
                // (ให้คุณหมุนลูกศรสีน้ำเงินของจุดปีนนี้ ให้พุ่งเข้าหาบันไดใน Scene ได้เลย)
                Vector3 lookDirection = climbStartPoint.forward;
                player.StartClimbing(lookDirection, climbStartPoint);
            }
        }
    }
}