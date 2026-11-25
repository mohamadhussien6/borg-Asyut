# Practical discussion — actual implementation

This guide describes the original, working project source on the main branch, not the untested simplified rewrite proposals. Source baseline: `2012628977486f016cef00e3daae02c52ee2efaf`. Only documentation was edited for this review; no new Unity test or Windows build was run. The existing release and its historical verification results remain unchanged.

## How did we implement each feature?

| Feature | Actual implementation / how to explain it | Where to open |
|---|---|---|
| Input and running | Read A/D or arrow keys in Update. FixedUpdate moves the Rigidbody2D horizontal velocity toward the target with separate ground/air acceleration and braking. | PlayerController.ReadInput, FixedUpdate |
| Jump | Calculate a speed ratio between 0 and 1; increase the vertical launch velocity using the momentum formula. Releasing Space early multiplies upward velocity by 0.48. | PlayerController.Jump, FixedUpdate |
| Ground and jump timing | Cast the player's BoxCollider2D downward and check the upward contact normal. JumpTiming stores the last ground and press times and consumes a jump once. | PlayerController.FindGround, JumpTiming |
| Wall bounce | Read the wall contact normal and the saved incoming horizontal velocity, then set the outgoing horizontal velocity away from the wall with 96% retention. | PlayerController.ResolveWallContact |
| Somersaults and character animation | Select SpriteRenderer frames in code; rotate only the visual child by 360 or 720 degrees based on momentum. The physics body's rotation stays frozen. | PlayerController.Jump, UpdatePose, SetPose |
| Landing dust and fear | Generate and reuse four dust sprites, animate their positions and opacity, and detect falling danger from vertical speed and distance from peak height. | PlayerController.Initialize, FixedUpdate, UpdateFear, UpdatePose |
| Platforms | Instantiate 28 platform objects once; inactive objects are stored in a Stack, active ones in a Queue. Recycle old ones as the camera climbs. | PlatformSpawner.Initialize, Update, Fill, Spawn |
| Random route and difficulty | A seeded System.Random produces varied gaps, widths and positions. Route.Next limits gap and horizontal reach using the jump and acceleration parameters. | PlatformSpawner.Route.Next, Difficulty.WidthFactor |
| Floor and stage | A List stores generated heights; BinarySearch finds the floor at a height. A Dictionary maps colliders to floors. Zone starts are 0, 26, 56, 91. | PlatformSpawner.FloorAtHeight, TryGetFloor, Spawn; GameManager.ZoneAt |
| Score and combo | Track maximum height; score is height points plus combo bonus. Qualifying jumps advance at least two floors beyond the previous high landing and earn bonuses. A chain expires after three seconds. | ScoreManager.HeightTracker, FloorCombo |
| Best score | Read and write the saved high score using PlayerPrefs; preserve the legacy height record too. | ScoreManager.Initialize, SaveBest |
| Camera and danger | Camera follows upwards with SmoothDamp; after a seven-second grace period it adds rising pressure, increasing with floor progress. Death occurs below DeathY; Danger is a clamped proximity value. | CameraFollow.LateUpdate, DeathY, Danger |
| Pause, restart and menus | GameState enum selects Menu, Running, Paused or GameOver. Set Time.timeScale to zero for pause/game over; reload scenes using SceneManager. | GameManager.TogglePause, ChangeState, Restart, LoadScene |
| UI and Arabic | Create Canvas, panels, labels and buttons in C# at runtime. TextMeshPro renders text after RTLTMPro shaping. State and score events update the UI. | UIManager.Initialize, SetText, Button, Show, Hud |
| Shisha danger meter | Set Image.fillAmount from the smoothed danger value; update marker, colors, labels and procedural smoke sprites in code. | UIManager.CreateDangerMeter, Update |
| Music and voice | AudioSource plays clips; a persistent music object survives scene changes. VoicePolicy uses priorities and cooldowns, and ChooseTake avoids immediate repetition when alternatives exist. | AudioManager.InitializeMusic, TryPlayVoice, ChooseTake, VoicePolicy |
| Buildings and inscriptions | Three decorative sprite panels follow the camera and are scaled to cover the viewport. Wall phrases are baked into the background artwork, not floating UI. | BuildingBackdrop.Initialize, Refresh; existing environment assets |

## Important source excerpts (not replacement scripts)

### Jump — PlayerController.Jump

```csharp
float momentum = Mathf.Clamp01(Mathf.Abs(Body.linearVelocity.x) / maxSpeed);
float launch = jumpSpeed + momentumJumpBoost * Mathf.Pow(momentum, 1.4f);
```

بالمصري: بناخد القيمة المطلقة للسرعة الأفقية ونقسمها على أقصى سرعة عشان نطلع نسبة من صفر لواحد. بنستخدم النسبة في زيادة سرعة القفز. في نفس الدالة الكود بيحط launch في السرعة الرأسية. لو زودنا jumpSpeed القفزة الأساسية هتعلى.

### Wall bounce — PlayerController.ResolveWallContact

```csharp
float outgoing = normal * Mathf.Abs(incoming) * wallMomentumRetention;
Body.linearVelocity = new Vector2(outgoing, Body.linearVelocity.y);
```

بالمصري: normal بيحدد الاتجاه بعيد عن الحيطة، وبنحتفظ بنسبة من سرعة الدخول (0.96 في الإعداد الحالي). بنغير السرعة الأفقية بس، والرأسية بتفضل زي ما هي.

### Combo — ScoreManager.FloorCombo.Land

```csharp
Count++; chainDeadline = now + 3;
Bonus += (advanced - 1) * 15 * Math.Min(Count, 5);
```

بالمصري: بعد قفزة مؤهلة بنزود العداد ونحدد مهلة 3 ثواني. عدد الأدوار المتخطاة بيضرب في 15 وفي معامل الكومبو بحد أقصى 5. فيه شروط قبل السطرين تمنع احتساب الأدوار القديمة.

### Pause — GameManager.ChangeState

```csharp
State = state;
Time.timeScale = state == GameState.Paused || state == GameState.GameOver ? 0 : 1;
StateChanged?.Invoke(state);
```

بالمصري: بنغير حالة اللعبة. لو إيقاف أو خسارة بنوقف الزمن الخاص باللعب. بنبلغ المشتركين في الحدث عشان الواجهة والصوت يتحدثوا.

### Danger — CameraFollow

```csharp
public float DeathY => transform.position.y - cameraSize - 0.8f;
public float Danger => Mathf.Clamp01(1 - (player.position.y - DeathY) / 4.5f);
```

بالمصري: حد الخسارة تحت الكاميرا، وكل ما اللاعب يقرب منه نسبة الخطر تعلى. UIManager بيستخدم النسبة لتحريك مؤشر الشيشة.

## Source links

- [PlayerController.cs](../../Assets/Ma3alem/Scripts/Gameplay/PlayerController.cs)
- [PlatformSpawner.cs](../../Assets/Ma3alem/Scripts/Generation/PlatformSpawner.cs)
- [GameManager.cs](../../Assets/Ma3alem/Scripts/Core/GameManager.cs)
- [ScoreManager.cs](../../Assets/Ma3alem/Scripts/Core/ScoreManager.cs)
- [CameraFollow.cs](../../Assets/Ma3alem/Scripts/Presentation/CameraFollow.cs)
- [UIManager.cs](../../Assets/Ma3alem/Scripts/Presentation/UIManager.cs)
- [AudioManager.cs](../../Assets/Ma3alem/Scripts/Audio/AudioManager.cs)
- [BuildingBackdrop.cs](../../Assets/Ma3alem/Scripts/Presentation/BuildingBackdrop.cs)

## Do not confuse proposed and actual implementation

The current game does not use a fixed 1.5-unit platform gap, a preplaced circular array of 28 platforms, Animator-controlled somersaults, a ParticleSystem for its procedural landing dust, or Inspector-authored gameplay menus as described in the separate simplified-code proposals. Those proposals were not merged or tested. The actual third stage name is **سوبر لوكس**, as in UIManager.ZoneNames. Do not rename it in documentation alone. Any future implementation change requires updating documentation from the accepted tested source.
