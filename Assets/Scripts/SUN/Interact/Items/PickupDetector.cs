using TMPro;
using UnityEngine;

public class PickupDetector : MonoBehaviour
{
    private static readonly Collider[] overlapBuffer = new Collider[32];

    public float detectRadius = 3f;
    public LayerMask itemLayer;
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

    void UpdatePromptUI()
    {
        if (promptUI == null) return;

        bool hasTarget = currentTarget != null;
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
