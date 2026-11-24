[![Play the official gameplay trailer](Media/Trailer_Thumbnail.png)](https://github.com/mohamadhussien6/borg-asyut/releases/latest/download/Borg_Asyut_Official_Trailer.mp4)

**[▶ Play the trailer](https://github.com/mohamadhussien6/borg-asyut/releases/latest/download/Borg_Asyut_Official_Trailer.mp4)** · **[Download Windows game](https://github.com/mohamadhussien6/borg-asyut/releases/latest/download/Borg_Asyut_Windows.zip)** · **[Game documentation](https://github.com/mohamadhussien6/borg-asyut/releases/latest/download/Borg_Asyut_Documentation.pdf)**

# برج أسيوط · icy tower Asyut

An Egyptian 2D arcade tower climber for Windows, built with Unity6000.3.2f1.

**اللي أبوه صعيدي ميخافِش!**

## The game

A Saidi character in a charcoal galabeya, white turban, shawl and balgha climbs a continuous tower framed by Egyptian residential architecture: exposed brick, worn plaster, balconies, laundry, rooftop details and hand-painted Arabic writing. The shisha-shaped danger meter warns when the camera catches up.

The challenge is built around momentum rather than repeated identical jumps. Faster takeoff produces higher jumps; early release cuts jump height. Successful floor-skipping jumps build combos, while increasing camera pressure and narrower routes raise the difficulty. Falling below the camera ends the run, with a quick restart available.

## Controls

| Input | Action |
|---|---|
| A / D or Left / Right | Move and build momentum |
| Space | Jump; release early for a shorter jump |
| Escape | Pause or resume |
| R after Game Over | Restart |
| Alt + Enter | Toggle display mode |

## Four continuous stages

| Floors | Arabic name | Platform appearance |
|---|---|---|
| 1–20 | بلكونات الحارة | Chipped beige balcony plaster and exposed brick |
| 21–45 | فوق السطوح | Worn terrazzo, cement and rooftop waterproofing |
| 46–75 | تحت التشطيب | Red clay brickwork with a thin concrete top |
| 76+ | سر الفراعنة | Gold and blue pharaonic coffin platforms |

Stage introductions appear for three gameplay seconds. The four art skins retain the existing platform alignment, collision dimensions, generation and difficulty settings.

![The four stages in real gameplay](Media/Four_Stages_Comparison.png)

## Gameplay features

- Speed-based jumps, coyote time and buffered jump input.
- Controlled wall rebounds and visual somersaults.
- Seeded reachable platform routes with pooling.
- Height scoring, combo bonuses and saved best scores.
- Connected Arabic menus, pause, help and sound controls.
- Original supplied Saidi voice reactions and separate audio controls.

![Momentum and combos](Media/Screenshots/09_Combo_High_Score.png)
![Coffin stage](Media/Screenshots/06_Stage_4.png)

All 15 original 1920×1080 promotional screenshots are in [Media/Screenshots](Media/Screenshots).

## Run the project

1. Clone this repository with Git LFS installed, then run `git lfs pull`.
2. Add the project folder in Unity Hub and use **Unity 6000.3.2f1**.
3. Allow Unity to restore packages and regenerate its Library folder.
4. Open `Assets/Ma3alem/Scenes/MainMenu.unity` and press Play.

For the standalone version, extract the complete Windows ZIP and run `icy tower Asyut.exe`. Keep its Data directory, UnityPlayer.dll and runtime folders together.

## Source organization

`Assets/Ma3alem/Scripts` contains seven main gameplay scripts: PlayerController, PlatformSpawner, CameraFollow, ScoreManager, GameManager, AudioManager and UIManager. BuildingBackdrop is the presentation helper. Scenes, prefabs, artwork, fonts, audio and metadata remain under Assets. Packages and ProjectSettings are included. Tests remain in Assets/Ma3alem/Tests.

## Assets and attribution

- Supplied Saidi voice recordings retain their original performance, pitch and speed. Project sound effects remain in Assets/Ma3alem/Audio.
- Lemonada and Noto fonts retain their SIL Open Font License notices under Assets/Ma3alem/Fonts and third-party resource folders.
- RTLTMPro retains its MIT license under Assets/ThirdParty. Other bundled TMP font notices remain alongside their resources.
- The selected local MP3 is excluded from repository source and the distributable Windows build. The private promotional trailer uses the owner-selected soundtrack; no replacement track was created. No public music-redistribution license is asserted.

