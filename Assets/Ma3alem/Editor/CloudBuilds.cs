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
        // Unity Build Automation Advanced Settings -> Script hooks -> Pre-Export Method.
        public static void PrepareBuildAutomation()
        {
            if (Application.unityVersion != "6000.3.29f1")
                throw new InvalidOperationException("Select the pinned patched Unity 6000.3.29f1 configuration.");
            if (PlayerSettings.companyName != "Mohamad El Hussein" || PlayerSettings.productName != "Borg Asyut")
                throw new InvalidOperationException("Unexpected creator or product identity.");
            if (EditorUserBuildSettings.development)
                throw new InvalidOperationException("Disable Development Build for release candidates.");
            ProjectTools.ValidateMusicExclusion();
            if (EditorUserBuildSettings.activeBuildTarget == BuildTarget.WebGL)
            {
                PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;
                PlayerSettings.WebGL.decompressionFallback = true;
                PlayerSettings.defaultWebScreenWidth = 1280;
                PlayerSettings.defaultWebScreenHeight = 720;
            }
            AssetDatabase.SaveAssets();
        }

        // Unity Build Automation Advanced Settings -> Script hooks -> Post-Export Method.
        public static void RecordBuildAutomationExport(string exportPath)
        {
            string folder = Directory.Exists(exportPath) ? exportPath : Path.GetDirectoryName(exportPath);
            if (string.IsNullOrEmpty(folder) || !Directory.Exists(folder))
                throw new IOException("Unity Build Automation export folder not found.");
            string notices = Path.Combine(folder, "Third_Party_Notices");
            Directory.CreateDirectory(notices);
            var paths = new[] { "CREDITS.md", "Assets/Ma3alem/Fonts/Lemonada-OFL.txt", "Assets/Ma3alem/Fonts/OFL.txt", "Assets/ThirdParty/RTLTMPro/LICENSE.txt", "Assets/ThirdParty/TMPShaders/LICENSE.txt" };
            var names = new[] { "CREDITS.md", "Lemonada-OFL.txt", "Noto-OFL.txt", "RTLTMPro-LICENSE.txt", "TMPShaders-LICENSE.txt" };
            for (int i = 0; i < paths.Length; i++) File.Copy(paths[i], Path.Combine(notices, names[i]), true);
            File.WriteAllLines(Path.Combine(folder, "CLOUD_BUILD_RECEIPT.txt"), new[] {
                "unity=" + Application.unityVersion,
                "platform=" + EditorUserBuildSettings.activeBuildTarget,
                "creator=Mohamad El Hussein",
                "product=Borg Asyut",
                "build_only=true; inspect separate test reports before accepting",
                "public_launch_approved=false"
            });
        }

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
