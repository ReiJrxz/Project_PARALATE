using UnityEngine;

[RequireComponent(typeof(InteractableObject))]
public class Ladder : MonoBehaviour, IInteractPromptSource
{
    [Header("การตั้งค่าบันได")]
    public Transform climbStartPoint;

    [Header("ข้อความ Prompt")]
    public string enterPrompt = "กด E เพื่อปีน";
    [Tooltip("โชว์ตอนกำลังเกาะบันไดอยู่ (กด E อีกครั้งเพื่อปล่อย) เว้นว่าง = ไม่โชว์")]
    public string exitPrompt = "กด E เพื่อลงจากบันได";

    private InteractableObject interactable;
    private GameObject cachedPlayerObject;
    private TopDownPlayerController cachedPlayer;

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
                Vector3 lookDirection = climbStartPoint.forward;
                player.StartClimbing(lookDirection, climbStartPoint);
            }
        }
    }

    // InteractableObject บนวัตถุเดียวกันจะเรียกใช้ทุกเฟรมที่ผู้เล่นอยู่ในโซน
    public string GetPromptMessage(GameObject player)
    {
        if (player != cachedPlayerObject)
        {
            cachedPlayerObject = player;
            cachedPlayer = player != null ? player.GetComponentInParent<TopDownPlayerController>() : null;
        }

        return cachedPlayer != null && cachedPlayer.IsClimbing ? exitPrompt : enterPrompt;
    }
}
