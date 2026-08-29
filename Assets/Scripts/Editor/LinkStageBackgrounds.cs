using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using SowurShield.Combat;

namespace SowurShield.Editor
{

/// <summary>
/// Assigns each StageData its background art from Assets/Art/Maps.
///
/// All 26 backgrounds were drawn and imported, but only the five Meadow stages ever had
/// theirs wired up. The other twenty fought over a flat blue-grey void with the art sitting
/// unused on disk — the same shape of gap as the villager portraits, where the asset
/// existed and nothing referenced it.
///
/// The filenames do not match the asset names, and not consistently: Meadow uses
/// zero-padded "Stage 001 — Sunny Fields" while Cave/Mountain/Volcano use "Stage 11 — …",
/// and several titles differ outright from the StageData (Stage_010_AncientWolf_Boss is
/// backed by "Mossy Hollow (Boss)", Stage_013_EchoChamber by "Mine Tunnels"). So matching
/// is by **stage number**, parsed from both sides, which is the one thing both naming
/// schemes agree on.
///
/// Only fills empty slots. A background already assigned by hand is left alone, so this is
/// safe to re-run.
///
/// Menu: Sowur Shield > Combat > Link Stage Backgrounds
/// </summary>
public static class LinkStageBackgrounds
{
    [MenuItem("Sowur Shield/Combat/Link Stage Backgrounds")]
    public static void Link()
    {
        // number -> sprite, built from the art folder
        var artByNumber = new Dictionary<int, Sprite>();

        foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets/Art/Maps" }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            int number = ParseStageNumber(System.IO.Path.GetFileNameWithoutExtension(path));
            if (number < 0 || artByNumber.ContainsKey(number))
                continue;

            // These import as spriteMode Multiple, so LoadAssetAtPath<Sprite> returns null —
            // take the first Sprite sub-asset instead.
            Sprite sprite = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().FirstOrDefault();
            if (sprite != null)
                artByNumber[number] = sprite;
        }

        int linked = 0, alreadySet = 0, noArt = 0;
        var report = new System.Text.StringBuilder();

        foreach (string guid in AssetDatabase.FindAssets("t:StageData"))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var stage = AssetDatabase.LoadAssetAtPath<StageData>(path);
            if (stage == null)
                continue;

            var serialized = new SerializedObject(stage);
            SerializedProperty bg = serialized.FindProperty("backgroundSprite");
            if (bg == null)
                continue;

            if (bg.objectReferenceValue != null)
            {
                alreadySet++;
                continue;
            }

            int number = ParseStageNumber(stage.name);
            if (number < 0 || !artByNumber.TryGetValue(number, out Sprite art))
            {
                noArt++;
                report.AppendLine($"  no art for {stage.name}");
                continue;
            }

            bg.objectReferenceValue = art;
            serialized.ApplyModifiedProperties();
            EditorUtility.SetDirty(stage);
            linked++;
            report.AppendLine($"  {stage.name} -> {art.name}");
        }

        AssetDatabase.SaveAssets();

        Debug.Log($"[LinkStageBackgrounds] linked {linked}, already set {alreadySet}, " +
                  $"no art {noArt}\n{report}");
    }

    /// <summary>
    /// Pulls the stage number out of either naming scheme: "Stage_007_DarkGrove" and
    /// "Stage 007 — Dark Grove" and "Stage 16 — Rocky Slopes" all yield their number.
    /// Returns -1 when there is none.
    /// </summary>
    private static int ParseStageNumber(string name)
    {
        var match = System.Text.RegularExpressions.Regex.Match(name, @"Stage[ _](\d+)");
        return match.Success && int.TryParse(match.Groups[1].Value, out int n) ? n : -1;
    }
}

}
