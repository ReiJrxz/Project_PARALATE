using UnityEngine;

[RequireComponent(typeof(Collider))]
public class ItemPickup : MonoBehaviour, IInteractable
{
    public ItemData itemData;
    public string PromptText => itemData != null ? itemData.promptText : "Press E to pick up";

    public void Interact(GameObject interactor)
    {
        PickupSystem pickupSystem = interactor.GetComponent<PickupSystem>();
        if (pickupSystem == null || !pickupSystem.PickUpObject(gameObject))
            return; // เก็บไม่ได้ (เช่น มีอาวุธชนิดนี้อยู่แล้ว) จึงไม่เล่นเสียงเก็บของ

        if (itemData != null)
            AudioManager.PlayPickup(itemData.pickupSound, transform.position);
    }
}
