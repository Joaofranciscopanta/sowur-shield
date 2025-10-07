using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Debug tool to diagnose and fix NPC interaction prompt visibility issues
/// Attach this to the NPC GameObject to check canvas configuration
/// </summary>
[RequireComponent(typeof(NPCDialogueInteractable))]
public class NPCCanvasDebugger : MonoBehaviour
{
    [Header("Debug Settings")]
    [SerializeField] private bool autoFixOnStart = true;
    [SerializeField] private bool showDebugLogs = true;

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
        Debug.Log($"=== NPC Canvas Diagnosis for {gameObject.name} ===");

        // Find the interaction prompt canvas
        promptCanvas = GetComponentInChildren<Canvas>(true);

        if (promptCanvas == null)
        {
            Debug.LogError($"[{gameObject.name}] No Canvas found in children! Add a Canvas to the NPC.");
            return;
        }

        Debug.Log($"[{gameObject.name}] Canvas found: {promptCanvas.gameObject.name}");

        // Check Canvas settings
        CheckCanvasRenderMode();
        CheckCanvasScaler();
        CheckTextComponents();
        CheckSortingLayer();
        CheckPosition();

        Debug.Log($"=== Diagnosis Complete ===");
    }

    private void CheckCanvasRenderMode()
    {
        Debug.Log($"Canvas Render Mode: {promptCanvas.renderMode}");

        if (promptCanvas.renderMode != RenderMode.WorldSpace)
        {
            Debug.LogWarning($"[{gameObject.name}] Canvas should be in World Space mode!");

            if (autoFixOnStart)
            {
                promptCanvas.renderMode = RenderMode.WorldSpace;
                Debug.Log($"[{gameObject.name}] ✅ Fixed: Set Canvas to World Space");
            }
        }

        if (promptCanvas.renderMode == RenderMode.WorldSpace)
        {
            if (promptCanvas.worldCamera == null)
            {
                Debug.LogWarning($"[{gameObject.name}] World Space Canvas needs a camera reference!");

                if (autoFixOnStart)
                {
                    promptCanvas.worldCamera = Camera.main;
                    Debug.Log($"[{gameObject.name}] ✅ Fixed: Assigned Main Camera to Canvas");
                }
            }
        }
    }

    private void CheckCanvasScaler()
    {
        CanvasScaler scaler = promptCanvas.GetComponent<CanvasScaler>();

        if (scaler == null)
        {
            Debug.LogWarning($"[{gameObject.name}] No CanvasScaler found!");

            if (autoFixOnStart)
            {
                scaler = promptCanvas.gameObject.AddComponent<CanvasScaler>();
                Debug.Log($"[{gameObject.name}] ✅ Added CanvasScaler component");
            }
        }

        if (scaler != null && promptCanvas.renderMode == RenderMode.WorldSpace)
        {
            Debug.Log($"CanvasScaler mode: {scaler.uiScaleMode}");
            Debug.Log($"Dynamic Pixels Per Unit: {scaler.dynamicPixelsPerUnit}");

            if (scaler.dynamicPixelsPerUnit != 10)
            {
                Debug.LogWarning($"[{gameObject.name}] World Space Canvas should use dynamicPixelsPerUnit = 10");

                if (autoFixOnStart)
                {
                    scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
                    scaler.dynamicPixelsPerUnit = 10;
                    Debug.Log($"[{gameObject.name}] ✅ Fixed: Set dynamicPixelsPerUnit to 10");
                }
            }
        }
    }

    private void CheckTextComponents()
    {
        var texts = promptCanvas.GetComponentsInChildren<Text>(true);
        var tmpTexts = promptCanvas.GetComponentsInChildren<TextMeshProUGUI>(true);

        Debug.Log($"Text components found: {texts.Length}");
        Debug.Log($"TextMeshPro components found: {tmpTexts.Length}");

        if (texts.Length == 0 && tmpTexts.Length == 0)
        {
            Debug.LogError($"[{gameObject.name}] No text components found in Canvas!");
        }

        foreach (var text in tmpTexts)
        {
            Debug.Log($"- TextMeshPro: '{text.text}' | Color: {text.color} | FontSize: {text.fontSize}");

            if (text.color.a < 0.1f)
            {
                Debug.LogWarning($"[{gameObject.name}] Text '{text.text}' has very low alpha ({text.color.a})!");
            }
        }
    }

    private void CheckSortingLayer()
    {
        Debug.Log($"Canvas Sorting Layer: {promptCanvas.sortingLayerName}");
        Debug.Log($"Canvas Order in Layer: {promptCanvas.sortingOrder}");

        if (promptCanvas.sortingOrder < 10)
        {
            Debug.LogWarning($"[{gameObject.name}] Canvas sorting order might be too low (behind other elements)");

            if (autoFixOnStart)
            {
                promptCanvas.sortingOrder = 100;
                Debug.Log($"[{gameObject.name}] ✅ Fixed: Set sorting order to 100");
            }
        }
    }

    private void CheckPosition()
    {
        RectTransform rectTransform = promptCanvas.GetComponent<RectTransform>();

        if (rectTransform != null)
        {
            Debug.Log($"Canvas Position: {rectTransform.localPosition}");
            Debug.Log($"Canvas Size: {rectTransform.sizeDelta}");
            Debug.Log($"Canvas Scale: {rectTransform.localScale}");

            if (rectTransform.localScale.x < 0.001f || rectTransform.localScale.y < 0.001f)
            {
                Debug.LogWarning($"[{gameObject.name}] Canvas scale is too small!");

                if (autoFixOnStart)
                {
                    rectTransform.localScale = Vector3.one * 0.01f;
                    Debug.Log($"[{gameObject.name}] ✅ Fixed: Set scale to 0.01");
                }
            }

            // Check if canvas is positioned above the NPC
            if (rectTransform.localPosition.y < 0.5f)
            {
                Debug.LogWarning($"[{gameObject.name}] Canvas might be positioned too low (below NPC)");

                if (autoFixOnStart)
                {
                    rectTransform.localPosition = new Vector3(0, 2f, 0);
                    Debug.Log($"[{gameObject.name}] ✅ Fixed: Moved canvas above NPC (y=2)");
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
