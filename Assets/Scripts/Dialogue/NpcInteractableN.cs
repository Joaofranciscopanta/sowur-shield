using UnityEngine;
using UnityEngine.InputSystem;

public class NPCInteractable : MonoBehaviour
{
    [Header("Configurações do NPC")]
    [SerializeField] private DialogueObject dialogueObject;
    [SerializeField] private GameObject interactionPrompt; // UI "Pressione E // Quadrado para falar"
    [SerializeField] private GameObject dialogueBox; // A caixinha/imagem do diálogo
    [SerializeField] private float interactionRange = 3f;
    
    private bool playerInRange = false;
    private bool isDialogueActive = false;
    private Transform player;
    private DialogueUI dialogueUI;

    private void Start()
    {
        // Encontra o player e o DialogueUI na cena
        player = GameObject.FindGameObjectWithTag("Player")?.transform;
        dialogueUI = FindFirstObjectByType<DialogueUI>();
        
        // Esconde o prompt e a caixa de diálogo inicialmente
        if (interactionPrompt != null)
            interactionPrompt.SetActive(false);
            
        if (dialogueBox != null)
            dialogueBox.SetActive(false);
    }

    private void Update()
    {
        CheckPlayerDistance();
        HandleInteraction();
    }

    private void CheckPlayerDistance()
    {
        if (player == null) return;

        float distance = Vector3.Distance(transform.position, player.position);
        bool wasInRange = playerInRange;
        playerInRange = distance <= interactionRange && !isDialogueActive;

        // Mostra/esconde o prompt baseado na distância
        if (playerInRange != wasInRange)
        {
            if (interactionPrompt != null)
                interactionPrompt.SetActive(playerInRange);
        }
    }

    private void HandleInteraction()
    {
        if (playerInRange && !isDialogueActive && (Keyboard.current.eKey.wasPressedThisFrame ||
           (Gamepad.current != null && Gamepad.current.buttonWest.wasPressedThisFrame) // botão quadrado
))

        {
            // Mostra a caixa de diálogo junto com o início da conversa
            if (dialogueBox != null)
                dialogueBox.SetActive(true);
                
            StartDialogue();
        }
    }

    public void StartDialogue()
    {
        // Verificações de segurança
        if (dialogueUI == null)
        {
            Debug.LogError("DialogueUI não encontrado na cena!");
            return;
        }
        
        if (dialogueObject == null)
        {
            Debug.LogError("DialogueObject não está configurado no NPC!");
            return;
        }
        
        isDialogueActive = true;
        
        // Esconde o prompt
        if (interactionPrompt != null)
            interactionPrompt.SetActive(false);
        
        // Inicia o diálogo
        dialogueUI.ShowDialogue(dialogueObject);
        
        // Espera o diálogo terminar
        StartCoroutine(WaitForDialogueEnd());
    }

    private System.Collections.IEnumerator WaitForDialogueEnd()
    {
        // Espera o diálogo terminar baseado no DialogueUI
        yield return new WaitUntil(() => !dialogueUI.IsDialogueActive);
        
        isDialogueActive = false;
        
        // Esconde a caixa de diálogo quando termina
        if (dialogueBox != null)
            dialogueBox.SetActive(false);
        
        // Reativa o prompt se o player ainda estiver perto
        CheckPlayerDistance();
    }

    // Visualização no Scene View
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactionRange);
    }
}