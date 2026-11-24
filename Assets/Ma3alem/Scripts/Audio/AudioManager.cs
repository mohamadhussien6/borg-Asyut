using System;
using System.Collections.Generic;
using UnityEngine;

namespace Ma3alem
{
    public sealed class AudioManager : MonoBehaviour
    {
        public enum VoiceCue { Jump, Scared, Falling, GameOver, Restart, HighJump, Landing, NewRecord, Start }
        #if UNITY_EDITOR
        public static Func<AudioClip> LocalPreviewMusicLoader;
        private static AudioClip previewMusic;
        #endif
        [Serializable] public sealed class VoiceGroup { public VoiceCue cue; public AudioClip[] clips = Array.Empty<AudioClip>(); }
        [SerializeField] private AudioSource musicSource;
        private static AudioSource persistentMusic;
        [SerializeField] private AudioSource effectsSource;
        [SerializeField] private AudioSource voiceSource;
        [SerializeField] private AudioClip music;
        [SerializeField] private AudioClip jumpEffect, landEffect, clickEffect, overEffect;
        [SerializeField] private VoiceGroup[] voices;
        public bool SoundEnabled { get; private set; }
        public float VoiceVolume => voiceSource.volume;
        public float MusicVolume => musicSource.volume;
        public AudioSource VoiceSource => voiceSource;
        private readonly VoicePolicy policy = new VoicePolicy();
        private readonly Dictionary<VoiceCue, AudioClip> previousTake = new Dictionary<VoiceCue, AudioClip>();
        private GameManager game;
        private PlayerController player;
        private int currentPriority;
        private bool recordPending;
        private bool landingPending;
        public void Initialize(GameManager gameManager)
        {
            game = gameManager;
            SoundEnabled = PlayerPrefs.GetInt("Ma3alem.Sound.v1", 1) != 0;
            float volume = PlayerPrefs.GetFloat("Ma3alem.Voice.v1", 0.85f);
            voiceSource.volume = float.IsNaN(volume) || float.IsInfinity(volume) ? 0.85f : Mathf.Clamp01(volume);
            InitializeMusic();
            voiceSource.pitch = 1; voiceSource.spatialBlend = 0;
            voiceSource.loop = false;
            voiceSource.bypassEffects = voiceSource.bypassListenerEffects = voiceSource.bypassReverbZones = true;
            ApplySound();
            game.StateChanged += StateChanged;
            game.Scores.NewRecord += Record;
        }
        private void InitializeMusic()
        {
            AudioSource sceneSource = musicSource;
            if (persistentMusic == null)
            {
                var root = new GameObject("Persistent tower music", typeof(AudioSource));
                DontDestroyOnLoad(root);
                persistentMusic = root.GetComponent<AudioSource>();
                persistentMusic.clip = music;
                #if UNITY_EDITOR
                previewMusic = LocalPreviewMusicLoader?.Invoke();
                if (previewMusic != null) persistentMusic.clip = previewMusic;
                #endif
                persistentMusic.loop = true; persistentMusic.pitch = 1;
                persistentMusic.spatialBlend = 0; persistentMusic.playOnAwake = false;
                persistentMusic.volume = Mathf.Clamp01(PlayerPrefs.GetFloat("Ma3alem.Music.v1", 0.16f));
                persistentMusic.mute = !SoundEnabled;
                if (persistentMusic.clip != null) persistentMusic.Play();
            }
            musicSource = persistentMusic;
            if (sceneSource != null && sceneSource != persistentMusic)
            {
                sceneSource.Stop(); Destroy(sceneSource);
            }
        }
        public void Bind(PlayerController controller)
        {
            player = controller;
            player.Jumped += Jump; player.Landed += Land;
            player.Scared += Fear; player.Falling += Fall;
            player.HighJumped += HighJump; player.DangerousLanding += SafeLanding;
        }
        private void Jump() { landingPending = false; effectsSource.PlayOneShot(jumpEffect); if (UnityEngine.Random.value < 0.28f) TryPlayVoice(VoiceCue.Jump); }
        private void Land() { effectsSource.PlayOneShot(landEffect); }
        private void HighJump() { TryPlayVoice(VoiceCue.HighJump); }
        private void SafeLanding() { landingPending = true; }
        private void Record() { recordPending = true; }
        private void Update()
        {
            if (!game.IsRunning || voiceSource.isPlaying) return;
            if (landingPending && player != null && player.Grounded && TryPlayVoice(VoiceCue.Landing)) landingPending = false;
            else if (recordPending && TryPlayVoice(VoiceCue.NewRecord)) recordPending = false;
        }
        private void Fear() { TryPlayVoice(VoiceCue.Scared); }
        private void Fall() { TryPlayVoice(VoiceCue.Falling); }
        public void Click() { effectsSource.PlayOneShot(clickEffect); }
        public bool TryPlayVoice(VoiceCue cue)
        {
            if (!SoundEnabled || voiceSource.volume <= 0) return false;
            if (!policy.CanPlay(cue, Time.unscaledTimeAsDouble, voiceSource.isPlaying, currentPriority)) return false;
            AudioClip clip = ChooseTake(cue);
            if (clip == null) return false;
            voiceSource.Stop(); voiceSource.clip = clip; voiceSource.Play();
            currentPriority = VoicePolicy.Priority(cue);
            policy.Start(cue, Time.unscaledTimeAsDouble);
            previousTake[cue] = clip;
            return true;
        }
        private AudioClip ChooseTake(VoiceCue cue)
        {
            if (voices == null) return null;
            var group = Array.Find(voices, entry => entry != null && entry.cue == cue);
            if (group == null || group.clips == null || group.clips.Length == 0) return null;
            previousTake.TryGetValue(cue, out AudioClip previous);
            int start = UnityEngine.Random.Range(0, group.clips.Length);
            AudioClip fallback = null;
            for (int i = 0; i < group.clips.Length; i++)
            {
                AudioClip clip = group.clips[(start + i) % group.clips.Length];
                if (clip == null) continue;
                fallback = clip;
                if (clip != previous) return clip;
            }
            return fallback;
        }
        private void StateChanged(GameState state)
        {
            if (state == GameState.Paused) { voiceSource.Pause(); effectsSource.Pause(); }
            else { voiceSource.UnPause(); effectsSource.UnPause(); }
            if (state == GameState.GameOver) { recordPending = landingPending = false; effectsSource.PlayOneShot(overEffect); TryPlayVoice(VoiceCue.GameOver); }
        }
        public void ToggleSound()
        {
            SoundEnabled = !SoundEnabled; ApplySound();
            if (!SoundEnabled) voiceSource.Stop();
            PlayerPrefs.SetInt("Ma3alem.Sound.v1", SoundEnabled ? 1 : 0); PlayerPrefs.Save();
        }
        private void ApplySound() { musicSource.mute = effectsSource.mute = voiceSource.mute = !SoundEnabled; }
        public void SetVoiceVolume(float volume)
        {
            voiceSource.volume = Mathf.Clamp01(volume);
            PlayerPrefs.SetFloat("Ma3alem.Voice.v1", voiceSource.volume); PlayerPrefs.Save();
        }
        public void SetMusicVolume(float volume)
        {
            musicSource.volume = Mathf.Clamp01(volume);
            PlayerPrefs.SetFloat("Ma3alem.Music.v1", musicSource.volume); PlayerPrefs.Save();
        }
        private void OnDestroy()
        {
            if (game != null) { game.StateChanged -= StateChanged; game.Scores.NewRecord -= Record; }
            if (player == null) return;
            player.Jumped -= Jump; player.Landed -= Land; player.Scared -= Fear; player.Falling -= Fall;
            player.HighJumped -= HighJump; player.DangerousLanding -= SafeLanding;
        }
        #if UNITY_EDITOR
        private void OnApplicationQuit()
        {
            if (previewMusic != null && !UnityEditor.AssetDatabase.Contains(previewMusic)) Destroy(previewMusic);
            previewMusic = null;
        }
        #endif
        public sealed class VoicePolicy
        {
            private readonly Dictionary<VoiceCue, double> lastCue = new Dictionary<VoiceCue, double>();
            private double lastStarted = double.NegativeInfinity;
            public static int Priority(VoiceCue cue)
            {
                switch (cue) { case VoiceCue.Restart: return 100; case VoiceCue.Falling: return 90;
                    case VoiceCue.GameOver: return 80; case VoiceCue.Scared: return 40;
                    case VoiceCue.HighJump: return 50; case VoiceCue.Landing: return 60;
                    case VoiceCue.NewRecord: return 30; case VoiceCue.Start: return 20; default: return 10; }
            }
            public bool CanPlay(VoiceCue cue, double now, bool speaking, int current)
            {
                double cooldown = cue == VoiceCue.Jump ? 6 : 4;
                if (lastCue.TryGetValue(cue, out double last) && now - last < cooldown) return false;
                bool preempting = speaking && Priority(cue) > current;
                if (cue != VoiceCue.Restart && !preempting && now - lastStarted < 0.25) return false;
                return !speaking || Priority(cue) > current;
            }
            public void Start(VoiceCue cue, double now) { lastCue[cue] = now; lastStarted = now; }
        }
    }
}
