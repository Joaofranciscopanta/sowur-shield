using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Localization;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Localization.Components;
using UnityEngine.Localization.Tables;
using TMPro;

namespace SowurShield.Editor
{

/// <summary>
/// Attaches LocalizeStringEvent components to the project's hardcoded UI labels and fills
/// the matching string table entries in en/pt/es.
///
/// Before this ran, there was not a single LocalizeStringEvent anywhere in the project:
/// every button caption and panel heading was literal text typed into the scene, so the
/// language dropdown changed dialogue and item names but left the interface in English.
///
/// The mapping below is written out by hand rather than generated from the English text,
/// because the key namespace is a design decision (shared ui_common.* keys vs per-screen
/// keys) and because several labels look translatable but must not be touched — language
/// names, the version string, and placeholders that code overwrites at runtime.
///
/// Menu: Tools > Sowur Shield > Localize Static UI Text
/// </summary>
public static class StaticUILocalizationTool
{
    /// <summary>One label to localize: where it lives, and which table entry drives it.</summary>
    private class LabelBinding
    {
        public string path;     // full hierarchy path within the scene
        public string table;    // string table collection name
        public string key;      // entry key
        public string en, pt, es;
        /// <summary>Enable TMP auto-sizing so longer translations shrink instead of overflowing.</summary>
        public bool autoSize;
        public float autoSizeMin;

        public LabelBinding(string path, string table, string key, string en, string pt, string es,
            bool autoSize = false, float autoSizeMin = 0f)
        {
            this.path = path; this.table = table; this.key = key;
            this.en = en; this.pt = pt; this.es = es;
            this.autoSize = autoSize; this.autoSizeMin = autoSizeMin;
        }
    }

    // ── MainMenu ───────────────────────────────────────────────────────────────
    private static readonly LabelBinding[] MainMenuLabels =
    {
        new LabelBinding("MainMenuCanvas/MainMenuPanel/ButtonsSection/NewGameButton/Text (TMP)",
            "MainMenu", "mainmenu.button.new_game", "New Game", "Novo Jogo", "Nueva Partida",
            autoSize: true, autoSizeMin: 12f),
        new LabelBinding("MainMenuCanvas/MainMenuPanel/ButtonsSection/ContinueButton/Text (TMP)",
            "MainMenu", "mainmenu.button.continue", "Continue", "Continuar", "Continuar",
            autoSize: true, autoSizeMin: 12f),
        new LabelBinding("MainMenuCanvas/MainMenuPanel/ButtonsSection/LoadGameButton/Text (TMP)",
            "MainMenu", "mainmenu.button.load_game", "Load Game", "Carregar Jogo", "Cargar Partida",
            autoSize: true, autoSizeMin: 12f),
        new LabelBinding("MainMenuCanvas/MainMenuPanel/ButtonsSection/SettingsButton/Text (TMP)",
            "MainMenu", "mainmenu.button.settings", "Settings", "Configurações", "Ajustes",
            autoSize: true, autoSizeMin: 12f),
        new LabelBinding("MainMenuCanvas/MainMenuPanel/ButtonsSection/QuitButton/Text (TMP)",
            "MainMenu", "mainmenu.button.quit", "Quit", "Sair", "Salir",
            autoSize: true, autoSizeMin: 12f),

        // These three overflow their scene-authored rects once translated — the Portuguese
        // and Spanish strings are 20-30% longer than the English they were sized for, and
        // wrapped onto a second line that spilled over the control below. Auto-sizing keeps
        // them on one line and covers future translations too.
        new LabelBinding("MainMenuCanvas/SettingsPanel/SettingsContent/Title",
            "MainMenu", "mainmenu.settings.title", "Game Settings", "Configurações do Jogo", "Ajustes del Juego",
            autoSize: true, autoSizeMin: 16f),
        new LabelBinding("MainMenuCanvas/SettingsPanel/SettingsContent/AudioSection/MasterVolumeSlider/Text",
            "MainMenu", "mainmenu.settings.master_volume", "Master Volume", "Volume Geral", "Volumen General",
            autoSize: true, autoSizeMin: 10f),
        new LabelBinding("MainMenuCanvas/SettingsPanel/SettingsContent/AudioSection/MusicVolumeSlider/Text",
            "MainMenu", "mainmenu.settings.music_volume", "Music Volume (BGM)", "Volume da Música (BGM)", "Volumen de Música (BGM)",
            autoSize: true, autoSizeMin: 10f),
        new LabelBinding("MainMenuCanvas/SettingsPanel/SettingsContent/AudioSection/SFXVolumeSlider/Text",
            "MainMenu", "mainmenu.settings.sfx_volume", "Effects Volume (SFX)", "Volume dos Efeitos (SFX)", "Volumen de Efectos (SFX)",
            autoSize: true, autoSizeMin: 10f),
        new LabelBinding("MainMenuCanvas/SettingsPanel/SettingsContent/VideoSection/FullScreenToggle/Background/Text (TMP)",
            "MainMenu", "mainmenu.settings.fullscreen", "Toggle Fullscreen", "Tela Cheia", "Pantalla Completa",
            autoSize: true, autoSizeMin: 10f),
        new LabelBinding("MainMenuCanvas/SettingsPanel/SettingsContent/VideoSection/LanguageRow/Label",
            "MainMenu", "mainmenu.settings.language", "Language", "Idioma", "Idioma",
            autoSize: true, autoSizeMin: 10f),
        new LabelBinding("MainMenuCanvas/SettingsPanel/SettingsContent/BackButton/Text (TMP)",
            "UI_Common", "ui_common.back", "Back", "Voltar", "Volver",
            autoSize: true, autoSizeMin: 10f),

        new LabelBinding("MainMenuCanvas/ConfirmationPanel/YesButton/Text (TMP)",
            "UI_Common", "ui_common.yes", "Yes", "Sim", "Sí",
            autoSize: true, autoSizeMin: 10f),
        new LabelBinding("MainMenuCanvas/ConfirmationPanel/NoButton/Text (TMP)",
            "UI_Common", "ui_common.no", "No", "Não", "No",
            autoSize: true, autoSizeMin: 10f),
        new LabelBinding("MainMenuCanvas/SlotPickerPanel/BackButton/Text (TMP)",
            "UI_Common", "ui_common.back", "Back", "Voltar", "Volver",
            autoSize: true, autoSizeMin: 10f),
    };

    // ── CombatScene ────────────────────────────────────────────────────────────
    private static readonly LabelBinding[] CombatLabels =
    {
        new LabelBinding("BattleResultsCanvas/VictoryPanel/VictoryTitleText",
            "Combat", "combat.results.victory_title", "Victory!", "Vitória!", "¡Victoria!"),
        new LabelBinding("BattleResultsCanvas/DefeatPanel/DefeatTitleText",
            "Combat", "combat.results.defeat_title", "Defeated...", "Derrota...", "Derrota..."),
        new LabelBinding("BattleResultsCanvas/VictoryPanel/ReturnToFarmButton/Text (TMP)",
            "Combat", "combat.results.to_farm", "To Farm", "Para a Fazenda", "A la Granja",
            autoSize: true, autoSizeMin: 10f),
        new LabelBinding("BattleResultsCanvas/DefeatPanel/ReturnToFarmButton/Text (TMP)",
            "Combat", "combat.results.to_farm", "To Farm", "Para a Fazenda", "A la Granja",
            autoSize: true, autoSizeMin: 10f),
        new LabelBinding("BattleResultsCanvas/VictoryPanel/VictoryRetryButton/Text (TMP)",
            "Combat", "combat.results.retry", "Retry", "Tentar de Novo", "Reintentar",
            autoSize: true, autoSizeMin: 10f),
        new LabelBinding("BattleResultsCanvas/DefeatPanel/RetryBattleButton/Text (TMP)",
            "Combat", "combat.results.retry", "Retry", "Tentar de Novo", "Reintentar",
            autoSize: true, autoSizeMin: 10f),
    };

    // ── SampleScene ────────────────────────────────────────────────────────────
    private static readonly LabelBinding[] SampleSceneLabels =
    {
        // Team assembler button row — the combat mode toggle beside these is localized in
        // code, which left the other four visibly English next to it.
        new LabelBinding("TeamAssemblerCanvas/AssemblerPanel/ButtonContainer/FeedAllButton/Text",
            "Combat", "combat.teamassembler.feed_all", "Feed All", "Alimentar Todos", "Alimentar Todos",
            autoSize: true, autoSizeMin: 8f),
        new LabelBinding("TeamAssemblerCanvas/AssemblerPanel/ButtonContainer/ClearGridButton/Text",
            "Combat", "combat.teamassembler.clear_grid", "Clear Grid", "Limpar Grade", "Limpiar Cuadrícula",
            autoSize: true, autoSizeMin: 8f),
        new LabelBinding("TeamAssemblerCanvas/AssemblerPanel/ButtonContainer/StartBattleButton/Text",
            "Combat", "combat.teamassembler.start_battle", "Start Battle", "Iniciar Batalha", "Iniciar Batalla",
            autoSize: true, autoSizeMin: 8f),
        new LabelBinding("TeamAssemblerCanvas/AssemblerPanel/ButtonContainer/CancelButton/Text",
            "UI_Common", "ui_common.cancel", "Cancel", "Cancelar", "Cancelar",
            autoSize: true, autoSizeMin: 8f),

        // Animal market. The rows inside these panels were already localized in code,
        // so the shop showed Portuguese animal names and prices under an English
        // title with English tabs — the chrome was the only part left untranslated.
        new LabelBinding("AnimalMarketCanvas/MarketPanel/TitleText",
            "Animals", "animals.market.title", "Animal Market", "Mercado de Animais", "Mercado de Animales",
            autoSize: true, autoSizeMin: 14f),
        new LabelBinding("AnimalMarketCanvas/MarketPanel/TabRow/BuyTabButton/Text",
            "UI_Common", "ui_common.buy", "Buy", "Comprar", "Comprar",
            autoSize: true, autoSizeMin: 8f),
        new LabelBinding("AnimalMarketCanvas/MarketPanel/TabRow/SellTabButton/Text",
            "UI_Common", "ui_common.sell", "Sell", "Vender", "Vender",
            autoSize: true, autoSizeMin: 8f),
        new LabelBinding("AnimalMarketCanvas/ConfirmationPanel/ConfirmButtonRow/ConfirmYesButton/Text",
            "UI_Common", "ui_common.sell", "Sell", "Vender", "Vender",
            autoSize: true, autoSizeMin: 8f),
        new LabelBinding("AnimalMarketCanvas/ConfirmationPanel/ConfirmButtonRow/ConfirmNoButton/Text",
            "UI_Common", "ui_common.cancel", "Cancel", "Cancelar", "Cancelar",
            autoSize: true, autoSizeMin: 8f),

        // Building shop, same story.
        new LabelBinding("BuildingShopCanvas/BuildingPanel/TitleText",
            "Farming", "farming.buildingshop.title", "Farm Buildings", "Construções da Fazenda", "Construcciones de la Granja",
            autoSize: true, autoSizeMin: 14f),
        new LabelBinding("BuildingShopCanvas/ConfirmationPanel/ConfirmButtonRow/ConfirmYesButton/Text",
            "Farming", "farming.buildingshop.build", "Build", "Construir", "Construir",
            autoSize: true, autoSizeMin: 8f),
        new LabelBinding("BuildingShopCanvas/ConfirmationPanel/ConfirmButtonRow/ConfirmNoButton/Text",
            "UI_Common", "ui_common.cancel", "Cancel", "Cancelar", "Cancelar",
            autoSize: true, autoSizeMin: 8f),

        new LabelBinding("UI/SleepUICanvas/SleepConfirmationPanel/Container/ButtonContainer/CancelButton/Text (TMP)",
            "UI_Common", "ui_common.cancel", "Cancel", "Cancelar", "Cancelar",
            autoSize: true, autoSizeMin: 8f),
    };

    [MenuItem("Tools/Sowur Shield/Localize Static UI Text")]
    public static void RunFromMenu() => Run(showDialogs: true);

    /// <summary>
    /// Create the table entries and wire the LocalizeStringEvents.
    /// Pass showDialogs: false from automation — EditorUtility.DisplayDialog is modal and
    /// hangs an MCP session until a human clicks it.
    /// </summary>
    public static void Run(bool showDialogs = true)
    {
        var report = new List<string>();

        int entriesWritten = WriteTableEntries(MainMenuLabels
            .Concat(CombatLabels)
            .Concat(SampleSceneLabels));
        report.Add($"Table entries written: {entriesWritten}");

        report.Add(WireScene("Assets/Scenes/MainMenu.unity", MainMenuLabels));
        report.Add(WireScene("Assets/Scenes/CombatScene.unity", CombatLabels));
        report.Add(WireScene("Assets/Scenes/SampleScene.unity", SampleSceneLabels));

        AssetDatabase.SaveAssets();

        string text = string.Join("\n", report);
        Debug.Log("[StaticUILocalizationTool] " + text.Replace("\n", " | "));
        if (showDialogs)
            EditorUtility.DisplayDialog("Localize Static UI Text — Done", text, "OK");
    }

    /// <summary>Create/update every referenced key in en, pt and es.</summary>
    private static int WriteTableEntries(IEnumerable<LabelBinding> bindings)
    {
        int written = 0;
        // Distinct by table+key: several labels deliberately share one entry (two "To Farm"
        // buttons, "Back" in two panels), and writing the same entry twice is wasted work.
        var unique = bindings
            .GroupBy(b => b.table + "::" + b.key)
            .Select(g => g.First());

        foreach (var b in unique)
        {
            var collection = LocalizationEditorSettings.GetStringTableCollection(b.table);
            if (collection == null)
            {
                Debug.LogWarning($"[StaticUILocalizationTool] String table '{b.table}' not found — skipping key '{b.key}'.");
                continue;
            }

            var shared = collection.SharedData;
            if (shared.GetEntry(b.key) == null)
                shared.AddKey(b.key);

            foreach (StringTable table in collection.StringTables)
            {
                string code = table.LocaleIdentifier.Code;
                string value = code.StartsWith("pt") ? b.pt : code.StartsWith("es") ? b.es : b.en;
                table.AddEntry(b.key, value);
                EditorUtility.SetDirty(table);
            }
            EditorUtility.SetDirty(shared);
            written++;
        }
        return written;
    }

    /// <summary>Open a scene, attach the localizers, and save it.</summary>
    private static string WireScene(string scenePath, LabelBinding[] bindings)
    {
        var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

        int wired = 0;
        var missing = new List<string>();

        foreach (var b in bindings)
        {
            TextMeshProUGUI label = FindLabel(scene, b.path);
            if (label == null)
            {
                missing.Add(b.path);
                continue;
            }

            if (Bind(label, b))
                wired++;
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        string result = $"{scene.name}: wired {wired}/{bindings.Length}";
        if (missing.Count > 0)
            result += $" (not found: {string.Join(", ", missing)})";
        return result;
    }

    /// <summary>
    /// Attach (or update) a LocalizeStringEvent driving this label's text.
    /// Returns false if it was already bound to the same entry.
    /// </summary>
    private static bool Bind(TextMeshProUGUI label, LabelBinding binding)
    {
        var localizer = label.GetComponent<LocalizeStringEvent>();
        if (localizer == null)
            localizer = label.gameObject.AddComponent<LocalizeStringEvent>();

        localizer.StringReference.TableReference = binding.table;
        localizer.StringReference.TableEntryReference = binding.key;

        // Rebuild the UnityEvent target through SerializedObject so it persists to the
        // scene file exactly as the Inspector would write it. Setting the property in
        // code alone does not survive a save.
        var so = new SerializedObject(localizer);
        var updateString = so.FindProperty("m_UpdateString");
        if (updateString != null)
        {
            var calls = updateString.FindPropertyRelative("m_PersistentCalls.m_Calls");
            if (calls != null)
            {
                calls.ClearArray();
                calls.InsertArrayElementAtIndex(0);
                var call = calls.GetArrayElementAtIndex(0);
                call.FindPropertyRelative("m_Target").objectReferenceValue = label;
                call.FindPropertyRelative("m_TargetAssemblyTypeName").stringValue =
                    typeof(TextMeshProUGUI).AssemblyQualifiedName;
                call.FindPropertyRelative("m_MethodName").stringValue = "set_text";
                // EventDefined (0), NOT String (5). In String mode the call sends a fixed
                // stored argument and ignores the value the event was raised with, so the
                // label was being set to the empty stored string instead of the translation.
                call.FindPropertyRelative("m_Mode").enumValueIndex =
                    (int)UnityEngine.Events.PersistentListenerMode.EventDefined;
                call.FindPropertyRelative("m_CallState").enumValueIndex =
                    (int)UnityEventCallState.EditorAndRuntime;
            }
        }
        so.ApplyModifiedPropertiesWithoutUndo();

        if (binding.autoSize && !label.enableAutoSizing)
        {
            // Keep the authored size as the ceiling and let TMP shrink from there, so
            // English is unchanged and only the longer translations scale down.
            label.fontSizeMax = label.fontSize;
            label.fontSizeMin = binding.autoSizeMin;
            label.enableAutoSizing = true;
        }

        EditorUtility.SetDirty(localizer);
        EditorUtility.SetDirty(label);
        return true;
    }

    private static TextMeshProUGUI FindLabel(UnityEngine.SceneManagement.Scene scene, string path)
    {
        int slash = path.IndexOf('/');
        string rootName = slash < 0 ? path : path.Substring(0, slash);
        string rest = slash < 0 ? "" : path.Substring(slash + 1);

        foreach (var root in scene.GetRootGameObjects())
        {
            if (root.name != rootName) continue;
            Transform t = string.IsNullOrEmpty(rest) ? root.transform : root.transform.Find(rest);
            if (t != null) return t.GetComponent<TextMeshProUGUI>();
        }
        return null;
    }
}

}
