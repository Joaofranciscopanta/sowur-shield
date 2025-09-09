using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using DG.Tweening;

public class PortraitManager : MonoBehaviour
{
    [Header("Portrait UI Elements")]
    [SerializeField] private Image leftPortrait;
    [SerializeField] private Image rightPortrait;
    [SerializeField] private CanvasGroup leftPortraitGroup;
    [SerializeField] private CanvasGroup rightPortraitGroup;
    
    [Header("Animation Settings")]
    [SerializeField] private float fadeInDuration = 0.3f;
    [SerializeField] private float fadeOutDuration = 0.2f;
    [SerializeField] private float portraitSwitchDuration = 0.25f;
    [SerializeField] private Ease fadeEase = Ease.OutQuad;
    
    [Header("Portrait States")]
    [SerializeField] private Color activePortraitColor = Color.white;
    [SerializeField] private Color inactivePortraitColor = new Color(0.7f, 0.7f, 0.7f, 0.8f);
    
    [Header("Size Settings")]
    [SerializeField] private Vector3 activePortraitScale = Vector3.one;
    [SerializeField] private Vector3 inactivePortraitScale = new Vector3(0.9f, 0.9f, 0.9f);
    
    // Current state
    private SpeakerPosition currentActiveSpeaker = SpeakerPosition.Center;
    private Sprite currentLeftSprite;
    private Sprite currentRightSprite;
    
    // Animation tracking
    private Coroutine currentAnimation;
    
    public bool IsAnimating => currentAnimation != null;
    
    private void Awake()
    {
        ValidateComponents();
        InitializePortraits();
    }
    
    private void ValidateComponents()
    {
        if (leftPortrait == null)
            Debug.LogError("PortraitManager: Left portrait Image not assigned!");
        
        if (rightPortrait == null)
            Debug.LogError("PortraitManager: Right portrait Image not assigned!");
        
        if (leftPortraitGroup == null && leftPortrait != null)
        {
            leftPortraitGroup = leftPortrait.GetComponent<CanvasGroup>();
            if (leftPortraitGroup == null)
                leftPortraitGroup = leftPortrait.gameObject.AddComponent<CanvasGroup>();
        }
        
        if (rightPortraitGroup == null && rightPortrait != null)
        {
            rightPortraitGroup = rightPortrait.GetComponent<CanvasGroup>();
            if (rightPortraitGroup == null)
                rightPortraitGroup = rightPortrait.gameObject.AddComponent<CanvasGroup>();
        }
    }
    
    private void InitializePortraits()
    {
        if (leftPortraitGroup != null)
        {
            leftPortraitGroup.alpha = 0f;
            leftPortrait.transform.localScale = inactivePortraitScale;
        }
        
        if (rightPortraitGroup != null)
        {
            rightPortraitGroup.alpha = 0f;
            rightPortrait.transform.localScale = inactivePortraitScale;
        }
        
        currentActiveSpeaker = SpeakerPosition.Center;
    }
    
    /// <summary>
    /// Shows a portrait for the specified speaker
    /// </summary>
    public void ShowPortrait(Sprite portraitSprite, SpeakerPosition position, bool isActiveSpeaker = true)
    {
        if (currentAnimation != null)
        {
            StopCoroutine(currentAnimation);
            currentAnimation = null;
        }
        
        currentAnimation = StartCoroutine(ShowPortraitCoroutine(portraitSprite, position, isActiveSpeaker));
    }
    
    /// <summary>
    /// Hides all portraits
    /// </summary>
    public void HideAllPortraits()
    {
        if (currentAnimation != null)
        {
            StopCoroutine(currentAnimation);
            currentAnimation = null;
        }
        
        currentAnimation = StartCoroutine(HideAllPortraitsCoroutine());
    }
    
    /// <summary>
    /// Updates which speaker is currently active
    /// </summary>
    public void SetActiveSpeaker(SpeakerPosition position)
    {
        if (currentActiveSpeaker == position) return;
        
        currentActiveSpeaker = position;
        
        // Update visual states without changing sprites
        UpdatePortraitStates();
    }
    
    private IEnumerator ShowPortraitCoroutine(Sprite portraitSprite, SpeakerPosition position, bool isActiveSpeaker)
    {
        if (portraitSprite == null)
        {
            Debug.LogWarning("PortraitManager: Attempting to show null portrait sprite");
            currentAnimation = null;
            yield break;
        }
        
        Image targetPortrait = position == SpeakerPosition.Left ? leftPortrait : rightPortrait;
        CanvasGroup targetGroup = position == SpeakerPosition.Left ? leftPortraitGroup : rightPortraitGroup;
        
        if (targetPortrait == null || targetGroup == null)
        {
            currentAnimation = null;
            yield break;
        }
        
        // Store the new sprite
        if (position == SpeakerPosition.Left)
            currentLeftSprite = portraitSprite;
        else
            currentRightSprite = portraitSprite;
        
        // If portrait is already visible with different sprite, fade out first
        bool needsFadeOut = targetGroup.alpha > 0.1f && targetPortrait.sprite != portraitSprite;
        
        if (needsFadeOut)
        {
            yield return targetGroup.DOFade(0f, fadeOutDuration).SetEase(fadeEase).WaitForCompletion();
        }
        
        // Set the new sprite
        targetPortrait.sprite = portraitSprite;
        
        // Fade in the portrait
        yield return targetGroup.DOFade(1f, fadeInDuration).SetEase(fadeEase).WaitForCompletion();
        
        // Update active speaker
        if (isActiveSpeaker)
        {
            currentActiveSpeaker = position;
        }
        
        // Update visual states
        UpdatePortraitStates();
        
        currentAnimation = null;
    }
    
    private IEnumerator HideAllPortraitsCoroutine()
    {
        var sequence = DOTween.Sequence();
        
        if (leftPortraitGroup != null && leftPortraitGroup.alpha > 0.1f)
        {
            sequence.Join(leftPortraitGroup.DOFade(0f, fadeOutDuration).SetEase(fadeEase));
        }
        
        if (rightPortraitGroup != null && rightPortraitGroup.alpha > 0.1f)
        {
            sequence.Join(rightPortraitGroup.DOFade(0f, fadeOutDuration).SetEase(fadeEase));
        }
        
        yield return sequence.WaitForCompletion();
        
        // Clear sprite references
        currentLeftSprite = null;
        currentRightSprite = null;
        currentActiveSpeaker = SpeakerPosition.Center;
        
        currentAnimation = null;
    }
    
    private void UpdatePortraitStates()
    {
        // Update left portrait
        if (leftPortraitGroup != null && leftPortraitGroup.alpha > 0.1f)
        {
            bool isActive = currentActiveSpeaker == SpeakerPosition.Left;
            Color targetColor = isActive ? activePortraitColor : inactivePortraitColor;
            Vector3 targetScale = isActive ? activePortraitScale : inactivePortraitScale;
            
            leftPortrait.DOColor(targetColor, portraitSwitchDuration).SetEase(fadeEase);
            leftPortrait.transform.DOScale(targetScale, portraitSwitchDuration).SetEase(fadeEase);
        }
        
        // Update right portrait
        if (rightPortraitGroup != null && rightPortraitGroup.alpha > 0.1f)
        {
            bool isActive = currentActiveSpeaker == SpeakerPosition.Right;
            Color targetColor = isActive ? activePortraitColor : inactivePortraitColor;
            Vector3 targetScale = isActive ? activePortraitScale : inactivePortraitScale;
            
            rightPortrait.DOColor(targetColor, portraitSwitchDuration).SetEase(fadeEase);
            rightPortrait.transform.DOScale(targetScale, portraitSwitchDuration).SetEase(fadeEase);
        }
    }
    
    /// <summary>
    /// Gets the currently displayed sprite for a position
    /// </summary>
    public Sprite GetCurrentPortrait(SpeakerPosition position)
    {
        switch (position)
        {
            case SpeakerPosition.Left: return currentLeftSprite;
            case SpeakerPosition.Right: return currentRightSprite;
            default: return null;
        }
    }
    
    /// <summary>
    /// Checks if a portrait is currently visible at the specified position
    /// </summary>
    public bool IsPortraitVisible(SpeakerPosition position)
    {
        CanvasGroup targetGroup = position == SpeakerPosition.Left ? leftPortraitGroup : rightPortraitGroup;
        return targetGroup != null && targetGroup.alpha > 0.1f;
    }
    
    /// <summary>
    /// Instantly sets portrait without animation (useful for initialization)
    /// </summary>
    public void SetPortraitImmediate(Sprite portraitSprite, SpeakerPosition position, bool isActiveSpeaker = true)
    {
        if (currentAnimation != null)
        {
            StopCoroutine(currentAnimation);
            currentAnimation = null;
        }
        
        Image targetPortrait = position == SpeakerPosition.Left ? leftPortrait : rightPortrait;
        CanvasGroup targetGroup = position == SpeakerPosition.Left ? leftPortraitGroup : rightPortraitGroup;
        
        if (targetPortrait == null || targetGroup == null) return;
        
        targetPortrait.sprite = portraitSprite;
        targetGroup.alpha = portraitSprite != null ? 1f : 0f;
        
        if (position == SpeakerPosition.Left)
            currentLeftSprite = portraitSprite;
        else
            currentRightSprite = portraitSprite;
        
        if (isActiveSpeaker && portraitSprite != null)
        {
            currentActiveSpeaker = position;
        }
        
        UpdatePortraitStatesImmediate();
    }
    
    private void UpdatePortraitStatesImmediate()
    {
        // Update left portrait
        if (leftPortraitGroup != null && leftPortraitGroup.alpha > 0.1f)
        {
            bool isActive = currentActiveSpeaker == SpeakerPosition.Left;
            leftPortrait.color = isActive ? activePortraitColor : inactivePortraitColor;
            leftPortrait.transform.localScale = isActive ? activePortraitScale : inactivePortraitScale;
        }
        
        // Update right portrait
        if (rightPortraitGroup != null && rightPortraitGroup.alpha > 0.1f)
        {
            bool isActive = currentActiveSpeaker == SpeakerPosition.Right;
            rightPortrait.color = isActive ? activePortraitColor : inactivePortraitColor;
            rightPortrait.transform.localScale = isActive ? activePortraitScale : inactivePortraitScale;
        }
    }
}