using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PickupDetector : MonoBehaviour
{
    public float detectRadius = 3f;
    public LayerMask itemLayer;
    public TMP_Text promptUI; // หรือ TMP_Text ถ้าใช้ TextMeshPro

    private IInteractable currentTarget;

    void Update()
    {
        FindNearestInteractable();
        UpdatePromptUI();
    }

    void FindNearestInteractable()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, detectRadius, itemLayer);
        float closestDist = float.MaxValue;
        IInteractable closest = null;

        foreach (var hit in hits)
        {
            IInteractable interactable = hit.GetComponentInParent<IInteractable>();
            if (interactable == null) continue;

            float dist = Vector3.Distance(transform.position, hit.transform.position);
            if (dist < closestDist)
            {
                closestDist = dist;
                closest = interactable;
            }
        }

        currentTarget = closest;
    }

    void UpdatePromptUI()
    {
        if (promptUI == null) return;
        promptUI.gameObject.SetActive(currentTarget != null);
        if (currentTarget != null)
            promptUI.text = currentTarget.PromptText;
    }

    public void TryInteract() => currentTarget?.Interact(gameObject);
}