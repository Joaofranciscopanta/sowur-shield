using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Components;
using TMPro;

namespace SowurShield.Editor
{

/// <summary>
/// Attaches <see cref="LocalizeStringEvent"/> to the scene's static UI labels.
///
/// The project has full EN/PT/ES string tables and ~370 localized strings, but the *static* text
/// baked into the scenes — button captions, panel titles, confirmation prompts — carried none of
/// the components that read them. Switching the game to Portuguese left half the interface in
/// English, which is the most visible defect a player hits after changing language.
///
/// Wiring 90-odd labels by hand in the Inspector is error-prone and unreviewable, so the mapping
/// lives here as data: GameObject path -> table + key. Running the menu item is idempotent, and
/// the same table doubles as documentation of what is deliberately NOT localized.
///
/// **Runtime-driven labels are excluded on purpose.** Anything a script overwrites every frame or
/// on open — "Gold: 0", "Zoom: 1,0x", item hover labels, the dialogue body — must not get a
/// component that fights the script for the same field. Those are localized in code via
/// SafeGetLocalizedString instead, and are listed in <see cref="IntentionallySkipped"/>.
/// </summary>
public static class LocalizeStaticLabels
{
    /// <summary>One static label: where it lives, and which table entry it should read.</summary>
    private readonly struct Binding
    {
        public readonly string Path;
        public readonly string Table;
        public readonly string Key;

        public Binding(string path, string table, string key)
        {
            Path = path; Table = table; Key = key;
        }
    }

    // ============================================================================
    // SAMPLESCENE — pause menu, settings, confirmations
    // ============================================================================

    private static readonly Binding[] SampleSceneBindings =
    {
        // Pause menu
        new Binding("UI/MenuPanel/MainMenuPanel/MenuTitle",                 "MainMenu", "mainmenu.gamemenu.title"),
        new Binding("UI/MenuPanel/MainMenuPanel/ResumeButton/Text (TMP)",   "MainMenu", "mainmenu.gamemenu.resume"),
        new Binding("UI/MenuPanel/MainMenuPanel/SaveInfoButton/Text (TMP)", "MainMenu", "mainmenu.gamemenu.save_game"),
        new Binding("UI/MenuPanel/MainMenuPanel/SettingsButton/Text (TMP)", "MainMenu", "mainmenu.button.settings"),
        new Binding("UI/MenuPanel/MainMenuPanel/QuitToMenuButton/Text (TMP)",    "MainMenu", "mainmenu.gamemenu.quit_to_menu"),
        new Binding("UI/MenuPanel/MainMenuPanel/QuitToDesktopButton/Text (TMP)", "MainMenu", "mainmenu.gamemenu.quit_to_desktop"),

        // Settings panel
        new Binding("UI/MenuPanel/SettingsPannel/SettingsTitle",                  "MainMenu", "mainmenu.settings.title"),
        new Binding("UI/MenuPanel/SettingsPannel/ResumeButton/Text (TMP)",        "UI_Common", "ui_common.back"),
        new Binding("UI/MenuPanel/SettingsPannel/ApplyButton/Text (TMP)",         "MainMenu", "mainmenu.settings.apply"),
        new Binding("UI/MenuPanel/SettingsPannel/LanguageRow/Label",              "MainMenu", "mainmenu.settings.language"),
        new Binding("UI/MenuPanel/SettingsPannel/MasterVolumeSlider/Text (TMP)",  "MainMenu", "mainmenu.settings.master_volume"),
        new Binding("UI/MenuPanel/SettingsPannel/MusicVolumeSlider/Text (TMP)",   "MainMenu", "mainmenu.settings.music_volume"),
        new Binding("UI/MenuPanel/SettingsPannel/SFXVolumeSlider/Text (TMP)",     "MainMenu", "mainmenu.settings.sfx_volume"),
        new Binding("UI/MenuPanel/SettingsPannel/FullscreenToggle/Text (TMP)",    "MainMenu", "mainmenu.settings.fullscreen"),

        // Confirmation / rename dialogs
        new Binding("UI/MenuPanel/ConfirmationPanel/YesButton/Text (TMP)",  "UI_Common", "ui_common.yes"),
        new Binding("UI/MenuPanel/ConfirmationPanel/NopeButton/Text (TMP)", "UI_Common", "ui_common.no"),
        new Binding("UI/MenuPanel/SlotRenamePanel/TitleText",               "MainMenu", "mainmenu.saveslotbutton.rename"),
        new Binding("UI/MenuPanel/SlotRenamePanel/ConfirmButton/Text (TMP)","UI_Common", "ui_common.ok"),
        new Binding("UI/MenuPanel/SlotRenamePanel/CancelButton/Text (TMP)", "UI_Common", "ui_common.cancel"),
        new Binding("UI/MenuPanel/SaveSlotPanel/TitleText",                 "MainMenu", "mainmenu.gamemenu.save_slot_title"),
        new Binding("UI/MenuPanel/SaveSlotPanel/BackButton/Text (TMP)",     "UI_Common", "ui_common.back"),

        // NOTE: the combat command bar (Attack / Defend / Back) is deliberately absent.
        // BattleCommandUI is built at runtime and lives in DontDestroyOnLoad, not in any scene —
        // GameObject.Find locates it during a Play session but it is never serialized, so a
        // component added here would vanish on exit. Its labels are set in code, and that is
        // where they must be localized.

        // Shop / market / quests close buttons and titles
        new Binding("ShopCanvas/ShopPanel/CloseButton/Text",          "UI_Common", "ui_common.close"),
        new Binding("BuildingShopCanvas/BuildingPanel/CloseButton/Text", "UI_Common", "ui_common.close"),
        new Binding("AnimalMarketCanvas/MarketPanel/CloseButton/Text","UI_Common", "ui_common.close"),
        new Binding("QuestsCanvas/QuestsPanel/CloseButton/Text",      "UI_Common", "ui_common.close"),
    };

    // ============================================================================
    // MAINMENU
    // ============================================================================

    // The five main buttons (New Game / Continue / Load / Settings / Quit) were ALREADY wired
    // before this pass — an earlier draft of this table guessed at "Canvas/MainPanel/..." paths
    // that do not exist and would have reported them as missing. What was actually left is the
    // slot-rename dialog, the confirmation prompt, and the slot picker title.
    private static readonly Binding[] MainMenuBindings =
    {
        new Binding("MainMenuCanvas/SlotPickerPanel/TitleText",                 "MainMenu", "mainmenu.gamemenu.load_slot_title"),
        new Binding("MainMenuCanvas/SlotRenamePanel/TitleText",                 "MainMenu", "mainmenu.saveslotbutton.rename"),
        new Binding("MainMenuCanvas/SlotRenamePanel/ConfirmButton/Text (TMP)",  "UI_Common", "ui_common.ok"),
        new Binding("MainMenuCanvas/SlotRenamePanel/CancelButton/Text (TMP)",   "UI_Common", "ui_common.cancel"),

        // ConfirmationPanel/ConfirmationText is NOT here: GameMenuUI, MainMenuUI and
        // QuitToMainMenuButton each write a different prompt into it (overwrite? load? quit?
        // delete?), already localized via SafeGetLocalizedString. A fixed binding would pin it
        // to one of those messages and show the wrong question for the other three.
    };

    /// <summary>
    /// Labels a script owns at runtime. Listed rather than silently omitted, so the next person
    /// does not "fix" the gap by attaching a component that then fights the script.
    /// </summary>
    private static readonly string[] IntentionallySkipped =
    {
        "UI/MoneyText — MoneyDisplay writes it every change",
        "UI/TimeText, UI/Days — GameTimeController writes them",
        "UI/MinimapPanel/InfoPanel/* — MinimapUI writes mode and zoom",
        "*/HoverLabel/Text — GroundItem writes the item's localized name",
        "UI/DialogueCanvas/* — DialogueTreeUI writes speaker and body",
        "*/PlayerGoldText, Confirm*Text — shop scripts write them with live values",
        "UI/Inventory/ItemTooltip/* — ItemTooltip writes them per item",
        "UI/TroughPanel/StatusText — FeedingTrough writes counts",
        "TeamAssemblerCanvas/**/InfoPanel/* — TeamAssemblerUI writes team state",
        "Dropdown Item Labels ('Option A') — template rows, filled by the dropdown",
    };

    // ============================================================================
    // MENU ITEMS
    // ============================================================================

    [MenuItem("Sowur Shield/Localization/Wire Static Labels In Open Scene")]
    public static void WireOpenScene()
    {
        Run(false);
    }

    [MenuItem("Sowur Shield/Localization/Report Static Labels (no changes)")]
    public static void ReportOnly()
    {
        Run(true);
    }

    private static void Run(bool dryRun)
    {
        var scene = EditorSceneManager.GetActiveScene();
        Binding[] bindings = BindingsFor(scene.name);

        if (bindings == null)
        {
            Debug.LogWarning($"[LocalizeStaticLabels] No binding table for scene '{scene.name}'. " +
                             "Add one before running.");
            return;
        }

        int wired = 0, already = 0, missingObject = 0, missingKey = 0;
        var problems = new List<string>();

        foreach (var b in bindings)
        {
            // Resolved against the active scene only — never GameObject.Find first, which also
            // reaches DontDestroyOnLoad objects that this scene cannot serialize.
            var go = FindIncludingInactive(b.Path);
            if (go == null)
            {
                missingObject++;
                problems.Add($"  no GameObject at '{b.Path}'");
                continue;
            }

            var label = go.GetComponent<TextMeshProUGUI>();
            if (label == null)
            {
                missingObject++;
                problems.Add($"  '{b.Path}' has no TextMeshProUGUI");
                continue;
            }

            if (!KeyExists(b.Table, b.Key))
            {
                missingKey++;
                problems.Add($"  table '{b.Table}' has no key '{b.Key}'");
                continue;
            }

            if (go.GetComponent<LocalizeStringEvent>() != null)
            {
                // Still apply the fitting pass: a label wired by an earlier run (or by hand)
                // needs the same room to shrink, and both steps are idempotent.
                if (!dryRun) EnableAutoSizing(label);
                already++;
                continue;
            }

            if (dryRun) { wired++; continue; }

            var lse = Undo.AddComponent<LocalizeStringEvent>(go);
            lse.StringReference.SetReference(b.Table, b.Key);

            EnableAutoSizing(label);

            // Route the resolved string into the label. Done via a persistent listener so the
            // wiring is visible in the Inspector and survives without this script.
            UnityEditor.Events.UnityEventTools.AddPersistentListener(
                lse.OnUpdateString, label.SetText);

            EditorUtility.SetDirty(go);
            wired++;
        }

        if (!dryRun && wired > 0)
            EditorSceneManager.MarkSceneDirty(scene);

        string verb = dryRun ? "would wire" : "wired";
        Debug.Log($"[LocalizeStaticLabels] {scene.name}: {verb} {wired}, already done {already}, " +
                  $"missing object {missingObject}, missing key {missingKey}.");

        if (problems.Count > 0)
            Debug.LogWarning("[LocalizeStaticLabels] problems:\n" + string.Join("\n", problems));
    }

    /// <summary>
    /// Lets a label shrink to fit rather than spill out of its button.
    ///
    /// Translations are not the same length: "Quit to Desktop" becomes "Sair para a Área de
    /// Trabalho", which needs 297px of the 300px rect — and the painted button pill is narrower
    /// than its rect, so it visibly overflowed the artwork. Fixed point sizes only ever work for
    /// the language they were set in, so every localized label gets a floor and a ceiling instead.
    ///
    /// The ceiling is the size the designer already chose, so nothing grows; only over-long
    /// translations shrink, and only as far as the floor.
    /// </summary>
    private static void EnableAutoSizing(TextMeshProUGUI label)
    {
        InsetLabelInsideButtonArt(label);

        if (label.enableAutoSizing) return;

        float chosen = label.fontSize;

        label.enableAutoSizing = true;
        label.fontSizeMax = chosen;

        // Half the designed size. "Sair para a Área de Trabalho" already needs 17pt of 24 (0.71),
        // and Spanish or a future string can be longer still — a floor that bites just re-creates
        // the overflow it was added to prevent. A caption should shrink, never truncate.
        label.fontSizeMin = Mathf.Max(8f, chosen * 0.5f);
        label.overflowMode = TMPro.TextOverflowModes.Overflow;

        EditorUtility.SetDirty(label);
    }

    /// <summary>
    /// Pulls a button's caption inside the painted pill.
    ///
    /// These labels stretch to their button's full rect with zero inset, but the button art does
    /// not fill that rect — `button_danger` is 600x120 of which the pill is roughly the middle
    /// two-thirds. "Sair para a Área de Trabalho" needs 297px of a 300px rect, so it fitted the
    /// *rect* and still spilled visibly over the *artwork*, which is the trap already documented
    /// for the panel frames: the rect is not the painted area.
    ///
    /// Auto-sizing alone could not fix it, because it was measuring against the wrong width.
    /// Insetting gives the shrink-to-fit something honest to shrink into.
    /// </summary>
    private static void InsetLabelInsideButtonArt(TextMeshProUGUI label)
    {
        var rect = label.rectTransform;
        var parent = rect.parent as RectTransform;
        if (parent == null) return;

        // Only touch captions that stretch edge-to-edge over a button image.
        var image = parent.GetComponent<UnityEngine.UI.Image>();
        if (image == null || image.sprite == null) return;
        if (rect.offsetMin != Vector2.zero || rect.offsetMax != Vector2.zero) return;

        // ...and only when the label actually stretches. On point anchors the offsets ARE the
        // size, so writing an inset produced a negative rect (-120x-100) and the panel title
        // floated off its wooden frame. Stretch anchors are the only case this maths is valid for.
        bool stretchesX = !Mathf.Approximately(rect.anchorMin.x, rect.anchorMax.x);
        bool stretchesY = !Mathf.Approximately(rect.anchorMin.y, rect.anchorMax.y);
        if (!stretchesX || !stretchesY) return;

        // A parent inside a layout group can still report 0x0 at edit time, before the layout has
        // been built. Insetting against that yields a negative rect just as surely as the
        // point-anchor case did — measured: an 8x6 inset on a 0x0 parent gave -16x-12.
        if (parent.rect.width <= 1f || parent.rect.height <= 1f) return;

        float insetX = parent.rect.width * ButtonHorizontalPadding;
        float insetY = parent.rect.height * ButtonVerticalPadding;

        rect.offsetMin = new Vector2(insetX, insetY);
        rect.offsetMax = new Vector2(-insetX, -insetY);

        EditorUtility.SetDirty(rect);
    }

    // Measured from the art, not guessed: button_danger is 600x120 and paints x[69..535], so
    // 11.5% of the width on each side is transparent. A little more is added on top so the
    // caption does not touch the pill's rounded ends. Vertically the art is full-bleed, so the
    // padding there is purely optical.
    private const float ButtonHorizontalPadding = 0.15f;
    private const float ButtonVerticalPadding = 0.10f;

    private static Binding[] BindingsFor(string sceneName)
    {
        switch (sceneName)
        {
            case "SampleScene": return SampleSceneBindings;
            case "MainMenu":    return MainMenuBindings;
            default:            return null;
        }
    }

    /// <summary>
    /// GameObject.Find skips inactive objects, and most of these panels are closed at edit time,
    /// so almost every binding would report "not found" without this fallback.
    /// </summary>
    private static GameObject FindIncludingInactive(string path)
    {
        var parts = path.Split('/');

        // Only the active scene: an object found anywhere else (DontDestroyOnLoad, an additive
        // scene) cannot be serialized by this scene, so wiring it would silently do nothing.
        // GameObject.Find does NOT make that distinction, which is how three runtime-only combat
        // labels looked bindable until the scene handles were compared.
        foreach (var root in EditorSceneManager.GetActiveScene().GetRootGameObjects())
        {
            if (root.name != parts[0]) continue;

            Transform current = root.transform;
            for (int i = 1; i < parts.Length && current != null; i++)
                current = current.Find(parts[i]);

            if (current != null) return current.gameObject;
        }

        return null;
    }

    /// <summary>
    /// Verifies the key before wiring. A LocalizeStringEvent pointing at a missing entry renders
    /// as an empty label at runtime — the same silent blank this work set out to remove.
    /// </summary>
    private static bool KeyExists(string table, string key)
    {
        var collection = UnityEditor.Localization.LocalizationEditorSettings
            .GetStringTableCollection(table);

        if (collection == null) return false;
        return collection.SharedData.Contains(key);
    }

    [MenuItem("Sowur Shield/Localization/List Intentionally Skipped Labels")]
    private static void ListSkipped()
    {
        Debug.Log("[LocalizeStaticLabels] Runtime-driven labels, deliberately not wired:\n  " +
                  string.Join("\n  ", IntentionallySkipped));
    }
}

}
