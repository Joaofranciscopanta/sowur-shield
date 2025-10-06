using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class BedInteractable : MonoBehaviour, IInteractable
{
    [Header("Configuration")]
    public int daysToAdvance = 1;
    public float wakeUpTime = 0.25f;
    public bool canOnlySleepAtNight = false;

    [Header("Interface")]
    public GameObject sleepConfirmUI;
    public Button confirmButton;
    public Button cancelButton;

    [Header("New Confirmation Panel")]
    public SleepConfirmationPanel confirmationPanel;

    [Header("Effects")]
    public GameObject sleepTransitionEffect;
    public AudioClip sleepSound;
    public AudioClip wakeUpSound;

    private GameTimeController timeController;
    private PlayerMove playerControls;
    private bool isSleeping = false;

    private void Awake()
    {
        timeController = GameTimeController.instance;
        if (timeController == null)
            timeController = FindFirstObjectByType<GameTimeController>();

        if (confirmationPanel != null)
        {
            SleepConfirmationPanel.OnSleepConfirmed += ConfirmSleep;
            SleepConfirmationPanel.OnSleepCancelled += CancelSleep;
        }
        else
        {
            SetupLegacyUI();
        }
    }

    private void SetupLegacyUI()
    {
        if (confirmButton != null)
            confirmButton.onClick.AddListener(ConfirmSleep);

        if (cancelButton != null)
            cancelButton.onClick.AddListener(CancelSleep);

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

        if (canOnlySleepAtNight && !timeController.IsEvening() && !timeController.IsNight())
        {
            return;
        }

        if (confirmationPanel != null)
        {
            confirmationPanel.ShowConfirmation();

            playerControls = FindFirstObjectByType<PlayerMove>();
        }
        else if (sleepConfirmUI != null)
        {
            sleepConfirmUI.SetActive(true);

            playerControls = FindFirstObjectByType<PlayerMove>();
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
        if (confirmationPanel != null)
        {
            confirmationPanel.HideConfirmation();
        }
        else if (sleepConfirmUI != null)
        {
            sleepConfirmUI.SetActive(false);
        }
    }

    private IEnumerator SleepSequence()
    {
        if (timeController == null)
        {
            yield break;
        }

        isSleeping = true;

        if (confirmationPanel != null)
        {
            confirmationPanel.HideConfirmation();
        }
        else if (sleepConfirmUI != null)
        {
            sleepConfirmUI.SetActive(false);
        }

        if (sleepSound != null)
            AudioSource.PlayClipAtPoint(sleepSound, transform.position);

        bool fadeComplete = false;
        if (confirmationPanel != null)
        {
            confirmationPanel.StartSleepFade(() => {
                fadeComplete = true;
            });

            yield return new WaitUntil(() => fadeComplete);
        }
        else
        {
            yield return new WaitForSeconds(1.5f);
        }

        ProcessSellBoxSales();

        timeController.AdvanceDay(daysToAdvance);

        if (SaveManager.Instance != null && SaveManager.Instance.CurrentGameData != null)
        {
            SaveManager.Instance.CurrentGameData.playerData.lastBedPosition = transform.position;
            SaveManager.Instance.CurrentGameData.playerData.hasSleptInBed = true;
        }

        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.TriggerAutoSave();

            yield return new WaitForSeconds(0.5f);
        }

        yield return new WaitForSeconds(0.5f);

        bool fadeInComplete = false;
        if (confirmationPanel != null)
        {
            confirmationPanel.StartSleepFadeIn(() => {
                fadeInComplete = true;
            });

            yield return new WaitUntil(() => fadeInComplete);
        }

        if (wakeUpSound != null)
            AudioSource.PlayClipAtPoint(wakeUpSound, transform.position);

        yield return new WaitForSeconds(1.0f);

        if (confirmationPanel != null)
        {
            confirmationPanel.ForceGameStateRestore();
        }

        isSleeping = false;
    }

    private void OnDestroy()
    {
        if (confirmationPanel != null)
        {
            SleepConfirmationPanel.OnSleepConfirmed -= ConfirmSleep;
            SleepConfirmationPanel.OnSleepCancelled -= CancelSleep;
        }
    }

    private void ProcessSellBoxSales()
    {
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
            if (sellBoxes.Length > 0 && sellBoxes[0].sellSound != null)
            {
                AudioSource.PlayClipAtPoint(sellBoxes[0].sellSound, transform.position);
            }
        }
    }
}
