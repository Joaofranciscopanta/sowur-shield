using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using SowurShield.Core;
using SowurShield.Inventory;

namespace SowurShield.Dialogue
{

/// <summary>
/// Self-spawning UI that lets the player give a giftable item from their inventory
/// to an NPC. Opened via <see cref="OpenForNpc"/> from the "Give a gift" dialogue
/// choice injected by <see cref="NPCDialogueInteractable"/>. Built procedurally
/// (no scene wiring required), following the same pattern as
/// <see cref="SowurShield.Combat.ConsumableBattleUI"/>.
/// </summary>
public class GiftSelectionUI : MonoBehaviour, IUIWindow
{
    private RectTransform listPanel;
    private bool isOpen = false;

    private NPCDialogueInteractable targetNpc;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (FindFirstObjectByType<GiftSelectionUI>() != null)
            return;

        var go = new GameObject("GiftSelectionUI");
        go.AddComponent<GiftSelectionUI>();
        DontDestroyOnLoad(go);
    }

    private void Awake()
    {
        BuildUI();
    }

    // =========================================================================
    // IUIWindow Implementation
    // =========================================================================

    public string WindowName => "GiftSelectionUI";
    public int WindowPriority => SowurShield.Core.WindowPriority.Dialogue;
    public bool IsWindowOpen => isOpen;
    public bool CanCloseWithEsc => true;

    public void OpenWindow()
    {
        listPanel.gameObject.SetActive(true);
        isOpen = true;

        FindFirstObjectByType<PlayerMove>()?.DisableMovement();
    }

    public void CloseWindow()
    {
        listPanel.gameObject.SetActive(false);
        isOpen = false;
        targetNpc = null;

        FindFirstObjectByType<PlayerMove>()?.EnableMovement();
    }

    public void OnWindowBlocked(string blockedBy) { }

    /// <summary>
    /// Opens the gift selection panel for the given NPC. Called from the
    /// "Give a gift" dialogue choice after the dialogue window has closed.
    /// </summary>
    public void OpenForNpc(NPCDialogueInteractable npc)
    {
        if (npc == null || !npc.CanGiftToday())
            return;

        targetNpc = npc;
        RefreshList();

        if (UIManager.Instance != null)
            UIManager.Instance.TryOpenWindow(this);
        else
            OpenWindow();
    }

    // =========================================================================
    // UI Construction
    // =========================================================================

    private void BuildUI()
    {
        Canvas canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 50;

        gameObject.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        gameObject.AddComponent<GraphicRaycaster>();

        // List panel (centered, hidden by default)
        GameObject panelObj = new GameObject("GiftList");
        panelObj.transform.SetParent(transform, false);

        listPanel = panelObj.AddComponent<RectTransform>();
        listPanel.anchorMin = new Vector2(0.5f, 0.5f);
        listPanel.anchorMax = new Vector2(0.5f, 0.5f);
        listPanel.pivot = new Vector2(0.5f, 0.5f);
        listPanel.anchoredPosition = Vector2.zero;
        listPanel.sizeDelta = new Vector2(320, 0);

        Image panelBg = panelObj.AddComponent<Image>();
        panelBg.color = new Color(0.1f, 0.1f, 0.15f, 0.95f);

        VerticalLayoutGroup vlg = panelObj.AddComponent<VerticalLayoutGroup>();
        vlg.padding = new RectOffset(12, 12, 12, 12);
        vlg.spacing = 6;
        vlg.childControlWidth = true;
        vlg.childControlHeight = false;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;

        ContentSizeFitter fitter = panelObj.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // Title
        GameObject titleObj = new GameObject("Title");
        titleObj.transform.SetParent(panelObj.transform, false);
        RectTransform titleRect = titleObj.AddComponent<RectTransform>();
        titleRect.sizeDelta = new Vector2(0, 28);
        TextMeshProUGUI titleLabel = CreateLabel(titleObj.transform, "Choose a gift");
        titleLabel.fontSize = 20;
        titleLabel.fontStyle = FontStyles.Bold;
        titleLabel.alignment = TextAlignmentOptions.Center;

        // Close button
        GameObject closeButtonObj = new GameObject("CloseButton");
        closeButtonObj.transform.SetParent(panelObj.transform, false);

        RectTransform closeRect = closeButtonObj.AddComponent<RectTransform>();
        closeRect.sizeDelta = new Vector2(0, 36);

        Image closeImage = closeButtonObj.AddComponent<Image>();
        closeImage.color = new Color(0.3f, 0.25f, 0.25f, 0.9f);

        Button closeButton = closeButtonObj.AddComponent<Button>();
        closeButton.onClick.AddListener(OnCloseButtonClicked);

        TextMeshProUGUI closeLabel = CreateLabel(closeButtonObj.transform, "Close");
        closeLabel.alignment = TextAlignmentOptions.Center;

        panelObj.SetActive(false);
    }

    private TextMeshProUGUI CreateLabel(Transform parent, string text)
    {
        GameObject obj = new GameObject("Label");
        obj.transform.SetParent(parent, false);

        RectTransform rect = obj.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        TextMeshProUGUI tmp = obj.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = 18;
        tmp.color = Color.white;

        return tmp;
    }

    // =========================================================================
    // Gift List / Panel
    // =========================================================================

    private void OnCloseButtonClicked()
    {
        if (UIManager.Instance != null)
            UIManager.Instance.TryCloseWindow(this);
        else
            CloseWindow();
    }

    private void RefreshList()
    {
        // Clear existing item rows (keep Title and CloseButton, which are the
        // first and last children added in BuildUI)
        for (int i = listPanel.childCount - 2; i >= 1; i--)
            Destroy(listPanel.GetChild(i).gameObject);

        SowurShield.Inventory.Inventory inventory = FindFirstObjectByType<SowurShield.Inventory.Inventory>();
        if (inventory == null)
        {
            CreateRow("No inventory found", null, 1);
            return;
        }

        List<ItemStack> giftableItems = inventory.GetAllItems()
            .Where(stack => !stack.IsEmpty && stack.item.giftAffinityValue > 0)
            .ToList();

        if (giftableItems.Count == 0)
        {
            CreateRow("No giftable items", null, 1);
            return;
        }

        foreach (ItemStack stack in giftableItems)
            CreateRow($"{stack.item.itemName} x{stack.quantity} (+{stack.item.giftAffinityValue} relationship)", stack.item, 1);
    }

    private void CreateRow(string label, Item item, int siblingIndex)
    {
        GameObject rowObj = new GameObject("Row");
        rowObj.transform.SetParent(listPanel, false);
        rowObj.transform.SetSiblingIndex(siblingIndex);

        RectTransform rowRect = rowObj.AddComponent<RectTransform>();
        rowRect.sizeDelta = new Vector2(0, 32);

        Image rowImage = rowObj.AddComponent<Image>();
        rowImage.color = item != null ? new Color(0.25f, 0.25f, 0.3f, 0.9f) : new Color(0f, 0f, 0f, 0f);

        TextMeshProUGUI rowLabel = CreateLabel(rowObj.transform, label);
        rowLabel.fontSize = 16;
        rowLabel.alignment = TextAlignmentOptions.MidlineLeft;
        rowLabel.margin = new Vector4(8, 0, 8, 0);

        if (item != null)
        {
            GameObject giveButtonObj = new GameObject("GiveButton");
            giveButtonObj.transform.SetParent(rowObj.transform, false);

            RectTransform giveRect = giveButtonObj.AddComponent<RectTransform>();
            giveRect.anchorMin = new Vector2(1, 0);
            giveRect.anchorMax = new Vector2(1, 1);
            giveRect.pivot = new Vector2(1, 0.5f);
            giveRect.sizeDelta = new Vector2(60, 0);
            giveRect.anchoredPosition = new Vector2(-4, 0);

            Image giveImage = giveButtonObj.AddComponent<Image>();
            giveImage.color = new Color(0.3f, 0.5f, 0.3f, 0.9f);

            Button giveButton = giveButtonObj.AddComponent<Button>();
            giveButton.onClick.AddListener(() => GiveItem(item));

            TextMeshProUGUI giveLabel = CreateLabel(giveButtonObj.transform, "Give");
            giveLabel.fontSize = 14;
            giveLabel.alignment = TextAlignmentOptions.Center;
        }
    }

    private void GiveItem(Item item)
    {
        if (targetNpc == null || item == null)
            return;

        SowurShield.Inventory.Inventory inventory = FindFirstObjectByType<SowurShield.Inventory.Inventory>();
        if (inventory == null)
            return;

        if (!inventory.RemoveItem(item, 1))
            return;

        targetNpc.ReceiveGift(item.giftAffinityValue);

        // Signal that a gift reaction dialogue should trigger on next interaction
        var memory = ConversationMemory.Instance;
        if (memory != null)
            memory.SetVariable($"{targetNpc.GetNPCId()}_first_gift_pending", "true");

        if (UIManager.Instance != null)
            UIManager.Instance.TryCloseWindow(this);
        else
            CloseWindow();
    }
}

} // namespace SowurShield.Dialogue
