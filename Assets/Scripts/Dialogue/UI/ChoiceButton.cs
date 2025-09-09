using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;
using DG.Tweening;

public class ChoiceButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler, ISelectHandler, IDeselectHandler
{
    [Header("UI Components")]
    [SerializeField] private Button button;
    [SerializeField] private TextMeshProUGUI choiceText;
    [SerializeField] private Image background;
    [SerializeField] private Image border;
    
    [Header("Visual States")]
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color highlightedColor = new Color(1f, 1f, 0.8f, 1f);
    [SerializeField] private Color selectedColor = new Color(0.8f, 1f, 0.8f, 1f);
    [SerializeField] private Color disabledColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);
    
    [Header("Animation Settings")]
    [SerializeField] private float animationDuration = 0.2f;
    [SerializeField] private Vector3 hoverScale = new Vector3(1.05f, 1.05f, 1f);
    [SerializeField] private Ease animationEase = Ease.OutQuad;
    
    [Header("Audio")]
    [SerializeField] private AudioClip hoverSound;
    [SerializeField] private AudioClip clickSound;
    
    // Internal state
    private DialogueChoice associatedChoice;
    private System.Action<DialogueChoice> onChoiceSelected;
    private bool isInteractable = true;
    private bool isCurrentlySelected = false;
    private AudioSource audioSource;
    
    // Animation tracking
    private Tween currentAnimation;
    
    public DialogueChoice AssociatedChoice => associatedChoice;
    public bool IsInteractable => isInteractable;
    
    private void Awake()
    {
        ValidateComponents();
        SetupAudio();
        SetupButton();
    }
    
    private void ValidateComponents()
    {
        if (button == null)
            button = GetComponent<Button>();
        
        if (choiceText == null)
            choiceText = GetComponentInChildren<TextMeshProUGUI>();
        
        if (background == null)
        {
            var images = GetComponentsInChildren<Image>();
            if (images.Length > 0)
                background = images[0];
        }
    }
    
    private void SetupAudio()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null && (hoverSound != null || clickSound != null))
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.volume = 0.7f;
        }
    }
    
    private void SetupButton()
    {
        if (button != null)
        {
            button.onClick.AddListener(OnButtonClicked);
        }
    }
    
    /// <summary>
    /// Initializes the choice button with choice data
    /// </summary>
    public void Initialize(DialogueChoice choice, System.Action<DialogueChoice> onSelected)
    {
        associatedChoice = choice;
        onChoiceSelected = onSelected;
        
        if (choice == null)
        {
            Debug.LogError("ChoiceButton: Cannot initialize with null choice");
            return;
        }
        
        // Set choice text
        if (choiceText != null)
            choiceText.text = choice.GetDisplayText();
        
        // Set interactable state
        SetInteractable(choice.IsAvailable());
        
        // Apply choice-specific styling
        ApplyChoiceStyling(choice);
        
        // Reset visual state
        ResetVisuals();
    }
    
    /// <summary>
    /// Sets whether this choice button is interactable
    /// </summary>
    public void SetInteractable(bool interactable)
    {
        isInteractable = interactable;
        
        if (button != null)
            button.interactable = interactable;
        
        // Update visual state
        Color targetColor = interactable ? normalColor : disabledColor;
        if (background != null)
        {
            StopCurrentAnimation();
            background.color = targetColor;
        }
        
        if (choiceText != null)
        {
            Color textColor = interactable ? Color.black : Color.gray;
            choiceText.color = textColor;
        }
    }
    
    /// <summary>
    /// Highlights this button (for keyboard navigation)
    /// </summary>
    public void SetHighlighted(bool highlighted)
    {
        if (!isInteractable) return;
        
        isCurrentlySelected = highlighted;
        
        Color targetColor = highlighted ? selectedColor : normalColor;
        Vector3 targetScale = highlighted ? hoverScale : Vector3.one;
        
        AnimateToState(targetColor, targetScale);
        
        if (highlighted && hoverSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(hoverSound);
        }
    }
    
    private void ApplyChoiceStyling(DialogueChoice choice)
    {
        if (background == null) return;
        
        // Apply choice color if specified
        if (choice.choiceColor != Color.clear && choice.choiceColor != Color.white)
        {
            normalColor = choice.choiceColor;
            highlightedColor = Color.Lerp(choice.choiceColor, Color.white, 0.3f);
            selectedColor = Color.Lerp(choice.choiceColor, Color.green, 0.3f);
        }
        
        // Special styling for exit choices
        if (choice.isExitChoice)
        {
            if (border != null)
            {
                border.color = Color.red;
                border.gameObject.SetActive(true);
            }
        }
    }
    
    private void ResetVisuals()
    {
        if (background != null)
            background.color = isInteractable ? normalColor : disabledColor;
        
        transform.localScale = Vector3.one;
        isCurrentlySelected = false;
    }
    
    private void OnButtonClicked()
    {
        if (!isInteractable || associatedChoice == null) return;
        
        // Play click sound
        if (clickSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(clickSound);
        }
        
        // Animate click
        AnimateClick();
        
        // Execute choice effects
        associatedChoice.ExecuteEffects();
        
        // Notify dialogue system
        onChoiceSelected?.Invoke(associatedChoice);
    }
    
    private void AnimateClick()
    {
        StopCurrentAnimation();
        
        var sequence = DOTween.Sequence();
        sequence.Append(transform.DOScale(Vector3.one * 0.95f, animationDuration * 0.5f).SetEase(animationEase));
        sequence.Append(transform.DOScale(Vector3.one, animationDuration * 0.5f).SetEase(animationEase));
        
        currentAnimation = sequence;
    }
    
    private void AnimateToState(Color targetColor, Vector3 targetScale)
    {
        StopCurrentAnimation();
        
        var sequence = DOTween.Sequence();
        
        if (background != null)
            sequence.Join(background.DOColor(targetColor, animationDuration).SetEase(animationEase));
        
        sequence.Join(transform.DOScale(targetScale, animationDuration).SetEase(animationEase));
        
        currentAnimation = sequence;
    }
    
    private void StopCurrentAnimation()
    {
        if (currentAnimation != null)
        {
            currentAnimation.Kill();
            currentAnimation = null;
        }
    }
    
    // Event handlers for mouse input
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!isInteractable) return;
        
        AnimateToState(highlightedColor, hoverScale);
        
        if (hoverSound != null && audioSource != null)
            audioSource.PlayOneShot(hoverSound);
    }
    
    public void OnPointerExit(PointerEventData eventData)
    {
        if (!isInteractable) return;
        
        Color targetColor = isCurrentlySelected ? selectedColor : normalColor;
        Vector3 targetScale = isCurrentlySelected ? hoverScale : Vector3.one;
        
        AnimateToState(targetColor, targetScale);
    }
    
    public void OnPointerClick(PointerEventData eventData)
    {
        if (!isInteractable || eventData.button != PointerEventData.InputButton.Left) return;
        
        OnButtonClicked();
    }
    
    // Event handlers for keyboard/gamepad navigation
    public void OnSelect(BaseEventData eventData)
    {
        SetHighlighted(true);
    }
    
    public void OnDeselect(BaseEventData eventData)
    {
        SetHighlighted(false);
    }
    
    private void OnDestroy()
    {
        StopCurrentAnimation();
    }
}