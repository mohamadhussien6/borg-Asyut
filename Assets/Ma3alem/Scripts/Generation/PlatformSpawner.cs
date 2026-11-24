using System;
using System.Collections.Generic;
using UnityEngine;

namespace Ma3alem
{
    public sealed class PlatformSpawner : MonoBehaviour
    {
        [SerializeField] private GameObject platformPrefab;
        [SerializeField] private int seed = 81473;
        [SerializeField] private float halfWidth = 6.65f;
        [SerializeField] private int[] zoneFloorStarts = { 0, 21, 46, 76 };
        private float[] zoneStarts;
        private Route guide;
        private readonly List<float> floorHeights = new List<float>();
        public int FinalZoneFloor => zoneFloorStarts[3];
        [SerializeField] private Sprite[] zoneSprites;
        [SerializeField] private Difficulty difficulty = new Difficulty();
        [Serializable] public sealed class Difficulty
        {
            public float firstWidth = 4f, lastWidth = 2.2f;
            public float firstGap = 1.12f, lastGap = 1.65f, gapVariation = 0.35f;
            public float zone1Width = 1.20f, zone2Width = 1.18f, zone3Width = 1.125f, zone4Width = 1.10f;
        }
        public float[] ZoneStarts => (float[])zoneStarts.Clone();
        private readonly Queue<GameObject> active = new Queue<GameObject>();
        private readonly Stack<GameObject> pool = new Stack<GameObject>();
        private readonly Dictionary<Collider2D, int> floorByCollider = new Dictionary<Collider2D, int>(28);
        public bool TryGetFloor(Collider2D collider, out int floor)
        { floor = 0; return collider != null && floorByCollider.TryGetValue(collider, out floor); }
        private CameraFollow follow;
        private Route route;
        public int ActiveCount => active.Count;
        public int CreatedCount { get; private set; }
        public void Initialize(PlayerController player, CameraFollow cameraFollow)
        {
            follow = cameraFollow;
            halfWidth = follow.PlayfieldHalfWidth;
            float runway = player.MaxSpeed * player.MaxSpeed / (2 * player.GroundAcceleration);
            float minimumWidth = difficulty.lastWidth * Math.Min(Math.Min(difficulty.zone1Width, difficulty.zone2Width), Math.Min(difficulty.zone3Width, difficulty.zone4Width));
            if (minimumWidth < runway + 0.95f) throw new InvalidOperationException("The narrowest platform needs body clearance and a full-speed acceleration runway.");
            float[] starts = Array.ConvertAll(zoneFloorStarts, value => (float)value);
            route = new Route(seed, player.JumpSpeed, player.Gravity, player.MaxSpeed, halfWidth, starts, difficulty, player.AirAcceleration, player.FallingGravityMultiplier);
            guide = new Route(seed, player.JumpSpeed, player.Gravity, player.MaxSpeed, halfWidth, starts, difficulty, player.AirAcceleration, player.FallingGravityMultiplier);
            floorHeights.Add(0);
            while (guide.Previous.Floor < FinalZoneFloor) floorHeights.Add(guide.Next().Y);
            zoneStarts = new[] { 0f, floorHeights[zoneFloorStarts[1]], floorHeights[zoneFloorStarts[2]], floorHeights[zoneFloorStarts[3]] };
            for (int i = 0; i < 28; i++)
            {
                var platform = Instantiate(platformPrefab, transform);
                platform.SetActive(false); pool.Push(platform); CreatedCount++;
            }
            Spawn(new Placement(0, 0, Mathf.Min(8, halfWidth * 2 - 0.4f)));
            Fill();
        }
        private void Update()
        {
            while (active.Count > 0 && active.Peek().transform.position.y < follow.DeathY - 2)
            {
                var platform = active.Dequeue(); platform.SetActive(false); pool.Push(platform);
            }
            Fill();
        }
        private void Fill()
        {
            while (route.Previous.Y < follow.TopY + 5)
            {
                var next = route.Next();
                if (next.Y >= follow.DeathY - 2) Spawn(next);
            }
        }
        private void Spawn(Placement placement)
        {
            if (Mathf.Abs(placement.X) + placement.Width / 2 > halfWidth - 0.15f + 0.001f)
                throw new InvalidOperationException("Generated platform exceeds the dedicated playfield walls.");
            if (pool.Count == 0) throw new InvalidOperationException("Platform pool exhausted; check generation bounds.");
            var platform = pool.Pop();
            platform.transform.position = new Vector3(placement.X + follow.PlayfieldCenterX, placement.Y - 0.18f, 0);
            platform.transform.localScale = Vector3.one;
            var renderer = platform.GetComponent<SpriteRenderer>();
            int zone = placement.Floor >= zoneFloorStarts[3] ? 3 : placement.Floor >= zoneFloorStarts[2] ? 2 : placement.Floor >= zoneFloorStarts[1] ? 1 : 0;
            renderer.sprite = zoneSprites[zone];
            renderer.size = new Vector2(placement.Width, 0.36f);
            platform.GetComponent<BoxCollider2D>().size = new Vector2(placement.Width, 0.36f);
            floorByCollider[platform.GetComponent<BoxCollider2D>()] = placement.Floor;
            platform.SetActive(true); active.Enqueue(platform);
        }
        public int FloorAtHeight(float height)
        {
            while (guide.Previous.Y < height) floorHeights.Add(guide.Next().Y);
            int index = floorHeights.BinarySearch(height);
            return index >= 0 ? index : Math.Max(0, ~index - 1);
        }
        public readonly struct Placement
        {
            public readonly float X, Y, Width;
            public readonly int Floor;
            public Placement(float x, float y, float width, int floor = 0) { X = x; Y = y; Width = width; Floor = floor; }
        }
        public sealed class Route
        {
            private readonly System.Random random;
            private readonly float jumpSpeed, gravity, maxSpeed, halfWidth;
            private readonly float[] starts;
            private readonly Difficulty tuning;
            private readonly float airAcceleration, fallingMultiplier;
            private float lastShift;
            public int RejectedCandidates { get; private set; }
            public Placement Previous { get; private set; }
            public Route(int seed, float jumpSpeed, float gravity, float maxSpeed, float halfWidth, float[] zoneStarts = null, Difficulty difficulty = null, float airAcceleration = 16, float fallingMultiplier = 1.15f)
            {
                if (jumpSpeed <= 0 || gravity <= 0 || maxSpeed <= 0 || halfWidth < 3.8f)
                    throw new ArgumentOutOfRangeException(nameof(jumpSpeed));
                tuning = difficulty ?? new Difficulty();
                if (airAcceleration <= 0 || fallingMultiplier < 1) throw new ArgumentOutOfRangeException(nameof(airAcceleration));
                this.airAcceleration = airAcceleration; this.fallingMultiplier = fallingMultiplier;
                if (tuning.firstWidth <= 0 || tuning.lastWidth <= 0 || Math.Max(tuning.firstWidth, tuning.lastWidth) > halfWidth * 2 - 0.3f || tuning.firstGap <= 0 || tuning.lastGap <= 0 || tuning.gapVariation < 0)
                    throw new ArgumentException("Positive platform sizes/gaps and wall clearance are required.");
                if (tuning.zone1Width <= 0 || tuning.zone2Width <= 0 || tuning.zone3Width <= 0 || tuning.zone4Width <= 0)
                    throw new ArgumentException("Positive zone width factors are required.");
                random = new System.Random(seed);
                this.jumpSpeed = jumpSpeed; this.gravity = gravity; this.maxSpeed = maxSpeed; this.halfWidth = halfWidth;
                starts = zoneStarts == null ? new[] { 0f, 21f, 46f, 76f } : (float[])zoneStarts.Clone();
                if (starts.Length != 4 || starts[0] != 0 || starts[1] <= 0 || starts[2] <= starts[1] || starts[3] <= starts[2])
                    throw new ArgumentException("Four increasing zone starts are required.");
                Previous = new Placement(0, 0, 3.8f);
            }
            public Placement Next()
            {
                int floor = Previous.Floor + 1;
                float difficulty = Math.Min(1, Math.Max(0, (floor - 1f) / (starts[3] - 1f)));
                float apex = jumpSpeed * jumpSpeed / (2 * gravity);
                float gap = Math.Min(apex * 0.72f, tuning.firstGap + (tuning.lastGap - tuning.firstGap) * difficulty + tuning.gapVariation * (float)random.NextDouble());
                float baseline = tuning.firstWidth + (tuning.lastWidth - tuning.firstWidth) * difficulty;
                float width = Math.Max(tuning.lastWidth, Math.Min(tuning.firstWidth + 0.6f,
                    baseline * (0.82f + (float)random.NextDouble() * 0.36f) + (floor % 6 == 0 ? 0.65f : 0)));
                float zoneWidth = floor >= starts[3] ? tuning.zone4Width : floor >= starts[2] ? tuning.zone3Width
                    : floor >= starts[1] ? tuning.zone2Width : tuning.zone1Width;
                width = Math.Min(halfWidth * 2 - 0.8f, width * zoneWidth);
                // A rest-start jump can accelerate in air. Use the shorter, faster-fall flight,
                // then retain 30% travel reserve and half a body width for a central landing.
                double flight = jumpSpeed / gravity + Math.Sqrt(2 * (apex - gap) / (gravity * fallingMultiplier));
                double accelerating = Math.Min(flight, maxSpeed / airAcceleration);
                double travel = 0.5 * airAcceleration * accelerating * accelerating + maxSpeed * (flight - accelerating);
                float reach = Math.Max(0, (float)(travel * 0.7) - 0.55f);
                float limit = halfWidth - width / 2 - 0.17f;
                float x = Previous.X;
                bool accepted = false;
                for (int attempt = 0; attempt < 8; attempt++)
                {
                    int style = random.Next(5);
                    float jitter = ((float)random.NextDouble() * 2 - 1) * 0.6f;
                    // Bias alternating anchors, interrupted by broad runway floors and varied central routes.
                    float alternatingSide = Previous.X > 0.2f ? -1 : Previous.X < -0.2f ? 1 : (floor % 2 == 0 ? 1 : -1);
                    float desired;
                    if (floor % 6 == 0) desired = jitter;
                    else if (style < 3) desired = alternatingSide * limit * (0.6f + (float)random.NextDouble() * 0.4f) + jitter * 0.3f;
                    else if (style == 3) desired = -Previous.X + jitter;
                    else desired = ((float)random.NextDouble() * 2 - 1) * limit;
                    float shift = desired - Previous.X;
                    // Reject distant anchors instead of clamping every one into an identical staircase step.
                    if (Math.Abs(desired) > limit || Math.Abs(shift) > reach ||
                        (floor % 7 != 0 && (Math.Abs(shift) < 0.3f || Math.Abs(shift - lastShift) < 0.12f)))
                    { RejectedCandidates++; continue; }
                    x = desired; accepted = true; break;
                }
                if (!accepted)
                    x = Math.Max(-limit, Math.Min(limit, Previous.X + ((float)random.NextDouble() * 2 - 1) * reach));
                lastShift = x - Previous.X;
                Previous = new Placement(x, Previous.Y + gap, width, floor);
                return Previous;
            }
        }
    }
}
