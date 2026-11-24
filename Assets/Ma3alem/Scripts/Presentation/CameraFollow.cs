using UnityEngine;

namespace Ma3alem
{
    [DefaultExecutionOrder(-50), RequireComponent(typeof(Camera))]
    public sealed class CameraFollow : MonoBehaviour
    {
        [SerializeField] private float cameraSize = 5.65f;
        [SerializeField] private float playfieldHalfWidth = 4.276f;
        [SerializeField] private float playfieldCenterX = -0.397f;
        public float PlayfieldCenterX => playfieldCenterX;
        public float LeftBoundary => playfieldCenterX - playfieldHalfWidth;
        public float RightBoundary => playfieldCenterX + playfieldHalfWidth;
        public float PlayfieldHalfWidth => playfieldHalfWidth;
        [SerializeField] private float graceSeconds = 7;
        [SerializeField] private float initialRiseSpeed = 0.38f;
        [SerializeField] private float maximumRiseSpeed = 1.35f;
        [SerializeField] private float followSmoothTime = 0.18f;
        [SerializeField] private float trackingLead = 1.5f;
        [SerializeField] private float maximumFollowSpeed = 18;
        public float DeathY => transform.position.y - cameraSize - 0.8f;
        public float TopY => transform.position.y + cameraSize;
        public float Danger => Mathf.Clamp01(1 - (player.position.y - DeathY) / 4.5f);
        public float RiseSpeed { get; private set; }
        public float ActualPressureSpeed => pressureSpeed;
        private GameManager game;
        private Transform player;
        private float elapsed;
        private float riseFloor = 4.5f;
        private float trackingVelocity;
        private float pressureSpeed;
        public void Initialize(GameManager gameManager, Transform target)
        {
            game = gameManager; player = target;
            GetComponent<Camera>().orthographicSize = cameraSize;
            pressureSpeed = initialRiseSpeed;
        }
        private void LateUpdate()
        {
            if (!game.IsRunning) return;
            elapsed += Time.deltaTime;
            float difficulty = Mathf.Clamp01((game.CurrentFloor - 1f) / (game.FinalZoneFloor - 1f));
            RiseSpeed = Mathf.Lerp(initialRiseSpeed, maximumRiseSpeed, difficulty);
            pressureSpeed = Mathf.MoveTowards(pressureSpeed, RiseSpeed, Time.deltaTime * 0.16f);
            if (elapsed > graceSeconds)
                riseFloor = Mathf.Max(riseFloor, transform.position.y) + pressureSpeed * Time.deltaTime;
            float y = Mathf.Max(transform.position.y, Mathf.Max(riseFloor, player.position.y + trackingLead));
            y = Mathf.Max(riseFloor, Mathf.Max(transform.position.y, Mathf.SmoothDamp(transform.position.y, y, ref trackingVelocity, followSmoothTime, maximumFollowSpeed, Time.deltaTime)));
            transform.position = new Vector3(0, y, -10);
            game.Scores.Observe(player.position.y - 0.7f);
            if (player.position.y < DeathY) game.EndRun();
        }
    }
}
