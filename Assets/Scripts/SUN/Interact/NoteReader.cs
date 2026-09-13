using UnityEngine;

[RequireComponent(typeof(Collider))]
public class NoteReader : MonoBehaviour, IInteractable
{
    [Header("เนื้อหาโน้ต")]
    [TextArea(3, 10)]
    public string noteText = "เนื้อหาโน้ตตรงนี้...";

    private bool isReading = false;
    private GameObject currentReader;

    public string PromptText => isReading ? "[ E ] to close" : "[ E ] to read";

    public void Interact(GameObject interactor)
    {
        if (isReading)
            CloseNote();
        else
            OpenNote(interactor);
    }

    void OpenNote(GameObject interactor)
    {
        isReading = true;
        currentReader = interactor;

        TopDownPlayerController player = interactor.GetComponent<TopDownPlayerController>();
        if (player != null) player.SetMovementLocked(true);

        NoteUIManager.Instance.ShowNote(noteText);
    }

    void CloseNote()
    {
        isReading = false;

        if (currentReader != null)
        {
            TopDownPlayerController player = currentReader.GetComponent<TopDownPlayerController>();
            if (player != null) player.SetMovementLocked(false);
        }

        currentReader = null;
        NoteUIManager.Instance.HideNote();
    }
}