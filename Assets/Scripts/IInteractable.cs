using UnityEngine;

public interface IInteractable
{
    void Interact();
    string GetInteractionPrompt();
    bool CanInteract();
    float GetInteractionRange();
}
