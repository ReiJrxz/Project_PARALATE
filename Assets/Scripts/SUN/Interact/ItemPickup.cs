using UnityEngine;

[RequireComponent(typeof(Collider))]
public class ItemPickup : MonoBehaviour, IInteractable
{
    public ItemData itemData;

    public string PromptText => itemData != null ? itemData.promptText : "Press E to pick up";

    public void Interact(GameObject interactor)
    {
        PickupSystem pickupSystem = interactor.GetComponent<PickupSystem>();
        if (pickupSystem != null)
            pickupSystem.PickUpObject(gameObject);
    }
}