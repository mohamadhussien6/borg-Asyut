#if ASYUT_VERIFY
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Ma3alem.Verification
{
    // Optional review driver: a normal launch never creates this component or replaces keyboard input.
    public sealed class StandaloneVerification : MonoBehaviour
    {
        [Serializable] private sealed class Result
        {
            public string scope = "Same playable Windows executable, opt-in automated input; owned score/audio preferences backed up and restored. Not human feel/listening acceptance.";
            public string resolution, displayMode, cpu, gpu;
            public int frames, runs = 1, errors, jumps, landings, focusResumes, highestFloor, maxCombo, platformsCreated, zonesSeenMask, wallBounces, maximumSkippedFloors;
            public int leftWallBounces, rightWallBounces;
            public float maximumWallIncoming, maximumWallOutgoing;
            public float[] zoneFirstSeenSeconds = { -1, -1, -1, -1 };
            public bool bestScorePersisted, scoreReset;
            public float seconds, meanFps, p95Ms, maximumMs, highestHeight, maximumSpeed, maximumFlip;
            public bool mainMenu, pauseResume, gameOverRestart, menuReturn, singleMusicSource, musicContinuous, physicsActive = true;
        }
        private readonly Result result = new Result();
        private readonly List<float> milliseconds = new List<float>(16000);
        private GameManager game;
        private Collider2D[] platforms;
        private Collider2D target;
        private AudioSource music;
        private float groundedAt = -1, direction = 1;
        private string output;
        private bool recording, runwayDriving;
        private float lastTakeoff = -10;
        private BuildingBackdrop scenery;
        private readonly Dictionary<Transform, Vector3> panelAnchors = new Dictionary<Transform, Vector3>();
        private int capturedZones;
        private readonly Dictionary<Transform, Sprite> panelSprites = new Dictionary<Transform, Sprite>();

        private static readonly string[] intKeys = { ScoreManager.BestKey, ScoreManager.TotalBestKey, "Ma3alem.Sound.v1" };
        private static readonly string[] floatKeys = { "Ma3alem.Voice.v1", "Ma3alem.Music.v1" };
        private static readonly bool[] existed = new bool[5];
        private static readonly int[] intValues = new int[3];
        private static readonly float[] floatValues = new float[2];
        private static bool preferencesSaved;
        private static void RestorePreferences()
        {
            if (!preferencesSaved) return;
            for (int i = 0; i < intKeys.Length; i++)
                if (existed[i]) PlayerPrefs.SetInt(intKeys[i], intValues[i]); else PlayerPrefs.DeleteKey(intKeys[i]);
            for (int i = 0; i < floatKeys.Length; i++)
                if (existed[i + 3]) PlayerPrefs.SetFloat(floatKeys[i], floatValues[i]); else PlayerPrefs.DeleteKey(floatKeys[i]);
            PlayerPrefs.Save(); preferencesSaved = false;
        }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Boot()
        {
            if (Array.IndexOf(Environment.GetCommandLineArgs(), "--verify-gameplay") < 0) return;
            for (int i = 0; i < intKeys.Length; i++) { existed[i] = PlayerPrefs.HasKey(intKeys[i]); intValues[i] = PlayerPrefs.GetInt(intKeys[i]); }
            for (int i = 0; i < floatKeys.Length; i++) { existed[i + 3] = PlayerPrefs.HasKey(floatKeys[i]); floatValues[i] = PlayerPrefs.GetFloat(floatKeys[i]); }
            preferencesSaved = true; Application.quitting += RestorePreferences;
            ScoreManager.SuppressReviewPersistence = true;
            PlayerPrefs.SetInt("Ma3alem.Sound.v1", 1);
            var root = new GameObject("Optional command-line gameplay review");
            DontDestroyOnLoad(root);
            var verifier = root.AddComponent<StandaloneVerification>();
            UnityEngine.SceneManagement.SceneManager.sceneLoaded += verifier.IsolateDesktopInput;
            verifier.IsolateDesktopInput(default, default);
        }
        private void IsolateDesktopInput(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
        {
            // Native state/performance tests use public game APIs, not physical desktop input.
            // Prevent the active desktop from submitting a menu button while a sample starts.
            if (UnityEngine.EventSystems.EventSystem.current != null)
                foreach (var module in UnityEngine.EventSystems.EventSystem.current.GetComponents<UnityEngine.EventSystems.BaseInputModule>())
                    module.enabled = false;
        }
        private static string Argument(string name, string fallback)
        {
            var args = Environment.GetCommandLineArgs(); int index = Array.IndexOf(args, name);
            return index >= 0 && index + 1 < args.Length ? args[index + 1] : fallback;
        }
        private void Check(bool condition, string message)
        { if (!condition) throw new InvalidOperationException(message); }
        private void Bind()
        {
            game = FindFirstObjectByType<GameManager>(); Check(game != null, "Game manager missing");
            ResumeFocusPause();
            game.Player.ReadKeyboard = false;
            var spawner = FindFirstObjectByType<PlatformSpawner>();
            platforms = spawner.GetComponentsInChildren<Collider2D>(true);
            result.platformsCreated = Math.Max(result.platformsCreated, spawner.CreatedCount);
            game.Player.WallBounced += (incoming, outgoing) =>
            {
                result.wallBounces++;
                if (outgoing > 0) result.leftWallBounces++; else result.rightWallBounces++;
                result.maximumWallIncoming = Mathf.Max(result.maximumWallIncoming, Mathf.Abs(incoming));
                result.maximumWallOutgoing = Mathf.Max(result.maximumWallOutgoing, Mathf.Abs(outgoing));
            };
            game.Player.Jumped += () => { result.jumps++; lastTakeoff = Time.time; };
            game.Player.Landed += () => { result.landings++; result.maximumSkippedFloors = Math.Max(result.maximumSkippedFloors, game.Scores.Combo.LastSkippedFloors); };
            target = null; groundedAt = -1;
            var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
            string[] fields = { "maxSpeed", "acceleration", "braking", "airAcceleration", "airBraking", "jumpSpeed", "momentumJumpBoost", "wallMomentumRetention" };
            var proof = new List<string>();
            foreach (string field in fields) proof.Add(field + "=" + typeof(PlayerController).GetField(field, flags).GetValue(game.Player));
            File.WriteAllText(Path.Combine(output, "ActualRuntimeTuning.txt"), string.Join("\n", proof));
            scenery = FindFirstObjectByType<BuildingBackdrop>(); panelAnchors.Clear(); panelSprites.Clear();
            var camera = game.Follow.GetComponent<Camera>();
            foreach (var renderer in scenery.GetComponentsInChildren<SpriteRenderer>(true))
                if (renderer.name.StartsWith("Fixed "))
                { panelAnchors.Add(renderer.transform, camera.WorldToViewportPoint(renderer.transform.position)); panelSprites.Add(renderer.transform, renderer.sprite); }
            Check(panelAnchors.Count == 3, "Expected only three fixed decorative panels");
        }
        private void ResumeFocusPause()
        {
            // Desktop focus can return before this coroutine resumes; input modules are isolated.
            if (game.State == GameState.Paused)
            { result.focusResumes++; game.TogglePause(); }
        }
        private void Drive()
        {
            var player = game.Player;
            float feet = player.Body.position.y - 0.575f;
            if (player.Grounded)
            {
                if (groundedAt < 0)
                {
                    groundedAt = Time.time; target = null;
                    Collider2D floor = null;
                    foreach (var platform in platforms)
                        if (platform.gameObject.activeInHierarchy && Mathf.Abs(platform.bounds.max.y - feet) < 0.15f && player.Body.position.x >= platform.bounds.min.x - 0.2f && player.Body.position.x <= platform.bounds.max.x + 0.2f) floor = platform;
                    direction = floor == null ? (player.Body.position.x < game.Follow.PlayfieldCenterX ? 1 : -1)
                        : floor.bounds.max.x - player.Body.position.x > player.Body.position.x - floor.bounds.min.x ? 1 : -1;
                }
                player.SetMove(direction);
                float edgeRoom = float.PositiveInfinity;
                foreach (var platform in platforms)
                    if (platform.gameObject.activeInHierarchy && Mathf.Abs(platform.bounds.max.y - feet) < 0.15f && player.Body.position.x >= platform.bounds.min.x - 0.325f && player.Body.position.x <= platform.bounds.max.x + 0.325f)
                        edgeRoom = Mathf.Min(edgeRoom, player.Body.linearVelocity.x >= 0 ? platform.bounds.max.x - player.Body.position.x : player.Body.position.x - platform.bounds.min.x);
                if (Mathf.Abs(player.Body.linearVelocity.x) >= player.MaxSpeed * 0.94f || Time.time - groundedAt > 0.36f || edgeRoom < 0.45f) player.PressJump();
                return;
            }
            groundedAt = -1;
            float vy = player.Body.linearVelocity.y;
            float peak = feet + (vy > 0 ? vy * vy / (2 * player.Gravity) : 0);
            Collider2D candidate = null; float best = float.NegativeInfinity;
            foreach (var platform in platforms)
            {
                if (!platform.gameObject.activeInHierarchy) continue;
                float top = platform.bounds.max.y;
                if (top > peak - 0.05f || top < game.Follow.DeathY + 0.5f || (vy <= 0 && top > feet + 0.04f)) continue;
                float g = player.Gravity * player.FallingGravityMultiplier;
                float disc = vy * vy + 2 * g * (feet - top);
                if (disc < 0) continue;
                float seconds = (vy + Mathf.Sqrt(disc)) / g;
                float dx = Mathf.Abs(platform.bounds.center.x - player.Body.position.x);
                if (dx > player.MaxSpeed * seconds + platform.bounds.extents.x - 0.25f) continue;
                float value = top * 3 - dx * 0.15f;
                if (value > best) { candidate = platform; best = value; }
            }
            if (candidate != null) target = candidate;
            if (target == null) { player.SetMove(0); return; }
            float error = target.bounds.center.x - player.Body.position.x;
            player.SetMove(Mathf.Clamp((error * 4 - player.Body.linearVelocity.x * 0.85f) / player.MaxSpeed, -1, 1));
            if (!runwayDriving && vy < 0 && feet - target.bounds.max.y < 1.0f) player.PressJump();
            if (runwayDriving && Time.time - lastTakeoff < 0.52f) player.SetMove(0);
        }
        private IEnumerator Start()
        {
            // The verifier can be occluded while the desktop is in use. Public builds retain
            // their normal background/focus behavior because this file is symbol-excluded.
            Application.runInBackground = true;
            Debug.Log("Verification boot scene=" + UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
            output = Path.GetFullPath(Argument("--verify-output", "Verification")); Directory.CreateDirectory(output);
            File.WriteAllText(Path.Combine(output, "OwnedPreferencesBefore.txt"), "height=" + intValues[0] + "\ntotal=" + intValues[1] + "\nsound=" + intValues[2] + "\nvoice=" + floatValues[0] + "\nmusic=" + floatValues[1]);
            Application.logMessageReceived += OnLog;
            var routine = Run();
            while (true)
            {
                bool more;
                try { more = routine.MoveNext(); }
                catch (Exception exception)
                {
                    result.errors++; File.WriteAllText(Path.Combine(output, "failure.txt"), exception.ToString());
                    File.WriteAllText(Path.Combine(output, "result.json"), JsonUtility.ToJson(result, true));
                    Application.Quit(2); yield break;
                }
                if (!more) break;
                yield return routine.Current;
            }
            Application.Quit(result.errors == 0 ? 0 : 2);
        }
        private IEnumerator Run()
        {
            yield return null; yield return null;
            int width = int.Parse(Argument("--verify-width", "1280")), height = int.Parse(Argument("--verify-height", "720"));
            bool fullscreen = Argument("--verify-fullscreen", "0") == "1";
            Screen.SetResolution(width, height, fullscreen ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed);
            yield return new WaitForSecondsRealtime(1);
            result.resolution = Screen.width + "x" + Screen.height; result.displayMode = Screen.fullScreenMode.ToString();
            result.cpu = SystemInfo.processorType; result.gpu = SystemInfo.graphicsDeviceName;
            string assembly = typeof(PlayerController).Assembly.Location;
            string hash;
            using (var sha = System.Security.Cryptography.SHA256.Create()) using (var stream = File.OpenRead(assembly)) hash = BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", "");
            File.WriteAllText(Path.Combine(output, "ExactExecutableReceipt.txt"),
                "Executable=" + System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName + "\nData=" + Application.dataPath + "\nVersion=" + Application.version + "\nRuntimeAssembly=" + assembly + "\nRuntimeSHA256=" + hash + "\n");
            game = FindFirstObjectByType<GameManager>(); result.mainMenu = game.State == GameState.Menu;
            Debug.Log("Verification startup scene=" + UnityEngine.SceneManagement.SceneManager.GetActiveScene().name + "; state=" + game.State);
            ScreenCapture.CaptureScreenshot(Path.Combine(output, "Startup.png"));
            yield return new WaitForSecondsRealtime(0.15f);
            Check(result.mainMenu, "Expected Main Menu; actual scene=" + UnityEngine.SceneManagement.SceneManager.GetActiveScene().name + "; state=" + game.State);
            foreach (var source in FindObjectsByType<AudioSource>(FindObjectsSortMode.None))
                if (source.name == "Persistent tower music") music = source;
            Check(music != null && music.isPlaying && music.loop, "Menu music must start and loop");
            File.WriteAllText(Path.Combine(output, "MusicBinding.txt"), "Clip=" + music.clip.name + "\nPlaying=" + music.isPlaying + "\nLoop=" + music.loop);
            string expectedMusic = Argument("--verify-music-name", "");
            if (expectedMusic.Length > 0) Check(music.clip.name == expectedMusic, "Unexpected music clip: " + music.clip.name);
            ScreenCapture.CaptureScreenshot(Path.Combine(output, "MainMenu.png"));
            yield return new WaitForSecondsRealtime(0.15f);
            game.Restart(); yield return null; yield return null; Bind();
            game.TogglePause(); Check(game.State == GameState.Paused && Time.timeScale == 0 && music.isPlaying, "Pause/music failed");
            game.TogglePause(); result.pauseResume = game.IsRunning && Time.timeScale == 1; Check(result.pauseResume, "Resume failed");
            game.EndRun(); Check(game.State == GameState.GameOver && music.isPlaying, "Game Over/music failed");
            game.Restart(); yield return null; yield return null; Bind(); result.gameOverRestart = game.IsRunning;
            game.MainMenu(); yield return null; yield return null;
            game = FindFirstObjectByType<GameManager>(); result.menuReturn = game.State == GameState.Menu; Check(result.menuReturn, "Menu return failed");
            game.Restart(); yield return null; yield return null; Bind();
            int musicCount = 0;
            foreach (var source in FindObjectsByType<AudioSource>(FindObjectsSortMode.None)) if (source.name == "Persistent tower music") musicCount++;
            result.singleMusicSource = musicCount == 1; result.musicContinuous = music != null && music.isPlaying;
            Check(result.singleMusicSource && result.musicContinuous, "Duplicate or stopped music after scene reloads");
            if (Argument("--verify-audit", "0") == "1")
            {
                int previousBest = game.Scores.Best;
                game.Scores.Observe((previousBest + 100) / 10f + 1);
                Check(game.Scores.Score > previousBest, "Persistence audit must write a genuinely higher record");
                ScoreManager.SuppressReviewPersistence = false;
                game.Scores.SaveBest();
                int expectedBest = game.Scores.Best;
                ScoreManager.SuppressReviewPersistence = true;
                game.Restart(); yield return null; yield return null; Bind();
                result.bestScorePersisted = game.Scores.Best == expectedBest;
                File.WriteAllText(Path.Combine(output, "PersistenceProof.txt"), "Original best=" + previousBest + "\nTemporary higher record=" + expectedBest + "\nReloaded best=" + game.Scores.Best + "\nReloaded run score=" + game.Scores.Score + "\nOriginal owned preferences restored at session end.");
                result.scoreReset = game.Scores.Score == 0;
                Check(result.bestScorePersisted && result.scoreReset, "Best score did not survive a fresh scene or run score did not reset");
            }
            runwayDriving = Argument("--verify-runway", "0") == "1";
            recording = Argument("--verify-record", "0") == "1";
            float duration = float.Parse(Argument("--verify-seconds", "120"), System.Globalization.CultureInfo.InvariantCulture);
            if (recording) { Application.targetFrameRate = 30; Time.captureFramerate = 30; Directory.CreateDirectory(Path.Combine(output, "frames")); }
            else Application.targetFrameRate = 60;
            float began = Time.realtimeSinceStartup; float progressAt = began; int captured = 0;
            int requestedFrames = int.Parse(Argument("--verify-record-frames", "1200"));
            while (recording ? captured < requestedFrames : Time.realtimeSinceStartup - began < duration)
            {
                ResumeFocusPause();
                if (game.State == GameState.GameOver)
                {
                    result.runs++; game.Restart(); yield return null; yield return null; Bind();
                }
                Check(game.IsRunning, "Unexpected state during automated session");
                Drive();
                var camera = game.Follow.GetComponent<Camera>();
                foreach (var panel in panelAnchors)
                {
                    Check(Vector3.Distance(camera.WorldToViewportPoint(panel.Key.position), panel.Value) < 0.003f, "Decorative panel drifted on screen");
                    var renderer = panel.Key.GetComponent<SpriteRenderer>();
                    Check(renderer.sprite == panelSprites[panel.Key] && renderer.color == Color.white, "Permanent decoration swapped or faded");
                }
                Check(scenery.PanelCount == 3, "Decorative panel count grew");
                result.zonesSeenMask |= 1 << game.CurrentZone;
                if (result.zoneFirstSeenSeconds[game.CurrentZone] < 0) result.zoneFirstSeenSeconds[game.CurrentZone] = Time.realtimeSinceStartup - began;
                if ((capturedZones & (1 << game.CurrentZone)) == 0)
                {
                    capturedZones |= 1 << game.CurrentZone;
                    ScreenCapture.CaptureScreenshot(Path.Combine(output, $"Zone_{game.CurrentZone + 1:00}.png"));
                }
                if (Time.realtimeSinceStartup - progressAt > 10)
                {
                    progressAt = Time.realtimeSinceStartup; result.seconds = progressAt - began;
                    File.WriteAllText(Path.Combine(output, "progress.json"), JsonUtility.ToJson(result, true));
                }
                var bodyBounds = game.Player.GetComponent<BoxCollider2D>().bounds;
                Check(bodyBounds.min.x >= game.Follow.LeftBoundary - 0.03f && bodyBounds.max.x <= game.Follow.RightBoundary + 0.03f, "Player crossed a dedicated wall");
                foreach (var platform in platforms) if (platform.gameObject.activeInHierarchy)
                    Check(platform.bounds.min.x >= game.Follow.LeftBoundary + 0.14f && platform.bounds.max.x <= game.Follow.RightBoundary - 0.14f, "Platform crossed dedicated playfield limits");
                result.physicsActive &= game.Player.Body.simulated;
                result.maximumSpeed = Mathf.Max(result.maximumSpeed, Mathf.Abs(game.Player.Body.linearVelocity.x));
                result.maximumFlip = Mathf.Max(result.maximumFlip, game.Player.FlipDegrees);
                result.highestFloor = Math.Max(result.highestFloor, game.CurrentFloor);
                result.highestHeight = Mathf.Max(result.highestHeight, game.Scores.Height.MaximumHeight);
                result.maxCombo = Math.Max(result.maxCombo, game.Scores.Combo.Count);
                if (recording)
                {
                    yield return new WaitForEndOfFrame();
                    var texture = ScreenCapture.CaptureScreenshotAsTexture();
                    File.WriteAllBytes(Path.Combine(output, "frames/" + (captured++).ToString("D5") + ".jpg"), texture.EncodeToJPG(88));
                    Destroy(texture);
                }
                yield return null;
                if (!recording && Time.realtimeSinceStartup - began > 3 && game.IsRunning) milliseconds.Add(Time.unscaledDeltaTime * 1000);
            }
            result.seconds = Time.realtimeSinceStartup - began; result.frames = milliseconds.Count;
            if (milliseconds.Count > 0)
            {
                float total = 0; foreach (float ms in milliseconds) total += ms;
                milliseconds.Sort(); result.meanFps = milliseconds.Count * 1000 / total;
                result.p95Ms = milliseconds[Math.Min(milliseconds.Count - 1, (int)(milliseconds.Count * 0.95f))];
                result.maximumMs = milliseconds[milliseconds.Count - 1];
            }
            Check(FindObjectsByType<GameManager>(FindObjectsSortMode.None).Length == 1, "Duplicated game manager");
            Check(result.physicsActive && result.jumps > 0 && result.landings > 0, "Session must exercise actual jumping/landing physics");
            Check(result.platformsCreated == 28, "Platform pool grew");
            Time.captureFramerate = 0;
            RestorePreferences();
            Check(PlayerPrefs.GetInt(ScoreManager.BestKey) == intValues[0] && PlayerPrefs.GetInt(ScoreManager.TotalBestKey) == intValues[1], "Owned scores were not restored");
            File.WriteAllText(Path.Combine(output, "PreferencesRestored.txt"), "Five owned preference values restored; test-only SaveBest suppression also prevents application-quit overwrite.");
            ScreenCapture.CaptureScreenshot(Path.Combine(output, "GameplayEnd.png"));
            File.WriteAllText(Path.Combine(output, "result.json"), JsonUtility.ToJson(result, true));
            yield return new WaitForSecondsRealtime(0.5f);
        }
        private void OnLog(string message, string stack, LogType type)
        { if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert) result.errors++; }
        private void OnDestroy()
        {
            Application.logMessageReceived -= OnLog;
            UnityEngine.SceneManagement.SceneManager.sceneLoaded -= IsolateDesktopInput;
            Time.captureFramerate = 0;
        }
    }
}
#endif
