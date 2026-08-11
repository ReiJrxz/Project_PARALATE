using UnityEngine;
using UnityEngine.Events; // สำคัญมาก: ต้องมีเพื่อใช้งาน UnityEvent
using UnityEngine.InputSystem;

public class InteractableObject : MonoBehaviour
{
    [Header("การตั้งค่าสิ่งของ")]
    public string interactMessage = "เปิดประตู";
    public InputActionReference interactAction;

    [Header("ผลลัพธ์เมื่อกด Interact")]
    public UnityEvent onInteract; // หัวใจสำคัญที่จะทำให้เกิดผลลัพธ์ต่างกัน

    private bool canInteract = false;

    void Update()
    {
        if (!canInteract)
            return;

        if (interactAction == null || interactAction.action == null)
            return;

        if (interactAction.action.WasPressedThisFrame())
        {
            // เมื่อกดปุ่ม สั่งให้ UnityEvent ทำงานตามที่ตั้งค่าไว้ใน Inspector
            onInteract?.Invoke();

            // (Optional) หากต้องการให้กดได้แค่ครั้งเดียว แล้วของชิ้นนั้นพังหรือเปิดค้างไปเลย
            // gameObject.GetComponent<Collider>().enabled = false;
            // InteractUIManager.Instance.HideInteract();
            // canInteract = false;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            canInteract = true;
            if (InteractUIManager.Instance != null)
                InteractUIManager.Instance.ShowInteract(interactMessage);
            else
                Debug.LogWarning("InteractableObject could not show interact UI because no InteractUIManager exists in the scene.", this);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            canInteract = false;
            if (InteractUIManager.Instance != null)
                InteractUIManager.Instance.HideInteract();
        }
    }
}
