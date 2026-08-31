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
                Vector3 lookDirection = climbStartPoint.forward;
                player.StartClimbing(lookDirection, climbStartPoint);
            }
        }
    }
}