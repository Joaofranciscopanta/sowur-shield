using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class InteractionManager : MonoBehaviour
{
    public static InteractionManager Instance { get; private set; }

    [Header("Settings")]
    [SerializeField] private float interactionCheckInterval = 0.1f;
    [SerializeField] private GameBalance balance;
    
    private List<IInteractable> registeredInteractables = new List<IInteractable>();
    private IInteractable currentClosestInteractable = null;
    private Transform player;
    private float lastCheckTime;
    
    public System.Action<IInteractable> OnClosestInteractableChanged;
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        if (balance == null)
            balance = Resources.Load<GameBalance>("GameBalance");
    }
    
    private void Start()
    {
        FindPlayer();

    }
    
    private void Update()
    {
        if (Time.time - lastCheckTime >= interactionCheckInterval)
        {
            UpdateClosestInteractable();
            lastCheckTime = Time.time;
        }
    }
    
    private void FindPlayer()
    {
        player = GameObject.FindGameObjectWithTag("Player")?.transform;
        if (player == null)
        {
            var playerMove = FindFirstObjectByType<PlayerMove>();
            if (playerMove != null)
                player = playerMove.transform;
        }
    }
    
    public void RegisterInteractable(IInteractable interactable)
    {
        if (!registeredInteractables.Contains(interactable))
        {
            registeredInteractables.Add(interactable);
        }
    }
    
    public void UnregisterInteractable(IInteractable interactable)
    {
        registeredInteractables.Remove(interactable);
        
        if (currentClosestInteractable == interactable)
        {
            currentClosestInteractable = null;
            UpdateClosestInteractable();
        }
    }
    
    private void UpdateClosestInteractable()
    {
        if (player == null)
        {
            FindPlayer();
            return;
        }
        
        IInteractable newClosest = null;
        float closestDistance = float.MaxValue;
        
        // Clean up null references
        registeredInteractables.RemoveAll(item => item == null || 
            (item is MonoBehaviour mb && mb == null));
        
        foreach (var interactable in registeredInteractables)
        {
            if (interactable == null) continue;
            
            // Get the GameObject from the interactable
            GameObject interactableObject = null;
            if (interactable is MonoBehaviour mb)
            {
                interactableObject = mb.gameObject;
            }
            
            if (interactableObject == null) continue;
            
            // Check if the interactable is available for interaction
            if (!IsInteractableAvailable(interactable)) continue;
            
            float distance = Vector3.Distance(player.position, interactableObject.transform.position);
            float interactionRange = GetInteractionRange(interactable);
            
            if (distance <= interactionRange && distance < closestDistance)
            {
                closestDistance = distance;
                newClosest = interactable;
            }
        }
        
        if (newClosest != currentClosestInteractable)
        {
            // Notify old interactable that it's no longer the closest
            if (currentClosestInteractable != null)
            {
                SetInteractablePromptVisibility(currentClosestInteractable, false);
            }

            currentClosestInteractable = newClosest;

            // Notify new interactable that it's now the closest
            if (currentClosestInteractable != null)
            {
                SetInteractablePromptVisibility(currentClosestInteractable, true);
            }
            
            OnClosestInteractableChanged?.Invoke(currentClosestInteractable);
        }
    }
    
    private bool IsInteractableAvailable(IInteractable interactable)
    {
        if (interactable == null) return false;

        // Check NPCDialogueInteractable
        if (interactable is NPCDialogueInteractable npc)
        {
            return npc.isActiveAndEnabled && !npc.IsDialogueActive();
        }

        // Check SellBox
        if (interactable is SellBox sellBox)
        {
            return sellBox.isActiveAndEnabled;
        }

        // Check Animal
        if (interactable is Animal animal)
        {
            return animal.isActiveAndEnabled && animal.CanInteract();
        }

        // Check other interactable types
        if (interactable is MonoBehaviour mb)
        {
            return mb.isActiveAndEnabled;
        }

        return true;
    }
    
    private float GetInteractionRange(IInteractable interactable)
    {
        float defaultRange = balance != null ? balance.defaultInteractionRange : 2f;

        if (interactable is NPCDialogueInteractable npc)
            return npc.GetInteractionRange();

        if (interactable is SellBox sellBox)
            return sellBox.GetInteractionRange();

        if (interactable is Animal animal)
            return animal.GetInteractionRange();

        return defaultRange;
    }
    
    private void SetInteractablePromptVisibility(IInteractable interactable, bool visible)
    {
        if (interactable is NPCDialogueInteractable npc)
        {
            npc.SetPromptVisibility(visible);
        }
        
        if (interactable is SellBox sellBox)
        {
            // SellBox doesn't have a prompt, but we could add one in the future
        }
    }
    
    public bool CanInteract()
    {
        return currentClosestInteractable != null;
    }
    
    public void TriggerInteraction()
    {
        if (currentClosestInteractable != null)
        {
            currentClosestInteractable.Interact();
        }
    }
    
    public IInteractable GetCurrentInteractable()
    {
        return currentClosestInteractable;
    }
    
    public string GetCurrentInteractableName()
    {
        if (currentClosestInteractable == null) return "";
        
        if (currentClosestInteractable is NPCDialogueInteractable npc)
        {
            return npc.GetNPCDisplayName();
        }
        
        if (currentClosestInteractable is SellBox)
        {
            return "SellBox";
        }
        
        if (currentClosestInteractable is MonoBehaviour mb)
        {
            return mb.gameObject.name;
        }
        
        return "Unknown";
    }
    
    // Debug information
    public int GetRegisteredCount()
    {
        return registeredInteractables.Count;
    }
    
    public List<string> GetRegisteredNames()
    {
        var names = new List<string>();
        foreach (var interactable in registeredInteractables)
        {
            if (interactable is MonoBehaviour mb && mb != null)
            {
                names.Add(mb.gameObject.name);
            }
        }
        return names;
    }
}