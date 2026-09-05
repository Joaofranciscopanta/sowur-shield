using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using SowurShield.Animals;

namespace SowurShield.Core
{

public class BedInteractable : MonoBehaviour, IInteractable
{
    [Header("Configurações")]
    public int daysToAdvance = 1;
    public float wakeUpTime = 0.25f; // 6:00 AM
    public bool canOnlySleepAtNight = false;

    [Header("Interface")]
    public GameObject sleepConfirmUI; // Legacy - kept for compatibility
    public Button confirmButton; // Legacy - kept for compatibility
    public Button cancelButton; // Legacy - kept for compatibility
    
    [Header("New Confirmation Panel")]
    public SleepConfirmationPanel confirmationPanel;

    [Header("Efeitos")]
    public GameObject sleepTransitionEffect;
    public AudioClip sleepSound;
    public AudioClip wakeUpSound;

    private GameTimeController timeController;
    private PlayerMove playerControls;
    private bool isSleeping = false;
     
    private void Awake()
    {
        // Busca o GameTimeController
        timeController = GameTimeController.instance;
        if (timeController == null)
            timeController = FindFirstObjectByType<GameTimeController>();

        // Verifica se encontrou
        if (timeController == null)
        {
            // TimeController is required for sleeping functionality
        }

        // Try to find confirmation panel if not assigned
        if (confirmationPanel == null)
        {
            confirmationPanel = FindFirstObjectByType<SleepConfirmationPanel>();
        }

        // Setup new confirmation panel events
        if (confirmationPanel != null)
        {
            SleepConfirmationPanel.OnSleepConfirmed += ConfirmSleep;
            SleepConfirmationPanel.OnSleepCancelled += CancelSleep;
        }
        else
        {
            // Fallback to legacy UI
            SetupLegacyUI();
        }
    }
    
    private void SetupLegacyUI()
    {
        // Configura os botões da UI (legacy)
        if (confirmButton != null)
            confirmButton.onClick.AddListener(ConfirmSleep);

        if (cancelButton != null)
            cancelButton.onClick.AddListener(CancelSleep);

        // Esconde a UI inicialmente
        if (sleepConfirmUI != null)
            sleepConfirmUI.SetActive(false);
    }

    public string GetInteractionPrompt() => "Sleep";
    public bool CanInteract() => !isSleeping;
    public float GetInteractionRange() => 2f;

    public void Interact()
    {
        if (timeController == null)
        {
            return;
        }

        if (isSleeping) return;

        // Verifica se é noite
        if (canOnlySleepAtNight && !timeController.IsEvening() && !timeController.IsNight())
        {
            return;
        }

        // Mostra UI de confirmação ou dorme diretamente
        if (confirmationPanel != null)
        {
            // Use new confirmation panel
            confirmationPanel.ShowConfirmation();

            playerControls = FindFirstObjectByType<PlayerMove>();
            playerControls?.DisableMovement();
        }
        else if (sleepConfirmUI != null)
        {
            // Fallback to legacy UI
            sleepConfirmUI.SetActive(true);

            playerControls = FindFirstObjectByType<PlayerMove>();
            playerControls?.DisableMovement();
        }
        else
        {
            ConfirmSleep();
        }
    }

    private void ConfirmSleep()
    {
        StartCoroutine(SleepSequence());
    }

    private void CancelSleep()
    {
        // Hide confirmation panel (new or legacy)
        if (confirmationPanel != null)
        {
            confirmationPanel.HideConfirmation();
        }
        else if (sleepConfirmUI != null)
        {
            sleepConfirmUI.SetActive(false);
        }

        playerControls?.EnableMovement();
    }

    private IEnumerator SleepSequence()
    {
        if (timeController == null)
        {
            yield break;
        }

        isSleeping = true;

        // Fechar TUDO o que ficou aberto antes de dormir.
        //
        // Sem isto, quem fosse dormir com o comedouro (ou a caixa de venda, ou a loja)
        // aberto acordava no dia seguinte com essa janela ainda na pilha do UIManager --
        // e uma janela na pilha faz TryOpenWindow recusar todas as outras, tocando
        // "Denied". O painel some de vista durante o fade, entao parecia fechado: o
        // sintoma era "no outro dia tudo dava Denied em todos os botoes". Relatado a
        // jogar a build, e reproduzido: dia 4 -> 5 com FeedingTrough preso na pilha.
        //
        // A cama nao passa pelo UIManager (o painel de confirmacao e mostrado a mao),
        // entao ninguem estava a limpar isto.
        if (UIManager.Instance != null)
            UIManager.Instance.ForceCloseAllWindows();

        // Hide confirmation panel
        if (confirmationPanel != null)
        {
            confirmationPanel.HideConfirmation();
        }
        else if (sleepConfirmUI != null)
        {
            sleepConfirmUI.SetActive(false);
        }

        // Keep the player locked for the entire sleep sequence (fade out, day advance, fade in)
        playerControls = FindFirstObjectByType<PlayerMove>();
        playerControls?.DisableMovement();

        // Som de dormir. sleepSound is null in SampleScene, and PlayClipAtPoint builds a 3D
        // source that the listener rarely hears — so this was silent twice over. SFXManager
        // resolves sfx_sleep.wav itself and plays it 2D. An assigned clip still wins.
        if (sleepSound != null)
            SFXManager.Play(sleepSound);
        else
            SFXManager.Play("Sleep");

        // Start sleep fade transition
        bool fadeComplete = false;
        if (confirmationPanel != null)
        {

            confirmationPanel.StartSleepFade(() => {
                fadeComplete = true;

            });
            
            // Wait for fade to complete
            yield return new WaitUntil(() => fadeComplete);
        }
        else
        {
            // Fallback - simple wait if no confirmation panel
            yield return new WaitForSeconds(1.5f);
        }

        // SELL ALL ITEMS FROM SELLBOXES BEFORE ADVANCING TIME
        ProcessSellBoxSales();

        // Avança o tempo
        TutorialManager.NotifyStepComplete("sleep");
        timeController.AdvanceDay(daysToAdvance);

        // Store bed position for respawning
        if (SaveManager.Instance != null && SaveManager.Instance.CurrentGameData != null)
        {
            SaveManager.Instance.CurrentGameData.playerData.lastBedPosition = transform.position;
            SaveManager.Instance.CurrentGameData.playerData.hasSleptInBed = true;
        }

        // TRIGGER AUTO-SAVE AFTER ADVANCING TIME
        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.TriggerAutoSave();

            // Wait a brief moment for save to complete
            yield return new WaitForSeconds(0.5f);
        }

        yield return new WaitForSeconds(0.5f);

        // Start fade-in from black to show new day
        bool fadeInComplete = false;
        if (confirmationPanel != null)
        {

            confirmationPanel.StartSleepFadeIn(() => {
                fadeInComplete = true;

            });
            
            // Wait for fade-in to complete
            yield return new WaitUntil(() => fadeInComplete);
        }

        // Som de acordar. Same fix as the sleep clip above; there is no generated wake-up
        // sound yet, so an unassigned wakeUpSound simply stays quiet rather than guessing.
        if (wakeUpSound != null)
            SFXManager.Play(wakeUpSound);

        yield return new WaitForSeconds(1.0f);

        // Ensure game state is fully restored (safety check for Unity editor)
        if (confirmationPanel != null)
        {

            confirmationPanel.ForceGameStateRestore();
        }

        // Restaura controles do jogador
        playerControls?.EnableMovement();

        isSleeping = false;
    }
    
    private void OnDestroy()
    {
        // Clean up event subscriptions
        if (confirmationPanel != null)
        {
            SleepConfirmationPanel.OnSleepConfirmed -= ConfirmSleep;
            SleepConfirmationPanel.OnSleepCancelled -= CancelSleep;
        }
    }

    /// <summary>
    /// Processes all SellBox sales automatically when sleeping
    /// </summary>
    private void ProcessSellBoxSales()
    {
        // Find all SellBox components in the scene
        SellBox[] sellBoxes = FindObjectsByType<SellBox>(FindObjectsSortMode.None);

        if (sellBoxes.Length == 0)
        {

            return;
        }

        int totalEarningsFromAllBoxes = 0;
        int boxesWithItems = 0;

        foreach (SellBox sellBox in sellBoxes)
        {
            if (sellBox.HasItemsToSell())
            {
                boxesWithItems++;
                int earnings = sellBox.SellAllItemsAutomatically();
                totalEarningsFromAllBoxes += earnings;


            }
        }

        if (totalEarningsFromAllBoxes > 0)
        {


            // Play sell sound effect if any items were sold
            if (sellBoxes.Length > 0 && sellBoxes[0].sellSound != null)
            {
                AudioSource.PlayClipAtPoint(sellBoxes[0].sellSound, transform.position);
            }
        }
        else
        {

        }
    }
}

} // namespace SowurShield.Core
