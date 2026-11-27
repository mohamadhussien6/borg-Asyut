# Borg Asyut Game Documentation 

برج أسيوط  |  icy tower Asyut

Windows x64 | Game description, assets, physics and C# implementation

## 1 Game Description

Our project is a single-player 2D arcade platform game set between Assiut-inspired residential buildings. The player controls a Saidi character wearing a galabeya, turban, shawl and balgha, and climbs four connected stages. Horizontal movement builds momentum, and running faster increases the jump launch speed. The camera follows the player and begins applying upward pressure after seven seconds. Falling below the camera's lower death boundary ends the run. The score combines the highest reached height with bonuses earned from successful floor-skipping landings.

The four stage ranges are 1–25, 26–55, 56–90 and 91+. Their platform artwork changes from chipped balcony plaster, to rooftop terrazzo and cement, to exposed red brick, and finally to a gold and blue pharaonic coffin. Stage banners appear for three gameplay seconds when a new stage begins.

| Floors | Stage name |
| --- | --- |
| 1–25 | بلكونة بنت الجيران |
| 26–55 | لحن الموزايكو |
| 56–90 | سوبر لوكس |
| 91+ | توت عنخ هموت |

Controls: A/D or arrow keys move; Space jumps and releasing it early reduces jump height; Escape pauses or resumes; R restarts after Game Over; Alt+Enter changes display mode. Menus provide replay, return to the main menu, sound controls and saved best-score display.

## 2 Assets Used

The following assets are integrated in the project. The descriptions below reflect how the eight scripts use the supplied character, platform, environment, interface and audio assets. Font and third-party notices remain with their resources.

| Asset group | Existing project content |
| --- | --- |
| Character | 18 transparent Player_*.png animation frames in Assets/Ma3alem/Art, used by the Player Animator. PlayerController sends Speed, Y, Grounded and Scared parameters, and Flip or DoubleFlip triggers to the Animator. |
| Platforms | Platform.png and Platform_Zone2.png through Platform_Zone4.png. PlatformSpawner selects one of the four sprites according to the current floor stage, then updates the SpriteRenderer size and BoxCollider2D size together. |
| Environment and interface | Environment_Assiut_Static.png contains three permanent panels with four supplied wall inscriptions. The existing logo, menu artwork, shisha indicator, smoke object and high-jump banner are retained. UIManager updates the assigned screens, banners, labels, text fields and danger Image; it does not create the Canvas or buttons at runtime. |
| Speech and sound | Nine supplied Saidi voice WAVs in Assets/Audio/VoiceLines and project sound effects in Assets/Ma3alem/Audio. AudioManager uses separate music, effects and voice AudioSources. |
| Fonts and Arabic | Lemonada and Noto font resources with SIL Open Font License notices; RTLTMPro support with its MIT license. Existing TextMeshPro resources remain included. |
| Project content | MainMenu and Gameplay scenes, player/platform/audio prefabs, PhysicsMaterial2D resources, Unity UI and Test Framework packages. The scripts use the assigned Rigidbody2D, BoxCollider2D, Animator, ParticleSystem, AudioSource, TextMeshPro and UI Image components. |

The selected soundtrack is included at Assets/Ma3alem/Audio/Music/Selected_Music.mp3, with distribution permission confirmed by the project owner. The duplicate LocalPreviewOnly directory is excluded from source packages. Original voice recordings, sound effects and third-party license notices are retained.

Promotional media includes 15 full-HD gameplay screenshots, a four-stage comparison and a 30-second 1920 x 1080 trailer at 60 FPS. The trailer ends with the original main menu for four seconds.

## 3 Type of Physics Used

The player uses a Rigidbody2D and BoxCollider2D. PlayerController reads keyboard input in Update and changes Rigidbody2D velocity in FixedUpdate. Horizontal movement uses Mathf.MoveTowards to approach the requested direction and speed. The controller uses different movement rates for air movement, grounded acceleration, stopping and reversing direction.

Ground detection uses Physics2D.BoxCast from the player's BoxCollider2D bounds, casting downward by 0.065 units with the assigned ground LayerMask. A surface counts as ground when the hit normal points sufficiently upward and the player's vertical velocity is not moving upward.

Jump timing combines coyote time and jump buffering. The player can still jump for 0.12 seconds after leaving a platform, and a Space press is remembered for 0.14 seconds. Jump strength depends on horizontal momentum:

Jump launch speed = 12 + 13 × (clamp(|horizontal speed| / 9.5, 0, 1))^1.4.

Releasing Space while the player is still moving upward multiplies vertical velocity by 0.48, creating a shorter jump. Rigidbody2D gravityScale is set to 3 while rising and 3.45 while falling. Downward velocity is limited to −24 units per second.

Wall rebounds are handled by OnCollisionEnter2D and OnCollisionStay2D, not by a double jump. The controller checks the contact normal and the direction of the incoming horizontal velocity. For a valid side collision on layer 8, the outgoing horizontal velocity points away from the wall and keeps 96% of the incoming speed. The vertical velocity is preserved. The DoubleFlip Animator trigger is only a visual animation; it does not give the player a second jump.

Platforms use BoxCollider2D. The script sets each platform collider to the same width and 0.36-unit height as its SpriteRenderer. Vertical spacing is fixed at 1.5 units. Platform width changes with floor progress: it interpolates from 4.8 to 2.5 units, receives a 0.4-unit increase every sixth floor, and the first platform is 7.5 units wide. Horizontal positions are varied with a seeded random value and clamped to the playfield.

| Setting in the scripts | Value |
| --- | --- |
| Maximum horizontal speed | 9.5 units/s |
| Air movement rate | 22 units/s² |
| Ground acceleration / stopping / reversal | 32 / 18 / 38 units/s² |
| Neutral horizontal braking in air | 0.12 units/s² |
| Base jump speed / momentum boost | 12 / 13 units/s |
| Coyote time / jump buffer | 0.12 / 0.14 seconds |
| Early-release vertical velocity multiplier | 0.48 |
| Rigidbody2D gravityScale while rising / falling | 3 / 3.45 |
| Maximum downward speed | 24 units/s |
| Wall momentum retention | 0.96 |
| Vertical platform gap | 1.5 units |

CameraFollow uses SmoothDamp to follow the player. After seven seconds, the camera begins moving upward even if the player does not rise. Camera speed increases from 0.38 to 1.35 based on floor progress. The target camera position stays 1.5 units above the player. The death boundary is 6.45 units below the camera position. The danger value increases as the player approaches that boundary, over a 4.5-unit range. UIManager displays this value through Image.fillAmount, changes the danger color at 0.45 and 0.75, and activates the smoke object when danger exceeds 0.4.

ScoreManager calculates the score as the highest height multiplied by 10, rounded down, plus the combo bonus. A landing advances the combo only when the player has gained at least two floors from the jump's starting floor and previous highest landing. The combo chain lasts three seconds. The bonus is calculated from the number of additional floors, 15 points per floor, and a combo multiplier capped at five. The best score is saved with PlayerPrefs.

## 4 C Sharp Scripts

The C# implementation described here consists of these eight scripts:

| Script | Responsibility |
| --- | --- |
| PlayerController.cs | Reads movement and jump input; detects the ground with BoxCast; applies horizontal movement, momentum-based jump strength, jump buffering, coyote time and early-release jump control; handles wall rebounds; updates Animator parameters and triggers; plays landing dust and fear/fall voice events. |
| PlatformSpawner.cs | Uses the assigned GameObject array and a seeded System.Random value to place platforms. It reuses the existing platform objects in a circular order using the oldest index; it does not create a new platform for every floor. It calculates the floor from the fixed 1.5-unit gap, adjusts platform width and horizontal position, resizes the SpriteRenderer and BoxCollider2D, and selects the stage sprite. |
| GameManager.cs | Controls Menu, Playing, Paused and GameOver states; sets Time.timeScale; handles Escape, restart, fullscreen, scene loading, application focus and quitting. |
| ScoreManager.cs | Tracks highest height, score, landing combos and bonus points; prevents counting floors already reached; saves the best score with PlayerPrefs. |
| CameraFollow.cs | Follows the player smoothly, starts upward camera pressure after seven seconds, increases camera speed with floor progress, calculates the death boundary and danger value, and ends the run when the player falls below the boundary. |
| UIManager.cs | Shows the assigned menu screens, stage banners and danger labels; updates score, best score, floor and combo TextMeshPro fields; fills and colors the shisha danger Image; controls the smoke object and high-jump banner. |
| AudioManager.cs | Plays persistent background music, sound effects and voice clips through separate AudioSources; saves sound and volume settings; limits repeated voice playback with a four-second cooldown, while urgent events can play immediately. |
| BuildingBackdrop.cs | Keeps the decorative Assiut building artwork aligned with the camera and scales it to cover the visible camera area. |

Platform generation in this code uses a GameObject array and a circular oldest index. It does not use Queue, Stack, Dictionary, BinarySearch or a newly instantiated platform for every floor. The UI script updates objects assigned to it; it does not build the Canvas or menu buttons at runtime.

Opening the source: clone the GitHub repository with Git LFS installed, run git lfs pull, add the project in Unity Hub, allow package import, open Assets/Ma3alem/Scenes/MainMenu.unity, and press Play. To play on Windows, extract Borg_Asyut_Windows.zip completely and run icy tower Asyut.exe beside its Data folder, UnityPlayer.dll and runtime folders.
