using UnityEngine;

namespace SowurShield.Core
{
    public interface IInteractable
    {
        void Interact();
        string GetInteractionPrompt();
        bool CanInteract();
        float GetInteractionRange();
    }
}
