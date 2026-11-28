using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
namespace Ma3alem.Editor
{
    public static class CloudBuilds
    {
        public static void Windows() => StoreBuilds.Windows();
        public static void Web() => StoreBuilds.Web();
        public static void VerifyWindows()
        {
            ProjectTools.ValidateMusicExclusion();
            string folder = "Builds/Store/Verification";
            Directory.CreateDirectory(folder);
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions {
                scenes = new[] { "Assets/Ma3alem/Scenes/MainMenu.unity", "Assets/Ma3alem/Scenes/Gameplay.unity" },
                target = BuildTarget.StandaloneWindows64,
                locationPathName = folder + "/Borg Asyut.exe",
                options = BuildOptions.None,
                extraScriptingDefines = new[] { "ASYUT_VERIFY" }
            });
            if (report.summary.result != BuildResult.Succeeded)
                throw new InvalidOperationException("Cloud verification build failed.");
        }
    }
}
