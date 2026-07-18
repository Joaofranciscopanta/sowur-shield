using UnityEngine;
using UnityEngine.UI;
using TMPro;
using SowurShield.Dialogue;

namespace SowurShield.Debugging
{

/// <summary>
/// Debug tool to diagnose and fix NPC interaction prompt visibility issues
/// Attach this to the NPC GameObject to check canvas configuration
/// </summary>
[RequireComponent(typeof(NPCDialogueInteractable))]
public class NPCCanvasDebugger : MonoBehaviour
{
    [Header("Debug Settings")]
    [SerializeField] private bool autoFixOnStart = true;

    private NPCDialogueInteractable npcScript;
    private Canvas promptCanvas;

    private void Start()
    {
        npcScript = GetComponent<NPCDialogueInteractable>();

        if (autoFixOnStart)
        {
            DiagnoseAndFixCanvas();
        }
    }

    [ContextMenu("Diagnose Canvas Issues")]
    public void DiagnoseAndFixCanvas()
    {

        // Find the interaction prompt canvas
        promptCanvas = GetComponentInChildren<Canvas>(true);

        if (promptCanvas == null)
        {
            return;
        }


        // Check Canvas settings
        CheckCanvasRenderMode();
        CheckCanvasScaler();
        CheckTextComponents();
        CheckSortingLayer();
        CheckPosition();

    }

    private void CheckCanvasRenderMode()
    {

        if (promptCanvas.renderMode != RenderMode.WorldSpace)
        {

            if (autoFixOnStart)
            {
                promptCanvas.renderMode = RenderMode.WorldSpace;
            }
        }

        if (promptCanvas.renderMode == RenderMode.WorldSpace)
        {
            if (promptCanvas.worldCamera == null)
            {

                if (autoFixOnStart)
                {
                    promptCanvas.worldCamera = Camera.main;
                }
            }
        }
    }

    private void CheckCanvasScaler()
    {
        CanvasScaler scaler = promptCanvas.GetComponent<CanvasScaler>();

        if (scaler == null)
        {

            if (autoFixOnStart)
            {
                scaler = promptCanvas.gameObject.AddComponent<CanvasScaler>();
            }
        }

        if (scaler != null && promptCanvas.renderMode == RenderMode.WorldSpace)
        {

            if (scaler.dynamicPixelsPerUnit != 10)
            {

                if (autoFixOnStart)
                {
                    scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
                    scaler.dynamicPixelsPerUnit = 10;
                }
            }
        }
    }

    private void CheckTextComponents()
    {
        var texts = promptCanvas.GetComponentsInChildren<Text>(true);
        var tmpTexts = promptCanvas.GetComponentsInChildren<TextMeshProUGUI>(true);


        if (texts.Length == 0 && tmpTexts.Length == 0)
        {
        }

        foreach (var text in tmpTexts)
        {

            if (text.color.a < 0.1f)
            {
            }
        }
    }

    private void CheckSortingLayer()
    {

        if (promptCanvas.sortingOrder < 10)
        {

            if (autoFixOnStart)
            {
                promptCanvas.sortingOrder = 100;
            }
        }
    }

    private void CheckPosition()
    {
        RectTransform rectTransform = promptCanvas.GetComponent<RectTransform>();

        if (rectTransform != null)
        {

            if (rectTransform.localScale.x < 0.001f || rectTransform.localScale.y < 0.001f)
            {

                if (autoFixOnStart)
                {
                    rectTransform.localScale = Vector3.one * 0.01f;
                }
            }

            // Check if canvas is positioned above the NPC
            if (rectTransform.localPosition.y < 0.5f)
            {

                if (autoFixOnStart)
                {
                    rectTransform.localPosition = new Vector3(0, 2f, 0);
                }
            }
        }
    }

    private void OnDrawGizmos()
    {
        if (promptCanvas != null)
        {
            // Draw a line from NPC to canvas position
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(transform.position, promptCanvas.transform.position);

            // Draw a sphere at canvas position
            Gizmos.DrawWireSphere(promptCanvas.transform.position, 0.2f);
        }
    }
}

} // namespace SowurShield.Debugging
