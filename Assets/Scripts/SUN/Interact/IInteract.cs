using UnityEngine;

public interface IInteractable
{
    string PromptText { get; }
    void Interact(GameObject interactor);
}