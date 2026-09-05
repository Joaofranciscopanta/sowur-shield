using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using SowurShield.Dialogue;

namespace SowurShield.Core
{

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
        int melhorPrioridade = int.MinValue;

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

            if (distance > interactionRange) continue;

            // Desempate por TIPO antes da distancia.
            //
            // Escolher puramente o mais proximo fazia um NPC a 2,9 ganhar de um ovo a 3,0.
            // Com os NPCs a 3 de alcance -- o maior do jogo -- isso significava falar com
            // alguem sempre que se queria apanhar algo, entrar no combate ou abrir a caixa
            // de venda. Medido: 27 sobreposicoes na cena.
            //
            // A prioridade so decide entre coisas que JA estao ao alcance; nao estende
            // alcance nenhum, entao nada passa a ser alcancavel de mais longe por causa dela.
            int prioridade = InteractionPreferences.Prioridade(interactable);

            // Com a mira ligada, o que esta sob o cursor sobe acima de qualquer tipo.
            if (InteractionPreferences.MirarNoCursor && EstaSobOCursor(interactableObject))
                prioridade += 100;

            bool melhor = prioridade > melhorPrioridade
                       || (prioridade == melhorPrioridade && distance < closestDistance);

            if (melhor)
            {
                melhorPrioridade = prioridade;
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

        // NPCs have an extra guard: active dialogue blocks re-interaction
        if (interactable is NPCDialogueInteractable npc)
            return npc.isActiveAndEnabled && !npc.IsDialogueActive();

        if (interactable is MonoBehaviour mb)
            return mb.isActiveAndEnabled && interactable.CanInteract();

        return interactable.CanInteract();
    }
    
    private float GetInteractionRange(IInteractable interactable)
    {
        return interactable.GetInteractionRange();
    }

    /// <summary>
    /// Se o cursor esta em cima deste objeto.
    ///
    /// Usado so quando <see cref="InteractionPreferences.MirarNoCursor"/> esta ligado: ai o
    /// que esta sob a seta ganha de qualquer outro, que e o pedido de "prioridade mais no
    /// clique do mouse". Testa os colliders e, em falhando, o desenho do sprite -- um NPC
    /// tem collider mas um item no chao pode nao ter.
    /// </summary>
    private bool EstaSobOCursor(GameObject alvo)
    {
        var cam = Camera.main;
        if (cam == null || alvo == null) return false;

        Vector2 mundo = cam.ScreenToWorldPoint(Input.mousePosition);

        foreach (var col in alvo.GetComponentsInChildren<Collider2D>())
            if (col.enabled && col.OverlapPoint(mundo)) return true;

        foreach (var sr in alvo.GetComponentsInChildren<SpriteRenderer>())
            if (sr.enabled && sr.sprite != null && sr.bounds.Contains(
                    new Vector3(mundo.x, mundo.y, sr.bounds.center.z)))
                return true;

        return false;
    }
    
    private void SetInteractablePromptVisibility(IInteractable interactable, bool visible)
    {
        if (interactable is NPCDialogueInteractable npc)
        {
            npc.SetPromptVisibility(visible);
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

} // namespace SowurShield.Core