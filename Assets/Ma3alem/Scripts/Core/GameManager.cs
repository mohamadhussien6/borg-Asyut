using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Ma3alem
{
    public enum GameState { Menu, Running, Paused, GameOver }

    public sealed class GameManager : MonoBehaviour
    {
        [SerializeField] private GameObject world;
        [SerializeField] private PlayerController player;
        [SerializeField] private PlatformSpawner platforms;
        [SerializeField] private CameraFollow cameraFollow;
        [SerializeField] private ScoreManager scores;
        [SerializeField] private UIManager ui;
        [SerializeField] private AudioManager audioPrefab;
        [SerializeField] private Transform background;
        public GameState State { get; private set; }
        public bool IsRunning => State == GameState.Running;
        public ScoreManager Scores => scores;
        public const string OfficialTitle = "برج أسيوط (icy tower Asyut)";
        public CameraFollow Follow => cameraFollow;
        public PlayerController Player => player;
        public float[] ZoneStarts => zoneStarts;
        public int CurrentFloor => platforms.FloorAtHeight(scores.Height.MaximumHeight);
        public int FinalZoneFloor => platforms.FinalZoneFloor;
        public bool TryGetFloor(Collider2D collider, out int floor) => platforms.TryGetFloor(collider, out floor);
        public int CurrentZone => ZoneAt(scores.Height.MaximumHeight);
        public int ZoneAt(float height)
        {
            float[] starts = zoneStarts;
            return height >= starts[3] ? 3 : height >= starts[2] ? 2 : height >= starts[1] ? 1 : 0;
        }
        public event Action<GameState> StateChanged;
        private AudioManager sound;
        private static bool restarting;
        private BuildingBackdrop buildings;
        private float[] zoneStarts;

        private void Awake()
        {
            Application.targetFrameRate = 60;
            QualitySettings.vSyncCount = 0;
            Physics2D.gravity = new Vector2(0, -9.81f);
            Time.timeScale = 1;
            State = SceneManager.GetActiveScene().name == "Gameplay" ? GameState.Running : GameState.Menu;
            scores.Initialize();
            sound = Instantiate(audioPrefab);
            sound.Initialize(this);
            player.Initialize(this);
            cameraFollow.Initialize(this, player.transform);
            platforms.Initialize(player, cameraFollow);
            zoneStarts = platforms.ZoneStarts;
            sound.Bind(player);
            world.SetActive(IsRunning);
            ui.Initialize(this, sound);
            buildings = background.GetComponent<BuildingBackdrop>();
            buildings.Initialize(this);
        }

        private void Start()
        {
            if (IsRunning) sound.TryPlayVoice(restarting ? AudioManager.VoiceCue.Restart : AudioManager.VoiceCue.Start);
            restarting = false;
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape)) TogglePause();
            if (State == GameState.GameOver && Input.GetKeyDown(KeyCode.R)) Restart();
            if ((Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt)) && Input.GetKeyDown(KeyCode.Return))
                Screen.fullScreen = !Screen.fullScreen;
        }

        private void LateUpdate()
        {
            if (buildings != null) buildings.Refresh();
        }

        public void TogglePause()
        {
            if (State == GameState.Running) ChangeState(GameState.Paused);
            else if (State == GameState.Paused) ChangeState(GameState.Running);
        }

        public void EndRun()
        {
            if (!IsRunning) return;
            scores.EndCombo();
            scores.SaveBest();
            ChangeState(GameState.GameOver);
        }

        private void ChangeState(GameState state)
        {
            State = state;
            Time.timeScale = state == GameState.Paused || state == GameState.GameOver ? 0 : 1;
            StateChanged?.Invoke(state);
        }

        public void Restart()
        {
            restarting = State != GameState.Menu;
            LoadScene("Gameplay");
        }

        public void MainMenu() { restarting = false; LoadScene("MainMenu"); }

        private void LoadScene(string scene)
        {
            scores.SaveBest();
            Time.timeScale = 1;
            SceneManager.LoadScene(scene);
        }

        private void OnApplicationFocus(bool focus) { if (!focus && IsRunning) TogglePause(); }
        private void OnApplicationQuit() { if (scores != null) scores.SaveBest(); }
        private void OnDestroy() { Time.timeScale = 1; }
    }
}
