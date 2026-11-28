using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Ma3alem.Editor
{
    public static class StoreBuilds
    {
        [MenuItem("Ma3alem/Store Release/Build Windows x64")]
        public static void Windows() => Build(BuildTarget.StandaloneWindows64);

        [MenuItem("Ma3alem/Store Release/Build WebGL")]
        public static void Web() => Build(BuildTarget.WebGL);

        private static void Build(BuildTarget target)
        {
            if (EditorUserBuildSettings.activeBuildTarget != target)
                throw new InvalidOperationException("Switch the active build profile to " + target + " first.");
            if (!BuildPipeline.IsBuildTargetSupported(BuildPipeline.GetBuildTargetGroup(target), target))
                throw new InvalidOperationException("Install Unity build support for " + target);
            var scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
            var expected = new[] { "Assets/Ma3alem/Scenes/MainMenu.unity", "Assets/Ma3alem/Scenes/Gameplay.unity" };
            if (!scenes.SequenceEqual(expected))
                throw new InvalidOperationException("Expected MainMenu followed by Gameplay.");
            if (PlayerSettings.companyName != "Mohamad El Hussein")
                throw new InvalidOperationException("Incorrect publisher identity.");
            if (EditorUserBuildSettings.development)
                throw new InvalidOperationException("Disable Development Build before exporting.");
            var root = Environment.GetEnvironmentVariable("BORG_STORE_OUTPUT");
            if (string.IsNullOrWhiteSpace(root)) root = "Builds/Store";
            var folder = Path.GetFullPath(Path.Combine(root, target == BuildTarget.WebGL ? "Web" : "Windows"));
            if (Directory.Exists(folder) && Directory.EnumerateFileSystemEntries(folder).Any())
                throw new IOException("Output directory must be empty: " + folder);
            Directory.CreateDirectory(folder);
            string destination = target == BuildTarget.WebGL ? folder : Path.Combine(folder, "icy tower Asyut.exe");
            var compression = PlayerSettings.WebGL.compressionFormat;
            var fallback = PlayerSettings.WebGL.decompressionFallback;
            var width = PlayerSettings.defaultScreenWidthWeb;
            var height = PlayerSettings.defaultScreenHeightWeb;
            try
            {
                if (target == BuildTarget.WebGL)
                {
                    // Portable static hosting: do not depend on custom gzip response headers.
                    PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;
                    PlayerSettings.WebGL.decompressionFallback = true;
                    PlayerSettings.defaultScreenWidthWeb = 1280;
                    PlayerSettings.defaultScreenHeightWeb = 720;
                }
                ProjectTools.ValidateMusicExclusion();
                // Existing VoiceBuildRefresh preprocessor refreshes voice references and checks music.
                var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions {
                    scenes = scenes, target = target, locationPathName = destination,
                    options = BuildOptions.None, extraScriptingDefines = Array.Empty<string>()
                });
                if (report.summary.result != BuildResult.Succeeded)
                    throw new InvalidOperationException("Store build failed: " + report.summary.result);
                Debug.Log("Store build succeeded: " + destination + " (" + report.summary.totalSize + " bytes)");
            }
            finally
            {
                if (target == BuildTarget.WebGL)
                {
                    PlayerSettings.WebGL.compressionFormat = compression;
                    PlayerSettings.WebGL.decompressionFallback = fallback;
                    PlayerSettings.defaultScreenWidthWeb = width;
                    PlayerSettings.defaultScreenHeightWeb = height;
                    AssetDatabase.SaveAssets();
                }
            }
        }
    }
}
