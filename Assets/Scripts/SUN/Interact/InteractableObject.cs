using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

public class InteractableObject : MonoBehaviour
{
    [Header("การตั้งค่าสิ่งของ")]
    [Tooltip("ข้อความ prompt เริ่มต้น (ถ้ามี Prompt Source จะใช้ข้อความจากตัวนั้นแทน)")]
    public string interactMessage = "กด E เพื่อโต้ตอบ";
    public InputActionReference interactAction;

    // ไม่ต้องตั้งเอง: บันไดหาจากตัวเอง/parent อัตโนมัติ ส่วนประตูให้ลากปุ่มนี้ไปใส่ช่อง Interact Zones ของ SlidingDoor
    [HideInInspector] public MonoBehaviour promptSource;

    [Header("ผลลัพธ์เมื่อกด Interact")]
    public UnityEvent onInteract;

    // เก็บค่าตัวละครผู้เล่นที่เดินเข้ามาใน Trigger
    [HideInInspector] public GameObject interactingPlayer;

    [Header("Debug")]
    [Tooltip("เปิดเพื่อดูใน Console ว่าผู้เล่นเข้า/ออกโซนนี้หรือไม่ (ใช้ไล่ปัญหา prompt ไม่ขึ้น)")]
    public bool debugLog = false;

    private TopDownPlayerController playerController;
    private IInteractPromptSource promptProvider;
    private bool canInteract = false;
    private bool warnedMissingManager;

    private void Awake()
    {
        // SlidingDoor อาจผูกตัวเองมาก่อนแล้ว (SetPromptSource) ห้ามเขียนทับ
        if (promptProvider != null)
            return;

        promptProvider = promptSource as IInteractPromptSource;

        if (promptProvider == null)
            promptProvider = GetComponentInParent<IInteractPromptSource>();
    }

    private void OnValidate()
    {
        if (promptSource != null && !(promptSource is IInteractPromptSource))
        {
            Debug.LogWarning($"{promptSource.GetType().Name} ไม่ได้เป็น IInteractPromptSource จึงใช้เป็น Prompt Source ไม่ได้", this);
            promptSource = null;
        }
    }

    // ให้ตัวอื่น (เช่น SlidingDoor) ผูกตัวเองเป็นผู้บอกข้อความ prompt ของปุ่มนี้
    public void SetPromptSource(IInteractPromptSource source)
    {
        promptProvider = source;
    }

    private void OnDisable()
    {
        canInteract = false;
        HidePrompt();
    }

    void Update()
    {
        if (!canInteract)
            return;

        // ผู้เล่นถูกล็อกอยู่ (อ่านโน้ต, รัดคอ, ตาย ฯลฯ): ซ่อน prompt และไม่รับปุ่ม
        if (playerController != null && playerController.IsMovementLocked)
        {
            HidePrompt();
            return;
        }

        ShowPrompt();

        if (interactAction != null && interactAction.action.WasPressedThisFrame())
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
            playerController = other.GetComponentInParent<TopDownPlayerController>();

            if (debugLog)
            {
                InteractUIManager ui = InteractUIManager.Instance;
                string uiStatus = ui == null ? "ไม่พบ InteractUIManager" : (ui.HasUI ? "พบ (ช่อง UI ครบ)" : "พบ แต่ช่อง Panel/Text ยังไม่ครบ");

                Debug.Log($"[{name}] ผู้เล่นเข้าโซน | controller: {(playerController != null ? "พบ" : "ไม่พบ")} | " +
                          $"ตัวบอกข้อความ: {(promptProvider != null ? promptProvider.GetType().Name : "ไม่มี (ใช้ Interact Message)")} | " +
                          $"UI: {uiStatus}", this);
            }
        }
        else if (debugLog)
        {
            Debug.Log($"[{name}] ชน '{other.name}' แต่ Tag = '{other.tag}' ไม่ใช่ Player", this);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            canInteract = false;
            interactingPlayer = null; // ล้างค่าเมื่อเดินออก
            playerController = null;
            HidePrompt();
        }
    }

    private void ShowPrompt()
    {
        string message = promptProvider != null
            ? promptProvider.GetPromptMessage(interactingPlayer)
            : interactMessage;

        if (string.IsNullOrEmpty(message))
        {
            HidePrompt();
            return;
        }

        if (InteractUIManager.Instance != null)
        {
            InteractUIManager.Instance.ShowInteract(this, message);
        }
        else if (!warnedMissingManager)
        {
            warnedMissingManager = true;
            Debug.LogWarning($"[{name}] ไม่พบ InteractUIManager ใน Scene (ยังไม่ได้สร้าง หรือวัตถุที่ใส่สคริปต์ถูกปิดอยู่)", this);
        }
    }

    private void HidePrompt()
    {
        if (InteractUIManager.Instance != null)
            InteractUIManager.Instance.HideInteract(this);
    }
}
