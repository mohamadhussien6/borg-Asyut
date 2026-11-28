using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Ma3alem.Editor
{
    [InitializeOnLoad]
    public static class ProjectTools
    {
        public const string VoiceFolder = "Assets/Audio/VoiceLines/";
        private const string AudioPrefab = "Assets/Ma3alem/Prefabs/Audio.prefab";
        private const string LocalMusic = "Assets/LocalPreviewOnly/مزمار صعيدي.mp3";
        private static bool localPreviewBuild;
        private static readonly string[] Scenes = { "Assets/Ma3alem/Scenes/MainMenu.unity", "Assets/Ma3alem/Scenes/Gameplay.unity" };
        [Serializable] private sealed class AtlasRect { public string name; public int x, y, width, height; }
        [Serializable] private sealed class AtlasLayout { public AtlasRect[] sprites; }

        [MenuItem("Ma3alem/Apply neighborhood modules")]
        public static void ApplyNeighborhoodArt()
        {
            const string atlasPath = "Assets/Ma3alem/Art/NeighborhoodModules.png";
            const string skylinePath = "Assets/Ma3alem/Art/AssiutResidentialSkyline.png";
            AssetDatabase.Refresh();
            var importer = (TextureImporter)AssetImporter.GetAtPath(atlasPath);
            importer.textureType = TextureImporterType.Sprite; importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.spritePixelsPerUnit = 240; importer.mipmapEnabled = false; importer.alphaIsTransparency = true;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            var layout = JsonUtility.FromJson<AtlasLayout>(File.ReadAllText("Documentation/NeighborhoodAtlasRects.json"));
            importer.spritesheet = layout.sprites.Select(rect => new SpriteMetaData { name = rect.name,
                rect = new Rect(rect.x, rect.y, rect.width, rect.height), pivot = Vector2.one * 0.5f, alignment = (int)SpriteAlignment.Center }).ToArray();
            importer.SaveAndReimport();
            var skylineImporter = (TextureImporter)AssetImporter.GetAtPath(skylinePath);
            skylineImporter.textureType = TextureImporterType.Sprite; skylineImporter.spriteImportMode = SpriteImportMode.Single;
            skylineImporter.spritePixelsPerUnit = 256; skylineImporter.mipmapEnabled = false; skylineImporter.alphaIsTransparency = true;
            skylineImporter.textureCompression = TextureImporterCompression.Uncompressed; skylineImporter.SaveAndReimport();
            var variants = AssetDatabase.LoadAllAssetsAtPath(atlasPath).OfType<Sprite>().OrderBy(sprite => sprite.name).ToArray();
            if (variants.Length != 9) throw new InvalidOperationException("Expected nine neighborhood sprite modules.");
            foreach (var sprite in variants)
            {
                var module = new GameObject(sprite.name, typeof(SpriteRenderer));
                module.GetComponent<SpriteRenderer>().sprite = sprite; module.GetComponent<SpriteRenderer>().sortingOrder = -15;
                PrefabUtility.SaveAsPrefabAsset(module, "Assets/Ma3alem/Prefabs/BuildingParts/" + sprite.name + ".prefab");
                UnityEngine.Object.DestroyImmediate(module);
            }
            var setup = EditorSceneManager.GetSceneManagerSetup();
            try
            {
                foreach (string path in Scenes)
                {
                    var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                    var backdrop = UnityEngine.Object.FindFirstObjectByType<BuildingBackdrop>();
                    var serialized = new SerializedObject(backdrop);
                    var modules = serialized.FindProperty("architecturalVariants"); modules.arraySize = variants.Length;
                    for (int i = 0; i < variants.Length; i++) modules.GetArrayElementAtIndex(i).objectReferenceValue = variants[i];
                    serialized.FindProperty("landmarkSprite").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Sprite>(skylinePath);
                    serialized.ApplyModifiedPropertiesWithoutUndo(); EditorSceneManager.SaveScene(scene);
                }
            }
            finally
            {
                if (setup.Any(entry => entry.isLoaded)) EditorSceneManager.RestoreSceneManagerSetup(setup);
                else EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            }
            AssetDatabase.SaveAssets(); Validate();
        }
        [MenuItem("Ma3alem/Apply reference facade artwork")]
        public static void ApplyReferenceFacades()
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            const string path = "Assets/Ma3alem/Art/AssiutClassicFacades.png";
            AssetDatabase.Refresh();
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite; importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.spritePixelsPerUnit = 240; importer.mipmapEnabled = false; importer.alphaIsTransparency = true;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            var layout = JsonUtility.FromJson<AtlasLayout>(File.ReadAllText("Documentation/ReferenceFacadeRects.json"));
            importer.spritesheet = layout.sprites.Select(rect => new SpriteMetaData { name = rect.name,
                rect = new Rect(rect.x, rect.y, rect.width, rect.height), pivot = Vector2.one * 0.5f, alignment = (int)SpriteAlignment.Center }).ToArray();
            importer.SaveAndReimport();
            var sprites = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().OrderBy(sprite => sprite.name).ToArray();
            if (sprites.Length != 9) throw new InvalidOperationException("Expected nine reference facade modules.");
            foreach (var sprite in sprites)
            {
                var module = new GameObject(sprite.name, typeof(SpriteRenderer));
                module.GetComponent<SpriteRenderer>().sprite = sprite; module.GetComponent<SpriteRenderer>().sortingOrder = -20;
                PrefabUtility.SaveAsPrefabAsset(module, "Assets/Ma3alem/Prefabs/BuildingParts/" + sprite.name + ".prefab");
                UnityEngine.Object.DestroyImmediate(module);
            }
            var setup = EditorSceneManager.GetSceneManagerSetup();
            try
            {
                foreach (string scenePath in Scenes)
                {
                    var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
                    var backdrop = UnityEngine.Object.FindFirstObjectByType<BuildingBackdrop>();
                    var serialized = new SerializedObject(backdrop); var facades = serialized.FindProperty("completeFacadeVariants");
                    serialized.FindProperty("foregroundStoreyHeight").floatValue = 3.3f;
                    facades.arraySize = sprites.Length;
                    for (int i = 0; i < sprites.Length; i++) facades.GetArrayElementAtIndex(i).objectReferenceValue = sprites[i];
                    serialized.ApplyModifiedPropertiesWithoutUndo(); EditorSceneManager.SaveScene(scene);
                }
            }
            finally
            {
                if (setup.Any(entry => entry.isLoaded)) EditorSceneManager.RestoreSceneManagerSetup(setup);
                else EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            }
            AssetDatabase.SaveAssets(); Validate();
        }
        [MenuItem("Ma3alem/Apply permanent static environment")]
        public static void ApplyStaticEnvironments()
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            const string imagePath = "Assets/Ma3alem/Art/Environment_Assiut_Static.png";
            AssetDatabase.Refresh();
            var importer = (TextureImporter)AssetImporter.GetAtPath(imagePath);
            importer.textureType = TextureImporterType.Sprite; importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.spritePixelsPerUnit = 100; importer.mipmapEnabled = false; importer.alphaIsTransparency = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed; importer.npotScale = TextureImporterNPOTScale.None;
            importer.maxTextureSize = 2048; importer.wrapMode = TextureWrapMode.Clamp;
            var settings = new TextureImporterSettings(); importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect; importer.SetTextureSettings(settings);
            var source = new Texture2D(2, 2);
            if (!ImageConversion.LoadImage(source, File.ReadAllBytes(imagePath))) throw new InvalidDataException(imagePath);
            int width = source.width, height = source.height; UnityEngine.Object.DestroyImmediate(source);
            int left = Mathf.RoundToInt(width * 0.2f);
            int[] edges = { 0, left, width - left, width };
            string[] names = { "Left", "Center", "Right" };
            importer.spritesheet = Enumerable.Range(0, 3).Select(i => new SpriteMetaData {
                name = "Assiut_" + names[i], rect = new Rect(edges[i], 0, edges[i + 1] - edges[i], height),
                pivot = Vector2.one * 0.5f, alignment = (int)SpriteAlignment.Center }).ToArray();
            importer.SaveAndReimport();
            var sprites = AssetDatabase.LoadAllAssetsAtPath(imagePath).OfType<Sprite>().ToArray();
            var setup = EditorSceneManager.GetSceneManagerSetup();
            try
            {
                foreach (string path in Scenes)
                {
                    var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                    var serialized = new SerializedObject(UnityEngine.Object.FindFirstObjectByType<BuildingBackdrop>());
                    var array = serialized.FindProperty("environmentPanels"); array.arraySize = 3;
                    for (int i = 0; i < 3; i++) array.GetArrayElementAtIndex(i).objectReferenceValue = sprites.Single(sprite => sprite.name == "Assiut_" + names[i]);
                    serialized.ApplyModifiedPropertiesWithoutUndo(); EditorSceneManager.SaveScene(scene);
                }
            }
            finally
            {
                if (setup.Any(entry => entry.isLoaded)) EditorSceneManager.RestoreSceneManagerSetup(setup);
                else EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            }
            AssetDatabase.SaveAssets(); Validate();
        }
        [MenuItem("Ma3alem/Configure dedicated playfield walls")]
        public static void ConfigurePlayfieldBoundaries()
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            // Authored innermost facade projections in the current 1671x941 plate.
            // Walls touch those planes with their outer faces; inward thickness reserves room for visual flips.
            const float leftPixel = 393, rightPixel = 1212, sourceWidth = 1671, sourceHeight = 941, visualClearance = 0.65f;
            var setup = EditorSceneManager.GetSceneManagerSetup();
            try
            {
                foreach (string path in Scenes)
                {
                    var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                    var camera = UnityEngine.Object.FindFirstObjectByType<CameraFollow>();
                    var serialized = new SerializedObject(camera);
                    float scale = (serialized.FindProperty("cameraSize").floatValue * 2 + 0.02f) / sourceHeight;
                    float facadeLeft = (leftPixel - sourceWidth / 2) * scale;
                    float facadeRight = (rightPixel - sourceWidth / 2) * scale;
                    float left = facadeLeft + visualClearance, right = facadeRight - visualClearance;
                    serialized.FindProperty("playfieldHalfWidth").floatValue = (right - left) / 2;
                    serialized.FindProperty("playfieldCenterX").floatValue = (right + left) / 2;
                    serialized.ApplyModifiedPropertiesWithoutUndo();
                    foreach (var wall in camera.GetComponentsInChildren<BoxCollider2D>(true))
                    {
                        bool isLeft = wall.transform.localPosition.x < 0;
                        wall.transform.localPosition = new Vector3(isLeft ? facadeLeft + visualClearance / 2 : facadeRight - visualClearance / 2, 0, 10);
                        wall.size = new Vector2(visualClearance, 40);
                        var body = wall.GetComponent<Rigidbody2D>();
                        body.bodyType = RigidbodyType2D.Kinematic; body.gravityScale = 0;
                        body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
                    }
                    var spawner = new SerializedObject(UnityEngine.Object.FindFirstObjectByType<PlatformSpawner>());
                    spawner.FindProperty("halfWidth").floatValue = (right - left) / 2;
                    spawner.ApplyModifiedPropertiesWithoutUndo(); EditorSceneManager.SaveScene(scene);
                }
            }
            finally
            {
                if (setup.Any(entry => entry.isLoaded)) EditorSceneManager.RestoreSceneManagerSetup(setup);
                else EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            }
            AssetDatabase.SaveAssets(); Validate();
        }
        private const string ArcadeFolder = "Builds/Arcade_20261123_R37";
        [MenuItem("Ma3alem/Apply arcade gameplay tuning")]
        public static void ApplyArcadeGameplayTuning()
        {
            var prefab = PrefabUtility.LoadPrefabContents("Assets/Ma3alem/Prefabs/Player.prefab");
            try
            {
                var player = new SerializedObject(prefab.GetComponent<PlayerController>());
                string[] names = { "maxSpeed", "acceleration", "braking", "reversalAcceleration", "jumpSpeed", "momentumJumpBoost", "airAcceleration", "airBraking", "coyoteTime", "jumpBuffer", "flipSpeedThreshold", "doubleFlipSpeedThreshold", "wallMomentumRetention", "reboundSteeringGrace" };
                float[] values = { 9.5f, 32, 18, 38, 12, 13, 22, 0.12f, 0.12f, 0.14f, 0.45f, 0.85f, 0.96f, 0.10f };
                for (int i = 0; i < names.Length; i++) player.FindProperty(names[i]).floatValue = values[i];
                player.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(prefab, "Assets/Ma3alem/Prefabs/Player.prefab");
            }
            finally { PrefabUtility.UnloadPrefabContents(prefab); }
            var setup = EditorSceneManager.GetSceneManagerSetup();
            try
            {
                foreach (string path in Scenes)
                {
                    var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                    var camera = new SerializedObject(UnityEngine.Object.FindFirstObjectByType<CameraFollow>());
                    camera.FindProperty("graceSeconds").floatValue = 7;
                    camera.FindProperty("initialRiseSpeed").floatValue = 0.38f;
                    camera.FindProperty("maximumRiseSpeed").floatValue = 1.35f;
                    camera.FindProperty("followSmoothTime").floatValue = 0.18f;
                    camera.FindProperty("trackingLead").floatValue = 1.5f;
                    camera.FindProperty("maximumFollowSpeed").floatValue = 18;
                    camera.ApplyModifiedPropertiesWithoutUndo();
                    var spawner = new SerializedObject(UnityEngine.Object.FindFirstObjectByType<PlatformSpawner>());
                    var difficulty = spawner.FindProperty("difficulty");
                    difficulty.FindPropertyRelative("firstWidth").floatValue = 4;
                    difficulty.FindPropertyRelative("lastWidth").floatValue = 2.2f;
                    difficulty.FindPropertyRelative("firstGap").floatValue = 1.12f;
                    difficulty.FindPropertyRelative("lastGap").floatValue = 1.65f;
                    difficulty.FindPropertyRelative("gapVariation").floatValue = 0.35f;
                    spawner.ApplyModifiedPropertiesWithoutUndo(); EditorSceneManager.SaveScene(scene);
                    var actualPlayer = UnityEngine.Object.FindFirstObjectByType<PlayerController>();
                    string playerPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(actualPlayer.gameObject);
                    Debug.Log("Active scene " + path + "; player prefab " + playerPath + "; controller " + AssetDatabase.GetAssetPath(MonoScript.FromMonoBehaviour(actualPlayer)));
                }
            }
            finally
            {
                if (setup.Any(entry => entry.isLoaded)) EditorSceneManager.RestoreSceneManagerSetup(setup);
                else EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            }
            PlayerSettings.bundleVersion = "1.2.0-arcade-r3"; AssetDatabase.SaveAssets(); Validate();
        }
        [MenuItem("Ma3alem/Build NEW arcade Windows")]
        public static void BuildArcadeWindows()
        { BuildAt(ArcadeFolder + "/Windows/icy tower Asyut.exe", true, false); }
        [MenuItem("Ma3alem/Build NEW arcade authorized local preview")]
        public static void BuildArcadeLocalPreview()
        { BuildLocalPreviewAt(ArcadeFolder + "/LocalPreview/icy tower Asyut.exe", true); }
        static ProjectTools()
        {
            EditorApplication.delayCall += RebuildVoices;
            AudioManager.LocalPreviewMusicLoader = LoadLocalPreview;
        }

        [MenuItem("Ma3alem/Play game")]
        public static void PlayGame()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            EditorSceneManager.OpenScene(Scenes[0], OpenSceneMode.Single);
            EditorApplication.isPlaying = true;
        }

        [MenuItem("Ma3alem/Refresh real voice recordings")]
        public static void RebuildVoices()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || !File.Exists(AudioPrefab)) return;
            GameObject prefab = PrefabUtility.LoadPrefabContents(AudioPrefab);
            try
            {
                var serialized = new SerializedObject(prefab.GetComponent<AudioManager>());
                var groups = serialized.FindProperty("voices");
                string[] identifiers = AssetDatabase.IsValidFolder(VoiceFolder.TrimEnd('/'))
                    ? AssetDatabase.FindAssets("t:AudioClip", new[] { VoiceFolder.TrimEnd('/') }) : Array.Empty<string>();
                string[] paths = identifiers
                    .Select(AssetDatabase.GUIDToAssetPath).Where(path => path.EndsWith(".wav", StringComparison.OrdinalIgnoreCase))
                    .OrderBy(path => path, StringComparer.Ordinal).ToArray();
                string[] saidi = paths.Where(path => path.StartsWith(VoiceFolder + "Saidi/", StringComparison.Ordinal)).ToArray();
                if (saidi.Length > 0)
                {
                    foreach (AudioManager.VoiceCue cue in Enum.GetValues(typeof(AudioManager.VoiceCue)))
                    {
                        string path = saidi.FirstOrDefault(file => MatchesCue(Path.GetFileNameWithoutExtension(file), cue.ToString()));
                        var clip = path == null ? null : AssetDatabase.LoadAssetAtPath<AudioClip>(path);
                        if (clip == null || clip.frequency != 48000 || clip.channels != 2)
                            throw new InvalidOperationException("Saidi replacement pack is not ready; previous references retained: " + cue);
                    }
                    paths = saidi;
                }
                groups.arraySize = Enum.GetValues(typeof(AudioManager.VoiceCue)).Length;
                for (int i = 0; i < groups.arraySize; i++) SetGroup(groups.GetArrayElementAtIndex(i), (AudioManager.VoiceCue)i, paths);
                if (!serialized.ApplyModifiedPropertiesWithoutUndo()) return;
                PrefabUtility.SaveAsPrefabAsset(prefab, AudioPrefab);
            }
            finally { PrefabUtility.UnloadPrefabContents(prefab); }
        }

        private static void SetGroup(SerializedProperty group, AudioManager.VoiceCue cue, string[] paths)
        {
            group.FindPropertyRelative("cue").enumValueIndex = (int)cue;
            string cueName = cue.ToString();
            var matches = paths.Where(path => MatchesCue(Path.GetFileNameWithoutExtension(path), cueName)).ToArray();
            var clips = group.FindPropertyRelative("clips"); clips.arraySize = matches.Length;
            for (int i = 0; i < matches.Length; i++)
                clips.GetArrayElementAtIndex(i).objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>(matches[i]);
        }

        public static bool MatchesCue(string filename, string cue)
        {
            if (filename.Equals(cue, StringComparison.OrdinalIgnoreCase)) return true;
            if (!filename.StartsWith(cue + "_", StringComparison.OrdinalIgnoreCase)) return false;
            string suffix = filename.Substring(cue.Length + 1);
            return suffix.Length >= 2 && suffix.All(char.IsDigit);
        }

        [MenuItem("Ma3alem/Select local preview music (Editor only)")]
        public static void SelectLocalPreview()
        {
            string path = EditorUtility.OpenFilePanel("Local-only 16-bit PCM WAV", "", "wav");
            if (!string.IsNullOrEmpty(path)) EditorPrefs.SetString("Ma3alem.LocalPreviewMusic", path);
        }

        [MenuItem("Ma3alem/Disable local preview music")]
        public static void DisableLocalPreview() { EditorPrefs.DeleteKey("Ma3alem.LocalPreviewMusic"); }

        public static void ConfigureProvidedPreview()
        {
            string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), "مزمار صعيدي.mp3");
            if (!File.Exists(path) && File.Exists(LocalMusic)) path = Path.GetFullPath(LocalMusic);
            if (!File.Exists(path)) throw new FileNotFoundException("The exact supplied MP3 is required", path);
            Directory.CreateDirectory("Assets/LocalPreviewOnly");
            if (Path.GetFullPath(path) != Path.GetFullPath(LocalMusic)) File.Copy(path, LocalMusic, true);
            AssetDatabase.Refresh();
            var importer = (AudioImporter)AssetImporter.GetAtPath(LocalMusic);
            var settings = importer.defaultSampleSettings;
            settings.loadType = AudioClipLoadType.Streaming; settings.compressionFormat = AudioCompressionFormat.Vorbis;
            settings.quality = 0.9f; settings.sampleRateSetting = AudioSampleRateSetting.PreserveSampleRate;
            importer.forceToMono = false; importer.defaultSampleSettings = settings; importer.SaveAndReimport();
            EditorPrefs.SetString("Ma3alem.LocalPreviewMusic", LocalMusic);
        }

        public static void ConfigureAssiut()
        {
            BuildArchitecturePrefabs(); RebuildVoices(); ConfigureProvidedPreview();
            foreach (string path in Scenes)
            {
                var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                foreach (var root in scene.GetRootGameObjects())
                {
                    var ui = root.GetComponent<UIManager>();
                    if (ui != null)
                    {
                        var settings = new SerializedObject(ui);
                        settings.FindProperty("gameTitle").stringValue = "برج أسيوط";
                        settings.FindProperty("buttonSprite").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Ma3alem/Art/Button.png");
                        settings.FindProperty("shishaSprite").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Ma3alem/Art/Shisha.png");
                        settings.FindProperty("menuLedge").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Ma3alem/Art/Platform.png");
                        settings.FindProperty("arabicFont").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Font>("Assets/Ma3alem/Fonts/Lemonada-SemiBold.ttf");
                        settings.ApplyModifiedPropertiesWithoutUndo();
                    }
                    foreach (var spawner in root.GetComponentsInChildren<PlatformSpawner>(true))
                    {
                        var spawnerSettings = new SerializedObject(spawner); var variants = spawnerSettings.FindProperty("zoneSprites"); variants.arraySize = 4;
                        for (int i = 0; i < 4; i++) variants.GetArrayElementAtIndex(i).objectReferenceValue = AssetDatabase.LoadAssetAtPath<Sprite>(i == 0 ? "Assets/Ma3alem/Art/Platform.png" : $"Assets/Ma3alem/Art/Platform_Zone{i + 1}.png");
                        spawnerSettings.ApplyModifiedPropertiesWithoutUndo();
                    }
                    if (root.GetComponent<SpriteRenderer>() == null) continue;
                    var building = root.GetComponent<BuildingBackdrop>() ?? root.AddComponent<BuildingBackdrop>();
                    var serialized = new SerializedObject(building);
                    serialized.FindProperty("floorPrefab").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Ma3alem/Prefabs/BuildingFloor.prefab");
                    var walls = serialized.FindProperty("wallMaterials"); walls.arraySize = 4;
                    for (int i = 0; i < 4; i++) walls.GetArrayElementAtIndex(i).objectReferenceValue = AssetDatabase.LoadAssetAtPath<Sprite>($"Assets/Ma3alem/Art/WallMaterial_{i}.png");
                    var pieces = serialized.FindProperty("componentSprites"); pieces.arraySize = 16;
                    for (int i = 0; i < 16; i++) pieces.GetArrayElementAtIndex(i).objectReferenceValue = AssetDatabase.LoadAssetAtPath<Sprite>($"Assets/Ma3alem/Art/BuildingPart_{i:00}.png");
                    serialized.ApplyModifiedPropertiesWithoutUndo();
                }
                EditorSceneManager.SaveScene(scene);
            }
            PlayerSettings.productName = GameManager.OfficialTitle;
            AssetDatabase.SaveAssets();
            VerifySaidiReferences();
        }

        public static void SetRestoredMenuVersion()
        {
            PlayerSettings.bundleVersion = "1.2.34-egyptian-platform-skins";
            PlayerSettings.productName = GameManager.OfficialTitle;
            AssetDatabase.SaveAssets();
        }

        private static void BuildArchitecturePrefabs()
        {
            Directory.CreateDirectory("Assets/Ma3alem/Prefabs/BuildingParts"); AssetDatabase.Refresh();
            for (int i = 0; i < 16; i++)
            {
                var part = new GameObject("Architecture component " + i, typeof(SpriteRenderer));
                part.GetComponent<SpriteRenderer>().sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"Assets/Ma3alem/Art/BuildingPart_{i:00}.png");
                PrefabUtility.SaveAsPrefabAsset(part, $"Assets/Ma3alem/Prefabs/BuildingParts/Part_{i:00}.prefab");
                UnityEngine.Object.DestroyImmediate(part);
            }
            var floor = new GameObject("Assembled apartment floor");
            int[] pieceIndices = { -1, -1, 0, 0, 3, 3, 4, 5, 7, 8, 9, 10, 11, 12, 13, 15 };
            for (int slot = 0; slot < pieceIndices.Length; slot++)
            {
                GameObject part = slot < 2 ? new GameObject(slot == 0 ? "Wall" : "Floor coping", typeof(SpriteRenderer))
                    : (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/Ma3alem/Prefabs/BuildingParts/Part_{pieceIndices[slot]:00}.prefab"));
                part.transform.SetParent(floor.transform, false);
                if (slot == 6) part.transform.localScale = new Vector3(1.3f, 0.45f, 1);
                if (slot == 7) part.transform.localScale = new Vector3(1.3f, 0.7f, 1);
                var sprite = part.GetComponent<SpriteRenderer>();
                sprite.sortingOrder = slot == 0 ? -20 : slot < 4 ? -18 : slot < 6 ? -17 : slot == 7 ? -13 : -15;
                if (slot < 2) sprite.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(slot == 0 ? "Assets/Ma3alem/Art/WallMaterial_0.png" : "Assets/Ma3alem/Art/Platform.png");
            }
            PrefabUtility.SaveAsPrefabAsset(floor, "Assets/Ma3alem/Prefabs/BuildingFloor.prefab");
            UnityEngine.Object.DestroyImmediate(floor);
        }

        public static void VerifySaidiReferences()
        {
            var audio = AssetDatabase.LoadAssetAtPath<GameObject>(AudioPrefab).GetComponent<AudioManager>();
            var serialized = new SerializedObject(audio); var groups = serialized.FindProperty("voices");
            if (groups.arraySize != 9) throw new InvalidOperationException("Expected nine active Saidi cues.");
            var lines = new System.Collections.Generic.List<string>();
            for (int i = 0; i < 9; i++)
            {
                var clip = (AudioClip)groups.GetArrayElementAtIndex(i).FindPropertyRelative("clips").GetArrayElementAtIndex(0).objectReferenceValue;
                string path = AssetDatabase.GetAssetPath(clip);
                if (!path.StartsWith(VoiceFolder + "Saidi/", StringComparison.Ordinal) || clip.name != ((AudioManager.VoiceCue)i).ToString() || clip.channels != 2 || clip.frequency != 48000)
                    throw new InvalidOperationException("Invalid replacement cue: " + i);
                lines.Add($"{clip.name}: {path}; stereo 48000 Hz; {clip.length:F3} seconds");
            }
            File.WriteAllLines("Documentation/SaidiReferenceValidation.txt", lines);
            Debug.Log("Verified nine Saidi references before removing any previous clips.");
        }

        public static void SetReviewResolution(int width, int height)
        {
            var assembly = typeof(EditorWindow).Assembly;
            var viewType = assembly.GetType("UnityEditor.GameView");
            var sizesType = assembly.GetType("UnityEditor.GameViewSizes");
            var singleton = typeof(ScriptableSingleton<>).MakeGenericType(sizesType);
            object sizes = singleton.GetProperty("instance").GetValue(null);
            var groupType = assembly.GetType("UnityEditor.GameViewSizeGroupType");
            object group = sizesType.GetMethod("GetGroup").Invoke(sizes, new[] { Enum.Parse(groupType, "Standalone") });
            var sizeType = assembly.GetType("UnityEditor.GameViewSize");
            var sizeKind = assembly.GetType("UnityEditor.GameViewSizeType");
            object size = Activator.CreateInstance(sizeType, new[] { Enum.Parse(sizeKind, "FixedResolution"), (object)width, height, "Visual review" });
            group.GetType().GetMethod("AddCustomSize").Invoke(group, new[] { size });
            int index = (int)group.GetType().GetMethod("GetTotalCount").Invoke(group, null) - 1;
            var view = EditorWindow.GetWindow(viewType); view.Show();
            viewType.GetProperty("selectedSizeIndex", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic).SetValue(view, index);
        }

        [MenuItem("Ma3alem/Apply revised artwork")]
        public static void ApplyRevisedArtwork()
        {
            const string playerPath = "Assets/Ma3alem/Prefabs/Player.prefab";
            var prefab = PrefabUtility.LoadPrefabContents(playerPath);
            try
            {
                var serialized = new SerializedObject(prefab.GetComponent<PlayerController>());
                SetFrames(serialized, "idleFrames", "Idle", 4);
                SetFrames(serialized, "runFrames", "Run", 4);
                SetFrames(serialized, "jumpFrames", "Jump", 1);
                SetFrames(serialized, "fallFrames", "Fall", 1);
                SetFrames(serialized, "scaredFrames", "Scared", 2);
                SetFrames(serialized, "flipFrames", "Flip", 1);
                SetFrames(serialized, "landingFrames", "Landing", 2);
                SetFrames(serialized, "gameOverFrames", "GameOver", 1);
                serialized.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(prefab, playerPath);
            }
            finally { PrefabUtility.UnloadPrefabContents(prefab); }
            const string platformPath = "Assets/Ma3alem/Prefabs/Platform.prefab";
            var platform = PrefabUtility.LoadPrefabContents(platformPath);
            try
            {
                platform.transform.localScale = Vector3.one;
                var renderer = platform.GetComponent<SpriteRenderer>();
                renderer.drawMode = SpriteDrawMode.Tiled;
                renderer.tileMode = SpriteTileMode.Continuous;
                renderer.size = new Vector2(3.8f, 0.36f);
                platform.GetComponent<BoxCollider2D>().size = renderer.size;
                PrefabUtility.SaveAsPrefabAsset(platform, platformPath);
            }
            finally { PrefabUtility.UnloadPrefabContents(platform); }
            AssetDatabase.SaveAssets();
        }

        private static void SetFrames(SerializedObject serialized, string property, string pose, int count)
        {
            var array = serialized.FindProperty(property); array.arraySize = count;
            for (int i = 0; i < count; i++)
            {
                string path = $"Assets/Ma3alem/Art/Player_{pose}_{i + 1:00}.png";
                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                if (sprite == null) throw new InvalidOperationException("Missing cartoon frame: " + path);
                array.GetArrayElementAtIndex(i).objectReferenceValue = sprite;
            }
        }

        private static AudioClip LoadLocalPreview()
        {
            if (Application.isBatchMode) return null;
            string path = EditorPrefs.GetString("Ma3alem.LocalPreviewMusic", "");
            if (path.EndsWith(".mp3", StringComparison.OrdinalIgnoreCase)) return AssetDatabase.LoadAssetAtPath<AudioClip>(path);
            if (!File.Exists(path)) return null;
            using (var reader = new BinaryReader(File.OpenRead(path)))
            {
                if (new string(reader.ReadChars(4)) != "RIFF") throw new InvalidDataException("Preview music must be PCM WAV.");
                reader.ReadInt32();
                if (new string(reader.ReadChars(4)) != "WAVE") throw new InvalidDataException("Not WAV.");
                int channels = 0, rate = 0, bits = 0, format = 0;
                while (reader.BaseStream.Position + 8 <= reader.BaseStream.Length)
                {
                    string chunk = new string(reader.ReadChars(4)); int size = reader.ReadInt32();
                    long next = reader.BaseStream.Position + size + (size & 1);
                    if (size < 0 || next > reader.BaseStream.Length) throw new InvalidDataException("Invalid WAV chunk.");
                    if (chunk == "fmt ")
                    {
                        format = reader.ReadInt16(); channels = reader.ReadInt16(); rate = reader.ReadInt32();
                        reader.ReadInt32(); reader.ReadInt16(); bits = reader.ReadInt16();
                    }
                    else if (chunk == "data")
                    {
                        if (format != 1 || bits != 16 || channels < 1 || channels > 2 || rate <= 0)
                            throw new InvalidDataException("Local preview supports unprocessed 16-bit mono/stereo PCM WAV.");
                        float[] samples = new float[size / 2];
                        for (int i = 0; i < samples.Length; i++) samples[i] = reader.ReadInt16() / 32768f;
                        var clip = AudioClip.Create("Local-only music preview", samples.Length / channels, channels, rate, false);
                        clip.SetData(samples, 0); return clip;
                    }
                    reader.BaseStream.Position = next;
                }
            }
            throw new InvalidDataException("No WAV samples.");
        }

        [MenuItem("Ma3alem/Validate project")]
        public static void Validate()
        {
            RebuildVoices();
            var setup = EditorSceneManager.GetSceneManagerSetup();
            if (setup.Any(entry => entry.isLoaded && UnityEngine.SceneManagement.SceneManager.GetSceneByPath(entry.path).isDirty))
                throw new InvalidOperationException("Save open scene changes before validation.");
            try
            {
                foreach (string path in Scenes)
                {
                    var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                    var roots = scene.GetRootGameObjects();
                    if (roots.SelectMany(root => root.GetComponentsInChildren<GameManager>(true)).Count() != 1)
                        throw new InvalidOperationException("Expected one GameManager: " + path);
                    foreach (var component in roots.SelectMany(root => root.GetComponentsInChildren<Component>(true)))
                        ValidateReferences(component, path);
                }
                var route = new PlatformSpawner.Route(81473, 12, 29.43f, 7, 6.65f);
                for (int i = 0; i < 10000; i++)
                {
                    var previous = route.Previous; var next = route.Next();
                    if (next.Y <= previous.Y || next.Width < 2 || Math.Abs(next.X) + next.Width / 2 > 6.5f)
                        throw new InvalidOperationException("Invalid generated platform at " + i);
                }
                EditorBuildSettings.scenes = Scenes.Select(path => new EditorBuildSettingsScene(path, true)).ToArray();
                Debug.Log("Static editor validation passed. Play Mode and Windows testing are still required.");
            }
            finally
            {
                if (setup.Any(entry => entry.isLoaded)) EditorSceneManager.RestoreSceneManagerSetup(setup);
                else EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            }
        }

        private static void ValidateReferences(Component component, string scene)
        {
            if (component == null) throw new InvalidOperationException("Missing script: " + scene);
            if (!(component is GameManager || component is PlayerController || component is UIManager || component is PlatformSpawner || component is BuildingBackdrop)) return;
            var serialized = new SerializedObject(component); var property = serialized.GetIterator();
            while (property.NextVisible(true))
                if (property.propertyType == SerializedPropertyType.ObjectReference && property.objectReferenceValue == null)
                    throw new InvalidOperationException("Missing reference: " + component.name + "." + property.propertyPath);
        }

        [MenuItem("Ma3alem/Build Windows x64")]
        public static void BuildWindows()
        {
            BuildAt("Builds/Windows/icy tower Asyut.exe");
        }

        [MenuItem("Ma3alem/Build local preview with supplied music")]
        public static void BuildLocalPreview()
        { BuildLocalPreviewAt("Builds/LocalPreview/icy tower Asyut.exe"); }

        [MenuItem("Ma3alem/Build neighborhood local comparison")]
        public static void BuildNeighborhoodLocalPreview()
        { BuildLocalPreviewAt("Builds/NeighborhoodLocal/icy tower Asyut.exe"); }

        private static void BuildLocalPreviewAt(string output, bool reviewDriver = false)
        {
            ConfigureProvidedPreview();
            var audio = AssetDatabase.LoadAssetAtPath<GameObject>(AudioPrefab).GetComponent<AudioManager>();
            var serialized = new SerializedObject(audio); var property = serialized.FindProperty("music");
            var original = property.objectReferenceValue;
            try
            {
                localPreviewBuild = true;
                property.objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>(LocalMusic);
                serialized.ApplyModifiedPropertiesWithoutUndo(); AssetDatabase.SaveAssets();
                BuildAt(output, reviewDriver, !reviewDriver);
            }
            finally
            {
                property.objectReferenceValue = original; serialized.ApplyModifiedPropertiesWithoutUndo();
                AssetDatabase.SaveAssets(); localPreviewBuild = false;
            }
        }

        [MenuItem("Ma3alem/Build isolated Windows verification")]
        public static void BuildVerificationWindows()
        {
            string company = PlayerSettings.companyName;
            try { BuildAt("Builds/Verification/icy tower Asyut.exe", true); }
            finally { PlayerSettings.companyName = company; AssetDatabase.SaveAssets(); }
        }

        private static void BuildAt(string output, bool verification = false, bool isolatedPreferences = true)
        {
            Validate();
            PlayerSettings.productName = GameManager.OfficialTitle;
            PlayerSettings.companyName = verification && isolatedPreferences ? "Mohamad Al Hussein Verification" : "Mohamad Al Hussein";
            PlayerSettings.defaultScreenWidth = 1280; PlayerSettings.defaultScreenHeight = 720;
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Standalone, ScriptingImplementation.Mono2x);
            Directory.CreateDirectory(Path.GetDirectoryName(output));
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions {
                scenes = Scenes, locationPathName = output, target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None, extraScriptingDefines = verification ? new[] { "ASYUT_VERIFY" } : Array.Empty<string>()
            });
            if (report.summary.result != BuildResult.Succeeded) throw new InvalidOperationException("Windows build failed.");
        }

        public static void ValidateMusicExclusion()
        {
            if (localPreviewBuild) return;
            var resources = AssetDatabase.GetAllAssetPaths().Where(path => path.Contains("/Resources/") && !AssetDatabase.IsValidFolder(path));
            var dependencies = AssetDatabase.GetDependencies(Scenes.Concat(resources).Append(AudioPrefab).ToArray(), true);
            foreach (string path in dependencies)
            {
                if (path.StartsWith("Assets/", StringComparison.Ordinal) &&
                    (path.IndexOf("El_Melouk", StringComparison.OrdinalIgnoreCase) >= 0 || path.IndexOf("LocalPreviewOnly", StringComparison.OrdinalIgnoreCase) >= 0))
                    throw new BuildFailedException("Local-only commercial music must remain outside Assets: " + path);
                if (!path.StartsWith("Assets/", StringComparison.Ordinal) || !path.EndsWith(".wav", StringComparison.OrdinalIgnoreCase)) continue;
                using (var sha = System.Security.Cryptography.SHA256.Create())
                using (var stream = File.OpenRead(path))
                {
                    string hash = BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", "");
                    if (hash == "5F89C944418A3BFD34A28C1D77401B2AAEDF2C0DDDA0BDD3ACA02D0F23E26521")
                        throw new BuildFailedException("The unlicensed local preview recording cannot enter a distributed build, even under another filename.");
                }
            }
        }
    }

    public sealed class VoiceImporter : AssetPostprocessor
    {
        private void OnPreprocessAudio()
        {
            if (!assetPath.StartsWith(ProjectTools.VoiceFolder, StringComparison.OrdinalIgnoreCase) || !assetPath.EndsWith(".wav", StringComparison.OrdinalIgnoreCase)) return;
            var importer = (AudioImporter)assetImporter;
            importer.forceToMono = false; importer.loadInBackground = false; importer.ambisonic = false;
            var serialized = new SerializedObject(importer);
            serialized.FindProperty("m_Normalize").boolValue = false;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            var settings = importer.defaultSampleSettings;
            settings.loadType = AudioClipLoadType.DecompressOnLoad; settings.compressionFormat = AudioCompressionFormat.PCM;
            settings.sampleRateSetting = AudioSampleRateSetting.PreserveSampleRate; settings.preloadAudioData = true;
            importer.defaultSampleSettings = settings;
        }

        private static void OnPostprocessAllAssets(string[] imported, string[] deleted, string[] moved, string[] movedFrom)
        {
            if (imported.Concat(deleted).Concat(moved).Concat(movedFrom)
                .Any(path => path.StartsWith(ProjectTools.VoiceFolder, StringComparison.OrdinalIgnoreCase)))
                EditorApplication.delayCall += ProjectTools.RebuildVoices;
        }
    }

    public sealed class VoiceBuildRefresh : IPreprocessBuildWithReport
    {
        public int callbackOrder => 0;
        public void OnPreprocessBuild(BuildReport report) { ProjectTools.ValidateMusicExclusion(); ProjectTools.RebuildVoices(); }
    }
}
