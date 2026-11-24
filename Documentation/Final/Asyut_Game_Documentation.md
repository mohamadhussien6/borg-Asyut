# Borg Asyut Game Documentation

برج أسيوط  |  icy tower Asyut

Windows x64Game description, assets, physics and C# implementation

## 1 Game Description

Our project is a single-player 2D arcade platform game set between Assiut-inspired residential buildings. The player controls a Saidi character wearing a galabeya, turban, shawl and balgha, and climbs an endless sequence of platforms. Running momentum increases jump height; the camera rises and falling below its lower boundary ends the run. The aim is to achieve the highest score through height and consecutive successful floor-skipping jumps.

Four difficulty zones occur in one continuous run. Their platform materials are chipped balcony plaster, rooftop terrazzo and cement, exposed red brick, and a gold and blue pharaonic coffin. Arabic stage introductions appear for three gameplay seconds at the beginning of each zone.

| Floors | Stage name |
| --- | --- |
| 1–20 | بلكونات الحارة |
| 21–45 | فوق السطوح |
| 46–75 | تحت التشطيب |
| 76+ | سر الفراعنة |

Controls: A/D or arrow keys move; Space jumps and early release reduces jump height; Escape pauses; R restarts after Game Over; Alt+Enter changes display mode. Menus provide replay, return, volume and saved best-score functions.

## 2 Assets Used

The following assets are integrated in the Unity project. The game uses its existing character animation, Egyptian environment, platform artwork, interface and supplied audio. Font and third-party notices remain with their resources.

| Asset group | Existing project content |
| --- | --- |
| Character | 18 transparent Player_*.png animation frames in Assets/Ma3alem/Art, bound to the existing Player prefab. |
| Platforms | Platform.png and Platform_Zone2.png through Platform_Zone4.png. Four separate transparent sprites; The coffin artwork retains its corrected proportions. |
| Environment and interface | Environment_Assiut_Static.png with three fixed panels, menu plaques, logo, shisha indicator, smoke and landing effects. |
| Speech and sound | Nine supplied Saidi voice WAVs in Assets/Audio/VoiceLines and project sound effects in Assets/Ma3alem/Audio. Voices retain their original performance. |
| Fonts and Arabic | Lemonada and Noto font resources with SIL Open Font License notices; RTLTMPro support with its MIT license. Existing TMP resources remain included. |
| Unity content | MainMenu and Gameplay scenes, player/platform/audio prefabs, PhysicsMaterial2D resources, Unity UI and Test Framework packages. |

The selected soundtrack is embedded in the Windows build and included at Assets/Ma3alem/Audio/Music/Selected_Music.mp3, with distribution permission confirmed by the project owner. The duplicate LocalPreviewOnly directory is excluded from source packages. Original voice recordings, sound effects and third-party license notices are retained.

Actual gameplay capture of the fourth stage. Promotional_Media contains 15 full-HD screenshots, a four-stage comparison and a 30-second 1920 x 1080 trailer at 60 FPS. The trailer ends with the original main menu for four seconds.

## 3 Type of Physics Used

The game uses Unity 2D physics with a dynamic Rigidbody2D and BoxCollider2D for the player. Gravity produces vertical motion, while the controller updates horizontal velocity during FixedUpdate. Rotation is frozen for the physics body; somersaults rotate the artwork only. Continuous collision detection and interpolation improve collision stability and presentation. Decorative buildings do not define the gameplay collision geometry.

Platforms use rectangular BoxCollider2D shapes with PlatformEffector2D one-way collision. The player can pass through a ledge from below and land on its upper surface. Collider thickness is 0.36 world units; generated platform widths vary according to the existing zone rules. All four platform skins preserve the original import metadata, pivots, PPU and collision alignment.

Wall rebounds are controlled in code rather than by a bouncy physics material. Horizontal outgoing speed retains 96% of incoming speed and points away from the wall. The physics material uses zero friction and zero bounce. There is no double jump; short coyote time and jump buffering make edge timing more forgiving.

| Current serialized setting | Value |
| --- | --- |
| Maximum horizontal speed | 9.5 units/s |
| Ground acceleration / braking / reversal | 32 / 18 / 38 units/s² |
| Air acceleration / neutral braking | 22 / 0.12 units/s² |
| Base jump speed / momentum boost | 12 / 13 units/s |
| Gravity / Rigidbody gravity scale | −9.81 units/s² / 3 |
| Falling multiplier / terminal fall speed | 1.15 / 24 units/s |
| Coyote time / jump buffer | 0.12 / 0.14 seconds |
| Early-release vertical velocity multiplier | 0.48 |
| Wall momentum retention | 0.96 |

Jump launch velocity = 12 + 13 × (|horizontal speed| / 9.5)^1.4, with the normalized speed clamped to 0–1. At full speed this gives an initial vertical velocity of 25 units/s. Actual jump height also depends on gravity and early release. These values were read from the current Player prefab and controller, not from earlier notes.

## 4 C Sharp Scripts

The implementation contains seven main gameplay scripts and one presentation helper. The full current C# source is included in the Unity source archive under Assets/Ma3alem/Scripts; the table describes the responsibility of each file.

| Script | Responsibility |
| --- | --- |
| PlayerController.cs | Keyboard input, momentum, braking, jump timing and height, wall rebounds, landing events and character animation. |
| PlatformSpawner.cs | Seeded platform generation, reachable placements, object pooling, floor indexing and selection of the four stage sprites. |
| CameraFollow.cs | Upward camera tracking, increasing camera pressure, playfield bounds and the lower death boundary. |
| ScoreManager.cs | Height points, landing combos, duplicate-floor protection and PlayerPrefs best-score persistence. |
| GameManager.cs | Menu, playing, pause and Game Over states; initialization, restart, return to menu and current-zone queries. |
| AudioManager.cs | Persistent music source, supplied voice events, voice priorities, sound effects, mute and volume. Missing music is handled without substituting a track. |
| UIManager.cs | Arabic menus and HUD, connected RTL text, score/floor/combo feedback, shisha indicator and three-second stage announcements. |
| BuildingBackdrop.cs | Displays the permanent decorative Assiut environment separately from the physical walls. |

Editor and verification support: ProjectTools.cs provides import, validation and Windows build utilities. Tests/EditMode and Tests/PlayMode contain NUnit/Unity tests. StandaloneVerification.cs is an opt-in automated-input driver compiled only into verification-enabled builds, not the submitted Windows build.

Verification status: recorded gameplay checks reached all four stages and covered jumping, landing, wall rebounds, pause, restart and menu return. The Windows build includes the selected soundtrack and excludes the optional verification driver. The owner tested the downloaded build and confirmed that everything worked on 24 November 2026. A complete passing automated Unity test suite is not claimed.

Opening the source: extract Unity_Source.zip, add the project in Unity Hub, allow package import, open Assets/Ma3alem/Scenes/MainMenu.unity, and press Play. To play on Windows, extract Windows_Game.zip completely and keep the executable beside its Data folder, UnityPlayer.dll and runtime folders.
