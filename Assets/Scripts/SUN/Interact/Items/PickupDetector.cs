using TMPro;
using UnityEngine;

public class PickupDetector : MonoBehaviour
{
    private static readonly Collider[] overlapBuffer = new Collider[32];

    public float detectRadius = 3f;
    public LayerMask itemLayer;
    [Tooltip("ข้อความ prompt แบบเดิม ใช้เฉพาะเมื่อใน Scene ไม่มี InteractUIManager " +
             "(ถ้ามี InteractUIManager จะใช้ข้อความตัวเดียวกันกับบันไดและประตูแทน)")]
    public TMP_Text promptUI;

    private IInteractable currentTarget;
    private IInteractable lastPromptTarget;
    private string lastPromptText;

    void Update()
    {
        FindNearestInteractable();
        UpdatePromptUI();
    }

    void FindNearestInteractable()
    {
        int count = Physics.OverlapSphereNonAlloc(transform.position, detectRadius, overlapBuffer, itemLayer);
        float closestSqrDist = float.MaxValue;
        IInteractable closest = null;

        for (int i = 0; i < count; i++)
        {
            Collider hit = overlapBuffer[i];
            IInteractable interactable = hit.GetComponentInParent<IInteractable>();
            if (interactable == null) continue;

            float sqrDist = (transform.position - hit.transform.position).sqrMagnitude;
            if (sqrDist < closestSqrDist)
            {
                closestSqrDist = sqrDist;
                closest = interactable;
            }
        }

        currentTarget = closest;
    }

    void OnDisable()
    {
        currentTarget = null;

        if (InteractUIManager.Instance != null)
            InteractUIManager.Instance.HideInteract(this);
    }

    void UpdatePromptUI()
    {
        // มี InteractUIManager = ใช้ข้อความตัวเดียวของทั้งเกม (ร่วมกับบันได/ประตู)
        InteractUIManager sharedUI = InteractUIManager.Instance;
        if (sharedUI != null)
        {
            // ข้อความแบบเดิมไม่ใช้แล้ว (ยกเว้นเป็นตัวเดียวกับที่ InteractUIManager ใช้ ห้ามปิดเด็ดขาด)
            if (promptUI != null && promptUI != sharedUI.interactText && promptUI.gameObject.activeSelf)
                promptUI.gameObject.SetActive(false);

            // ข้อความว่าง = เป้าหมายขอซ่อนป้าย (เช่น หน้าเลือกด่านกำลังเปิดอยู่)
            string sharedText = currentTarget != null ? currentTarget.PromptText : null;

            if (!string.IsNullOrEmpty(sharedText))
                sharedUI.ShowInteract(this, sharedText);
            else
                sharedUI.HideInteract(this);

            return;
        }

        if (promptUI == null) return;

        bool hasTarget = currentTarget != null && !string.IsNullOrEmpty(currentTarget.PromptText);
        if (promptUI.gameObject.activeSelf != hasTarget)
            promptUI.gameObject.SetActive(hasTarget);

        if (!hasTarget)
        {
            lastPromptTarget = null;
            return;
        }

        // อัปเดตข้อความเมื่อเปลี่ยนเป้าหมายหรือข้อความเปลี่ยนจริงเท่านั้น (NoteReader เปลี่ยนข้อความได้)
        string text = currentTarget.PromptText;
        if (lastPromptTarget != currentTarget || lastPromptText != text)
        {
            promptUI.text = text;
            lastPromptTarget = currentTarget;
            lastPromptText = text;
        }
    }

    public void TryInteract()
    {
        if (currentTarget == null)
            return;

        // เป้าหมายอาจถูก Destroy ไปแล้ว (เช่น ResourcePickup) ซึ่ง interface ตรวจ null แบบ Unity ไม่ได้
        if (currentTarget is Object unityObject && unityObject == null)
        {
            currentTarget = null;
            return;
        }

        currentTarget.Interact(gameObject);
    }
}
