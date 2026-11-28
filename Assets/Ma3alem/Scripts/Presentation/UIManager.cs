using System;
using RTLTMPro;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TextCore;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.UI;

namespace Ma3alem
{
    public sealed class UIManager : MonoBehaviour
    {
        [SerializeField] private Font arabicFont;
        [SerializeField] private Font latinFont;
        [SerializeField] private Shader fontShader;
        [SerializeField] private Sprite panelSprite;
        [SerializeField] private Sprite portrait;
        [SerializeField] private string gameTitle = "برج أسيوط";
        [SerializeField] private Sprite buttonSprite;
        [SerializeField] private Sprite shishaSprite;
        [SerializeField] private Sprite menuLedge;
        private const string OfficialSlogan = "اللي أبوه صعيدي ميخافِش!";
        private GameManager game;
        private AudioManager sound;
        private RectTransform root;
        private TMP_FontAsset font;
        private TMP_FontAsset previousDefaultFont;
        private TMP_FontAsset latinFallback;
        private Sprite solidFill;
        private TextMeshProUGUI scoreText;
        private TextMeshProUGUI bestText;
        private Image dangerFill;
        private RectTransform dangerMarker;
        private TextMeshProUGUI dangerLabel;
        private TextMeshProUGUI zoneLabel;
        private Image zonePanel;
        private TextMeshProUGUI floorLabel, comboLabel;
        private RectTransform[] smoke;
        private Image[] smokeImages;
        private Sprite smokeSprite;
        private Texture2D smokeTexture;
        private int shownFloor = -1, dangerBand = -1;
        private float comboUntil;
        private float zoneUntil;
        private int announcedZone = -1;
        private static readonly string[] ZoneNames = { "بلكونة بنت الجيران", "لحن الموزايكو", "سوبر لوكس", "توت عنخ هموت" };
        private static readonly string[] ZoneOrder = { "الأولى", "الثانية", "الثالثة", "الرابعة" };
        private static readonly string[] ComboMessages = { "عاش يا واد!", "يا سلام!", "كمل يا بطل!", "يا ساتر!" };
        private float shownDanger;
        private int shownZone = -1;
                public float ShownDanger => shownDanger;
        private readonly FastStringBuilder shaped = new FastStringBuilder(256);
        private static readonly Color Ink = new Color(0.08f, 0.19f, 0.22f, 0.96f);
        private static readonly Color Gold = new Color(0.62f, 0.28f, 0.1f);
        private static readonly Color SafeDangerColor = new Color(0.57f, 0.83f, 0.38f);
        private static readonly Color WarningDangerColor = new Color(1f, 0.73f, 0.20f);
        private static readonly Color CriticalDangerColor = new Color(0.96f, 0.25f, 0.16f);

        public void Initialize(GameManager gameManager, AudioManager audioManager)
        {
            game = gameManager; sound = audioManager;
            solidFill = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 1, 1), Vector2.one * 0.5f);
            font = TMP_FontAsset.CreateFontAsset(arabicFont, 48, 5, GlyphRenderMode.SDFAA, 1024, 1024, AtlasPopulationMode.Dynamic);
            font.isMultiAtlasTexturesEnabled = true;
            font.material.shader = fontShader;
            latinFallback = TMP_FontAsset.CreateFontAsset(latinFont, 48, 5, GlyphRenderMode.SDFAA, 512, 512, AtlasPopulationMode.Dynamic);
            latinFallback.material.shader = fontShader;
            font.fallbackFontAssetTable = new System.Collections.Generic.List<TMP_FontAsset> { latinFallback };
            previousDefaultFont = TMP_Settings.defaultFontAsset;
            TMP_Settings.defaultFontAsset = font;
            var canvasObject = new GameObject("Arabic interface", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(transform, false);
            canvasObject.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280, 720); scaler.matchWidthOrHeight = 0.5f;
            root = canvasObject.GetComponent<RectTransform>();
            new GameObject("Keyboard and mouse UI", typeof(EventSystem), typeof(StandaloneInputModule)).transform.SetParent(transform);
            game.StateChanged += Show;
            game.Scores.Changed += UpdateScore;
            game.Scores.ComboChanged += ShowCombo;
            game.Player.HighJumped += ShowHighJump;
            smokeTexture = new Texture2D(16, 16, TextureFormat.RGBA32, false);
            for (int y = 0; y < 16; y++) for (int x = 0; x < 16; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), new Vector2(7.5f, 7.5f)) / 7.5f;
                smokeTexture.SetPixel(x, y, new Color(1, 1, 1, Mathf.Clamp01(1 - distance) * 0.8f));
            }
            smokeTexture.Apply(false, true);
            smokeSprite = Sprite.Create(smokeTexture, new Rect(0, 0, 16, 16), Vector2.one * 0.5f);
            Show(game.State);
        }

        private RectTransform Rect(string name, Transform parent, Vector2 position, Vector2 size)
        {
            var node = new GameObject(name, typeof(RectTransform));
            node.transform.SetParent(parent, false);
            var rect = node.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position; rect.sizeDelta = size;
            return rect;
        }

        private Image Panel(Transform parent, Vector2 position, Vector2 size)
        {
            var image = Rect("Panel", parent, position, size).gameObject.AddComponent<Image>();
            image.sprite = panelSprite; image.type = Image.Type.Sliced; image.color = Color.white;
            return image;
        }

        private TextMeshProUGUI Label(Transform parent, string caption, Vector2 position, int size)
        {
            var label = Rect("Arabic label", parent, position, new Vector2(640, size * 2.5f)).gameObject.AddComponent<TextMeshProUGUI>();
            label.font = font; label.fontSize = size; label.alignment = TextAlignmentOptions.Center;
            label.color = Ink; label.raycastTarget = false; label.richText = false;
            label.enableWordWrapping = false;
            SetText(label, caption);
            return label;
        }

        private void SetText(TextMeshProUGUI label, string logicalText)
        {
            shaped.Clear();
            RTLSupport.FixRTL(logicalText, shaped, farsi: false, fixTextTags: false, preserveNumbers: true);
            // The shaper returns visual order; TMP must not reverse it a second time.
            label.isRightToLeftText = false;
            label.text = shaped.ToString();
        }

        private Button Button(Transform parent, string caption, float y, Action action)
        {
            var image = Panel(parent, new Vector2(0, y), new Vector2(350, 60));
            image.sprite = buttonSprite; image.color = Color.white;
            var button = image.gameObject.AddComponent<Button>();
            var colors = button.colors;
            colors.highlightedColor = new Color(1, 0.9f, 0.65f); colors.selectedColor = new Color(1, 0.94f, 0.78f);
            colors.pressedColor = new Color(0.85f, 0.65f, 0.26f); button.colors = colors;
            button.onClick.AddListener(() => { sound.Click(); action(); });
            var text = Label(image.transform, caption, Vector2.zero, 24);
            text.color = new Color(1, 0.94f, 0.78f);
            text.rectTransform.anchorMin = Vector2.zero; text.rectTransform.anchorMax = Vector2.one;
            text.rectTransform.offsetMin = new Vector2(32, 6); text.rectTransform.offsetMax = new Vector2(-32, -6);
            return button;
        }

        private void Clear()
        {
            scoreText = bestText = null;
            dangerFill = null; dangerMarker = null; smoke = null; smokeImages = null; zoneLabel = floorLabel = comboLabel = null; zonePanel = null;
            shownFloor = -1; shownZone = -1; dangerBand = -1;
            foreach (Transform child in root) { child.gameObject.SetActive(false); Destroy(child.gameObject); }
        }

        private void Show(GameState state)
        {
            Clear();
            if (state == GameState.Running) { Hud(); return; }
            var shade = Panel(root, Vector2.zero, new Vector2(2400, 1600));
            shade.color = new Color(0.03f, 0.10f, 0.14f, state == GameState.Menu ? 0.04f : 0.60f);
            if (state == GameState.Menu) Menu(); else Results(state);
        }

        private void Menu()
        {
            var title = Rect("Assiut rooftop sign", root, new Vector2(0, 240), new Vector2(620, 180));
            var titleImage = title.gameObject.AddComponent<Image>(); titleImage.sprite = buttonSprite;
            var heading = Label(title, gameTitle, new Vector2(0, 20), 48);
            heading.rectTransform.sizeDelta = new Vector2(460, 82);
            heading.color = new Color(1, 0.87f, 0.58f);
            var subtitle = Label(title, "icy tower Asyut", new Vector2(0, -36), 22);
            subtitle.rectTransform.sizeDelta = new Vector2(400, 34); subtitle.color = Color.white;
            var sloganPlaque = Rect("Official slogan plaque", title, new Vector2(0, -112), new Vector2(520, 54));
            var plaqueImage = sloganPlaque.gameObject.AddComponent<Image>();
            plaqueImage.sprite = buttonSprite; plaqueImage.type = Image.Type.Sliced; plaqueImage.raycastTarget = false;
            var slogan = Label(sloganPlaque, OfficialSlogan, Vector2.zero, 22);
            slogan.name = "Official slogan";
            slogan.color = new Color(1, 0.90f, 0.65f);
            slogan.fontStyle = FontStyles.Bold;
            slogan.enableAutoSizing = true; slogan.fontSizeMin = 20; slogan.fontSizeMax = 22;
            slogan.rectTransform.sizeDelta = new Vector2(460, 48);
            slogan.fontFeatures = new System.Collections.Generic.List<OTL_FeatureTag> { OTL_FeatureTag.kern, OTL_FeatureTag.mark, OTL_FeatureTag.mkmk };
            // Visual-order shaping reverses this combining mark; TMP attaches it after the base glyph.
            int kasra = slogan.text.IndexOf('\u0650');
            if (kasra >= 0 && kasra + 1 < slogan.text.Length)
                slogan.text = slogan.text.Remove(kasra, 1).Insert(kasra + 1, "\u0650");
            var card = Rect("Menu buttons", root, new Vector2(0, -65), new Vector2(420, 370));
            Button first = Button(card.transform, "العب", 120, game.Restart);
            Button(card.transform, "طريقة اللعب", 44, Help);
            Button(card.transform, "الصوت", -32, SoundPanel);
            #if !UNITY_WEBGL || UNITY_EDITOR
            Button(card.transform, "خروج", -108, Quit);
            #endif
            var record = Panel(card.transform, new Vector2(0, -172), new Vector2(310, 40));
            Label(record.transform, $"أعلى نتيجة: {game.Scores.Best}", Vector2.zero, 22);
            var credit = Panel(root, new Vector2(0, -311), new Vector2(770, 48));
            credit.raycastTarget = false;
            var creator = Label(credit.transform, "تطوير وإخراج إبداعي: محمد الحسين محمد رمزي", Vector2.zero, 20);
            creator.name = "Developer credit";
            creator.rectTransform.sizeDelta = new Vector2(730, 44);
            creator.enableAutoSizing = true; creator.fontSizeMin = 14; creator.fontSizeMax = 20;
            Select(first);
        }

        private void Hud()
        {
            var points = Panel(root, new Vector2(475, 321), new Vector2(245, 44));
            scoreText = Label(points.transform, "", Vector2.zero, 20);
            var record = Panel(root, new Vector2(195, 321), new Vector2(275, 44));
            bestText = Label(record.transform, "", Vector2.zero, 18);
            var pause = Button(root, "إيقاف", 302, game.TogglePause);
            pause.GetComponent<RectTransform>().anchoredPosition = new Vector2(-530, 321);
            pause.GetComponent<RectTransform>().sizeDelta = new Vector2(145, 44);
            UpdateScore(game.Scores.Score);
            CreateDangerMeter();
            floorLabel = Label(root, "", new Vector2(-75, 321), 19);
            floorLabel.rectTransform.sizeDelta = new Vector2(190, 44);
            zonePanel = Panel(root, new Vector2(0, 260), new Vector2(500, 60));
            zonePanel.sprite = buttonSprite;
            zonePanel.raycastTarget = false;
            zoneLabel = Label(zonePanel.transform, "", Vector2.zero, 23);
            zoneLabel.rectTransform.sizeDelta = new Vector2(440, 52);
            zoneLabel.enableAutoSizing = true;
            zoneLabel.fontSizeMin = 18; zoneLabel.fontSizeMax = 23;
            zoneLabel.fontStyle = FontStyles.Bold;
            zoneLabel.color = new Color(1f, 0.90f, 0.65f);
            comboLabel = Label(root, "", new Vector2(0, 205), 26);
            ShowCombo(game.Scores.Combo.Count);
        }

        private void CreateDangerMeter()
        {
            var holder = Rect("Shisha danger meter", root, new Vector2(76, -10), new Vector2(120, 340));
            holder.anchorMin = holder.anchorMax = new Vector2(0, 0.5f);
            var art = holder.gameObject.AddComponent<Image>(); art.sprite = shishaSprite; art.preserveAspect = true;
            art.raycastTarget = false;
            var rim = Panel(holder, new Vector2(0, 12), new Vector2(16, 90));
            rim.sprite = solidFill; rim.color = new Color(0.91f, 0.82f, 0.62f);
            var track = Panel(rim.transform, Vector2.zero, new Vector2(12, 86));
            track.color = new Color(0.035f, 0.055f, 0.045f);
            track.sprite = solidFill; track.type = Image.Type.Simple;
            dangerFill = Panel(track.transform, Vector2.zero, new Vector2(8, 82));
            dangerFill.sprite = solidFill;
            dangerFill.type = Image.Type.Filled; dangerFill.fillMethod = Image.FillMethod.Vertical; dangerFill.fillOrigin = 0;
            var marker = Panel(track.transform, new Vector2(0, -41), new Vector2(12, 2));
            marker.sprite = solidFill; marker.color = new Color(1f, 0.98f, 0.87f);
            dangerMarker = marker.rectTransform;
            var caption = Panel(holder, new Vector2(0, -143), new Vector2(90, 34));
            caption.sprite = solidFill; caption.color = new Color(0.035f, 0.065f, 0.075f, 0.97f);
            dangerLabel = Label(caption.transform, "أمان", Vector2.zero, 21);
            dangerLabel.rectTransform.sizeDelta = new Vector2(88, 32);
            dangerLabel.fontStyle = FontStyles.Bold;
            dangerLabel.color = new Color(1, 0.94f, 0.81f);
            smoke = new RectTransform[3]; smokeImages = new Image[3];
            for (int i = 0; i < smoke.Length; i++)
            {
                var puff = Panel(holder, new Vector2(0, 170), new Vector2(16, 9)); puff.raycastTarget = false;
                puff.sprite = smokeSprite; puff.type = Image.Type.Simple; puff.color = Color.clear;
                smoke[i] = puff.rectTransform;
                smokeImages[i] = puff;
            }
        }

        private void Update()
        {
            if (dangerFill == null) return;
            shownDanger = Mathf.MoveTowards(shownDanger, game.Follow.Danger, Time.unscaledDeltaTime * 2.5f);
            dangerFill.fillAmount = shownDanger;
            dangerMarker.anchoredPosition = new Vector2(0, Mathf.Lerp(-41, 41, shownDanger));
            var dangerColor = shownDanger < 0.65f
                ? Color.Lerp(SafeDangerColor, WarningDangerColor, Mathf.InverseLerp(0.15f, 0.65f, shownDanger))
                : Color.Lerp(WarningDangerColor, CriticalDangerColor, Mathf.InverseLerp(0.65f, 1, shownDanger));
            if (shownDanger > 0.75f) dangerColor.a = 0.94f + 0.06f * Mathf.Sin(Time.unscaledTime * 6);
            dangerFill.color = dangerColor;
            int band = shownDanger < 0.45f ? 0 : shownDanger < 0.75f ? 1 : 2;
            if (band != dangerBand) { dangerBand = band; SetText(dangerLabel, band == 0 ? "أمان" : band == 1 ? "خلي بالك" : "اجري!"); }
            for (int i = 0; i < smoke.Length; i++)
            {
                float phase = (Time.unscaledTime * 0.5f + i / 3f) % 1;
                smoke[i].anchoredPosition = new Vector2(Mathf.Sin(phase * 6 + i) * 9, 154 + phase * 34);
                smokeImages[i].color = new Color(0.91f, 0.90f, 0.85f, Mathf.InverseLerp(0.4f, 1, shownDanger) * (1 - phase) * 0.65f);
            }
            if (zoneLabel == null) return;
            if (shownFloor != game.CurrentFloor) { shownFloor = game.CurrentFloor; SetText(floorLabel, $"الدور: {Mathf.Max(1, shownFloor)}"); }
            if (shownZone != game.CurrentZone)
            {
                shownZone = game.CurrentZone;
                SetText(zoneLabel, $"المرحلة {ZoneOrder[shownZone]}: {ZoneNames[shownZone]}");
                if (announcedZone != shownZone)
                {
                    announcedZone = shownZone;
                    zoneUntil = Time.time + 3;
                }
            }
            zonePanel.gameObject.SetActive(game.IsRunning && Time.time < zoneUntil);
            if (comboLabel != null)
            {
                float remaining = comboUntil - Time.time;
                float opacity = game.Scores.Combo.Count > 0 ? 0.95f : Mathf.Clamp01(remaining * 3);
                comboLabel.color = new Color(0.65f, 0.22f, 0.05f, opacity);
                comboLabel.rectTransform.localScale = Vector3.one * (1 + Mathf.Max(0, remaining - 1.6f) * 0.14f);
            }
        }

        private void ShowCombo(int count)
        {
            if (comboLabel == null) return;
            comboUntil = count > 0 ? Time.time + 2 : 0;
            SetText(comboLabel, count > 0 ? $"{ComboMessages[Mathf.Min(count - 1, 3)]} ×{count}  +{game.Scores.Combo.Bonus}" : "");
        }
        private void ShowHighJump()
        {
            if (comboLabel == null || game.Scores.Combo.Count > 0) return;
            comboUntil = Time.time + 1.2f;
            SetText(comboLabel, "يا ساتر! قفزة جامدة!");
        }

        private void UpdateScore(int score)
        {
            if (scoreText != null) SetText(scoreText, $"النقاط: {score}");
            if (bestText != null) SetText(bestText, $"أعلى نتيجة: {Mathf.Max(game.Scores.Best, score)}");
        }

        private void Results(GameState state)
        {
            bool paused = state == GameState.Paused;
            var card = Panel(root, Vector2.zero, new Vector2(470, 590));
            Label(card.transform, "برج أسيوط", new Vector2(0, 250), 25);
            Label(card.transform, paused ? "استراحة صغيرة" : "اتقفشت يا واد!", new Vector2(0, 200), 42).color = Gold;
            Label(card.transform, $"النقاط: {game.Scores.Score}", new Vector2(0, 145), 33);
            Label(card.transform, $"أعلى نتيجة: {game.Scores.Best}", new Vector2(0, 96), 25);
            Button first = paused ? Button(card.transform, "كمل", 16, game.TogglePause)
                : Button(card.transform, "العب تاني", 16, game.Restart);
            if (paused) Button(card.transform, "إعادة", -49, game.Restart);
            Button(card.transform, "القائمة الرئيسية", paused ? -114 : -49, game.MainMenu);
            Button(card.transform, "الصوت", paused ? -179 : -114, SoundPanel);
            Label(card.transform, paused ? "اضغط Esc علشان تكمل" : "اضغط R علشان تعيد", new Vector2(0, -247), 20);
            Select(first);
            CreateDangerMeter();
        }

        private void Help()
        {
            Clear(); var card = Panel(root, Vector2.zero, new Vector2(800, 600));
            Label(card.transform, "طريقة اللعب", new Vector2(0, 220), 36).color = Gold;
            Label(card.transform, "اتحرك بالأسهم أو بالحروف A و D", new Vector2(0, 126), 22);
            Label(card.transform, "اضغط مسافة للقفز... وطوّل الضغطة علشان تعلى", new Vector2(0, 65), 21);
            Label(card.transform, "اجري أسرع واقفز أعلى... واعمل شقلباظ!", new Vector2(0, 4), 22);
            Label(card.transform, "الكاميرا بتطلع... لو وقعت تحتها اللعبة بتخلص", new Vector2(0, -57), 21);
            Label(card.transform, "Esc للإيقاف | Alt + Enter لملء الشاشة", new Vector2(0, -118), 21);
            Select(Button(card.transform, "رجوع", -225, () => Show(game.State)));
        }

        private void SoundPanel()
        {
            Clear(); var card = Panel(root, Vector2.zero, new Vector2(650, 580));
            Label(card.transform, "الصوت", new Vector2(0, 220), 44).color = Gold;
            Button toggle = null;
            toggle = Button(card.transform, sound.SoundEnabled ? "كتم الصوت" : "تشغيل الصوت", 86, () =>
            { sound.ToggleSound(); SetText(toggle.GetComponentInChildren<TextMeshProUGUI>(), sound.SoundEnabled ? "كتم الصوت" : "تشغيل الصوت"); });
            VolumeSlider(card.transform, "صوت الشخصية", 13, sound.VoiceVolume, sound.SetVoiceVolume);
            VolumeSlider(card.transform, "الموسيقى", -100, sound.MusicVolume, sound.SetMusicVolume);
            Select(Button(card.transform, "رجوع", -220, () => Show(game.State)));
        }

        private void VolumeSlider(Transform parent, string caption, float y, float value, UnityEngine.Events.UnityAction<float> changed)
        {
            Label(parent, caption, new Vector2(0, y), 26);
            var track = Panel(parent, new Vector2(0, y - 42), new Vector2(330, 18));
            track.color = new Color(0.3f, 0.5f, 0.5f);
            var handle = Panel(track.transform, Vector2.zero, new Vector2(25, 34)); handle.color = Gold;
            var slider = track.gameObject.AddComponent<Slider>();
            slider.direction = Slider.Direction.RightToLeft; slider.handleRect = handle.rectTransform;
            slider.targetGraphic = handle; slider.value = value;
            slider.onValueChanged.AddListener(changed);
        }

        private static void Select(Button first) { EventSystem.current.SetSelectedGameObject(first.gameObject); }
        private static void Quit()
        {
            #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
            #else
            Application.Quit();
            #endif
        }

        private void OnDestroy()
        {
            if (game != null) { game.StateChanged -= Show; game.Scores.Changed -= UpdateScore; game.Scores.ComboChanged -= ShowCombo; game.Player.HighJumped -= ShowHighJump; }
            if (font == null) return;
            if (solidFill != null) Destroy(solidFill);
            if (smokeSprite != null) Destroy(smokeSprite);
            if (smokeTexture != null) Destroy(smokeTexture);
            if (TMP_Settings.defaultFontAsset == font) TMP_Settings.defaultFontAsset = previousDefaultFont;
            ReleaseFont(latinFallback);
            ReleaseFont(font);
        }

        private static void ReleaseFont(TMP_FontAsset asset)
        {
            foreach (var atlas in asset.atlasTextures) if (atlas != null) Destroy(atlas);
            Destroy(asset.material); Destroy(asset);
        }
    }
}
