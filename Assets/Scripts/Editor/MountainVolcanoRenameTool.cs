using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.Localization;
using UnityEditor.Localization;
using SowurShield.Combat;

namespace SowurShield.Editor
{
/// <summary>
/// Renames the Mountain and Volcano enemies to match the art that already exists on disk, and
/// assigns each one its sprite.
///
/// The situation: 12 PNGs shipped in Assets/Art/Enemies/Mountain and /Volcano named after
/// creatures ("Enemy 19 — Snow Wolf") that bear no relation to the EnemyData assets
/// ("ArmoredBear"). The data therefore had sprite: {fileID: 0} and rendered as a placeholder
/// sphere in combat, while perfectly good art sat unused beside it.
///
/// What is renamed and what is NOT:
/// - The .asset FILES keep their names. Every one is referenced by a StageData by GUID, and
///   renaming the file would not break the GUID but renaming the asset would churn the diff
///   for no benefit — the filename is not player-facing.
/// - `enemyName` (internal, used for GameObject names and logs) IS updated, because
///   EnemyData documents the localization key as "enemy.&lt;enemyName&gt;.name" and leaving them
///   inconsistent would mislead the next person wiring a key.
/// - `displayName` (what the player reads) is repointed at a new key, and the Combat string
///   table gets en/pt/es entries for it.
///
/// Six art files per biome against seven enemies each: IronGolem and ObsidianGolem keep their
/// current names and stay without a sprite. They are the two that had no plausible match, and
/// inventing one would mean two enemies sharing art.
///
/// Idempotent: re-running re-applies the same names, keys and sprites.
/// </summary>
public static class MountainVolcanoRenameTool
{
    private const string TableName = "Combat";

    /// <summary>
    /// One rename: which asset, what it becomes, and which art file it takes.
    ///
    /// Art was matched to stats rather than alphabetically, so the creature the player sees
    /// fits how it fights: the fastest/frailest enemy gets the wolf, the heavy bruiser gets the
    /// yeti, and each biome's highest-HP enemy gets that biome's boss art.
    /// </summary>
    private struct Rename
    {
        public string assetPath;     // EnemyData to modify
        public string newInternal;   // PascalCase stem for the localization key
        public string en, pt, es;    // player-facing name in each locale
        public string artPath;       // sprite to assign
    }

    /// <summary>
    /// The project's existing convention, followed by all 19 enemies that shipped before this:
    /// `enemyName` is the spaced English name ("Cave Bat") while the localization key uses the
    /// PascalCase stem ("enemy.CaveBat.name"). Writing the PascalCase form into enemyName would
    /// have made these twelve the odd ones out, and enemyName is what appears in logs and
    /// GameObject names.
    /// </summary>
    private static string SpacedName(string en) => en;

    private static List<Rename> BuildRenames()
    {
        const string M = "Assets/Resources/Enemies/Mountain";
        const string V = "Assets/Resources/Enemies/Volcano";
        const string MA = "Assets/Art/Enemies/Mountain";
        const string VA = "Assets/Art/Enemies/Volcano";

        return new List<Rename>
        {
            // ── Mountain ──────────────────────────────────────────────────────────────
            // Harpy: 60hp, speed 24 — the fast, fragile one, so the wolf.
            new Rename { assetPath = $"{M}/Harpy.asset", newInternal = "SnowWolf",
                         en = "Snow Wolf", pt = "Lobo da Neve", es = "Lobo de Nieve",
                         artPath = $"{MA}/Enemy 19 — Snow Wolf.png" },

            // ThunderEagle: 90hp, speed 26 — fastest in the biome, an elemental fits.
            new Rename { assetPath = $"{M}/ThunderEagle.asset", newInternal = "IceElemental",
                         en = "Ice Elemental", pt = "Elemental de Gelo", es = "Elemental de Hielo",
                         artPath = $"{MA}/Enemy 20 — Ice Elemental.png" },

            // RockHound: 85hp, middling everything — the humanoid bandit.
            new Rename { assetPath = $"{M}/RockHound.asset", newInternal = "MountainBandit",
                         en = "Mountain Bandit", pt = "Bandido da Montanha", es = "Bandido de la Montaña",
                         artPath = $"{MA}/Enemy 21 — Mountain Bandit.png" },

            // ArmoredBear: 150hp, attack 20, speed 8 — slow heavy bruiser, the yeti.
            new Rename { assetPath = $"{M}/ArmoredBear.asset", newInternal = "StoneYeti",
                         en = "Stone Yeti", pt = "Yeti de Pedra", es = "Yeti de Piedra",
                         artPath = $"{MA}/Enemy 22 — Stone Yeti.png" },

            // FrostDrake: 130hp — keeps an ice theme, becomes the frost golem.
            new Rename { assetPath = $"{M}/FrostDrake.asset", newInternal = "FrostGolem",
                         en = "Frost Golem", pt = "Golem de Gelo", es = "Gólem de Escarcha",
                         artPath = $"{MA}/Enemy 23 — Frost Golem.png" },

            // MountainKing: 800hp, the biome's boss — takes the mini-boss art.
            new Rename { assetPath = $"{M}/MountainKing.asset", newInternal = "MountainTitan",
                         en = "Mountain Titan", pt = "Titã da Montanha", es = "Titán de la Montaña",
                         artPath = $"{MA}/Enemy 24 — Mountain Titan (Mini-boss).png" },

            // ── Volcano ───────────────────────────────────────────────────────────────
            // MagmaSlime: 130hp, speed 10 — already a slime, keeps the concept.
            new Rename { assetPath = $"{V}/MagmaSlime.asset", newInternal = "FireSlime",
                         en = "Fire Slime", pt = "Gosma de Fogo", es = "Limo de Fuego",
                         artPath = $"{VA}/Enemy 25 — Fire Slime.png" },

            // LavaSalamander: 90hp — a lizard already, matches directly.
            new Rename { assetPath = $"{V}/LavaSalamander.asset", newInternal = "MagmaLizard",
                         en = "Magma Lizard", pt = "Lagarto de Magma", es = "Lagarto de Magma",
                         artPath = $"{VA}/Enemy 26 — Magma Lizard.png" },

            // Hellhound: 140hp, attack 45 — the aggressive melee threat, so the knight.
            new Rename { assetPath = $"{V}/Hellhound.asset", newInternal = "LavaKnight",
                         en = "Lava Knight", pt = "Cavaleiro de Lava", es = "Caballero de Lava",
                         artPath = $"{VA}/Enemy 27 — Lava Knight.png" },

            // FireImp: 60hp, speed 28 — the small fast one, an imp reads as a demon.
            new Rename { assetPath = $"{V}/FireImp.asset", newInternal = "InfernalDemon",
                         en = "Infernal Demon", pt = "Demônio Infernal", es = "Demonio Infernal",
                         artPath = $"{VA}/Enemy 28 — Infernal Demon.png" },

            // VolcanicDrake: 700hp — the second-heaviest, becomes the colossus.
            new Rename { assetPath = $"{V}/VolcanicDrake.asset", newInternal = "FlameColossus",
                         en = "Flame Colossus", pt = "Colosso de Chamas", es = "Coloso de Llamas",
                         artPath = $"{VA}/Enemy 29 — Flame Colossus.png" },

            // InfernoDragon: 1200hp, the strongest enemy in the game — the final boss art.
            new Rename { assetPath = $"{V}/InfernoDragon.asset", newInternal = "LordOfAshes",
                         en = "Lord of Ashes", pt = "Senhor das Cinzas", es = "Señor de las Cenizas",
                         artPath = $"{VA}/Enemy 30 — Lord of Ashes (Final Boss).png" },
        };
    }

    /// <summary>
    /// No modal dialog: this menu item is also driven through automation, and
    /// EditorUtility.DisplayDialog blocks the editor until a human clicks it.
    /// </summary>
    [MenuItem("Tools/Combat/Rename Mountain and Volcano Enemies To Match Art")]
    public static void ApplyRenames()
    {
        var collection = LocalizationEditorSettings.GetStringTableCollection(TableName);
        if (collection == null)
        {
            Debug.LogError($"[MountainVolcanoRenameTool] String table '{TableName}' not found — " +
                           "enemy names would render blank. Aborting.");
            return;
        }

        int renamed = 0, spritesAssigned = 0, missingArt = 0;

        foreach (var r in BuildRenames())
        {
            var enemy = AssetDatabase.LoadAssetAtPath<EnemyData>(r.assetPath);
            if (enemy == null)
            {
                Debug.LogWarning($"[MountainVolcanoRenameTool] Missing EnemyData at {r.assetPath} — skipped.");
                continue;
            }

            // Spaced English name, matching every enemy that shipped before this batch.
            enemy.enemyName = SpacedName(r.en);

            string key = $"enemy.{r.newInternal}.name";
            if (WriteEntry(collection, key, r.en, r.pt, r.es) == 0)
            {
                Debug.LogWarning($"[MountainVolcanoRenameTool] Could not create '{key}' in the " +
                                 $"{TableName} table. {r.en} will fall back to its enemyName.");
            }

            // Bound by key string, not by the shared-entry id. Both resolve at runtime, but an
            // id-only binding writes `m_Key:` empty into the .asset — which makes the YAML diff
            // unreadable and, worse, silently opts the enemy out of
            // CombatContentTests.EnemyLocalizationKeys_MatchTheirAssetName, which skips entries
            // it cannot compare. The test then passes by omission rather than by correctness.
            //
            // The constructor is not enough: LocalizedString(table, key) resolves the key against
            // the table and serializes only the id it found, leaving m_Key empty. Assigning the
            // string to TableEntryReference afterwards is what makes the name survive into YAML.
            var localized = new LocalizedString(TableName, key);
            localized.TableEntryReference = key;
            enemy.displayName = localized;

            var sprite = LoadSprite(r.artPath);
            if (sprite != null)
            {
                enemy.sprite = sprite;
                spritesAssigned++;
            }
            else
            {
                missingArt++;
                Debug.LogWarning($"[MountainVolcanoRenameTool] No sprite at '{r.artPath}' for " +
                                 $"{r.newInternal}. It will still render as a placeholder in combat.");
            }

            EditorUtility.SetDirty(enemy);
            renamed++;
        }

        EditorUtility.SetDirty(collection.SharedData);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[MountainVolcanoRenameTool] {renamed} enemies renamed, {spritesAssigned} sprites " +
                  $"assigned, {missingArt} without art. IronGolem and ObsidianGolem intentionally " +
                  "left unchanged — there are only 6 art files per biome for 7 enemies.");
    }

    /// <summary>
    /// Resolves the Sprite for an imported texture.
    ///
    /// These files import with spriteMode: Multiple, so the Sprite is a sub-asset and
    /// LoadAssetAtPath&lt;Sprite&gt; returns null even though the file is a perfectly valid
    /// sprite — it only finds the main asset, which is the Texture2D. Falling back to
    /// LoadAllAssetsAtPath and picking the first Sprite handles both Single and Multiple.
    /// </summary>
    private static Sprite LoadSprite(string path)
    {
        var direct = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (direct != null) return direct;

        foreach (var obj in AssetDatabase.LoadAllAssetsAtPath(path))
        {
            if (obj is Sprite s) return s;
        }
        return null;
    }

    /// <summary>Writes one key into all three locales and returns its shared-entry id.</summary>
    private static long WriteEntry(StringTableCollection collection, string key,
                                   string en, string pt, string es)
    {
        var shared = collection.SharedData;
        var sharedEntry = shared.GetEntry(key) ?? shared.AddKey(key);
        if (sharedEntry == null) return 0;

        foreach (var table in collection.StringTables)
        {
            string code = table.LocaleIdentifier.Code;
            string value = code.StartsWith("pt") ? pt : (code.StartsWith("es") ? es : en);
            table.AddEntry(key, value);
            EditorUtility.SetDirty(table);
        }

        return sharedEntry.Id;
    }
}
} // namespace SowurShield.Editor
