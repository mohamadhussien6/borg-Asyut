using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
namespace Ma3alem.Tests
{
    public sealed class GameplayTests
    {
        private GameManager game;
        private PlayerController player;
        private int savedBest, savedTotalBest;
        private bool hadTotalBest;
        private bool recordChainFrames;
        private int chainFrame;
        private bool hadBest;
        private bool savedSound;
        private float savedVoiceVolume;
        private float savedMusicVolume;
        private System.Func<AudioClip> savedMusicLoader;
        [UnitySetUp] public IEnumerator Setup()
        {
            savedTotalBest = PlayerPrefs.GetInt(ScoreManager.TotalBestKey, 0); hadTotalBest = PlayerPrefs.HasKey(ScoreManager.TotalBestKey);
            savedBest = PlayerPrefs.GetInt(ScoreManager.BestKey, 0);
            hadBest = PlayerPrefs.HasKey(ScoreManager.BestKey);
            savedSound = PlayerPrefs.GetInt("Ma3alem.Sound.v1", 1) != 0;
            PlayerPrefs.SetInt("Ma3alem.Sound.v1", 1);
            savedVoiceVolume = PlayerPrefs.GetFloat("Ma3alem.Voice.v1", 0.85f);
            PlayerPrefs.SetFloat("Ma3alem.Voice.v1", 0.85f);
            savedMusicVolume = PlayerPrefs.GetFloat("Ma3alem.Music.v1", 0.16f);
            PlayerPrefs.SetFloat("Ma3alem.Music.v1", 0.16f);
            savedMusicLoader = AudioManager.LocalPreviewMusicLoader;
            AudioManager.LocalPreviewMusicLoader = () => UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/LocalPreviewOnly/مزمار صعيدي.mp3");
            Time.timeScale = 1;
            SceneManager.LoadScene("Gameplay"); yield return null; yield return null;
            game = Object.FindFirstObjectByType<GameManager>();
            player = Object.FindFirstObjectByType<PlayerController>(); player.ReadKeyboard = false;
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            Time.timeScale = 1;
            foreach (var source in Object.FindObjectsByType<AudioSource>(FindObjectsSortMode.None))
                if (source.name == "Persistent tower music") Object.Destroy(source.gameObject);
            var previous = SceneManager.GetActiveScene();
            var empty = SceneManager.CreateScene("Test cleanup"); SceneManager.SetActiveScene(empty);
            yield return SceneManager.UnloadSceneAsync(previous);
            if (hadTotalBest) PlayerPrefs.SetInt(ScoreManager.TotalBestKey, savedTotalBest); else PlayerPrefs.DeleteKey(ScoreManager.TotalBestKey);
            if (hadBest) PlayerPrefs.SetInt(ScoreManager.BestKey, savedBest); else PlayerPrefs.DeleteKey(ScoreManager.BestKey);
            PlayerPrefs.SetInt("Ma3alem.Sound.v1", savedSound ? 1 : 0);
            PlayerPrefs.SetFloat("Ma3alem.Voice.v1", savedVoiceVolume);
            PlayerPrefs.SetFloat("Ma3alem.Music.v1", savedMusicVolume);
            AudioManager.LocalPreviewMusicLoader = savedMusicLoader;
            PlayerPrefs.Save();
        }
        private AudioSource MusicSource()
        {
            return (AudioSource)new UnityEditor.SerializedObject(Object.FindFirstObjectByType<AudioManager>()).FindProperty("musicSource").objectReferenceValue;
        }
        private void IsolateFloor()
        {
            Object.FindFirstObjectByType<PlatformSpawner>().gameObject.SetActive(false);
            var floor = new GameObject("Test floor", typeof(BoxCollider2D)); floor.layer = 6;
            floor.transform.position = new Vector3(0, -0.2f, 0);
            floor.GetComponent<BoxCollider2D>().size = new Vector2(15, 0.4f);
        }
        [UnityTest] public IEnumerator MovementAndWalls()
        {
            IsolateFloor(); game.Follow.enabled = false;
            var wallTrace = new System.Collections.Generic.List<string>();
            foreach (int side in new[] { -1, 1 }) foreach (float speed in new[] { 1f, 5f, player.MaxSpeed }) foreach (bool airborne in new[] { false, true })
            {
                player.SetMove(0); float edge = side < 0 ? game.Follow.LeftBoundary : game.Follow.RightBoundary;
                player.Body.position = new Vector2(edge - side * 0.55f, airborne ? 3 : 0.7f); player.Body.linearVelocity = Vector2.zero;
                yield return new WaitForSecondsRealtime(0.2f);
                float incoming = 0, outgoing = 0; int count = 0;
                System.Action<float, float> hit = (before, after) => { incoming = before; outgoing = after; count++; };
                player.WallBounced += hit;
                player.SetMove(side * speed / player.MaxSpeed); player.Body.linearVelocity = new Vector2(side * speed, airborne ? 6 : 0);
                float previousY = player.Body.linearVelocity.y;
                for (int tick = 0; tick < 60 && count == 0; tick++)
                { previousY = player.Body.linearVelocity.y; yield return new WaitForFixedUpdate(); }
                player.WallBounced -= hit;
                Assert.That(count, Is.EqualTo(1), $"side={side}, speed={speed}, airborne={airborne}");
                wallTrace.Add($"side={side}; requestedSpeed={speed}; airborne={airborne}; incoming={incoming}; outgoing={outgoing}; retention={Mathf.Abs(outgoing / incoming)}");
                Assert.That(incoming * outgoing, Is.LessThan(0));
                Assert.That(Mathf.Abs(outgoing / incoming), Is.EqualTo(0.96f).Within(0.001f));
                Assert.That(Mathf.Abs(player.Body.linearVelocity.y - previousY), Is.LessThan(1.5f), "No artificial upward wall impulse.");
                Assert.That(player.GetComponent<BoxCollider2D>().bounds.min.x, Is.GreaterThanOrEqualTo(game.Follow.LeftBoundary - 0.02f));
                Assert.That(player.GetComponent<BoxCollider2D>().bounds.max.x, Is.LessThanOrEqualTo(game.Follow.RightBoundary + 0.02f));
                player.SetMove(-side); yield return new WaitForSecondsRealtime(0.1f);
                Assert.That(player.Body.linearVelocity.x * side, Is.LessThan(-speed * 0.8f), "Can steer away without sticking.");
                Assert.That(player.Body.rotation, Is.EqualTo(0));
            }
            System.IO.File.WriteAllLines("Documentation/FinalAudit_WallMeasurements.txt", wallTrace);
        }
        [UnityTest] public IEnumerator JumpRelease()
        {
            IsolateFloor(); yield return new WaitForSecondsRealtime(0.25f);
            player.PressJump(); float heldPeak = player.transform.position.y;
            for (float until = Time.realtimeSinceStartup + 1; Time.realtimeSinceStartup < until;)
            { heldPeak = Mathf.Max(heldPeak, player.transform.position.y); yield return null; }
            Assert.That(heldPeak, Is.GreaterThan(2.6f));
            player.PressJump(); yield return new WaitForSecondsRealtime(0.04f); player.ReleaseJump();
            float shortPeak = player.transform.position.y;
            for (float until = Time.realtimeSinceStartup + 0.8f; Time.realtimeSinceStartup < until;)
            { shortPeak = Mathf.Max(shortPeak, player.transform.position.y); yield return null; }
            Assert.That(shortPeak, Is.LessThan(heldPeak - 0.6f));
        }
        [UnityTest] public IEnumerator OneWayLanding()
        {
            IsolateFloor();
            var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Ma3alem/Prefabs/Platform.prefab");
            var ledge = Object.Instantiate(prefab); ledge.transform.position = new Vector3(0, 1.22f, 0);
            ledge.transform.localScale = Vector3.one;
            ledge.GetComponent<SpriteRenderer>().size = new Vector2(3, 0.36f);
            ledge.GetComponent<BoxCollider2D>().size = new Vector2(3, 0.36f);
            yield return new WaitForSecondsRealtime(0.25f); player.PressJump();
            yield return new WaitForSecondsRealtime(1.1f);
            Assert.That(player.transform.position.y, Is.InRange(1.85f, 2.12f));
            Assert.That(player.Grounded, Is.True);
        }
        [UnityTest] public IEnumerator MovementAndJumpRemainStableAcrossRenderRates()
        {
            IsolateFloor(); game.Follow.enabled = false;
            foreach (var collider in Object.FindObjectsByType<Collider2D>(FindObjectsSortMode.None))
            {
                if (collider.gameObject.layer == 8) collider.enabled = false;
                if (collider.name == "Test floor") ((BoxCollider2D)collider).size = new Vector2(50, 0.4f);
            }
            player.Body.interpolation = RigidbodyInterpolation2D.None;
            var rows = new System.Collections.Generic.List<string> { "targetFps,accelerationSeconds,standingRise,powerRise" };
            int previousRate = Application.targetFrameRate;
            try
            {
                foreach (int rate in new[] { 30, 60, 120 })
                {
                    Application.targetFrameRate = rate;
                    var rises = new float[2]; float accelerationSeconds = 0;
                    for (int condition = 0; condition < 2; condition++)
                    {
                        player.SetMove(0); player.Body.position = new Vector2(0, 0.7f); player.Body.linearVelocity = Vector2.zero;
                        yield return new WaitForSecondsRealtime(0.25f);
                        Assert.That(player.Grounded, Is.True);
                        if (condition == 1)
                        {
                            float began = Time.fixedTime; player.SetMove(1);
                            for (int tick = 0; tick < 40 && player.Body.linearVelocity.x < player.MaxSpeed - 0.01f; tick++) yield return new WaitForFixedUpdate();
                            accelerationSeconds = Time.fixedTime - began;
                            Assert.That(player.Body.linearVelocity.x, Is.EqualTo(9.5f).Within(0.01f));
                            Assert.That(accelerationSeconds, Is.InRange(0.28f, 0.34f));
                        }
                        float baseline = player.Body.position.y, peak = baseline;
                        player.PressJump();
                        for (int tick = 0; tick < 110; tick++)
                        {
                            yield return new WaitForFixedUpdate(); peak = Mathf.Max(peak, player.Body.position.y);
                            if (tick > 4 && player.Grounded) break;
                        }
                        rises[condition] = peak - baseline;
                        Assert.That(rises[condition], Is.EqualTo(condition == 0 ? 2.3279f : 10.3699f).Within(0.08f));
                    }
                    rows.Add($"{rate},{accelerationSeconds:R},{rises[0]:R},{rises[1]:R}");
                }
            }
            finally { Application.targetFrameRate = previousRate; }
            System.IO.File.WriteAllLines("Documentation/FinalAudit_RenderRateMeasurements.csv", rows);
        }

        [UnityTest] public IEnumerator ActualSerializedRouteProvidesFairWidthsAndGapsAcrossZones()
        {
            var spawner = Object.FindFirstObjectByType<PlatformSpawner>();
            var serialized = new UnityEditor.SerializedObject(spawner);
            var tuning = new PlatformSpawner.Difficulty();
            foreach (var field in typeof(PlatformSpawner.Difficulty).GetFields())
                field.SetValue(tuning, serialized.FindProperty("difficulty").FindPropertyRelative(field.Name).floatValue);
            var route = new PlatformSpawner.Route(serialized.FindProperty("seed").intValue, player.JumpSpeed, player.Gravity,
                player.MaxSpeed, game.Follow.PlayfieldHalfWidth, difficulty: tuning, airAcceleration: player.AirAcceleration, fallingMultiplier: player.FallingGravityMultiplier);
            var minWidth = new[] { 100f, 100f, 100f, 100f }; var maxWidth = new float[4];
            var minGap = new[] { 100f, 100f, 100f, 100f }; var maxGap = new float[4];
            for (int floor = 1; floor <= 2000; floor++)
            {
                var before = route.Previous; var next = route.Next();
                int zone = floor < 21 ? 0 : floor < 46 ? 1 : floor < 76 ? 2 : 3;
                float gap = next.Y - before.Y;
                minWidth[zone] = Mathf.Min(minWidth[zone], next.Width); maxWidth[zone] = Mathf.Max(maxWidth[zone], next.Width);
                minGap[zone] = Mathf.Min(minGap[zone], gap); maxGap[zone] = Mathf.Max(maxGap[zone], gap);
                Assert.That(gap, Is.GreaterThan(1)); Assert.That(gap, Is.LessThan(player.JumpSpeed * player.JumpSpeed / (2 * player.Gravity)));
            }
            var rows = new System.Collections.Generic.List<string> { "zone,minWidth,maxWidth,minGap,maxGap" };
            for (int zone = 0; zone < 4; zone++) rows.Add($"{zone+1},{minWidth[zone]:R},{maxWidth[zone]:R},{minGap[zone]:R},{maxGap[zone]:R}");
            System.IO.File.WriteAllLines("Documentation/FinalAudit_ZoneMeasurements.csv", rows);
            yield return null;
        }

        [UnityTest] public IEnumerator ActualCameraPressureTargetsMatchZoneFloors()
        {
            player.Body.simulated = false;
            var route = new PlatformSpawner.Route(81473, player.JumpSpeed, player.Gravity, player.MaxSpeed,
                game.Follow.PlayfieldHalfWidth, airAcceleration: player.AirAcceleration, fallingMultiplier: player.FallingGravityMultiplier);
            var rows = new System.Collections.Generic.List<string> { "floor,zone,targetRiseSpeed,actualRampedPressure" };
            foreach (int floor in new[] { 1, 20, 21, 45, 46, 75, 76, 2000 })
            {
                while (route.Previous.Floor < floor) route.Next();
                float height = route.Previous.Y + 0.002f;
                game.Scores.Observe(height); player.transform.position = new Vector3(0, height + 0.7f, 0);
                yield return null; yield return null;
                Assert.That(game.CurrentFloor, Is.EqualTo(floor));
                int zone = floor < 21 ? 0 : floor < 46 ? 1 : floor < 76 ? 2 : 3;
                Assert.That(game.CurrentZone, Is.EqualTo(zone));
                Assert.That(game.Follow.RiseSpeed, Is.InRange(0.38f, 1.3501f));
                rows.Add($"{floor},{zone+1},{game.Follow.RiseSpeed:R},{game.Follow.ActualPressureSpeed:R}");
            }
            Assert.That(game.Follow.RiseSpeed, Is.EqualTo(1.35f).Within(0.001f));
            System.IO.File.WriteAllLines("Documentation/FinalAudit_CameraPressure.csv", rows);
        }

        [UnityTest] public IEnumerator HigherRecordSurvivesRestartWithoutDuplicatingRunScore()
        {
            game.Follow.enabled = false; player.Body.simulated = false;
            PlayerPrefs.SetInt(ScoreManager.BestKey, 7);
            PlayerPrefs.SetInt(ScoreManager.TotalBestKey, 10);
            game.Scores.Initialize();
            int recordEvents = 0; game.Scores.NewRecord += () => recordEvents++;
            game.Scores.Observe(25); game.Scores.Observe(25); game.Scores.Observe(24);
            game.Scores.SaveBest();
            Assert.That(game.Scores.Score, Is.EqualTo(250));
            Assert.That(recordEvents, Is.EqualTo(1));
            Assert.That(PlayerPrefs.GetInt(ScoreManager.TotalBestKey), Is.EqualTo(250), "A genuinely higher score must replace the old record.");
            game.Restart(); yield return null; yield return null;
            var fresh = Object.FindFirstObjectByType<GameManager>();
            Assert.That(fresh.Scores.Best, Is.EqualTo(250));
            Assert.That(fresh.Scores.Score, Is.Zero); Assert.That(fresh.Scores.Combo.Bonus, Is.Zero);
            fresh.MainMenu(); yield return null; yield return null;
            fresh = Object.FindFirstObjectByType<GameManager>();
            Assert.That(fresh.Scores.Best, Is.EqualTo(250)); Assert.That(fresh.Scores.Score, Is.Zero);
            Assert.That(Object.FindObjectsByType<GameManager>(FindObjectsSortMode.None).Length, Is.EqualTo(1));
        }

        [UnityTest] public IEnumerator PauseDiscardsPendingJumpButAcceptsFreshJump()
        {
            IsolateFloor(); game.Follow.enabled = false;
            yield return new WaitForSecondsRealtime(0.25f);
            Assert.That(player.Grounded, Is.True);
            player.PressJump(); game.TogglePause();
            yield return new WaitForSecondsRealtime(0.2f);
            game.TogglePause();
            yield return new WaitForFixedUpdate(); yield return new WaitForFixedUpdate();
            Assert.That(player.Body.linearVelocity.y, Is.LessThanOrEqualTo(0.05f), "A pre-pause buffered press must not launch after resume.");
            player.PressJump();
            yield return new WaitForFixedUpdate(); yield return new WaitForFixedUpdate();
            Assert.That(player.Body.linearVelocity.y, Is.GreaterThan(8), "Fresh input after resume still jumps normally.");
        }

        [UnityTest] public IEnumerator DangerMeterMatchesZeroAndFailureBoundary()
        {
            game.Follow.enabled = false; player.Body.simulated = false;
            foreach (float danger in new[] { 0f, 1f })
            {
                player.transform.position = new Vector3(0, game.Follow.DeathY + (1 - danger) * 4.5f, 0);
                yield return new WaitForSecondsRealtime(0.6f);
                Image fill = null;
                foreach (var image in Object.FindFirstObjectByType<UIManager>().GetComponentsInChildren<Image>())
                    if (image.type == Image.Type.Filled) fill = image;
                Assert.That(fill, Is.Not.Null);
                Assert.That(fill.fillAmount, Is.EqualTo(danger).Within(0.001f), "Fill must match the real death-boundary danger, including zero.");
            }
        }

        [UnityTest] public IEnumerator PauseRestartMenu()
        {
            game.Scores.Observe(8); game.TogglePause();
            Assert.That(game.State, Is.EqualTo(GameState.Paused)); Assert.That(Time.timeScale, Is.Zero);
            bool escHint = false;
            foreach (var label in Object.FindObjectsByType<TMPro.TextMeshProUGUI>(FindObjectsSortMode.None))
                if (label.gameObject.activeInHierarchy && label.text.Contains("Esc")) escHint = true;
            Assert.That(escHint, Is.True, "Pause hint must name the real Esc key.");
            game.TogglePause(); Assert.That(Time.timeScale, Is.EqualTo(1));
            game.Restart(); yield return null; yield return null;
            game = Object.FindFirstObjectByType<GameManager>(); Assert.That(game.Scores.Score, Is.Zero);
            Assert.That(Object.FindFirstObjectByType<AudioManager>().VoiceSource.clip.name, Is.EqualTo("Restart"));
            game.MainMenu(); yield return null; yield return null;
            Assert.That(Object.FindFirstObjectByType<GameManager>().State, Is.EqualTo(GameState.Menu));
            Assert.That(Object.FindObjectsByType<UnityEngine.EventSystems.EventSystem>(FindObjectsSortMode.None).Length, Is.EqualTo(1));
        }
        [UnityTest] public IEnumerator MainMenuUsesOriginalArtWithCenteredButtons()
        {
            game.MainMenu(); yield return null; yield return null;
            var canvas = GameObject.Find("Arabic interface");
            Assert.That(canvas.transform.Find("Menu rooftop ledge"), Is.Null);
            Assert.That(canvas.transform.Find("Egyptian menu rooftop"), Is.Null);
            var buttons = canvas.GetComponentsInChildren<Button>();
            Assert.That(buttons.Length, Is.EqualTo(4));
            foreach (var button in buttons) Assert.That(button.GetComponent<Image>().sprite.name, Is.EqualTo("Button"));
            Assert.That(canvas.transform.Find("Assiut rooftop sign").GetComponent<Image>().sprite.name, Is.EqualTo("Button"));
            Assert.That(canvas.transform.Find("Egyptian climber"), Is.Null);
            Assert.That(canvas.transform.Find("Menu buttons").GetComponent<RectTransform>().anchoredPosition.x, Is.Zero);
            foreach (var label in canvas.GetComponentsInChildren<TMPro.TextMeshProUGUI>())
                Assert.That(label.text, Does.Not.Contain("..."), "Removed character caption must not remain in the menu.");
            var events = UnityEngine.EventSystems.EventSystem.current;
            var pointer = new UnityEngine.EventSystems.PointerEventData(events)
            { button = UnityEngine.EventSystems.PointerEventData.InputButton.Left };
            UnityEngine.EventSystems.ExecuteEvents.Execute(buttons[1].gameObject, pointer, UnityEngine.EventSystems.ExecuteEvents.pointerClickHandler);
            yield return null;
            Assert.That(canvas.GetComponentsInChildren<Button>().Length, Is.EqualTo(1));
            var back = canvas.GetComponentsInChildren<Button>()[0];
            UnityEngine.EventSystems.ExecuteEvents.Execute(back.gameObject, pointer, UnityEngine.EventSystems.ExecuteEvents.pointerClickHandler);
            yield return null; yield return null;
            buttons = canvas.GetComponentsInChildren<Button>();
            Assert.That(buttons.Length, Is.EqualTo(4));
            events.SetSelectedGameObject(buttons[0].gameObject);
            UnityEngine.EventSystems.ExecuteEvents.Execute(buttons[0].gameObject,
                new UnityEngine.EventSystems.BaseEventData(events), UnityEngine.EventSystems.ExecuteEvents.submitHandler);
            yield return null; yield return null;
            Assert.That(Object.FindFirstObjectByType<GameManager>().State, Is.EqualTo(GameState.Running));
        }
        [UnityTest] public IEnumerator CameraFallScore()
        {
            // Camera-only teleports must not be delayed or caught by procedural platform collisions.
            player.Body.simulated = false;
            player.transform.position = new Vector3(0, 15, 0);
            yield return null; yield return null;
            var camera = Object.FindFirstObjectByType<CameraFollow>(); float highest = camera.transform.position.y;
            Assert.That(game.Scores.Score, Is.GreaterThan(130));
            player.transform.position = new Vector3(0, 14, 0);
            yield return null; yield return null;
            Assert.That(camera.transform.position.y, Is.GreaterThanOrEqualTo(highest));
            player.transform.position = new Vector3(0, camera.DeathY - 1, 0);
            yield return null; yield return null;
            Assert.That(game.State, Is.EqualTo(GameState.GameOver)); Assert.That(Time.timeScale, Is.Zero);
        }
        [UnityTest] public IEnumerator SavePersistenceAndRealVoices()
        {
            game.Scores.Observe(25); game.Scores.SaveBest();
            Assert.That(PlayerPrefs.GetInt(ScoreManager.BestKey), Is.GreaterThanOrEqualTo(250));
            var audio = Object.FindFirstObjectByType<AudioManager>();
            Assert.That(audio.TryPlayVoice(AudioManager.VoiceCue.Scared), Is.True);
            Assert.That(audio.VoiceSource.clip.name, Is.EqualTo("Scared"));
            Assert.That(audio.TryPlayVoice(AudioManager.VoiceCue.Falling), Is.True);
            Assert.That(audio.VoiceSource.clip.name, Is.EqualTo("Falling"));
            Assert.That(audio.TryPlayVoice(AudioManager.VoiceCue.GameOver), Is.False);
            Assert.That(audio.VoiceSource.pitch, Is.EqualTo(1));
            yield return null;
            Assert.That(game.IsRunning, Is.True);
        }

        [UnityTest] public IEnumerator MomentumJumpFlipsVisualOnly()
        {
            IsolateFloor();
            Object.FindFirstObjectByType<CameraFollow>().enabled = false;
            yield return new WaitForSecondsRealtime(0.25f);
            player.PressJump(); yield return new WaitForSecondsRealtime(0.04f);
            float standing = player.Body.linearVelocity.y;
            yield return new WaitForSecondsRealtime(1.1f);
            player.SetMove(1); yield return new WaitForSecondsRealtime(0.26f);
            var visual = player.GetComponentInChildren<SpriteRenderer>();
            Sprite initial = visual.sprite;
            player.PressJump(); yield return new WaitForSecondsRealtime(0.04f);
            Assert.That(player.Body.linearVelocity.y, Is.GreaterThan(standing + 4));
            Assert.That(Object.FindFirstObjectByType<AudioManager>().VoiceSource.clip.name, Is.EqualTo("HighJump"));
            float startY = player.Body.position.y;
            yield return new WaitForSecondsRealtime(0.6f);
            Assert.That(player.Body.position.y - startY, Is.GreaterThan(4), "Power jump skips several floor gaps.");
            Assert.That(player.FlipDegrees, Is.GreaterThanOrEqualTo(360));
            Assert.That(player.Body.rotation, Is.EqualTo(0).Within(0.01f));
            Assert.That(visual.sprite, Is.Not.EqualTo(initial), "Airborne animation changes pose.");
            yield return new WaitForSecondsRealtime(1.1f);
            Assert.That(player.Grounded, Is.True);
            Assert.That(Quaternion.Angle(visual.transform.localRotation, Quaternion.identity), Is.LessThan(0.1f));
        }

        [UnityTest] public IEnumerator GeneratedPlatformsUseTiledConcrete()
        {
            var spawner = Object.FindFirstObjectByType<PlatformSpawner>();
            var platforms = spawner.GetComponentsInChildren<SpriteRenderer>();
            Assert.That(platforms.Length, Is.GreaterThan(5));
            foreach (var renderer in platforms)
            {
                Assert.That(renderer.drawMode, Is.EqualTo(SpriteDrawMode.Tiled));
                Assert.That(renderer.tileMode, Is.EqualTo(SpriteTileMode.Continuous));
                Assert.That(renderer.transform.lossyScale, Is.EqualTo(Vector3.one));
                Assert.That(renderer.GetComponent<BoxCollider2D>().size, Is.EqualTo(renderer.size));
                Assert.That(renderer.GetComponent<BoxCollider2D>().usedByEffector, Is.True);
                Assert.That(renderer.GetComponent<PlatformEffector2D>().useOneWay, Is.True);
                Assert.That(renderer.size.y, Is.EqualTo(0.36f));
            }
            yield return null;
        }

        [UnityTest] public IEnumerator StageIntroductionUsesMenuStyleAndAppearsOnlyAtZoneStart()
        {
            game.Follow.enabled = false; player.Body.simulated = false;
            var ui = Object.FindFirstObjectByType<UIManager>();
            for (int zone = 0; zone < 4; zone++)
            {
                game.Scores.Observe(game.ZoneStarts[zone] + 0.1f);
                yield return null; yield return null;
                var labels = ui.GetComponentsInChildren<TMPro.TextMeshProUGUI>(true);
                TMPro.TextMeshProUGUI title = null;
                foreach (var label in labels)
                    if (label.fontStyle == TMPro.FontStyles.Bold && label.rectTransform.sizeDelta.x > 400)
                        title = label;
                Assert.That(title, Is.Not.Null, "Stage title exists.");
                var plaque = title.transform.parent.GetComponent<Image>();
                var pause = ui.GetComponentInChildren<Button>().GetComponent<Image>();
                Assert.That(plaque.sprite, Is.SameAs(pause.sprite), "Stage plaque uses the same artwork as menu buttons.");
                title.ForceMeshUpdate();
                Assert.That(title.textBounds.size.x, Is.LessThanOrEqualTo(plaque.rectTransform.rect.width - 24), "Every Arabic stage title fits inside the menu plaque.");
                Assert.That(title.gameObject.activeInHierarchy && plaque.enabled, Is.True);
                string caption = title.text;
                Assert.That(caption, Is.Not.Empty);
                yield return new WaitForSeconds(3.2f);
                Assert.That(title.gameObject.activeInHierarchy, Is.False, "Both title and plaque disappear after the introduction.");
                game.TogglePause(); yield return null;
                game.TogglePause(); yield return null; yield return null;
                foreach (var label in ui.GetComponentsInChildren<TMPro.TextMeshProUGUI>(true))
                    if (label.text == caption)
                        Assert.That(label.gameObject.activeInHierarchy, Is.False, "Resuming the same stage must not replay its title.");
            }
        }

        [UnityTest] public IEnumerator ShishaDangerMeterIsReadableAndTracksDangerWithSmoothTransitions()
        {
            var review = System.Type.GetType("Ma3alem.Editor.ProjectTools, Ma3alem.Editor");
            review.GetMethod("SetReviewResolution").Invoke(null, new object[] { 1280, 720 });
            game.Follow.enabled = false; player.Body.simulated = false;
            var ui = Object.FindFirstObjectByType<UIManager>();
            Image fill = null;
            foreach (var image in ui.GetComponentsInChildren<Image>())
                if (image.type == Image.Type.Filled) fill = image;
            Assert.That(fill, Is.Not.Null);
            Assert.That(fill.rectTransform.rect.width, Is.EqualTo(8), "Owner requested the original slender fill width.");
            var colors = new Color[7];
            float[] danger = { 0.15f, 0.44f, 0.46f, 0.6f, 0.74f, 0.76f, 0.98f };
            string output = System.IO.Path.GetFullPath("../UI_R32_Verification");
            System.IO.Directory.CreateDirectory(output);
            for (int i = 0; i < danger.Length; i++)
            {
                player.transform.position = new Vector3(0, game.Follow.DeathY + (1 - danger[i]) * 4.5f, 0);
                yield return new WaitForSecondsRealtime(0.5f);
                Assert.That(fill.fillAmount, Is.EqualTo(danger[i]).Within(0.025f), "Indicator still tracks actual danger.");
                colors[i] = fill.color;
                if (i == 0 || i == 3 || i == 6)
                {
                    string capture = output + "/Danger_" + i + ".png";
                    ScreenCapture.CaptureScreenshot(capture);
                    yield return new WaitForSecondsRealtime(0.25f);
                    Assert.That(System.IO.File.Exists(capture), Is.True, "Actual Unity capture was written.");
                }
            }
            Assert.That(colors[0].g, Is.GreaterThan(colors[0].r), "Safe state is olive green.");
            Assert.That(colors[0].g - colors[0].b, Is.GreaterThan(0.1f), "Safe green is earthy rather than cyan.");
            Assert.That(colors[0].g, Is.GreaterThan(0.75f), "Safe fill is bright enough to read during movement.");
            Assert.That(colors[3].r, Is.GreaterThan(colors[3].g));
            Assert.That(colors[3].g, Is.GreaterThan(colors[3].b), "Warning is warm amber.");
            Assert.That(colors[6].r, Is.GreaterThan(colors[6].g * 1.5f), "High danger is brick red.");
            Assert.That(colors[6].a, Is.GreaterThanOrEqualTo(0.85f), "High danger remains readable while pulsing.");
            Assert.That(Vector3.Distance((Vector4)colors[1], (Vector4)colors[2]), Is.LessThan(0.1f), "No color jump at the first warning boundary.");
            Assert.That(Vector3.Distance((Vector4)colors[4], (Vector4)colors[5]), Is.LessThan(0.1f), "No color jump at the high-danger boundary.");
        }

        [UnityTest] public IEnumerator FourZonesBuildingCoverageAndRealDanger()
        {
            var camera = game.Follow; camera.enabled = false;
            player.Body.simulated = false;
            var buildings = Object.FindFirstObjectByType<BuildingBackdrop>();
            Assert.That(buildings.PanelCount, Is.EqualTo(3));
            var seen = new System.Collections.Generic.HashSet<int>();
            var platformLooks = new System.Collections.Generic.HashSet<Sprite>();
            for (int height = 0; height <= 400; height += 2)
            {
                game.Scores.Observe(height); seen.Add(game.CurrentZone);
                camera.transform.position = new Vector3(0, height + 4.5f, -10);
                player.transform.position = new Vector3(0, height + 0.7f, 0);
                buildings.Refresh();
                var modules = Object.FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None);
                foreach (var module in modules)
                    if (module.GetComponent<PlatformEffector2D>() != null) platformLooks.Add(module.sprite);
                foreach (float edge in new[] { camera.transform.position.y - camera.GetComponent<Camera>().orthographicSize, camera.TopY })
                    foreach (int side in new[] { -1, 1 })
                    {
                        bool covered = false;
                        foreach (var module in modules)
                            if (module.enabled && module.name == (side < 0 ? "Fixed left facade" : "Fixed right facade") && module.bounds.min.y <= edge && module.bounds.max.y >= edge) covered = true;
                        Assert.That(covered, Is.True, "Facade coverage at height " + height);
                    }
                yield return null;
            }
            Assert.That(seen.Count, Is.EqualTo(4));
            Assert.That(platformLooks.Count, Is.EqualTo(4), "Every zone must have a distinct platform appearance.");
            foreach (var floor in Object.FindObjectsByType<Transform>(FindObjectsSortMode.None))
                if (floor.name.StartsWith("Fixed environment frame")) Assert.That(floor.GetComponentsInChildren<Collider2D>(true), Is.Empty);
            player.transform.position = new Vector3(0, camera.DeathY + 0.1f, 0);
            Assert.That(camera.Danger, Is.GreaterThan(0.95f));
            yield return new WaitForSecondsRealtime(0.5f);
            Assert.That(Object.FindFirstObjectByType<UIManager>().ShownDanger, Is.GreaterThan(0.9f));
            player.transform.position = new Vector3(0, camera.DeathY + 6, 0);
            Assert.That(camera.Danger, Is.Zero);
            Assert.That(game.Scores.Score, Is.EqualTo(4000));
        }

        [UnityTest] public IEnumerator SuppliedMusicLoopsPausesAndHasIndependentVolume()
        {
            var audio = Object.FindFirstObjectByType<AudioManager>();
            var settings = new UnityEditor.SerializedObject(audio);
            var music = (AudioSource)settings.FindProperty("musicSource").objectReferenceValue;
            var clip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/LocalPreviewOnly/مزمار صعيدي.mp3");
            if (clip == null) Assert.Ignore("The authorized local recording is excluded from submission source; this local-only check is unavailable.");
            float previousMusic = audio.MusicVolume, previousVoice = audio.VoiceVolume;
            try
            {
                audio.SetMusicVolume(0.13f); audio.SetVoiceVolume(0.81f);
                Assert.That(audio.MusicVolume, Is.EqualTo(0.13f)); Assert.That(audio.VoiceVolume, Is.EqualTo(0.81f));
                music.clip = clip; music.loop = true; music.Play();
                yield return new WaitForSecondsRealtime(0.3f);
                Assert.That(music.isPlaying, Is.True); Assert.That(music.pitch, Is.EqualTo(1));
                game.TogglePause(); Assert.That(music.isPlaying, Is.True);
                game.TogglePause(); Assert.That(music.isPlaying, Is.True);
                music.time = clip.length - 0.2f;
                yield return new WaitForSecondsRealtime(0.7f);
                Assert.That(music.isPlaying, Is.True);
                Assert.That(music.time, Is.LessThan(2), "Playback must wrap at the end, not stop.");
            }
            finally { audio.SetMusicVolume(previousMusic); audio.SetVoiceVolume(previousVoice); }
        }

        [UnityTest] public IEnumerator MusicSurvivesMenuGameplayPauseGameOverAndRestart()
        {
            Object.Destroy(MusicSource().gameObject);
            yield return null;
            game.MainMenu(); yield return null; yield return null;
            game = Object.FindFirstObjectByType<GameManager>();
            var music = MusicSource();
            Assert.That(game.State, Is.EqualTo(GameState.Menu));
            Assert.That(music.isPlaying, Is.True, "Music must start in the Main Menu.");
            var expectedMusic = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/LocalPreviewOnly/مزمار صعيدي.mp3");
            if (expectedMusic == null)
            {
                var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Ma3alem/Prefabs/Audio.prefab").GetComponent<AudioManager>();
                expectedMusic = (AudioClip)new UnityEditor.SerializedObject(prefab).FindProperty("music").objectReferenceValue;
            }
            Assert.That(music.clip, Is.EqualTo(expectedMusic));
            Assert.That(music.loop, Is.True); Assert.That(music.pitch, Is.EqualTo(1));
            Assert.That(music.volume, Is.EqualTo(0.16f));
            music.time = 5;
            for (int run = 0; run < 3; run++)
            {
                float before = music.time;
                game.Restart(); yield return null; yield return null;
                game = Object.FindFirstObjectByType<GameManager>(); game.Player.Body.simulated = false;
                Assert.That(MusicSource(), Is.SameAs(music));
                yield return new WaitForSecondsRealtime(0.15f);
                Assert.That(music.time, Is.GreaterThan(before));
                game.TogglePause(); before = music.time;
                yield return new WaitForSecondsRealtime(0.15f);
                Assert.That(music.isPlaying, Is.True); Assert.That(music.time, Is.GreaterThan(before));
                game.TogglePause(); game.EndRun(); before = music.time;
                yield return new WaitForSecondsRealtime(0.15f);
                Assert.That(music.isPlaying, Is.True); Assert.That(music.time, Is.GreaterThan(before));
                var audio = Object.FindFirstObjectByType<AudioManager>();
                audio.ToggleSound(); Assert.That(music.mute, Is.True);
                before = music.time; yield return new WaitForSecondsRealtime(0.15f);
                audio.ToggleSound(); Assert.That(music.mute, Is.False);
                Assert.That(music.time, Is.GreaterThan(before), "Muting must not reset the track.");
                before = music.time; game.MainMenu(); yield return null; yield return null;
                game = Object.FindFirstObjectByType<GameManager>();
                Assert.That(MusicSource(), Is.SameAs(music));
                yield return new WaitForSecondsRealtime(0.15f);
                Assert.That(music.time, Is.GreaterThan(before));
                int count = 0;
                foreach (var source in Object.FindObjectsByType<AudioSource>(FindObjectsSortMode.None))
                    if (source.name == "Persistent tower music") count++;
                Assert.That(count, Is.EqualTo(1), "Scene reloads must not duplicate the music source.");
            }
        }

        [UnityTest] public IEnumerator CameraPressureContinuesAtHighAltitude()
        {
            player.Body.simulated = false;
            var camera = game.Follow;
            var tuning = new UnityEditor.SerializedObject(camera);
            tuning.FindProperty("graceSeconds").floatValue = 0; tuning.ApplyModifiedPropertiesWithoutUndo();
            player.transform.position = new Vector3(0, 122, 0);
            yield return new WaitForSecondsRealtime(0.5f);
            Assert.That(game.CurrentZone, Is.EqualTo(3));
            Assert.That(camera.RiseSpeed, Is.GreaterThan(1));
            float start = camera.transform.position.y;
            player.transform.position = new Vector3(0, start - 3, 0);
            yield return new WaitForSecondsRealtime(1);
            Assert.That(camera.transform.position.y - start, Is.InRange(0.28f, 0.7f), "Pressure ramps toward the high-floor target without a speed spike.");
            Assert.That(game.IsRunning, Is.True);
        }

        [UnityTest] public IEnumerator StartAndGameOverVoiceEvents()
        {
            var audio = Object.FindFirstObjectByType<AudioManager>();
            Assert.That(audio.VoiceSource.clip.name, Is.EqualTo("Start"));
            yield return new WaitForSecondsRealtime(0.3f);
            game.EndRun();
            Assert.That(audio.VoiceSource.clip.name, Is.EqualTo("GameOver"));
            Assert.That(audio.VoiceSource.pitch, Is.EqualTo(1)); Assert.That(audio.VoiceSource.loop, Is.False);
        }

        [UnityTest] public IEnumerator NormalJumpVoiceEvent()
        {
            IsolateFloor(); var audio = Object.FindFirstObjectByType<AudioManager>();
            yield return new WaitForSecondsRealtime(audio.VoiceSource.clip.length + 0.3f);
            var previous = Random.state; int seed = 0;
            for (; seed < 1000; seed++) { Random.InitState(seed); if (Random.value < 0.28f) break; }
            try
            {
                Random.InitState(seed); player.PressJump();
                yield return new WaitForSecondsRealtime(0.08f);
                Assert.That(audio.VoiceSource.clip.name, Is.EqualTo("Jump"));
                Assert.That(audio.VoiceSource.isPlaying, Is.True);
            }
            finally { Random.state = previous; }
        }

        [UnityTest] public IEnumerator ControlledFallAndRecoveryVoiceEvents()
        {
            IsolateFloor(); game.Follow.enabled = false;
            player.Body.interpolation = RigidbodyInterpolation2D.None; player.Body.gravityScale = 0;
            player.Body.position = new Vector2(0, 6); player.Body.linearVelocity = new Vector2(0, -5);
            var audio = Object.FindFirstObjectByType<AudioManager>();
            var heard = new System.Collections.Generic.HashSet<string>();
            for (float until = Time.realtimeSinceStartup + 4; Time.realtimeSinceStartup < until;)
            {
                if (audio.VoiceSource.isPlaying) heard.Add(audio.VoiceSource.clip.name);
                yield return null;
            }
            Assert.That(heard, Does.Contain("Scared")); Assert.That(heard, Does.Contain("Falling"));
            Assert.That(heard, Does.Contain("Landing")); Assert.That(player.Grounded, Is.True);
            Assert.That(Object.FindObjectsByType<AudioManager>(FindObjectsSortMode.None).Length, Is.EqualTo(1));
        }

        [UnityTest] public IEnumerator NewRecordVoiceEvent()
        {
            var audio = Object.FindFirstObjectByType<AudioManager>();
            game.Scores.Observe((game.Scores.Best + 10) / 10f);
            yield return new WaitForSecondsRealtime(audio.VoiceSource.clip.length + 0.4f);
            Assert.That(audio.VoiceSource.clip.name, Is.EqualTo("NewRecord"));
        }

        private SpriteRenderer[] FramePanels()
        { return Object.FindFirstObjectByType<BuildingBackdrop>().GetComponentsInChildren<SpriteRenderer>(true); }

        [UnityTest] public IEnumerator StaticEnvironmentReferencesAndOpaquePanelsAreComplete()
        {
            var scenery = Object.FindFirstObjectByType<BuildingBackdrop>();
            var panels = new UnityEditor.SerializedObject(scenery).FindProperty("environmentPanels");
            Assert.That(panels.arraySize, Is.EqualTo(3));
            for (int i = 0; i < 3; i++)
            {
                var sprite = panels.GetArrayElementAtIndex(i).objectReferenceValue as Sprite;
                Assert.That(sprite, Is.Not.Null);
                Assert.That(UnityEditor.AssetDatabase.GetAssetPath(sprite), Does.EndWith("Environment_Assiut_Static.png"));
            }
            Assert.That(scenery.PanelCount, Is.EqualTo(3));
            int visible = 0;
            foreach (var panel in FramePanels())
            {
                if (!panel.enabled) continue;
                visible++; Assert.That(panel.color.a, Is.EqualTo(1));
                Assert.That(panel.drawMode, Is.EqualTo(SpriteDrawMode.Simple));
                Assert.That(panel.transform.lossyScale.x, Is.EqualTo(panel.transform.lossyScale.y));
            }
            Assert.That(visible, Is.EqualTo(3), "Only one opaque left/center/right set may be visible while settled.");
            Assert.That(scenery.GetComponentsInChildren<Collider2D>(true), Is.Empty);
            foreach (var item in Object.FindObjectsByType<Transform>(FindObjectsSortMode.None))
                Assert.That(item.name, Does.Not.Contain("apartment floor"), "Retired facade strips must not be instantiated.");
            yield return null;
        }
        [UnityTest] public IEnumerator AllEnvironmentPanelsStayFixedDuringContinuousAndFastClimbs()
        {
            player.Body.simulated = false; game.Follow.enabled = false;
            var scenery = Object.FindFirstObjectByType<BuildingBackdrop>();
            var view = game.Follow.GetComponent<Camera>(); var panels = FramePanels();
            var colliders = Object.FindObjectsByType<Collider2D>(FindObjectsSortMode.None);
            var local = new Vector3[colliders.Length];
            for (int i = 0; i < colliders.Length; i++) local[i] = colliders[i].transform.localPosition;
            scenery.Refresh();
            var positions = new Vector3[panels.Length]; var sprites = new Sprite[panels.Length];
            for (int i = 0; i < panels.Length; i++)
            { positions[i] = view.WorldToViewportPoint(panels[i].transform.position); sprites[i] = panels[i].sprite; }
            for (int step = 0; step < 1000; step++)
            {
                game.Follow.transform.position = new Vector3(0, step * 10.01f, -10); scenery.Refresh();
                for (int i = 0; i < panels.Length; i++)
                {
                    Assert.That(Vector3.Distance(view.WorldToViewportPoint(panels[i].transform.position), positions[i]), Is.LessThan(0.002f));
                    Assert.That(panels[i].sprite, Is.SameAs(sprites[i]));
                }
            }
            Assert.That(scenery.PanelCount, Is.EqualTo(3));
            for (int i = 0; i < colliders.Length; i++) Assert.That(colliders[i].transform.localPosition, Is.EqualTo(local[i]));
            yield return null;
        }
        [UnityTest] public IEnumerator StaticPanelsCoverDesktopScreensWithoutSeamsOrStretching()
        {
            player.Body.simulated = false; game.Follow.enabled = false;
            var scenery = Object.FindFirstObjectByType<BuildingBackdrop>(); var view = game.Follow.GetComponent<Camera>();
            foreach (float aspect in new[] { 16f / 9, 16f / 10, 21f / 9, 4f / 3 })
            {
                view.aspect = aspect; scenery.Refresh();
                var visible = new System.Collections.Generic.List<SpriteRenderer>();
                foreach (var panel in FramePanels()) if (panel.enabled) visible.Add(panel);
                visible.Sort((left, right) => left.bounds.center.x.CompareTo(right.bounds.center.x));
                Assert.That(visible.Count, Is.EqualTo(3));
                Assert.That(visible[0].bounds.min.x, Is.LessThanOrEqualTo(-view.orthographicSize * aspect + 0.001f));
                Assert.That(visible[2].bounds.max.x, Is.GreaterThanOrEqualTo(view.orthographicSize * aspect - 0.001f));
                for (int i = 0; i < 3; i++)
                {
                    Assert.That(visible[i].bounds.min.y, Is.LessThanOrEqualTo(view.transform.position.y - view.orthographicSize + 0.001f));
                    Assert.That(visible[i].bounds.max.y, Is.GreaterThanOrEqualTo(view.transform.position.y + view.orthographicSize - 0.001f));
                    Assert.That(visible[i].transform.lossyScale.x, Is.EqualTo(visible[i].transform.lossyScale.y));
                    if (i > 0) Assert.That(visible[i - 1].bounds.max.x, Is.EqualTo(visible[i].bounds.min.x).Within(0.001f));
                }
            }
            yield return null;
        }
        [UnityTest] public IEnumerator PermanentEnvironmentNeverSwapsFadesOrChangesAcrossZonesAndRestart()
        {
            player.Body.simulated = false; game.Follow.enabled = false;
            var scenery = Object.FindFirstObjectByType<BuildingBackdrop>(); var panels = FramePanels();
            var sprites = new Sprite[panels.Length]; var positions = new Vector3[panels.Length];
            scenery.Refresh();
            for (int i = 0; i < panels.Length; i++) { sprites[i] = panels[i].sprite; positions[i] = panels[i].transform.localPosition; }
            foreach (int zone in new[] { 0, 1, 2, 3 })
            {
                game.Scores.Observe(game.ZoneStarts[zone] + 1);
                yield return new WaitForSecondsRealtime(0.3f);
                int visible = 0;
                for (int i = 0; i < panels.Length; i++)
                {
                    Assert.That(panels[i].sprite, Is.SameAs(sprites[i])); Assert.That(panels[i].transform.localPosition, Is.EqualTo(positions[i]));
                    if (panels[i].enabled) { visible++; Assert.That(panels[i].color, Is.EqualTo(Color.white)); }
                }
                Assert.That(visible, Is.EqualTo(3)); Assert.That(scenery.PanelCount, Is.EqualTo(3));
            }
            game.TogglePause(); yield return new WaitForSecondsRealtime(0.2f); game.TogglePause();
            for (int i = 0; i < panels.Length; i++) Assert.That(panels[i].sprite, Is.SameAs(sprites[i]));
            game.Restart(); yield return null; yield return null;
            scenery = Object.FindFirstObjectByType<BuildingBackdrop>();
            Assert.That(Object.FindObjectsByType<BuildingBackdrop>(FindObjectsSortMode.None).Length, Is.EqualTo(1));
            Assert.That(scenery.PanelCount, Is.EqualTo(3));
            int count = 0; foreach (var panel in FramePanels()) if (panel.enabled)
            { count++; Assert.That(panel.sprite.name, Does.StartWith("Assiut_")); Assert.That(panel.color, Is.EqualTo(Color.white)); }
            Assert.That(count, Is.EqualTo(3));
        }
        [UnityTest] public IEnumerator CharacterFramesRemainAnimatedWithStaticEnvironment()
        {
            var serialized = new UnityEditor.SerializedObject(player);
            foreach (string group in new[] { "idleFrames", "runFrames", "jumpFrames", "fallFrames", "scaredFrames", "flipFrames", "landingFrames", "gameOverFrames" })
            {
                var frames = serialized.FindProperty(group); Assert.That(frames.arraySize, Is.GreaterThan(0), group);
                for (int i = 0; i < frames.arraySize; i++)
                {
                    var sprite = frames.GetArrayElementAtIndex(i).objectReferenceValue as Sprite;
                    Assert.That(sprite, Is.Not.Null); Assert.That(sprite.rect.size, Is.EqualTo(new Vector2(384, 384)));
                    Assert.That(sprite.pixelsPerUnit, Is.EqualTo(256));
                }
            }
            IsolateFloor(); yield return new WaitForSecondsRealtime(0.3f); player.SetMove(1);
            var visual = player.GetComponentInChildren<SpriteRenderer>(); var poses = new System.Collections.Generic.HashSet<Sprite>();
            for (int i = 0; i < 5; i++) { yield return new WaitForSecondsRealtime(0.1f); poses.Add(visual.sprite); }
            Assert.That(poses.Count, Is.GreaterThan(2));
        }

        [UnityTest] public IEnumerator AirControlAndNoDoubleJump()
        {
            IsolateFloor(); game.Follow.enabled = false;
            yield return new WaitForSecondsRealtime(0.2f);
            int launches = 0; player.Jumped += () => launches++;
            player.SetMove(1); yield return new WaitForSecondsRealtime(0.2f);
            player.PressJump(); yield return new WaitForSecondsRealtime(0.12f);
            player.SetMove(-1); float upward = player.Body.linearVelocity.y;
            player.PressJump(); yield return new WaitForSecondsRealtime(0.04f);
            Assert.That(launches, Is.EqualTo(1));
            Assert.That(player.Body.linearVelocity.y, Is.LessThan(upward));
            for (int step = 0; step < 24; step++) yield return new WaitForFixedUpdate();
            Assert.That(player.Body.linearVelocity.x, Is.LessThan(-0.5f), "Air control must reverse direction with a fixed-step sampling reserve.");
        }

        [System.Serializable] private sealed class RuntimeGameplayAudit
        {
            public string projectRoot, activeScene, unityVersion, playerPrefab, playerScript, platformScript, cameraScript, scoreScript, assemblyPath;
            public string[] prefabOverrides;
            public float acceleration, maxSpeed, braking, reversal, airAcceleration, airBraking, jumpSpeed, jumpBoost, gravity, cameraSmoothTime, cameraInitialPressure, cameraMaximumPressure;
        }
        [UnityTest, Explicit("Fresh active-scene/prefab audit plus measured baseline physics")]
        public IEnumerator CaptureCurrentRuntimeAuditAndMeasurements()
        {
            var controller = new UnityEditor.SerializedObject(player);
            var camera = new UnityEditor.SerializedObject(game.Follow);
            var modifications = UnityEditor.PrefabUtility.GetPropertyModifications(player);
            var audit = new RuntimeGameplayAudit {
                projectRoot = System.IO.Directory.GetParent(Application.dataPath).FullName,
                activeScene = SceneManager.GetActiveScene().path, unityVersion = Application.unityVersion,
                playerPrefab = UnityEditor.PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(player.gameObject),
                playerScript = UnityEditor.AssetDatabase.GetAssetPath(UnityEditor.MonoScript.FromMonoBehaviour(player)),
                platformScript = UnityEditor.AssetDatabase.GetAssetPath(UnityEditor.MonoScript.FromMonoBehaviour(Object.FindFirstObjectByType<PlatformSpawner>())),
                cameraScript = UnityEditor.AssetDatabase.GetAssetPath(UnityEditor.MonoScript.FromMonoBehaviour(game.Follow)),
                scoreScript = UnityEditor.AssetDatabase.GetAssetPath(UnityEditor.MonoScript.FromMonoBehaviour(game.Scores)),
                assemblyPath = player.GetType().Assembly.Location,
                prefabOverrides = modifications == null ? new string[0] : System.Array.ConvertAll(modifications, item => item.propertyPath + "=" + item.value),
                acceleration = controller.FindProperty("acceleration").floatValue, maxSpeed = player.MaxSpeed,
                braking = controller.FindProperty("braking").floatValue, reversal = controller.FindProperty("reversalAcceleration").floatValue,
                airAcceleration = player.AirAcceleration, airBraking = controller.FindProperty("airBraking").floatValue,
                jumpSpeed = player.JumpSpeed, jumpBoost = controller.FindProperty("momentumJumpBoost").floatValue, gravity = player.Gravity,
                cameraSmoothTime = camera.FindProperty("followSmoothTime").floatValue,
                cameraInitialPressure = camera.FindProperty("initialRiseSpeed").floatValue,
                cameraMaximumPressure = camera.FindProperty("maximumRiseSpeed").floatValue
            };
            System.IO.File.WriteAllText("Documentation/RuntimeGameplayAudit.json", JsonUtility.ToJson(audit, true));
            yield return MeasuredMovementAndFloorZones();
        }
        [System.Serializable] private sealed class MovementMeasurements
        {
            public float[] apexSeconds = new float[4], descentSeconds = new float[4];
            public float[] horizontalLaunchSpeed = new float[4], peakRise = new float[4], airborneTravel = new float[4];
            public float accelerationSeconds, brakingSeconds, reversalSeconds, completedFlipDegrees;
            public int floorHeightsCleared;
            public float[] zoneHeightThresholds, cameraRiseSpeed = new float[4];
        }
        [UnityTest] public IEnumerator MeasuredMovementAndFloorZones()
        {
            IsolateFloor(); game.Follow.enabled = false;
            foreach (var collider in Object.FindObjectsByType<Collider2D>(FindObjectsSortMode.None))
            {
                if (collider.gameObject.layer == 8) collider.enabled = false;
                if (collider.name == "Test floor") ((BoxCollider2D)collider).size = new Vector2(50, 0.4f);
            }
            player.Body.interpolation = RigidbodyInterpolation2D.None;
            var data = new MovementMeasurements { zoneHeightThresholds = game.ZoneStarts };
            var visual = player.GetComponentInChildren<SpriteRenderer>();
            for (int condition = 0; condition < 4; condition++)
            {
                player.SetMove(0); player.Body.position = new Vector2(0, 0.7f); player.Body.linearVelocity = Vector2.zero;
                yield return new WaitForSecondsRealtime(0.3f);
                float ratio = condition == 0 ? 0 : condition == 1 ? 0.25f : condition == 2 ? 0.5f : 1;
                player.SetMove(ratio); player.Body.linearVelocity = new Vector2(player.MaxSpeed * ratio, 0);
                float baseline = player.Body.position.y, peak = baseline, distance = 0, lastX = player.Body.position.x;
                float launchedAt = Time.fixedTime, apexAt = launchedAt;
                player.PressJump(); yield return new WaitForFixedUpdate(); yield return new WaitForFixedUpdate();
                data.horizontalLaunchSpeed[condition] = Mathf.Abs(player.Body.linearVelocity.x);
                for (float end = Time.time + 2; Time.time < end;)
                {
                    if (player.Body.position.y > peak) { peak = player.Body.position.y; apexAt = Time.fixedTime; }
                    distance += Mathf.Abs(player.Body.position.x - lastX); lastX = player.Body.position.x;
                    if (player.Grounded && Time.time > end - 1.8f) break;
                    yield return new WaitForFixedUpdate();
                }
                data.apexSeconds[condition] = apexAt - launchedAt; data.descentSeconds[condition] = Time.fixedTime - apexAt;
                data.peakRise[condition] = peak - baseline; data.airborneTravel[condition] = distance;
                if (condition == 3) data.completedFlipDegrees = player.FlipDegrees;
                Assert.That(player.Body.rotation, Is.EqualTo(0).Within(0.01f));
                Assert.That(Quaternion.Angle(visual.transform.localRotation, Quaternion.identity), Is.LessThan(0.1f));
            }
            Assert.That(data.peakRise[2], Is.GreaterThan(data.peakRise[0] * 1.2f));
            Assert.That(data.peakRise[3], Is.GreaterThan(data.peakRise[0] * 2));
            Assert.That(data.completedFlipDegrees, Is.GreaterThanOrEqualTo(720));
            var route = new PlatformSpawner.Route(81473, player.JumpSpeed, player.Gravity, player.MaxSpeed, game.Follow.PlayfieldHalfWidth);
            while (route.Next().Y <= data.peakRise[3]) data.floorHeightsCleared++;
            Assert.That(data.floorHeightsCleared, Is.GreaterThanOrEqualTo(3));
            player.SetMove(0); player.Body.position = new Vector2(0, 0.7f); player.Body.linearVelocity = Vector2.zero;
            yield return new WaitForSecondsRealtime(0.3f);
            float started = Time.fixedTime; player.SetMove(1);
            float deadline = Time.realtimeSinceStartup + 2;
            while (player.Body.linearVelocity.x < player.MaxSpeed - 0.01f && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.That(player.Body.linearVelocity.x, Is.GreaterThanOrEqualTo(player.MaxSpeed - 0.01f));
            data.accelerationSeconds = Time.fixedTime - started;
            started = Time.fixedTime; player.SetMove(0);
            deadline = Time.realtimeSinceStartup + 2;
            while (player.Body.linearVelocity.x > 0.01f && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.That(player.Body.linearVelocity.x, Is.LessThanOrEqualTo(0.01f));
            data.brakingSeconds = Time.fixedTime - started;
            player.Body.linearVelocity = new Vector2(player.MaxSpeed, 0); started = Time.fixedTime; player.SetMove(-1);
            yield return new WaitForFixedUpdate();
            Assert.That(player.Body.linearVelocity.x, Is.GreaterThan(0), "Direction reversal must preserve initial momentum.");
            deadline = Time.realtimeSinceStartup + 2;
            while (player.Body.linearVelocity.x > -player.MaxSpeed + 0.01f && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.That(player.Body.linearVelocity.x, Is.LessThanOrEqualTo(-player.MaxSpeed + 0.01f));
            data.reversalSeconds = Time.fixedTime - started; player.SetMove(0);
            Assert.That(data.accelerationSeconds, Is.InRange(0.22f, 0.34f));
            for (int zone = 0; zone < 4; zone++)
            {
                float height = game.ZoneStarts[zone];
                Assert.That(game.ZoneAt(height), Is.EqualTo(zone));
                if (zone > 0) Assert.That(game.ZoneAt(height - 0.01f), Is.EqualTo(zone - 1));
                game.Scores.Observe(height); player.Body.simulated = false;
                player.transform.position = new Vector3(0, height + 4, 0);
                game.Follow.enabled = true; yield return null; yield return null;
                data.cameraRiseSpeed[zone] = game.Follow.RiseSpeed;
                game.Follow.enabled = false;
            }
            Assert.That(data.cameraRiseSpeed[3], Is.GreaterThan(data.cameraRiseSpeed[0] * 3));
            System.IO.File.WriteAllText("Documentation/MovementMeasurements.json", JsonUtility.ToJson(data, true));
            System.IO.File.WriteAllText("Documentation/ClassicMovementMeasurements.json", JsonUtility.ToJson(data, true));
        }

        [UnityTest] public IEnumerator FloorCombo_UsesRealLandingAndKeepsLegacyHeightSeparate()
        {
            game.Follow.enabled = false;
            player.Body.interpolation = RigidbodyInterpolation2D.None;
            var spawner = Object.FindFirstObjectByType<PlatformSpawner>();
            Collider2D target = null;
            foreach (var platform in spawner.GetComponentsInChildren<PlatformEffector2D>())
                if (spawner.TryGetFloor(platform.GetComponent<Collider2D>(), out int floor) && floor == 3)
                    target = platform.GetComponent<Collider2D>();
            Assert.That(target, Is.Not.Null);
            foreach (var platform in spawner.GetComponentsInChildren<PlatformEffector2D>())
                if (platform.GetComponent<Collider2D>() != target) platform.GetComponent<Collider2D>().enabled = false;
            game.Scores.BeginJump(0);
            player.Body.position = new Vector2(target.transform.position.x, target.bounds.max.y + 1.4f);
            player.Body.linearVelocity = new Vector2(0, -5);
            for (int i = 0; i < 80 && !player.Grounded; i++) yield return new WaitForFixedUpdate();
            Assert.That(player.Grounded, Is.True); Assert.That(game.Scores.Combo.Count, Is.EqualTo(1));
            Assert.That(game.Scores.Combo.Bonus, Is.EqualTo(30));
            game.Scores.Observe(10); game.Scores.SaveBest();
            Assert.That(game.Scores.Score, Is.EqualTo(130));
            Assert.That(PlayerPrefs.GetInt(ScoreManager.BestKey), Is.GreaterThanOrEqualTo(100));
            Assert.That(PlayerPrefs.GetInt(ScoreManager.TotalBestKey), Is.GreaterThanOrEqualTo(130));
            game.EndRun(); Assert.That(game.Scores.Combo.Count, Is.Zero);
            game.Restart(); yield return null; yield return null;
            var fresh = Object.FindFirstObjectByType<GameManager>();
            Assert.That(fresh.Scores.Score, Is.Zero); Assert.That(fresh.Scores.Combo.Bonus, Is.Zero);
        }

        [UnityTest] public IEnumerator EnlargedArtworkPreservesColliderAndFallSpeedIsBounded()
        {
            game.Follow.enabled = false;
            Object.FindFirstObjectByType<PlatformSpawner>().gameObject.SetActive(false);
            var collider = player.GetComponent<BoxCollider2D>();
            Assert.That(collider.size, Is.EqualTo(new Vector2(0.65f, 1.15f)));
            Assert.That(player.transform.localScale, Is.EqualTo(Vector3.one));
            Assert.That(player.GetComponentInChildren<SpriteRenderer>().transform.localScale.x, Is.EqualTo(1.30f).Within(0.001f));
            player.Body.position = new Vector2(0, 200); player.Body.linearVelocity = new Vector2(0, -100);
            yield return new WaitForFixedUpdate(); yield return new WaitForFixedUpdate();
            Assert.That(player.Body.linearVelocity.y, Is.InRange(-25, -23));
            Assert.That(player.Body.rotation, Is.EqualTo(0));
        }

        [UnityTest] public IEnumerator SeededRestStartJumpsLandOnGeneratedTargets()
        {
            IsolateFloor(); game.Follow.enabled = false;
            var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Ma3alem/Prefabs/Platform.prefab");
            var ledge = Object.Instantiate(prefab);
            foreach (int seed in new[] { 0, 4313, 81473 })
            {
            var route = new PlatformSpawner.Route(seed, player.JumpSpeed, player.Gravity, player.MaxSpeed, game.Follow.PlayfieldHalfWidth);
            foreach (int sample in new[] { 1, 20, 21, 45, 46, 75, 76, 100 })
            {
                while (route.Previous.Floor < sample - 1) route.Next();
                var previous = route.Previous; var next = route.Next();
                float targetX = next.X - previous.X, top = next.Y - previous.Y;
                ledge.SetActive(false); player.SetMove(0); player.Body.position = new Vector2(0, 0.7f); player.Body.linearVelocity = Vector2.zero;
                yield return new WaitForSecondsRealtime(0.15f);
                ledge.transform.position = new Vector3(targetX, top - 0.18f, 0);
                ledge.GetComponent<BoxCollider2D>().size = new Vector2(next.Width, 0.36f);
                ledge.GetComponent<SpriteRenderer>().size = new Vector2(next.Width, 0.36f); ledge.SetActive(true);
                player.PressJump(); yield return new WaitForFixedUpdate(); yield return new WaitForFixedUpdate();
                bool landed = false;
                for (int tick = 0; tick < 70; tick++)
                {
                    player.SetMove(Mathf.Clamp((targetX - player.Body.position.x) * 5 / player.MaxSpeed, -1, 1));
                    yield return new WaitForFixedUpdate();
                    if (player.Grounded && player.Body.position.y > top + 0.5f) { landed = true; break; }
                }
                Assert.That(landed, Is.True, "Rest-start generated seed " + seed + " floor " + sample);
            }
            }
        }

        [UnityTest] public IEnumerator DedicatedWallsAndPlatformsShareOneStablePlayfield()
        {
            player.Body.simulated = false; game.Follow.enabled = false;
            var view = game.Follow.GetComponent<Camera>(); var walls = game.Follow.GetComponentsInChildren<BoxCollider2D>();
            Assert.That(walls.Length, Is.EqualTo(2));
            foreach (float aspect in new[] { 16f / 9, 16f / 10, 21f / 9 })
            {
                view.aspect = aspect;
                foreach (float height in new[] { 4.5f, 150f, 10000f })
                {
                    game.Follow.transform.position = new Vector3(0, height, -10);
                    Object.FindFirstObjectByType<BuildingBackdrop>().Refresh();
                    Physics2D.SyncTransforms();
                    foreach (var wall in walls)
                    {
                        bool left = wall.bounds.center.x < 0;
                        Assert.That(left ? wall.bounds.max.x : wall.bounds.min.x, Is.EqualTo(left ? game.Follow.LeftBoundary : game.Follow.RightBoundary).Within(0.002f));
                        Assert.That(wall.bounds.min.y, Is.LessThan(height - view.orthographicSize));
                        Assert.That(wall.bounds.max.y, Is.GreaterThan(height + view.orthographicSize));
                        Assert.That(wall.GetComponent<Rigidbody2D>().bodyType, Is.EqualTo(RigidbodyType2D.Kinematic));
                        Assert.That(wall.GetComponent<Rigidbody2D>().collisionDetectionMode, Is.EqualTo(CollisionDetectionMode2D.Continuous));
                    }
                }
            }
            foreach (var platform in Object.FindFirstObjectByType<PlatformSpawner>().GetComponentsInChildren<PlatformEffector2D>())
            {
                var bounds = platform.GetComponent<BoxCollider2D>().bounds;
                Assert.That(bounds.min.x, Is.GreaterThanOrEqualTo(game.Follow.LeftBoundary + 0.15f - 0.001f));
                Assert.That(bounds.max.x, Is.LessThanOrEqualTo(game.Follow.RightBoundary - 0.15f + 0.001f));
            }
            Assert.That(Object.FindFirstObjectByType<BuildingBackdrop>().GetComponentsInChildren<Collider2D>(true), Is.Empty);
            yield return null;
        }
        private bool recordWallFrames;
        private int wallFrame;
        [UnityTest] public IEnumerator FullSpeedWallJumpsAndFlipsStayOutsideBuildingPlanes()
        {
            IsolateFloor();
            var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Ma3alem/Prefabs/Platform.prefab");
            var landing = Object.Instantiate(prefab); landing.transform.position = new Vector3(game.Follow.PlayfieldCenterX, 7.02f, 0);
            landing.GetComponent<BoxCollider2D>().size = new Vector2(4, 0.36f); landing.GetComponent<SpriteRenderer>().size = new Vector2(4, 0.36f);
            player.SetMove(0); player.Body.position = new Vector2(game.Follow.RightBoundary - 0.9f, 0.7f); player.Body.linearVelocity = Vector2.zero;
            yield return new WaitForSecondsRealtime(0.25f);
            var hits = new System.Collections.Generic.List<float>();
            System.Action<float, float> bounced = (before, after) => hits.Add(before);
            player.WallBounced += bounced;
            player.SetMove(1); player.Body.linearVelocity = new Vector2(player.MaxSpeed, 0); player.PressJump();
            bool landed = false; float peak = player.Body.position.y, flip = 0;
            for (int tick = 0; tick < (recordWallFrames ? 190 : 150); tick++)
            {
                if (recordWallFrames)
                {
                    yield return new WaitForEndOfFrame();
                    var frame = ScreenCapture.CaptureScreenshotAsTexture();
                    System.IO.File.WriteAllBytes(System.IO.Path.GetFullPath(Application.dataPath + "/../../../work/arcade-wall-footage/" + (wallFrame++).ToString("D5") + ".jpg"), frame.EncodeToJPG(90)); Object.Destroy(frame);
                    yield return null;
                }
                else yield return new WaitForFixedUpdate();
                player.SetMove(0);
                peak = Mathf.Max(peak, player.Body.position.y); flip = Mathf.Max(flip, player.FlipDegrees);
                var bounds = player.GetComponent<BoxCollider2D>().bounds;
                Assert.That(bounds.min.x, Is.GreaterThanOrEqualTo(game.Follow.LeftBoundary - 0.02f));
                Assert.That(bounds.max.x, Is.LessThanOrEqualTo(game.Follow.RightBoundary + 0.02f));
                Assert.That(player.Body.position.x - 0.92f, Is.GreaterThan(game.Follow.LeftBoundary - 0.65f));
                Assert.That(player.Body.position.x + 0.92f, Is.LessThan(game.Follow.RightBoundary + 0.65f));
                Assert.That(player.Body.rotation, Is.EqualTo(0));
                if (player.Grounded && player.Body.position.y > 7.5f) { landed = true; if (!recordWallFrames) break; }
            }
            player.WallBounced -= bounced;
            Assert.That(hits.Count, Is.GreaterThanOrEqualTo(2)); Assert.That(hits[0], Is.GreaterThan(0)); Assert.That(hits[1], Is.LessThan(0));
            Assert.That(landed, Is.True, "Must land on the higher ledge after actual right/left wall rebounds.");
            Assert.That(peak, Is.GreaterThan(10)); Assert.That(flip, Is.GreaterThanOrEqualTo(360)); Assert.That(game.IsRunning, Is.True);
        }
        [UnityTest, Explicit("Actual Play Mode wall-to-wall rebound and higher-platform landing footage")]
        public IEnumerator ArcadeWallBounceFootage()
        {
            System.IO.Directory.CreateDirectory(System.IO.Path.GetFullPath(Application.dataPath + "/../../../work/arcade-wall-footage"));
            var tools = System.Type.GetType("Ma3alem.Editor.ProjectTools, Ma3alem.Editor"); tools.GetMethod("SetReviewResolution").Invoke(null, new object[] { 1280, 720 });
            recordWallFrames = true; wallFrame = 0; Time.captureFramerate = 60;
            try { yield return FullSpeedWallJumpsAndFlipsStayOutsideBuildingPlanes(); }
            finally { Time.captureFramerate = 0; recordWallFrames = false; }
        }
        [UnityTest] public IEnumerator NarrowTowerCollisionAndVisualEdgesAgree()
        {
            var walls = game.Follow.GetComponentsInChildren<BoxCollider2D>();
            Assert.That(walls.Length, Is.EqualTo(2));
            float left = float.NegativeInfinity, right = float.PositiveInfinity;
            foreach (var wall in walls)
                if (wall.bounds.center.x < 0) left = wall.bounds.max.x; else right = wall.bounds.min.x;
            Assert.That(right - left, Is.EqualTo(game.Follow.PlayfieldHalfWidth * 2).Within(0.001f));
            Assert.That(left, Is.EqualTo(game.Follow.LeftBoundary).Within(0.001f));
            Assert.That(right, Is.EqualTo(game.Follow.RightBoundary).Within(0.001f));
            Assert.That(Object.FindFirstObjectByType<BuildingBackdrop>().GetComponentsInChildren<Collider2D>(true), Is.Empty);
            yield return null;
        }

        [UnityTest] public IEnumerator BufferedPowerJumpsChainRealFloorCombos()
        {
            var spawner = Object.FindFirstObjectByType<PlatformSpawner>(); spawner.enabled = false; game.Follow.enabled = false;
            foreach (var platform in spawner.GetComponentsInChildren<PlatformEffector2D>())
            {
                var collider = platform.GetComponent<BoxCollider2D>();
                spawner.TryGetFloor(collider, out int floor);
                collider.enabled = floor == 0 || floor == 3 || floor == 6;
                if (!collider.enabled) continue;
                float top = floor == 0 ? 0 : floor == 3 ? 3.8f : 12f;
                platform.transform.position = new Vector3(game.Follow.PlayfieldCenterX, top - 0.18f, 0);
                collider.size = new Vector2(game.Follow.PlayfieldHalfWidth * 2 - 0.3f, 0.36f);
                platform.GetComponent<SpriteRenderer>().size = collider.size;
            }
            player.Body.interpolation = RigidbodyInterpolation2D.None;
            player.Body.position = new Vector2(-1, 0.7f); player.SetMove(0);
            yield return new WaitForSecondsRealtime(0.2f);
            int launches = 0; float launchedAt = Time.fixedTime;
            var trace = new System.Collections.Generic.List<string>();
            game.Scores.ComboChanged += count => trace.Add($"combo={count} bonus={game.Scores.Combo.Bonus} pos={player.Body.position} vel={player.Body.linearVelocity}");
            player.Jumped += () => { launches++; launchedAt = Time.fixedTime; trace.Add($"jump={launches} pos={player.Body.position} vel={player.Body.linearVelocity}"); };
            player.SetMove(1); player.Body.linearVelocity = new Vector2(player.MaxSpeed, 0); player.PressJump();
            for (int tick = 0; tick < 220 && game.IsRunning && game.Scores.Combo.Count < 2; tick++)
            {
                float flightTime = Time.fixedTime - launchedAt;
                player.SetMove(0);
                float targetTop = game.Scores.Combo.Count == 0 ? 3.8f : 12f;
                if (player.Body.linearVelocity.y < 0 && player.Body.position.y < targetTop + 1.5f) player.PressJump();
                if (recordChainFrames)
                {
                    ScreenCapture.CaptureScreenshot(System.IO.Path.GetFullPath(Application.dataPath + "/../../../work/classic-chain-video/chain_" + (chainFrame++).ToString("D4") + ".png"));
                    yield return null;
                }
                else yield return new WaitForSecondsRealtime(0.02f);
            }
            System.IO.File.WriteAllText("Documentation/ClassicChain.txt", string.Join("\n", trace) + "\n" + $"Launches={launches}; combo={game.Scores.Combo.Count}; bonus={game.Scores.Combo.Bonus}; skipped={game.Scores.Combo.LastSkippedFloors}; position={player.Body.position}; velocity={player.Body.linearVelocity}; state={game.State}\n");
            Assert.That(game.IsRunning, Is.True, "Camera must allow two valid powered jumps.");
            Assert.That(launches, Is.GreaterThanOrEqualTo(2));
            Assert.That(game.Scores.Combo.Count, Is.EqualTo(2));
            Assert.That(game.Scores.Combo.Bonus, Is.EqualTo(90));
            Assert.That(game.Scores.Combo.LastSkippedFloors, Is.EqualTo(2));
            Assert.That(player.Body.rotation, Is.EqualTo(0));
        }

        [UnityTest] public IEnumerator MissingVoiceClipsRemainSilent()
        {
            var audio = Object.FindFirstObjectByType<AudioManager>();
            audio.VoiceSource.Stop();
            var serialized = new UnityEditor.SerializedObject(audio);
            serialized.FindProperty("voices").ClearArray(); serialized.ApplyModifiedPropertiesWithoutUndo();
            yield return new WaitForSecondsRealtime(0.3f);
            Assert.That(audio.TryPlayVoice(AudioManager.VoiceCue.Scared), Is.False);
            Assert.That(audio.VoiceSource.isPlaying, Is.False);
            Assert.That(game.IsRunning, Is.True);
        }

        [UnityTest, Explicit("Captures real standing/running jumps, both wall contacts and procedural gameplay")]
        public IEnumerator PolishJumpAndWallFootage()
        {
            var review = System.Type.GetType("Ma3alem.Editor.ProjectTools, Ma3alem.Editor");
            review.GetMethod("SetReviewResolution").Invoke(null, new object[] { 1280, 720 });
            string output = System.IO.Path.GetFullPath(Application.dataPath + "/../../../work/polish-jump-video");
            System.IO.Directory.CreateDirectory(output);
            IsolateFloor(); game.Follow.enabled = false;
            player.Body.interpolation = RigidbodyInterpolation2D.None;
            int captured = 0;
            Time.captureFramerate = 30;
            try
            {
                for (int condition = 0; condition < 3; condition++)
                {
                    player.SetMove(0); player.Body.position = new Vector2(-4, 0.7f); player.Body.linearVelocity = Vector2.zero;
                    for (int i = 0; i < 10; i++) { ResumeUnfocusedCapture(); yield return null; }
                    float ratio = condition / 2f;
                    player.SetMove(ratio); player.Body.linearVelocity = new Vector2(player.MaxSpeed * ratio, 0);
                    TourCaption(condition == 0 ? "اختبار فيزياء فعلي | قفزة من الثبات" : condition == 1 ? "اختبار فيزياء فعلي | قفزة بسرعة متوسطة" : "اختبار فيزياء فعلي | قفزة جري كاملة وشقلبتين");
                    player.PressJump();
                    for (int i = 0; i < 60; i++)
                    { ResumeUnfocusedCapture(); ScreenCapture.CaptureScreenshot(output + "/jump_" + (captured++).ToString("D4") + ".png"); yield return null; }
                }
                player.Body.position = new Vector2(game.Follow.RightBoundary - 0.8f, 0.7f); player.Body.linearVelocity = new Vector2(7, 0); player.SetMove(1);
                bool right = false, left = false;
                TourCaption("اختبار فيزياء فعلي | الحائطين وتغيير الاتجاه بثبات");
                for (int i = 0; i < 120; i++)
                {
                    ResumeUnfocusedCapture();
                    if (player.Body.position.x > game.Follow.RightBoundary - 0.6f && Mathf.Abs(player.Body.linearVelocity.x) < 0.1f) { right = true; player.SetMove(-1); }
                    if (player.Body.position.x < game.Follow.LeftBoundary + 0.6f && Mathf.Abs(player.Body.linearVelocity.x) < 0.1f) { left = true; player.SetMove(1); }
                    ScreenCapture.CaptureScreenshot(output + "/jump_" + (captured++).ToString("D4") + ".png"); yield return null;
                }
                Assert.That(right && left, Is.True, $"Wall footage: right={right}, left={left}, position={player.Body.position}, velocity={player.Body.linearVelocity}, state={game.State}, timeScale={Time.timeScale}");
            }
            finally { Time.captureFramerate = 0; }
            game.Restart(); yield return null; yield return null;
            game = Object.FindFirstObjectByType<GameManager>(); player = game.Player; player.ReadKeyboard = false;
            yield return FinalGameplayVideo();
            game.Restart(); yield return null; yield return null;
            game = Object.FindFirstObjectByType<GameManager>(); player = game.Player; player.ReadKeyboard = false;
            System.IO.Directory.CreateDirectory(System.IO.Path.GetFullPath(Application.dataPath + "/../../../work/classic-chain-video"));
            recordChainFrames = true; chainFrame = 0; Time.captureFramerate = 30;
            TourCaption("اختبار مضبوط | قفزات متصلة وتخزين القفزة وكومبو الأدوار");
            try { yield return BufferedPowerJumpsChainRealFloorCombos(); }
            finally { Time.captureFramerate = 0; recordChainFrames = false; }
            player.Body.simulated = false; game.Follow.enabled = false;
            yield return null; ScreenCapture.CaptureScreenshot("Documentation/Previews/Neighborhood/PhysicsCombo.png");
            yield return new WaitForSecondsRealtime(0.2f);
        }

        private void ResumeUnfocusedCapture()
        {
            // Batch graphics windows can lose desktop focus. Preserve production auto-pause;
            // only this automated recorder resumes an explicitly unfocused application.
            if (!Application.isFocused && game.State == GameState.Paused)
            {
                Debug.Log("Capture resumed after desktop focus loss; this is automated input, not manual play.");
                game.TogglePause();
            }
        }

        [UnityTest, Explicit("Quick real Unity inspection of continuous building joins")]
        public IEnumerator BuildingJoinPreview()
        {
            string output = System.IO.Path.GetFullPath("Documentation/Previews/SeamRepair");
            System.IO.Directory.CreateDirectory(output);
            var review = System.Type.GetType("Ma3alem.Editor.ProjectTools, Ma3alem.Editor");
            review.GetMethod("SetReviewResolution").Invoke(null, new object[] { 1280, 720 });
            player.Body.simulated = false; game.Follow.enabled = false;
            foreach (float height in new[] { 3f, 35f, 150f })
            {
                game.Follow.transform.position = new Vector3(0, height, -10);
                player.transform.position = new Vector3(0, height - 2, 0);
                yield return null; yield return new WaitForSecondsRealtime(0.15f);
                ScreenCapture.CaptureScreenshot(output + "/Height_" + height.ToString("F0") + ".png");
                yield return new WaitForSecondsRealtime(0.15f);
            }
        }
        [UnityTest, Explicit("Controlled in-game screenshots at four gameplay zones with the same permanent frame")]
        public IEnumerator StaticEnvironmentZoneScreenshots()
        {
            string output = System.IO.Path.GetFullPath("Documentation/Previews/PermanentAssiut");
            System.IO.Directory.CreateDirectory(output);
            var tools = System.Type.GetType("Ma3alem.Editor.ProjectTools, Ma3alem.Editor");
            var scenery = Object.FindFirstObjectByType<BuildingBackdrop>();
            player.Body.simulated = false; game.Follow.enabled = false;
            foreach (int width in new[] { 1280, 1920 })
            {
                int height = width == 1280 ? 720 : 1080;
                tools.GetMethod("SetReviewResolution").Invoke(null, new object[] { width, height });
                for (int zone = 0; zone < 4; zone++)
                {
                    // Reload for a second resolution because the maximum-height score never decreases.
                    if (width == 1920 && zone == 0)
                    {
                        game.Restart(); yield return null; yield return null;
                        game = Object.FindFirstObjectByType<GameManager>(); player = game.Player;
                        player.ReadKeyboard = false; player.Body.simulated = false; game.Follow.enabled = false;
                        scenery = Object.FindFirstObjectByType<BuildingBackdrop>();
                    }
                    float y = game.ZoneStarts[zone] + 3;
                    game.Scores.Observe(y);
                    game.Follow.transform.position = new Vector3(0, y + 2, -10);
                    player.transform.position = new Vector3(0, y, 0);
                    yield return new WaitForSecondsRealtime(1.1f);
                    Assert.That(scenery.PanelCount, Is.EqualTo(3));
                    ScreenCapture.CaptureScreenshot(output + $"/Zone_{zone + 1:00}_{width}x{height}.png");
                    yield return new WaitForSecondsRealtime(0.2f);
                }
            }
        }
        [UnityTest, Explicit("Actual boundary contact screenshots; cyan collider-face guides are test-only")]
        public IEnumerator PlayfieldBoundaryScreenshots()
        {
            string output = System.IO.Path.GetFullPath("Documentation/Previews/PlayfieldBoundaries"); System.IO.Directory.CreateDirectory(output);
            game.Follow.enabled = false;
            var tools = System.Type.GetType("Ma3alem.Editor.ProjectTools, Ma3alem.Editor");
            foreach (float x in new[] { game.Follow.LeftBoundary, game.Follow.RightBoundary })
            {
                var line = new GameObject("QA collider face guide", typeof(LineRenderer)).GetComponent<LineRenderer>();
                line.material = new Material(Shader.Find("Sprites/Default")); line.startColor = line.endColor = new Color(0.1f, 0.9f, 0.9f, 0.8f);
                line.startWidth = line.endWidth = 0.025f; line.positionCount = 2; line.sortingOrder = 20;
                line.SetPosition(0, new Vector3(x, game.Follow.transform.position.y - 5.65f, 0));
                line.SetPosition(1, new Vector3(x, game.Follow.transform.position.y + 5.65f, 0));
            }
            foreach (int width in new[] { 1280, 1920 })
            {
                int height = width == 1280 ? 720 : 1080;
                tools.GetMethod("SetReviewResolution").Invoke(null, new object[] { width, height });
                foreach (int side in new[] { -1, 1 })
                {
                    float edge = side < 0 ? game.Follow.LeftBoundary : game.Follow.RightBoundary;
                    player.SetMove(0); player.Body.position = new Vector2(edge - side * 0.9f, 0.7f); player.Body.linearVelocity = Vector2.zero;
                    yield return new WaitForSecondsRealtime(0.2f);
                    bool bounced = false; System.Action<float, float> contact = (before, after) => bounced = true;
                    player.WallBounced += contact; player.SetMove(side);
                    for (int i = 0; i < 90 && !bounced; i++) yield return new WaitForFixedUpdate();
                    player.WallBounced -= contact; Assert.That(bounced, Is.True);
                    Assert.That(player.Body.linearVelocity.x * side, Is.LessThan(0));
                    ScreenCapture.CaptureScreenshot(output + $"/Contact_{(side < 0 ? "Left" : "Right")}_{width}x{height}.png");
                    yield return new WaitForSecondsRealtime(0.2f);
                }
            }
        }
        [System.Serializable] private sealed class FrameSample
        {
            public int width, height, zone, frames;
            public int targetFps;
            public bool physicsActive, musicPlaying, survived;
            public float averageFps, medianMs, p95Ms, maximumMs;
        }
        private sealed class ReviewInput
        {
            public Collider2D[] Platforms;
            public Collider2D Target;
            public float GroundedAt = -1, Direction = 1;
        }
        private void DriveReview(ReviewInput input)
        {
            if (!game.IsRunning) return;
            if (player.Grounded)
            {
                if (input.GroundedAt < 0)
                {
                    input.GroundedAt = Time.time; float best = float.PositiveInfinity;
                    foreach (var platform in input.Platforms)
                    {
                        if (!platform.gameObject.activeInHierarchy) continue;
                        float dy = platform.bounds.max.y - (player.Body.position.y - 0.59f);
                        float dx = Mathf.Abs(platform.transform.position.x - player.Body.position.x);
                        if (dy < 0.8f || dy > 4.1f || dx > 4) continue;
                        float cost = Mathf.Abs(dy - 2.8f) + dx * 0.12f;
                        if (cost < best) { best = cost; input.Target = platform; }
                    }
                    if (input.Target != null) input.Direction = input.Target.transform.position.x >= player.Body.position.x ? 1 : -1;
                }
                player.SetMove(input.Direction);
                if (Time.time - input.GroundedAt >= 0.20f) player.PressJump();
            }
            else
            {
                input.GroundedAt = -1;
                player.SetMove(input.Target == null ? input.Direction : Mathf.Clamp((input.Target.transform.position.x - player.Body.position.x) * 4 / player.MaxSpeed, -1, 1));
            }
        }
        [UnityTest, Explicit("Fresh neighborhood captures and active Rigidbody/audio/HUD frame samples at 30/60 FPS targets")]
        public IEnumerator NeighborhoodPresentationActivePhysics()
        {
            string output = System.IO.Path.GetFullPath("Documentation/Previews/Neighborhood"); System.IO.Directory.CreateDirectory(output);
            var review = System.Type.GetType("Ma3alem.Editor.ProjectTools, Ma3alem.Editor");
            var measured = new PerformanceSamples { runtime = "Unity Editor; real player physics, input driver, generation, scenery, music and HUD active. Short samples, not human/standalone certification.", cpu = SystemInfo.processorType, gpu = SystemInfo.graphicsDeviceName, ramMB = SystemInfo.systemMemorySize };
            float[] matchedCameraHeights = { 9.88f, 36.603595f, 72.43409f, 118.31975f };
            foreach (var resolution in new[] { new Vector2Int(1280, 720), new Vector2Int(1920, 1080) })
            {
                review.GetMethod("SetReviewResolution").Invoke(null, new object[] { resolution.x, resolution.y });
                foreach (int cap in new[] { 30, 60 })
                    for (int zone = 0; zone < 4; zone++)
                    {
                        yield return TourRestart(); Application.targetFrameRate = cap;
                        float height = game.ZoneStarts[zone] + 3;
                        player.Body.simulated = false; game.Follow.enabled = false;
                        player.Body.position = new Vector2(0, height + 1); game.Follow.transform.position = new Vector3(0, height + 4.5f, -10);
                        game.Scores.Observe(height); yield return null; yield return null;
                        var platforms = Object.FindFirstObjectByType<PlatformSpawner>().GetComponentsInChildren<Collider2D>(true);
                        Collider2D starting = null; float nearest = float.PositiveInfinity;
                        foreach (var platform in platforms)
                            if (platform.gameObject.activeInHierarchy && Mathf.Abs(platform.bounds.max.y - height) < nearest)
                            { nearest = Mathf.Abs(platform.bounds.max.y - height); starting = platform; }
                        Assert.That(starting, Is.Not.Null);
                        player.transform.position = new Vector3(starting.transform.position.x, starting.bounds.max.y + 0.65f, 0);
                        player.Body.position = player.transform.position;
                        player.Body.linearVelocity = Vector2.zero; player.Body.simulated = true;
                        yield return new WaitForFixedUpdate(); yield return null; game.Follow.enabled = true;
                        var input = new ReviewInput { Platforms = platforms };
                        for (int frame = 0; frame < 15; frame++) { DriveReview(input); yield return null; }
                        var milliseconds = new System.Collections.Generic.List<float>(120);
                        bool activePhysics = true;
                        for (int frame = 0; frame < 120 && game.IsRunning; frame++)
                        {
                            DriveReview(input); activePhysics &= player.Body.simulated; yield return null;
                            if (game.IsRunning) milliseconds.Add(Time.unscaledDeltaTime * 1000);
                        }
                        Assert.That(milliseconds.Count, Is.GreaterThanOrEqualTo(30));
                        float total = 0; foreach (float ms in milliseconds) total += ms; milliseconds.Sort();
                        measured.samples.Add(new FrameSample { width = resolution.x, height = resolution.y, zone = zone + 1, frames = milliseconds.Count, targetFps = cap,
                            averageFps = milliseconds.Count * 1000 / total, medianMs = milliseconds[milliseconds.Count / 2], p95Ms = milliseconds[Mathf.Min(milliseconds.Count - 1, Mathf.CeilToInt(milliseconds.Count * 0.95f))], maximumMs = milliseconds[milliseconds.Count - 1],
                            physicsActive = activePhysics, musicPlaying = MusicSource().isPlaying, survived = game.IsRunning });
                    }
                Application.targetFrameRate = 60;
                yield return TourRestart();
                player.Body.simulated = false; game.Follow.enabled = false;
                for (int zone = 0; zone < 4; zone++)
                {
                    float cameraY = matchedCameraHeights[zone]; game.Follow.transform.position = new Vector3(0, cameraY, -10);
                    player.transform.position = new Vector3(0, cameraY - 2, 0); game.Scores.Observe(game.ZoneStarts[zone] + 3);
                    yield return new WaitForSecondsRealtime(0.4f);
                    ScreenCapture.CaptureScreenshot(output + "/Zone_" + zone + "_" + resolution.x + ".png"); yield return new WaitForSecondsRealtime(0.2f);
                }
                game.Scores.BeginJump(0); game.Scores.LandOnFloor(3); game.Scores.BeginJump(3); game.Scores.LandOnFloor(7);
                yield return null; ScreenCapture.CaptureScreenshot(output + "/Combo_" + resolution.x + ".png"); yield return new WaitForSecondsRealtime(0.2f);
                player.transform.position = new Vector3(0, game.Follow.DeathY + 0.4f, 0); yield return new WaitForSecondsRealtime(0.6f);
                ScreenCapture.CaptureScreenshot(output + "/Danger_" + resolution.x + ".png"); yield return new WaitForSecondsRealtime(0.2f);
                game.EndRun(); yield return null; ScreenCapture.CaptureScreenshot(output + "/GameOver_" + resolution.x + ".png"); yield return new WaitForSecondsRealtime(0.2f);
                game.MainMenu(); yield return null; yield return null; yield return new WaitForSecondsRealtime(0.4f);
                ScreenCapture.CaptureScreenshot(output + "/MainMenu_" + resolution.x + ".png"); yield return new WaitForSecondsRealtime(0.2f);
            }
            Assert.That(measured.samples.Count, Is.EqualTo(16));
            System.IO.File.WriteAllText("Documentation/NeighborhoodActivePerformance.json", JsonUtility.ToJson(measured, true));
        }
        [System.Serializable] private sealed class PerformanceSamples
        {
            public string runtime = "Unity Editor, controlled scenery scrolling; not standalone performance";
            public string cpu, gpu;
            public int ramMB;
            public System.Collections.Generic.List<FrameSample> samples = new System.Collections.Generic.List<FrameSample>();
        }
        [UnityTest, Explicit("Fresh graphics, combo/danger evidence and short Editor frame-time samples")]
        public IEnumerator PolishPresentationAndFrameTimes()
        {
            string output = System.IO.Path.GetFullPath("Documentation/Previews/Classic");
            System.IO.Directory.CreateDirectory(output);
            var review = System.Type.GetType("Ma3alem.Editor.ProjectTools, Ma3alem.Editor");
            var measurements = new PerformanceSamples { cpu = SystemInfo.processorType, gpu = SystemInfo.graphicsDeviceName, ramMB = SystemInfo.systemMemorySize };
            foreach (var resolution in new[] { new Vector2Int(1280, 720), new Vector2Int(1920, 1080) })
            {
                review.GetMethod("SetReviewResolution").Invoke(null, new object[] { resolution.x, resolution.y });
                game.MainMenu(); yield return null; yield return null; game = Object.FindFirstObjectByType<GameManager>();
                yield return new WaitForSecondsRealtime(0.4f);
                ScreenCapture.CaptureScreenshot(output + "/MainMenu_" + resolution.x + ".png");
                yield return new WaitForSecondsRealtime(0.2f);
                game.Restart(); yield return null; yield return null;
                game = Object.FindFirstObjectByType<GameManager>(); player = game.Player; player.ReadKeyboard = false;
                player.Body.simulated = false; game.Follow.enabled = false;
                for (int zone = 0; zone < 4; zone++)
                {
                    float start = game.ZoneStarts[zone] + 3;
                    for (int frame = 0; frame < 30; frame++)
                    {
                        game.Follow.transform.position = new Vector3(0, start + 4.5f, -10);
                        player.transform.position = new Vector3(0, start + 1.2f, 0);
                        game.Scores.Observe(start); yield return null;
                    }
                    var milliseconds = new float[120];
                    for (int frame = 0; frame < milliseconds.Length; frame++)
                    {
                        float cameraY = start + 4.5f + frame * 0.02f;
                        game.Follow.transform.position = new Vector3(0, cameraY, -10);
                        player.transform.position = new Vector3(Mathf.Sin(frame * 0.04f) * 3, cameraY - (frame > 60 ? 5.8f : 3.3f), 0);
                        yield return null; milliseconds[frame] = Time.unscaledDeltaTime * 1000;
                    }
                    float total = 0; foreach (float ms in milliseconds) total += ms;
                    System.Array.Sort(milliseconds);
                    measurements.samples.Add(new FrameSample { width = resolution.x, height = resolution.y, zone = zone + 1, frames = milliseconds.Length,
                        averageFps = milliseconds.Length * 1000 / total, medianMs = milliseconds[60], p95Ms = milliseconds[114], maximumMs = milliseconds[119] });
                    player.transform.position = new Vector3(0, game.Follow.transform.position.y - 2, 0);
                    yield return null;
                    ScreenCapture.CaptureScreenshot(output + "/Zone_" + zone + "_" + resolution.x + ".png");
                    yield return new WaitForSecondsRealtime(0.15f);
                }
                game.Scores.BeginJump(0); game.Scores.LandOnFloor(3);
                game.Scores.BeginJump(3); game.Scores.LandOnFloor(7);
                yield return new WaitForSecondsRealtime(0.15f);
                ScreenCapture.CaptureScreenshot(output + "/Combo_" + resolution.x + ".png");
                yield return new WaitForSecondsRealtime(0.15f);
                player.transform.position = new Vector3(0, game.Follow.DeathY + 0.4f, 0);
                yield return new WaitForSecondsRealtime(0.6f);
                Assert.That(Object.FindFirstObjectByType<UIManager>().ShownDanger, Is.GreaterThan(0.85f));
                ScreenCapture.CaptureScreenshot(output + "/Danger_" + resolution.x + ".png");
                yield return new WaitForSecondsRealtime(0.15f);
                game.EndRun(); yield return new WaitForSecondsRealtime(0.2f);
                ScreenCapture.CaptureScreenshot(output + "/GameOver_" + resolution.x + ".png");
                yield return new WaitForSecondsRealtime(0.15f);
            }
            System.IO.File.WriteAllText("Documentation/ClassicPerformance.json", JsonUtility.ToJson(measurements, true));
        }

        private TMPro.TextMeshProUGUI CaptureLabel(string caption)
        {
            var canvas = new GameObject("QA video overlay", typeof(RectTransform), typeof(Canvas));
            canvas.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.GetComponent<Canvas>().sortingOrder = 100;
            var label = new GameObject("QA evidence caption", typeof(RectTransform), typeof(TMPro.TextMeshProUGUI)).GetComponent<TMPro.TextMeshProUGUI>();
            label.transform.SetParent(canvas.transform, false);
            label.font = Object.FindFirstObjectByType<UIManager>().GetComponentInChildren<TMPro.TextMeshProUGUI>().font;
            label.fontSize = 18; label.alignment = TMPro.TextAlignmentOptions.Center;
            label.color = new Color(0.04f, 0.12f, 0.18f); label.text = caption;
            label.rectTransform.anchorMin = label.rectTransform.anchorMax = new Vector2(0.5f, 0);
            label.rectTransform.anchoredPosition = new Vector2(0, 28); label.rectTransform.sizeDelta = new Vector2(1200, 38);
            return label;
        }
        [System.Serializable] private sealed class TourPart
        {
            public string id, caption, voiceAsset;
            public int firstFrame, frameCount;
        }
        [System.Serializable] private sealed class TourTimeline
        {
            public int fps = 30;
            public System.Collections.Generic.List<TourPart> parts = new System.Collections.Generic.List<TourPart>();
        }
        private int tourFrame;
        private string tourRoot;
        private TourTimeline tourTimeline;
        private void TourCaption(string caption)
        {
            var old = GameObject.Find("QA video overlay"); if (old != null) { old.SetActive(false); Object.Destroy(old); }
            var label = CaptureLabel(""); label.fontSize = 22; label.fontStyle = TMPro.FontStyles.Bold;
            label.color = new Color(1, 0.93f, 0.75f); label.rectTransform.sizeDelta = new Vector2(1200, 52); label.rectTransform.anchoredPosition = new Vector2(0, 30);
            var backing = new GameObject("Caption backing", typeof(RectTransform), typeof(UnityEngine.UI.Image)).GetComponent<UnityEngine.UI.Image>();
            backing.transform.SetParent(label.transform.parent, false); backing.transform.SetAsFirstSibling(); backing.color = new Color(0.035f, 0.10f, 0.14f, 0.96f);
            backing.rectTransform.anchorMin = new Vector2(0, 0); backing.rectTransform.anchorMax = new Vector2(1, 0); backing.rectTransform.pivot = new Vector2(0.5f, 0);
            backing.rectTransform.anchoredPosition = Vector2.zero; backing.rectTransform.sizeDelta = new Vector2(0, 60);
            typeof(UIManager).GetMethod("SetText", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).Invoke(Object.FindFirstObjectByType<UIManager>(), new object[] { label, caption });
        }
        private IEnumerator TourHold(string id, string caption, int frames, string voiceAsset = null)
        {
            TourCaption(caption); tourTimeline.parts.Add(new TourPart { id = id, caption = caption, firstFrame = tourFrame, frameCount = frames, voiceAsset = voiceAsset });
            for (int i = 0; i < frames; i++) { ScreenCapture.CaptureScreenshot(tourRoot + "/frames/" + (tourFrame++).ToString("D5") + ".png"); yield return null; }
        }
        private void OpenTourPanel(string method)
        {
            typeof(UIManager).GetMethod(method, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).Invoke(Object.FindFirstObjectByType<UIManager>(), null);
        }
        private IEnumerator TourRestart()
        {
            game = Object.FindFirstObjectByType<GameManager>(); game.Restart(); yield return null; yield return null;
            game = Object.FindFirstObjectByType<GameManager>(); player = game.Player; player.ReadKeyboard = false;
        }
        [UnityTest, Explicit("Records feature-tour media only; no runtime gameplay changes")]
        public IEnumerator GitHubFeatureTourCapture()
        {
            var review = System.Type.GetType("Ma3alem.Editor.ProjectTools, Ma3alem.Editor");
            review.GetMethod("SetReviewResolution").Invoke(null, new object[] { 1280, 720 });
            tourRoot = System.IO.Path.GetFullPath(Application.dataPath + "/../../../work/github-feature-tour");
            System.IO.Directory.CreateDirectory(tourRoot + "/frames"); System.IO.Directory.CreateDirectory(tourRoot + "/screens");
            tourFrame = 0; tourTimeline = new TourTimeline();
            float oldListener = AudioListener.volume; AudioListener.volume = 0;
            Object.Destroy(MusicSource().gameObject); yield return null;
            AudioManager.LocalPreviewMusicLoader = () => UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/LocalPreviewOnly/مزمار صعيدي.mp3");
            game.MainMenu(); yield return null; yield return null; game = Object.FindFirstObjectByType<GameManager>();
            Time.captureFramerate = 30;
            try
            {
                yield return TourHold("main-menu", "برج أسيوط | سطوح مصرية وشخصية صعيدية", 90);
                OpenTourPanel("Help"); yield return TourHold("controls", "التحكم | الأسهم أو الحروف للحركة والمسافة للقفز", 120);
                OpenTourPanel("SoundPanel"); yield return TourHold("audio-settings", "الصوت | تحكم مستقل في الموسيقى وصوت الشخصية", 90);
                var sliders = Object.FindObjectsByType<UnityEngine.UI.Slider>(FindObjectsSortMode.None);
                foreach (var slider in sliders) slider.value = Mathf.Clamp01(slider.value + 0.1f);
                yield return TourHold("volume-controls", "غيّر مستوى الصوت أو اكتمه من الإعدادات", 60);
                yield return TourRestart(); game.TogglePause();
                yield return TourHold("pause", "إيقاف مؤقت | كمل أو ابدأ من جديد أو ارجع للقائمة", 90);
                game.TogglePause();
                yield return TourHold("resume", "الموسيقى مستمرة عند استكمال اللعب", 60);
                game.EndRun(); yield return TourHold("game-over", "اتقفشت يا واد! | النتيجة وأعلى رقم محفوظ", 90);
                yield return TourRestart(); yield return TourHold("restart", "إعادة اللعب | جولة جديدة بنفس التحكم", 60);
                for (int zone = 0; zone < 4; zone++)
                {
                    yield return TourRestart();
                    float height = game.ZoneStarts[zone];
                    game.Follow.enabled = false; player.Body.simulated = false;
                    game.Follow.transform.position = new Vector3(0, height + 4.5f, -10); player.transform.position = new Vector3(0, height + 1, 0);
                    game.Scores.Observe(height); yield return null; yield return null;
                    float nearest = float.PositiveInfinity; Transform ledge = null;
                    foreach (var platform in Object.FindObjectsByType<PlatformEffector2D>(FindObjectsSortMode.None))
                    { float delta = Mathf.Abs(platform.transform.position.y + 0.18f - height); if (delta < nearest) { nearest = delta; ledge = platform.transform; } }
                    Assert.That(ledge, Is.Not.Null);
                    player.Body.position = new Vector2(ledge.position.x, ledge.position.y + 0.18f + 0.7f); player.Body.linearVelocity = Vector2.zero;
                    player.Body.simulated = true; player.SetMove(0); game.Follow.enabled = true;
                    string[] captions = { "المنطقة الأولى | بلكونات الحارة والأدوار من ١ إلى ٢٠", "المنطقة الثانية | البيوت القديمة والأدوار من ٢١ إلى ٤٥", "المنطقة الثالثة | عمارات تحت التشطيب والأدوار من ٤٦ إلى ٧٥", "المنطقة الرابعة | السطوح العالية من الدور ٧٦" };
                    yield return TourHold("zone-" + (zone + 1), captions[zone], 90);
                }
                game.MainMenu(); yield return null; yield return null; game = Object.FindFirstObjectByType<GameManager>();
                string[] cues = { "Start", "Jump", "HighJump", "Scared", "Falling", "Landing", "NewRecord", "GameOver", "Restart" };
                string[] labels = { "بداية الجولة", "القفز", "قفزة قوية", "الخوف", "السقوط", "الهبوط", "رقم قياسي جديد", "نهاية الجولة", "إعادة اللعب" };
                foreach (var cue in cues)
                {
                    string path = "Assets/Audio/VoiceLines/Saidi/" + cue + ".wav";
                    var clip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(path); Assert.That(clip, Is.Not.Null);
                    int index = System.Array.IndexOf(cues, cue);
                    yield return TourHold("voice-" + cue, "عينات أصوات الشخصية | " + labels[index], Mathf.CeilToInt((clip.length + 0.45f) * 30), path);
                }
                yield return TourHold("closing", "برج أسيوط | العب واقفز وحاول تكسر رقمك", 75);
            }
            finally { Time.captureFramerate = 0; AudioListener.volume = oldListener; }
            System.IO.File.WriteAllText(tourRoot + "/timeline.json", JsonUtility.ToJson(tourTimeline, true));
            var overlay = GameObject.Find("QA video overlay"); if (overlay != null) Object.Destroy(overlay);
            review.GetMethod("SetReviewResolution").Invoke(null, new object[] { 1920, 1080 });
            game.MainMenu(); yield return null; yield return null; game = Object.FindFirstObjectByType<GameManager>();
            yield return new WaitForSecondsRealtime(0.5f); ScreenCapture.CaptureScreenshot(tourRoot + "/screens/main-menu.png"); yield return new WaitForSecondsRealtime(0.3f);
            OpenTourPanel("Help"); yield return new WaitForSecondsRealtime(0.3f); ScreenCapture.CaptureScreenshot(tourRoot + "/screens/controls.png"); yield return new WaitForSecondsRealtime(0.3f);
            OpenTourPanel("SoundPanel"); yield return new WaitForSecondsRealtime(0.3f); ScreenCapture.CaptureScreenshot(tourRoot + "/screens/audio-settings.png"); yield return new WaitForSecondsRealtime(0.3f);
            Assert.That(tourFrame, Is.GreaterThan(1000));
        }

        [UnityTest, Explicit("Records actual Unity rendering with automated inputs and controlled camera-pressure fixtures")]
        public IEnumerator FinalGameplayVideo()
        {
            var type = System.Type.GetType("Ma3alem.Editor.ProjectTools, Ma3alem.Editor");
            type.GetMethod("SetReviewResolution").Invoke(null, new object[] { 1280, 720 });
            string output = System.IO.Path.GetFullPath(Application.dataPath + "/../../../work/final-video");
            System.IO.Directory.CreateDirectory(output);
            yield return new WaitForSecondsRealtime(0.3f);
            bool highJump = false, flip = false;
            float captureDirection = 1;
            TourCaption("لقطة Unity فعلية | تحكم آلي للحركة والقفز والشقلبة");
            Time.captureFramerate = 30;
            try
            {
                for (int frame = 0; frame < 240; frame++)
                {
                    ResumeUnfocusedCapture();
                    if (game.State == GameState.GameOver)
                    {
                        game.Restart(); yield return null; yield return null;
                        game = Object.FindFirstObjectByType<GameManager>(); player = game.Player; player.ReadKeyboard = false;
                        TourCaption("لقطة Unity فعلية | المنصات المولدة والفيزياء");
                    }
                    if (player.transform.position.x > 4.5f) captureDirection = -1;
                    if (player.transform.position.x < -4.5f) captureDirection = 1;
                    player.SetMove(captureDirection);
                    if (frame > 11 && player.Grounded) player.PressJump();
                    highJump |= player.IsFlipping; flip |= player.FlipDegrees >= 360;
                    ScreenCapture.CaptureScreenshot(output + "/play_" + frame.ToString("D4") + ".png");
                    yield return null;
                }
                Assert.That(highJump && flip, Is.True, "The recorded gameplay must actually show a powerful jump and full flip.");
                for (int phase = 0; phase < 2; phase++)
                {
                    game.Restart(); yield return null; yield return null;
                    game = Object.FindFirstObjectByType<GameManager>(); player = game.Player; player.ReadKeyboard = false; player.Body.simulated = false;
                    float height = phase == 0 ? 0 : game.ZoneStarts[3];
                    game.Scores.Observe(height); player.transform.position = new Vector3(0, height + 2, 0);
                    game.Follow.transform.position = new Vector3(0, height + 4.5f, -10);
                    var settings = new UnityEditor.SerializedObject(game.Follow); settings.FindProperty("graceSeconds").floatValue = 0; settings.ApplyModifiedPropertiesWithoutUndo();
                    TourCaption(phase == 0 ? "اختبار مضبوط | ضغط الكاميرا عند بداية البرج" : "اختبار مضبوط | تدرج الضغط عند الدور ٧٦");
                    for (int frame = 0; frame < 90; frame++)
                    {
                        ScreenCapture.CaptureScreenshot(output + "/pressure_" + (phase * 90 + frame).ToString("D4") + ".png");
                        yield return null;
                    }
                }
            }
            finally { Time.captureFramerate = 0; }
            yield return new WaitForSecondsRealtime(0.5f);
        }

        [UnityTest, Explicit("Capture the corrected run sprite and Esc pause hint")]
        public IEnumerator SmallVisualFixCaptures()
        {
            string output = System.IO.Path.GetFullPath("Documentation/Previews/SmallFixes");
            System.IO.Directory.CreateDirectory(output);
            var tools = System.Type.GetType("Ma3alem.Editor.ProjectTools, Ma3alem.Editor");
            tools.GetMethod("SetReviewResolution").Invoke(null, new object[] { 1280, 720 });
            player.SetMove(0.5f);
            yield return new WaitForSecondsRealtime(0.3f);
            ScreenCapture.CaptureScreenshot(output + "/Running_1280x720.png");
            yield return new WaitForSecondsRealtime(0.2f);
            game.TogglePause(); yield return null;
            ScreenCapture.CaptureScreenshot(output + "/Pause_Esc_1280x720.png");
            yield return new WaitForSecondsRealtime(0.3f);
            Assert.That(game.State, Is.EqualTo(GameState.Paused));
        }

        [UnityTest, Explicit("Graphics review of Arabic slogan at supported desktop resolutions")]
        public IEnumerator MainMenuSloganArabicCaptures()
        {
            string output = System.IO.Path.GetFullPath("Documentation/Previews/Slogan");
            System.IO.Directory.CreateDirectory(output);
            var type = System.Type.GetType("Ma3alem.Editor.ProjectTools, Ma3alem.Editor");
            game.MainMenu(); yield return null; yield return null;
            foreach (var resolution in new[] { new Vector2Int(1280, 720), new Vector2Int(1920, 1080) })
            {
                type.GetMethod("SetReviewResolution").Invoke(null, new object[] { resolution.x, resolution.y });
                yield return new WaitForSecondsRealtime(0.5f);
                TMPro.TextMeshProUGUI slogan = null;
                foreach (var text in Object.FindObjectsByType<TMPro.TextMeshProUGUI>(FindObjectsSortMode.None))
                    if (text.name == "Official slogan") slogan = text;
                Assert.That(slogan, Is.Not.Null);
                slogan.ForceMeshUpdate();
                System.IO.File.AppendAllText(output + "/Layout.txt", resolution + ": preferred=" + slogan.preferredWidth + "x" + slogan.preferredHeight + "; bounds=" + slogan.rectTransform.rect.size + "; font=" + slogan.fontSize + "\n");
                Assert.That(slogan.isTextOverflowing, Is.False);
                Assert.That(slogan.textInfo.lineCount, Is.EqualTo(1));
                Assert.That(slogan.text.Normalize(System.Text.NormalizationForm.FormKC), Does.Contain("فِ"));
                int markIndex = slogan.text.IndexOf('\u0650');
                Assert.That(markIndex, Is.GreaterThan(0));
                var mark = slogan.textInfo.characterInfo[markIndex];
                var feh = slogan.textInfo.characterInfo[markIndex - 1];
                Assert.That(mark.isVisible, Is.True, "Kasra must have a rendered glyph.");
                Assert.That(mark.fontAsset, Is.SameAs(slogan.font), "Arabic mark must use the same font.");
                float markX = (mark.bottomLeft.x + mark.topRight.x) / 2;
                Assert.That(markX, Is.InRange(feh.bottomLeft.x - 2, feh.topRight.x + 2), "Kasra must sit under Feh.");
                Assert.That(mark.topRight.y, Is.LessThan(feh.baseLine + 2), "Kasra belongs below the baseline.");
                System.IO.File.AppendAllText(output + "/GlyphGeometry.txt", resolution + ": Feh=" + feh.bottomLeft + ".." + feh.topRight + "; Kasra=" + mark.bottomLeft + ".." + mark.topRight + "\n");
                ScreenCapture.CaptureScreenshot(output + "/MainMenu_" + resolution.x + "x" + resolution.y + ".png");
                yield return new WaitForSecondsRealtime(0.4f);
            }
        }

        [UnityTest, Explicit("Run separately with graphics to capture real rendered frames")]
        public IEnumerator VisualIdentityCaptures()
        {
            var type = System.Type.GetType("Ma3alem.Editor.ProjectTools, Ma3alem.Editor");
            type.GetMethod("SetReviewResolution").Invoke(null, new object[] { 1920, 1080 });
            string output = System.IO.Path.GetFullPath("Documentation/Previews/Final");
            System.IO.Directory.CreateDirectory(output);
            game.MainMenu(); yield return null; yield return null;
            yield return new WaitForSecondsRealtime(0.7f);
            ScreenCapture.CaptureScreenshot(output + "/MainMenu.png");
            yield return new WaitForSecondsRealtime(0.5f);
            game = Object.FindFirstObjectByType<GameManager>(); game.Restart(); yield return null; yield return null;
            game = Object.FindFirstObjectByType<GameManager>(); player = game.Player; player.ReadKeyboard = false;
            player.Body.simulated = false; player.Body.interpolation = RigidbodyInterpolation2D.None; game.Follow.enabled = false;
            foreach (float height in new[] { 0f, game.ZoneStarts[1] + 3, game.ZoneStarts[2] + 3, game.ZoneStarts[3] + 3 })
            {
                game.Follow.transform.position = new Vector3(0, height + 4.5f, -10);
                player.transform.position = new Vector3(0, height + 0.7f, 0); game.Scores.Observe(height);
                yield return new WaitForSecondsRealtime(0.6f);
                ScreenCapture.CaptureScreenshot(output + "/Zone_" + game.CurrentZone + ".png");
                yield return new WaitForSecondsRealtime(0.4f);
            }
            game.Restart(); yield return null; yield return null;
            game = Object.FindFirstObjectByType<GameManager>(); player = game.Player; player.ReadKeyboard = false;
            IsolateFloor(); game.Follow.enabled = false;
            game.Follow.transform.position = new Vector3(0, 4.5f, -10);
            yield return new WaitForSecondsRealtime(0.3f);
            player.SetMove(1); yield return new WaitForSecondsRealtime(0.35f);
            ScreenCapture.CaptureScreenshot(output + "/Character_Run.png");
            yield return new WaitForSecondsRealtime(0.12f);
            player.PressJump(); yield return new WaitForSecondsRealtime(0.15f);
            ScreenCapture.CaptureScreenshot(output + "/Character_Somersault.png");
            yield return new WaitForSecondsRealtime(0.5f);
            ScreenCapture.CaptureScreenshot(output + "/Character_Jump.png");
            yield return new WaitForSecondsRealtime(0.3f);
            game.TogglePause(); yield return new WaitForSecondsRealtime(0.3f);
            ScreenCapture.CaptureScreenshot(output + "/Pause.png"); yield return new WaitForSecondsRealtime(0.4f);
            game.TogglePause(); game.EndRun(); yield return new WaitForSecondsRealtime(0.3f);
            ScreenCapture.CaptureScreenshot(output + "/GameOver.png"); yield return new WaitForSecondsRealtime(0.4f);
            Assert.That(System.IO.File.Exists(output + "/Zone_3.png"), Is.True);
        }
    }
}
