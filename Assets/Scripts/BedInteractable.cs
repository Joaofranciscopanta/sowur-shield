using UnityEngine;
using UnityEngine.UI;
using System.Collections;

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
            Debug.LogError("GameTimeController não encontrado! Adicione-o à cena.");

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
            if (playerControls != null)
            {
                // Desativar controles temporariamente, se necessário
                // Por exemplo: playerControls.DisableControls();
            }
        }
        else if (sleepConfirmUI != null)
        {
            // Fallback to legacy UI
            sleepConfirmUI.SetActive(true);

            playerControls = FindFirstObjectByType<PlayerMove>();
            if (playerControls != null)
            {
                // Desativar controles temporariamente, se necessário
                // Por exemplo: playerControls.DisableControls();
            }
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
        Debug.Log("BedInteractable: CancelSleep called");
        
        // Hide confirmation panel (new or legacy)
        if (confirmationPanel != null)
        {
            confirmationPanel.HideConfirmation();
        }
        else if (sleepConfirmUI != null)
        {
            sleepConfirmUI.SetActive(false);
        }

        if (playerControls != null)
        {
            // Reativar controles, se necessário
            // Por exemplo: playerControls.EnableControls();
        }
        
        Debug.Log("BedInteractable: Sleep cancelled, controls should be restored");
    }

    private IEnumerator SleepSequence()
    {
        if (timeController == null)
        {
            yield break;
        }

        isSleeping = true;

        // Hide confirmation panel
        if (confirmationPanel != null)
        {
            confirmationPanel.HideConfirmation();
        }
        else if (sleepConfirmUI != null)
        {
            sleepConfirmUI.SetActive(false);
        }

        // Som de dormir
        if (sleepSound != null)
            AudioSource.PlayClipAtPoint(sleepSound, transform.position);

        // Start sleep fade transition
        bool fadeComplete = false;
        if (confirmationPanel != null)
        {
            Debug.Log("Starting sleep fade transition...");
            confirmationPanel.StartSleepFade(() => {
                fadeComplete = true;
                Debug.Log("Sleep fade complete - continuing with sleep sequence");
            });
            
            // Wait for fade to complete
            yield return new WaitUntil(() => fadeComplete);
        }
        else
        {
            // Fallback - simple wait if no confirmation panel
            yield return new WaitForSeconds(1.5f);
        }

        // TRIGGER AUTO-SAVE BEFORE ADVANCING TIME
        if (SaveManager.Instance != null)
        {
            Debug.Log("BedInteractable: Triggering auto-save before sleeping...");
            SaveManager.Instance.TriggerAutoSave();
            
            // Wait a brief moment for save to complete
            yield return new WaitForSeconds(0.5f);
        }
        else
        {
            Debug.LogWarning("BedInteractable: SaveManager not found! Save not triggered.");
        }

        // SELL ALL ITEMS FROM SELLBOXES BEFORE ADVANCING TIME
        ProcessSellBoxSales();

        // Avança o tempo
        timeController.AdvanceDay(daysToAdvance);
        timeController.SetTimeOfDay(wakeUpTime);

        yield return new WaitForSeconds(0.5f);

        // Start fade-in from black to show new day
        bool fadeInComplete = false;
        if (confirmationPanel != null)
        {
            Debug.Log("Starting sleep fade-in for new day...");
            confirmationPanel.StartSleepFadeIn(() => {
                fadeInComplete = true;
                Debug.Log("Sleep fade-in complete - new day visible");
            });
            
            // Wait for fade-in to complete
            yield return new WaitUntil(() => fadeInComplete);
        }

        // Som de acordar
        if (wakeUpSound != null)
            AudioSource.PlayClipAtPoint(wakeUpSound, transform.position);

        yield return new WaitForSeconds(1.0f);

        // Ensure game state is fully restored (safety check for Unity editor)
        if (confirmationPanel != null)
        {
            Debug.Log("BedInteractable: Final game state restoration");
            confirmationPanel.ForceGameStateRestore();
        }

        // Restaura controles do jogador
        if (playerControls != null)
        {
            // Por exemplo: playerControls.EnableControls();
        }

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
            Debug.Log("[Sleep Sale] No SellBoxes found in scene.");
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
                
                Debug.Log($"[Sleep Sale] SellBox '{sellBox.name}' sold items for {earnings} coins");
            }
        }
        
        if (totalEarningsFromAllBoxes > 0)
        {
            Debug.Log($"[Sleep Sale] TOTAL EARNINGS: {totalEarningsFromAllBoxes} coins from {boxesWithItems} SellBox(es)");
            
            // Play sell sound effect if any items were sold
            if (sellBoxes.Length > 0 && sellBoxes[0].sellSound != null)
            {
                AudioSource.PlayClipAtPoint(sellBoxes[0].sellSound, transform.position);
            }
        }
        else
        {
            Debug.Log("[Sleep Sale] No items were sold - all SellBoxes were empty.");
        }
    }
}
