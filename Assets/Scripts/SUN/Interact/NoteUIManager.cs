using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class NoteUIManager : MonoBehaviour
{
    public static NoteUIManager Instance;

    [Header("UI References")]
    public GameObject notePanel;
    public TMP_Text noteTextUI; // ถ้าใช้ TextMeshPro เปลี่ยนเป็น TMP_Text แทน

    void Awake()
    {
        Instance = this;
        if (notePanel != null) notePanel.SetActive(false);
    }

    public void ShowNote(string text)
    {
        if (noteTextUI != null) noteTextUI.text = text;
        if (notePanel != null) notePanel.SetActive(true);

        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    public void HideNote()
    {
        if (notePanel != null) notePanel.SetActive(false);

        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Confined;
    }
}