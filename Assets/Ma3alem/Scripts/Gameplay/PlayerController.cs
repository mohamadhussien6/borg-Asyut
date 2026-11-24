using System;
using UnityEngine;

namespace Ma3alem
{
    [RequireComponent(typeof(Rigidbody2D), typeof(BoxCollider2D))]
    public sealed class PlayerController : MonoBehaviour
    {
        [Header("Movement: units / seconds")]
        [SerializeField] private float maxSpeed = 9.5f;
        [SerializeField] private float acceleration = 32;
        [SerializeField] private float braking = 18;
        [SerializeField] private float reversalAcceleration = 38;
        [SerializeField] private float jumpSpeed = 12;
        [SerializeField] private float gravityScale = 3;
        [SerializeField] private float fallingGravityMultiplier = 1.15f;
        [SerializeField] private float terminalFallSpeed = 24;
        [SerializeField, Range(1, 1.3f)] private float visualScale = 1.25f;
        [SerializeField] private float jumpCut = 0.48f;
        [SerializeField] private float coyoteTime = 0.12f;
        [SerializeField] private float jumpBuffer = 0.14f;
        [SerializeField] private float momentumJumpBoost = 13;
        [SerializeField] private float airAcceleration = 22;
        [SerializeField] private float airBraking = 0.12f;
        [SerializeField] private float flipDegreesPerSecond = 720;
        [SerializeField] private float flipSpeedThreshold = 0.45f;
        [SerializeField] private float doubleFlipSpeedThreshold = 0.85f;
        [SerializeField] private float animationFramesPerSecond = 10;
        [SerializeField, Min(0.01f)] private float flipEaseInSeconds = 0.06f;
        [SerializeField, Range(0.8f, 1f)] private float wallMomentumRetention = 0.96f;
        [SerializeField, Range(0.04f, 0.2f)] private float reboundSteeringGrace = 0.10f;
        [Header("Cartoon poses")]
        [SerializeField] private SpriteRenderer visual;
        [SerializeField] private Sprite[] idleFrames;
        [SerializeField] private Sprite[] runFrames;
        [SerializeField] private Sprite[] jumpFrames;
        [SerializeField] private Sprite[] fallFrames;
        [SerializeField] private Sprite[] scaredFrames;
        [SerializeField] private Sprite[] flipFrames;
        [SerializeField] private Sprite[] landingFrames;
        [SerializeField] private Sprite[] gameOverFrames;
        public event Action<float, float> WallBounced;
        public event Action Jumped;
        public event Action Landed;
        public event Action Scared;
        public event Action Falling;
        public event Action HighJumped;
        public event Action DangerousLanding;
        public float FlipDegrees { get; private set; }
        public bool IsFlipping => flipTarget > 0 && FlipDegrees < flipTarget;
        public Rigidbody2D Body { get; private set; }
        public bool Grounded { get; private set; }
        public float JumpSpeed => jumpSpeed;
        public float MaxSpeed => maxSpeed;
        public float Gravity => Mathf.Abs(Physics2D.gravity.y) * gravityScale;
        public float AirAcceleration => airAcceleration;
        public float GroundAcceleration => acceleration;
        public float FallingGravityMultiplier => fallingGravityMultiplier;
        public bool ReadKeyboard { get; set; } = true;
        private readonly JumpTiming timing = new JumpTiming();
        private readonly RaycastHit2D[] groundHits = new RaycastHit2D[8];
        private BoxCollider2D box;
        private GameManager game;
        private ContactFilter2D groundFilter;
        private float movement;
        private bool released;
        private float takeoffHeight;
        private float peakHeight;
        private bool scaredThisFall;
        private bool calledForHelp;
        private float landingUntil;
        private float flipTarget;
        private float flipDirection;
        private Collider2D groundCollider;
        private int groundedFloor;
        private Vector3 artworkScale;
        private Vector3 visualBasePosition;
        private float jumpedAt;
        private float requestedHorizontalVelocity;
        private float reboundUntil;
        private float reboundDirection;
        private float lastBounceAt = -10;
        private float activeFlipRate;
        private float landingImpact;
        private float previousFallSpeed;
        private readonly SpriteRenderer[] dust = new SpriteRenderer[4];
        private float dustStarted = -10;
        private Sprite dustSprite;
        private Texture2D dustTexture;

        public void Initialize(GameManager gameManager)
        {
            game = gameManager;
            game.StateChanged += OnGameStateChanged;
            Body = GetComponent<Rigidbody2D>();
            box = GetComponent<BoxCollider2D>();
            Body.gravityScale = gravityScale;
            Body.freezeRotation = true;
            Body.interpolation = RigidbodyInterpolation2D.Interpolate;
            Body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            groundFilter = new ContactFilter2D { useLayerMask = true, layerMask = 1 << 6, useTriggers = false };
            artworkScale = visual.transform.localScale * visualScale;
            visual.transform.localScale = artworkScale;
            // Feet remain aligned with the unchanged collider while the artwork grows upward.
            visual.transform.localPosition += Vector3.up * (visualScale - 1) * 0.56f;
            visualBasePosition = visual.transform.localPosition;
            dustTexture = new Texture2D(8, 8, TextureFormat.RGBA32, false);
            for (int y = 0; y < 8; y++) for (int x = 0; x < 8; x++)
                dustTexture.SetPixel(x, y, new Color(1, 1, 1, Mathf.Clamp01(1 - Vector2.Distance(new Vector2(x, y), new Vector2(3.5f, 3.5f)) / 3.5f)));
            dustTexture.Apply(false, true);
            dustSprite = Sprite.Create(dustTexture, new Rect(0, 0, 8, 8), Vector2.one * 0.5f, 32);
            for (int i = 0; i < dust.Length; i++)
            {
                var puff = new GameObject("Reused landing dust", typeof(SpriteRenderer)); puff.transform.SetParent(transform.parent);
                dust[i] = puff.GetComponent<SpriteRenderer>(); dust[i].sprite = dustSprite; dust[i].sortingOrder = 6; dust[i].enabled = false;
            }
            ResetAirborne();
        }

        private void Update()
        {
            if (!game.IsRunning)
            {
                movement = 0; released = false;
                if (game.State == GameState.GameOver) { visual.transform.localRotation = Quaternion.identity; SetPose(gameOverFrames); }
                return;
            }
            if (ReadKeyboard) ReadInput();
            UpdateFear();
            UpdatePose();
        }

        private void OnGameStateChanged(GameState state)
        {
            if (state == GameState.Running) return;
            movement = 0; released = false;
            timing.CancelPress();
        }

        private void ReadInput()
        {
            SetMove((Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow) ? 1 : 0)
                - (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow) ? 1 : 0));
            if (Input.GetKeyDown(KeyCode.Space)) PressJump();
            if (Input.GetKeyUp(KeyCode.Space)) ReleaseJump();
        }

        public void SetMove(float direction) { movement = Mathf.Clamp(direction, -1, 1); }
        public void PressJump() { if (game.IsRunning) timing.Press(Time.timeAsDouble); }
        public void ReleaseJump() { if (game.IsRunning) released = true; }

        private void FixedUpdate()
        {
            if (!game.IsRunning) return;
            Body.gravityScale = gravityScale * (Body.linearVelocity.y < 0 ? fallingGravityMultiplier : 1);
            bool wasGrounded = Grounded;
            Grounded = FindGround();
            if (Grounded) timing.Ground(Time.fixedTimeAsDouble);
            if (Grounded && game.TryGetFloor(groundCollider, out int floor)) groundedFloor = floor;
            if (wasGrounded && !Grounded && Body.linearVelocity.y <= 0) { ResetAirborne(); game.Scores.EndCombo(); }
            if (Grounded && !wasGrounded)
            {
                bool survived = scaredThisFall || calledForHelp;
                landingImpact = Mathf.Clamp01(previousFallSpeed / terminalFallSpeed);
                if (previousFallSpeed > 9)
                {
                    dustStarted = Time.time;
                    for (int i = 0; i < dust.Length; i++) dust[i].transform.position = new Vector3(Body.position.x + (i - 1.5f) * 0.12f, box.bounds.min.y + 0.03f, 0);
                }
                game.Scores.LandOnFloor(groundedFloor);
                ResetAirborne(); flipTarget = 0;
                landingUntil = Time.time + 0.16f; Landed?.Invoke();
                if (survived) DangerousLanding?.Invoke();
            }
            float rate = Grounded ? (movement == 0 ? braking : acceleration) : (movement == 0 ? airBraking : airAcceleration);
            if (Grounded && movement * Body.linearVelocity.x < 0) rate = reversalAcceleration;
            float x = Mathf.MoveTowards(Body.linearVelocity.x, movement * maxSpeed, rate * Time.fixedDeltaTime);
            // Preserve the rebound briefly against input back into the same wall; steering away remains immediate.
            if (Time.fixedTime < reboundUntil && movement * reboundDirection <= 0) x = Body.linearVelocity.x;
            requestedHorizontalVelocity = x;
            Body.linearVelocity = new Vector2(x, Mathf.Max(-terminalFallSpeed, Body.linearVelocity.y));
            if (timing.Consume(Time.fixedTimeAsDouble, coyoteTime, jumpBuffer)) Jump();
            if (released && Body.linearVelocity.y > 0)
                Body.linearVelocity = new Vector2(x, Body.linearVelocity.y * jumpCut);
            released = false;
            previousFallSpeed = Mathf.Max(0, -Body.linearVelocity.y);
        }

        private bool FindGround()
        {
            if (Body.linearVelocity.y > 0.05f) return false;
            int count = box.Cast(Vector2.down, groundFilter, groundHits, 0.065f);
            for (int i = 0; i < count; i++)
                if (groundHits[i].normal.y > 0.65f && groundHits[i].point.y <= box.bounds.min.y + 0.08f)
                { groundCollider = groundHits[i].collider; return true; }
            return false;
        }

        private void Jump()
        {
            float momentum = Mathf.Clamp01(Mathf.Abs(Body.linearVelocity.x) / maxSpeed);
            float launch = jumpSpeed + momentumJumpBoost * Mathf.Pow(momentum, 1.4f);
            jumpedAt = Time.time;
            game.Scores.BeginJump(groundedFloor);
            Body.linearVelocity = new Vector2(Body.linearVelocity.x, launch);
            Grounded = false;
            ResetAirborne();
            FlipDegrees = 0; flipTarget = momentum >= flipSpeedThreshold ? (momentum >= doubleFlipSpeedThreshold ? 720 : 360) : 0;
            flipDirection = Body.linearVelocity.x >= 0 ? -1 : 1;
            float flightSeconds = launch / Gravity * (1 + 1 / Mathf.Sqrt(fallingGravityMultiplier));
            activeFlipRate = flipTarget == 0 ? 0 : Mathf.Min(flipDegreesPerSecond, flipTarget / Mathf.Max(0.45f, flightSeconds * 0.72f));
            Jumped?.Invoke();
            if (flipTarget > 0) HighJumped?.Invoke();
        }

        private void OnCollisionEnter2D(Collision2D collision) { ResolveWallContact(collision); }
        private void OnCollisionStay2D(Collision2D collision) { ResolveWallContact(collision); }
        private void ResolveWallContact(Collision2D collision)
        {
            if (collision.gameObject.layer != 8 || !game.IsRunning) return;
            for (int i = 0; i < collision.contactCount; i++)
            {
                float normal = collision.GetContact(i).normal.x;
                if (Mathf.Abs(normal) < 0.8f) continue;
                // Use the pre-solver velocity: Unity has already removed the incoming normal velocity here.
                float incoming = requestedHorizontalVelocity;
                if (incoming * normal >= -0.01f || Time.fixedTime - lastBounceAt < 0.06f) return;
                float outgoing = normal * Mathf.Abs(incoming) * wallMomentumRetention;
                Body.linearVelocity = new Vector2(outgoing, Body.linearVelocity.y);
                requestedHorizontalVelocity = outgoing;
                reboundDirection = normal; reboundUntil = Time.fixedTime + reboundSteeringGrace;
                lastBounceAt = Time.fixedTime;
                WallBounced?.Invoke(incoming, outgoing);
                return;
            }
        }

        private void ResetAirborne()
        {
            takeoffHeight = peakHeight = transform.position.y;
            scaredThisFall = false;
            calledForHelp = false;
        }

        private void UpdateFear()
        {
            peakHeight = Mathf.Max(peakHeight, transform.position.y);
            bool danger = !Grounded && Body.linearVelocity.y < -4 && peakHeight - transform.position.y > 2.8f;
            if (danger && !scaredThisFall) { scaredThisFall = true; Scared?.Invoke(); }
            if (danger && !calledForHelp && peakHeight - transform.position.y >= 3.2f)
            { calledForHelp = true; Falling?.Invoke(); }
        }

        private void UpdatePose()
        {
            float speed = Body.linearVelocity.x;
            if (Mathf.Abs(speed) > 0.1f) visual.flipX = speed < 0;
            if (!Grounded && IsFlipping)
            {
                float ease = Mathf.Lerp(0.4f, 1, Mathf.Clamp01((Time.time - jumpedAt) / flipEaseInSeconds));
                FlipDegrees = Mathf.Min(flipTarget, FlipDegrees + Time.deltaTime * activeFlipRate * ease);
                visual.transform.localRotation = Quaternion.Euler(0, 0, flipDirection * FlipDegrees);
            }
            Sprite[] frames = IsFlipping ? flipFrames : scaredThisFall && !Grounded ? scaredFrames
                : !Grounded ? (Body.linearVelocity.y > 0 ? jumpFrames : fallFrames)
                : Time.time < landingUntil ? landingFrames : Mathf.Abs(speed) > 0.4f ? runFrames : idleFrames;
            SetPose(frames);
            if (Grounded) visual.transform.localRotation = Quaternion.RotateTowards(visual.transform.localRotation, Quaternion.identity, Time.deltaTime * 1800);
            float compression = Grounded ? Mathf.Clamp01((landingUntil - Time.time) / 0.16f) * landingImpact * 0.09f : 0;
            visual.transform.localScale = Vector3.Scale(artworkScale, new Vector3(1 + compression, 1 - compression, 1));
            visual.transform.localPosition = visualBasePosition + Vector3.down * (compression * artworkScale.y * 0.56f);
            float age = Time.time - dustStarted;
            for (int i = 0; i < dust.Length; i++)
            {
                dust[i].enabled = age < 0.22f;
                if (age >= 0.22f) continue;
                dust[i].transform.position += new Vector3((i - 1.5f) * Time.deltaTime, Time.deltaTime * 0.3f, 0);
                dust[i].color = new Color(0.79f, 0.70f, 0.55f, (1 - age / 0.22f) * 0.55f);
            }
        }

        private void OnDestroy()
        {
            if (game != null) game.StateChanged -= OnGameStateChanged;
            foreach (var puff in dust) if (puff != null) Destroy(puff.gameObject);
            if (dustSprite != null) Destroy(dustSprite);
            if (dustTexture != null) Destroy(dustTexture);
        }

        private void SetPose(Sprite[] frames)
        {
            if (frames != null && frames.Length > 0) visual.sprite = frames[(int)(Time.time * animationFramesPerSecond) % frames.Length];
        }

        // Kept inside the player script so the timing rule is easy to explain and test without Unity physics.
        public sealed class JumpTiming
        {
            private double groundedAt = double.NegativeInfinity;
            private double pressedAt = double.NegativeInfinity;
            private bool spent;
            public void Press(double now) { pressedAt = now; }
            public void CancelPress() { pressedAt = double.NegativeInfinity; }
            public void Ground(double now) { groundedAt = now; spent = false; }
            public bool Consume(double now, double coyote, double buffer)
            {
                if (spent || now - groundedAt > coyote || now - pressedAt > buffer) return false;
                spent = true;
                pressedAt = double.NegativeInfinity;
                return true;
            }
        }
    }
}
