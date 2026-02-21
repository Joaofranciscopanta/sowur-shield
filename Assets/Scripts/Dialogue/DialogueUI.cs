using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

public class DialogueUI : MonoBehaviour
{
    [SerializeField] public TMPro.TextMeshProUGUI dialogueText;
    [SerializeField] private GameObject dialoguePanel; // Panel do diálogo
    private TypewriterEffect typewriter;
    
    private bool isTyping = false;
    private bool canContinue = false;
    private bool isDialogueActive = false;
    private Coroutine dialogueCoroutine;

    public bool IsDialogueActive => isDialogueActive;

    private void Start()
    {
        // Primeiro tenta no mesmo GameObject
        typewriter = GetComponent<TypewriterEffect>();
        
        // Se não encontrar, procura nos filhos
        if (typewriter == null)
        {
            typewriter = GetComponentInChildren<TypewriterEffect>();
        }
        
        // Se ainda não encontrar, procura na cena
        if (typewriter == null)
        {
            typewriter = FindFirstObjectByType<TypewriterEffect>();
        }
        
        // Verifica se encontrou o TypewriterEffect
        if (typewriter == null)
        {

        }
        
        // REMOVIDO: não esconde mais o painel aqui
        // O NPC vai controlar o dialogueBox
    }

    public void ShowDialogue(DialogueObject dialogueObject)
    {
        // Verificações de segurança
        if (dialogueObject == null)
        {

            return;
        }
        
        if (dialogueObject.Dialogue == null || dialogueObject.Dialogue.Length == 0)
        {

            return;
        }
        
        if (typewriter == null)
        {

            return;
        }
        
        if (dialogueCoroutine != null)
        {
            StopCoroutine(dialogueCoroutine);
        }
        
        // REMOVIDO: não controla mais o painel aqui
        // O NPC vai controlar o dialogueBox
            
        isDialogueActive = true;
        dialogueCoroutine = StartCoroutine(StepThroughDialogue(dialogueObject));
    }

    private IEnumerator StepThroughDialogue(DialogueObject dialogueObject)
    {
        // Verificação adicional de segurança
        if (dialogueObject?.Dialogue == null)
        {

            EndDialogue();
            yield break;
        }
        
        for (int i = 0; i < dialogueObject.Dialogue.Length; i++)
        {
            string dialogue = dialogueObject.Dialogue[i];
            
            // Pula diálogos vazios ou null
            if (string.IsNullOrEmpty(dialogue))
            {

                continue;
            }
            
            // Marca que está digitando e não pode continuar ainda
            isTyping = true;
            canContinue = false;
            
            // Inicia o efeito typewriter
            yield return typewriter.Run(dialogue, dialogueText);
            
            // Quando termina de digitar, marca que pode continuar
            isTyping = false;
            canContinue = true;
            
            // Espera o jogador apertar Z para continuar
            yield return new WaitUntil(() => !canContinue);
        }
        
        // Diálogo terminou
        EndDialogue();
    }

    private void EndDialogue()
    {
        isDialogueActive = false;
        
        // REMOVIDO: não controla mais o painel aqui
        // O NPC vai controlar o dialogueBox
        
        // Limpa o texto
        if (dialogueText != null)
            dialogueText.text = "";
            

    }
    
    private void Update()
    {
        // Só processa input se o diálogo estiver ativo
        if (!isDialogueActive) return;

        if ((Keyboard.current != null && Keyboard.current.zKey.wasPressedThisFrame) ||
           (Gamepad.current != null && Gamepad.current.buttonSouth.wasPressedThisFrame)) // botão X
        {
            if (isTyping)
            {
                // Se está digitando, completa o texto imediatamente
                typewriter.Skip();
            }
            else if (canContinue)
            {
                // Se o texto já terminou, vai para o próximo diálogo
                canContinue = false;
            }
        }
    }
}