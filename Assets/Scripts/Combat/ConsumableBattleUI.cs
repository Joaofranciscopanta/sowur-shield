using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Localization;
using SowurShield.Core;
using SowurShield.Inventory;
using SowurShield.UI;

namespace SowurShield.Combat
{

/// <summary>
/// Self-spawning in-battle UI for using consumable items from the player's
/// inventory. Built procedurally (no scene wiring required). Clicking a
/// consumable heals the most-injured living player unit via
/// <see cref="TurnManager.UseConsumableOnUnit"/> (a free action).
/// </summary>
public class ConsumableBattleUI : MonoBehaviour
{
    private RectTransform listPanel;
    private GameObject toggleButtonObj;
    private TextMeshProUGUI toggleButtonLabel;
    private bool isOpen = false;
    private UITheme theme;

    [SerializeField] private LocalizedString titleText_Localized; // table "Combat", key "combat.consumables.title"
    [SerializeField] private LocalizedString noInventoryText_Localized; // table "Combat", key "combat.consumables.no_inventory"
    [SerializeField] private LocalizedString noneText_Localized; // table "Combat", key "combat.consumables.none"
    [SerializeField] private LocalizedString itemRowText_Localized; // table "Combat", key "combat.consumables.item_row"

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (FindFirstObjectByType<ConsumableBattleUI>(FindObjectsInactive.Include) != null)
            return;

        var go = new GameObject("ConsumableBattleUI");
        go.AddComponent<ConsumableBattleUI>();
        DontDestroyOnLoad(go);
    }

    // This component is created at runtime (never saved to a scene/prefab), so the
    // Tools > Sowur Shield > Auto-Wire Localized Fields editor pass can never reach it —
    // wire its LocalizedString table/key references here instead.
    private void WireLocalizedStrings()
    {
        titleText_Localized = new LocalizedString("Combat", "combat.consumables.title");
        noInventoryText_Localized = new LocalizedString("Combat", "combat.consumables.no_inventory");
        noneText_Localized = new LocalizedString("Combat", "combat.consumables.none");
        itemRowText_Localized = new LocalizedString("Combat", "combat.consumables.item_row");
    }

    private void Awake()
    {
        TryBuildUI();
        LocalizationManager.OnLanguageChanged += HandleLanguageChanged;
    }

    private void OnDestroy()
    {
        LocalizationManager.OnLanguageChanged -= HandleLanguageChanged;
    }

    private void HandleLanguageChanged(UnityEngine.Localization.Locale locale)
    {
        if (toggleButtonLabel != null) toggleButtonLabel.text = titleText_Localized.SafeGetLocalizedString();
        if (isOpen) RefreshList();
    }

    private void TryBuildUI()
    {
        theme = Resources.Load<UITheme>("UI/CozyUITheme");
        WireLocalizedStrings();
        try
        {
            BuildUI();
        }
        catch (System.Exception e)
        {
            // Localization tables may not be configured yet (see MOBILE_LOCALIZATION_SETUP.md) —
            // fail safe rather than leaving a half-built panel visible, but retry shortly instead
            // of staying broken for the rest of the session. Clear any partially-built children
            // first (BuildUI may have thrown partway through), and keep the root active so the
            // Invoke below can still fire.
            Debug.LogError($"[ConsumableBattleUI] BuildUI failed (Localization not configured?): {e}");
            for (int i = transform.childCount - 1; i >= 0; i--)
                Destroy(transform.GetChild(i).gameObject);
            foreach (var c in new System.Type[] { typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster) })
            {
                var comp = GetComponent(c);
                if (comp != null) Destroy(comp);
            }
            Invoke(nameof(RetryBuildUI), 1f);
        }
    }

    private void RetryBuildUI()
    {
        TryBuildUI();
    }

    private void BuildUI()
    {
        Canvas canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        gameObject.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        gameObject.AddComponent<GraphicRaycaster>();

        // Toggle button (bottom-left corner)
        toggleButtonObj = new GameObject("ItemsButton");
        toggleButtonObj.transform.SetParent(transform, false);

        RectTransform btnRect = toggleButtonObj.AddComponent<RectTransform>();
        btnRect.anchorMin = new Vector2(0, 0);
        btnRect.anchorMax = new Vector2(0, 0);
        btnRect.pivot = new Vector2(0, 0);
        btnRect.anchoredPosition = new Vector2(20, 20);
        btnRect.sizeDelta = new Vector2(120, 40);

        Color woodDark = theme != null ? theme.woodDark : new Color(0.2f, 0.2f, 0.25f);
        Image btnImage = toggleButtonObj.AddComponent<Image>();
        Sprite btnSprite = Resources.Load<Sprite>("Sprites/UI/Buttons/button_small_action");
        if (btnSprite != null)
        {
            btnImage.sprite = btnSprite;
            btnImage.type = Image.Type.Sliced;
            btnImage.color = Color.white;
        }
        else
        {
            // Sprite missing from Resources — keep the flat tint so the button stays usable.
            btnImage.color = new Color(woodDark.r, woodDark.g, woodDark.b, 0.85f);
        }

        Button toggleButton = toggleButtonObj.AddComponent<Button>();
        toggleButton.onClick.AddListener(ToggleList);

        toggleButtonLabel = CreateLabel(toggleButtonObj.transform, titleText_Localized.SafeGetLocalizedString());
        toggleButtonLabel.alignment = TextAlignmentOptions.Center;
        if (btnSprite != null)
        {
            // The gold button sprite needs dark text; cream is only readable on the flat wood tint.
            toggleButtonLabel.color = theme != null ? theme.textDark : Color.black;
        }

        // List panel (above the toggle button, hidden by default)
        GameObject panelObj = new GameObject("ConsumableList");
        panelObj.transform.SetParent(transform, false);

        listPanel = panelObj.AddComponent<RectTransform>();
        listPanel.anchorMin = new Vector2(0, 0);
        listPanel.anchorMax = new Vector2(0, 0);
        listPanel.pivot = new Vector2(0, 0);
        listPanel.anchoredPosition = new Vector2(20, 70);
        // 300, not the original 220: the padding above spends 72 of the width on the sprite's
        // frame, and a row label spends another 16 on its own margin. At 220 that left 132 for
        // text, and "RareFish x2 (+10 PV)" needs 171 — it wrapped to a second line and pushed
        // out past the panel's bottom border. 300 leaves 212, enough headroom for longer item
        // names than the two currently in the game.
        listPanel.sizeDelta = new Vector2(300, 0);

        Color panelTint = theme != null ? theme.woodDark : new Color(0.1f, 0.1f, 0.15f);
        Image panelBg = panelObj.AddComponent<Image>();
        Sprite panelSprite = Resources.Load<Sprite>("Sprites/UI/Panels/panel_wood_generic");
        if (panelSprite != null)
        {
            panelBg.sprite = panelSprite;
            panelBg.type = Image.Type.Sliced;
            panelBg.color = Color.white;
        }
        else
        {
            panelBg.color = new Color(panelTint.r, panelTint.g, panelTint.b, 0.9f);
        }

        VerticalLayoutGroup vlg = panelObj.AddComponent<VerticalLayoutGroup>();
        // Padding must clear panel_wood_generic's 9-slice border (32 units per side), not just
        // sit inside the RectTransform. With the old 8 the rows were laid out across the wood
        // frame itself, and because ContentSizeFitter sizes the panel to the content, the sliced
        // centre never got enough height to appear: at two rows the panel was 84 units tall
        // against 64 units of top+bottom border, leaving a 20-unit cream sliver that read on
        // screen as a stray gold bar struck through the rows rather than as a panel.
        // 36 = the 32 border + the 4 of breathing room the old value was going for.
        vlg.padding = new RectOffset(36, 36, 36, 36);
        vlg.spacing = 4;
        vlg.childControlWidth = true;
        vlg.childControlHeight = false;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;

        ContentSizeFitter fitter = panelObj.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        panelObj.SetActive(false);

        // Hidden by default outside of battle; Update() shows it when TurnManager.Instance exists.
        toggleButtonObj.SetActive(false);
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
        tmp.color = theme != null ? theme.backgroundCream : Color.white;

        return tmp;
    }

    private void ToggleList()
    {
        isOpen = !isOpen;
        listPanel.gameObject.SetActive(isOpen);

        if (isOpen)
            RefreshList();
    }

    private void RefreshList()
    {
        // Clear existing entries
        for (int i = listPanel.childCount - 1; i >= 0; i--)
            Destroy(listPanel.GetChild(i).gameObject);

        SowurShield.Inventory.Inventory inventory = FindFirstObjectByType<SowurShield.Inventory.Inventory>();
        if (inventory == null)
        {
            CreateRow(noInventoryText_Localized.SafeGetLocalizedString(), null, 0);
            return;
        }

        List<ItemStack> consumables = inventory.GetAllItems()
            .Where(stack => !stack.IsEmpty && stack.item.isConsumable && stack.item.healthRestore > 0)
            .ToList();

        if (consumables.Count == 0)
        {
            CreateRow(noneText_Localized.SafeGetLocalizedString(), null, 0);
            return;
        }

        foreach (ItemStack stack in consumables)
        {
            itemRowText_Localized.Arguments = new object[] { stack.item.GetDisplayName(), stack.quantity, stack.item.healthRestore };
            CreateRow(itemRowText_Localized.SafeGetLocalizedString(), stack.item, stack.quantity);
        }
    }

    private void CreateRow(string label, Item item, int quantity)
    {
        GameObject rowObj = new GameObject("Row");
        rowObj.transform.SetParent(listPanel, false);

        RectTransform rowRect = rowObj.AddComponent<RectTransform>();
        // Stretch across the list: a fresh RectTransform defaults to point anchors, where a
        // sizeDelta.x of 0 is a literal 0-wide rect and the label wraps one character per line.
        rowRect.anchorMin = new Vector2(0f, 1f);
        rowRect.anchorMax = new Vector2(1f, 1f);
        // Empty-state messages wrap onto 2-3 lines — give them room so the panel
        // doesn't collapse into a sliver behind the text.
        rowRect.sizeDelta = new Vector2(0, item != null ? 32 : 60);

        // woodDark at full alpha, not woodLight at 0.9. The rows carry the cream label from
        // CreateLabel, and panel_wood_generic is light art — a cream field, not a dark one — so a
        // translucent light-wood row composited up to rgb(175,120,79) and the label measured a
        // 3.98 contrast ratio on screen, under the 4.5 wanted for body text. Opaque woodDark
        // reaches 7.59. Alpha matters as much as the tone here: letting the cream panel bleed
        // through is what washed the row out in the first place (woodDark at 0.9 only makes 5.90).
        Color rowTint = theme != null ? theme.woodDark : new Color(0.15f, 0.15f, 0.2f);
        Image rowImage = rowObj.AddComponent<Image>();
        rowImage.color = item != null ? new Color(rowTint.r, rowTint.g, rowTint.b, 1f) : new Color(0f, 0f, 0f, 0f);

        TextMeshProUGUI rowLabel = CreateLabel(rowObj.transform, label);
        rowLabel.fontSize = 16;
        rowLabel.alignment = TextAlignmentOptions.MidlineLeft;
        rowLabel.margin = new Vector4(8, 0, 8, 0);

        // Empty-state rows have no tinted background — they sit directly on the light
        // parchment panel, where the default cream label would be unreadable.
        if (item == null)
            rowLabel.color = theme != null ? theme.textDark : Color.black;

        if (item != null)
        {
            Button rowButton = rowObj.AddComponent<Button>();
            rowButton.onClick.AddListener(() => UseConsumable(item));
        }
    }

    private void UseConsumable(Item item)
    {
        if (TurnManager.Instance == null)
            return;

        CombatUnit target = TurnManager.Instance.GetPlayerUnits()
            .Where(u => u != null && u.IsAlive())
            .OrderBy(u => u.currentHealth / Mathf.Max(1f, u.GetMaxHealth()))
            .FirstOrDefault();

        if (target == null)
            return;

        if (TurnManager.Instance.UseConsumableOnUnit(item, target))
            RefreshList();
    }

    private void Update()
    {
        // combatActive also hides the button/list over the victory/defeat results screen
        bool inBattle = TurnManager.Instance != null && TurnManager.Instance.combatActive;

        if (toggleButtonObj.activeSelf != inBattle)
            toggleButtonObj.SetActive(inBattle);

        if (!inBattle && isOpen)
        {
            // No active battle — hide the list to avoid stale state.
            isOpen = false;
            listPanel.gameObject.SetActive(false);
        }
    }
}

}
