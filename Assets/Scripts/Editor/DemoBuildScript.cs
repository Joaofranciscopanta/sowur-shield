using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
using System;
using System.IO;

/// <summary>
/// Build script for creating demo builds (Windows & WebGL)
/// Disables save/load functionality for demo purposes
/// </summary>
public class DemoBuildScript
{
    private const string DEMO_DEFINE = "DEMO_BUILD";
    private const string BUILD_FOLDER = "Builds";

    [MenuItem("Build/Demo/Build Windows Demo")]
    public static void BuildWindowsDemo()
    {
        BuildDemo(BuildTarget.StandaloneWindows64, "Windows");
    }

    [MenuItem("Build/Demo/Build WebGL Demo")]
    public static void BuildWebGLDemo()
    {
        BuildDemo(BuildTarget.WebGL, "WebGL");
    }

    [MenuItem("Build/Demo/Build All Demos")]
    public static void BuildAllDemos()
    {

        bool windowsSuccess = BuildDemo(BuildTarget.StandaloneWindows64, "Windows");
        bool webglSuccess = BuildDemo(BuildTarget.WebGL, "WebGL");

    }

    private static bool BuildDemo(BuildTarget target, string platformName)
    {

        // Get current build target group
        BuildTargetGroup targetGroup = BuildPipeline.GetBuildTargetGroup(target);

        // Store original scripting defines
        string originalDefines = PlayerSettings.GetScriptingDefineSymbolsForGroup(targetGroup);

        try
        {
            // Platform-specific settings BEFORE adding define symbols
            // WebGL-specific settings
            if (target == BuildTarget.WebGL)
            {
                // Switch to WebGL platform first
                if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.WebGL)
                {
                    EditorUserBuildSettings.SwitchActiveBuildTarget(targetGroup, BuildTarget.WebGL);
                }

                // Disable compression for local file testing (works by double-clicking index.html)
                PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Disabled;
                PlayerSettings.WebGL.exceptionSupport = WebGLExceptionSupport.None;
                PlayerSettings.WebGL.dataCaching = false;

                // Disable auto-fullscreen on first user interaction
                PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
                PlayerSettings.defaultIsNativeResolution = false;

            }

            // Windows-specific settings
            if (target == BuildTarget.StandaloneWindows64)
            {
                // Switch to Windows platform first
                if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.StandaloneWindows64)
                {
                    EditorUserBuildSettings.SwitchActiveBuildTarget(targetGroup, BuildTarget.StandaloneWindows64);
                }

                PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
                PlayerSettings.defaultScreenWidth = 1920;
                PlayerSettings.defaultScreenHeight = 1080;
            }

            // Add DEMO_BUILD define symbol
            string demoDefines = AddDefineSymbol(originalDefines, DEMO_DEFINE);
            PlayerSettings.SetScriptingDefineSymbolsForGroup(targetGroup, demoDefines);


            // Wait for compilation to finish
            AssetDatabase.Refresh();

            // Check if scripts are compiling
            if (EditorApplication.isCompiling)
            {
                return false;
            }

            // Small delay to ensure settings are applied
            System.Threading.Thread.Sleep(500);

            // Configure build settings
            string buildPath = GetBuildPath(target, platformName);

            // Get scenes to build
            string[] scenes = GetScenesToBuild();

            // Configure build options
            BuildPlayerOptions buildOptions = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = buildPath,
                target = target,
                options = BuildOptions.None
            };


            // Build
            BuildReport report = BuildPipeline.BuildPlayer(buildOptions);
            BuildSummary summary = report.summary;

            if (summary.result == BuildResult.Succeeded)
            {

                // Open build folder
                EditorUtility.RevealInFinder(buildPath);

                return true;
            }
            else
            {
                return false;
            }
        }
        catch (Exception e)
        {
            return false;
        }
        finally
        {
            // Restore original scripting defines
            PlayerSettings.SetScriptingDefineSymbolsForGroup(targetGroup, originalDefines);
            AssetDatabase.Refresh();
        }
    }

    private static string GetBuildPath(BuildTarget target, string platformName)
    {
        string projectRoot = Directory.GetParent(Application.dataPath).FullName;
        string buildsFolder = Path.Combine(projectRoot, BUILD_FOLDER);

        // Create builds folder if it doesn't exist
        if (!Directory.Exists(buildsFolder))
        {
            Directory.CreateDirectory(buildsFolder);
        }

        // Use fixed folder names (no timestamps) for easier deployment
        string platformFolder = Path.Combine(buildsFolder, $"{platformName}_Demo");

        if (target == BuildTarget.StandaloneWindows64)
        {
            return Path.Combine(platformFolder, "Sowur Shield Demo.exe");
        }
        else if (target == BuildTarget.WebGL)
        {
            return platformFolder;
        }

        return platformFolder;
    }

    private static string[] GetScenesToBuild()
    {
        // Get all enabled scenes from build settings
        var scenes = new System.Collections.Generic.List<string>();

        foreach (EditorBuildSettingsScene scene in EditorBuildSettings.scenes)
        {
            if (scene.enabled)
            {
                scenes.Add(scene.path);
            }
        }

        if (scenes.Count == 0)
        {

            // Fallback: find all scenes in Assets/Scenes
            string[] sceneGuids = AssetDatabase.FindAssets("t:Scene", new[] { "Assets/Scenes" });
            foreach (string guid in sceneGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                scenes.Add(path);
            }
        }

        return scenes.ToArray();
    }

    private static string AddDefineSymbol(string defines, string symbol)
    {
        if (string.IsNullOrEmpty(defines))
        {
            return symbol;
        }

        if (defines.Contains(symbol))
        {
            return defines;
        }

        return defines + ";" + symbol;
    }

    private static string FormatBytes(ulong bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB" };
        double len = bytes;
        int order = 0;
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len = len / 1024;
        }
        return $"{len:0.##} {sizes[order]}";
    }
}
