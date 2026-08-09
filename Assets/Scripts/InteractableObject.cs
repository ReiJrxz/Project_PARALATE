using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

public class InteractableObject : MonoBehaviour
{
    [Header("การตั้งค่าสิ่งของ")]
    public string interactMessage = "กด E เพื่อโต้ตอบ";
    public InputActionReference interactAction;

    [Header("ผลลัพธ์เมื่อกด Interact")]
    public UnityEvent onInteract;

    // --- ส่วนที่เพิ่มเข้ามา ---
    // เก็บค่าตัวละครผู้เล่นที่เดินเข้ามาใน Trigger
    [HideInInspector] public GameObject interactingPlayer;
    private bool canInteract = false;

    void Update()
    {
        if (canInteract && interactAction.action.WasPressedThisFrame())
        {
            onInteract.Invoke();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            canInteract = true;
            interactingPlayer = other.gameObject; // จำตัวผู้เล่นไว้
            // InteractUIManager.Instance.ShowInteract(interactMessage); // คอมเมนต์ไว้ถ้ายังไม่มีระบบ UI
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            canInteract = false;
            interactingPlayer = null; // ล้างค่าเมื่อเดินออก
            // InteractUIManager.Instance.HideInteract();
        }
    }
}