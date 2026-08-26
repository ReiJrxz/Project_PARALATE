using UnityEngine;

[RequireComponent(typeof(InteractableObject))]
public class NoteReader : MonoBehaviour
{
    [Header("เนื้อหาโน้ต")]
    [TextArea(3, 10)]
    public string noteText = "เนื้อหาโน้ตตรงนี้...";

    private InteractableObject interactable;
    private bool isReading = false;

    void Start()
    {
        interactable = GetComponent<InteractableObject>();
    }

    // ผูกฟังก์ชันนี้เข้ากับ onInteract ใน Inspector (จุดเดียวที่ฟังปุ่ม E)
    public void OnInteractPressed()
    {
        if (isReading)
        {
            CloseNote();
        }
        else
        {
            OpenNote();
        }
    }

    void OpenNote()
    {
        isReading = true;

        if (interactable.interactingPlayer != null)
        {
            TopDownPlayerController player = interactable.interactingPlayer.GetComponent<TopDownPlayerController>();
            if (player != null) player.SetMovementLocked(true);
        }

        NoteUIManager.Instance.ShowNote(noteText);
    }

    void CloseNote()
    {
        isReading = false;

        if (interactable.interactingPlayer != null)
        {
            TopDownPlayerController player = interactable.interactingPlayer.GetComponent<TopDownPlayerController>();
            if (player != null) player.SetMovementLocked(false);
        }

        NoteUIManager.Instance.HideNote();
    }
}