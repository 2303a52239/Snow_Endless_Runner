# Snow Endless Runner (Unity 3D)

A 3D endless runner with a winter theme. Everything (character, obstacles, coins, power-ups, scenery,
UI, sounds, music) is generated procedurally in code, so there are **no external or paid assets**.

## IMPORTANT: honest status
This project was written without access to a Unity Editor, so it has **not been compiled or play-tested**.
Brace/parenthesis balance and cross-script references were checked by hand, but you may still see a
compile error or small bug on first open. If so, copy the Console message and I'll fix it.

## Open and play
1. Install **Unity 2022.3 LTS** (2021.3+ should also work) via Unity Hub. Add *Android Build Support* if you want APKs.
2. Unity Hub -> **Add** -> select this folder -> open it (first import takes a few minutes).
3. On first open, an editor script (`Assets/Editor/SnowRunnerProjectSetup.cs`) automatically creates
   `Assets/Scenes/MainMenu.unity` and `Gameplay.unity`, registers them in Build Settings and applies
   Android/Windows player settings. You can re-run it any time: **Snow Runner > Setup Project**.
4. Open `Assets/Scenes/MainMenu.unity` (opened automatically) and press **Play**.

If the Unity Package Manager asks about `com.unity.ugui`, accept it (it is in `Packages/manifest.json`).
If Unity says "Input System" is active, set **Edit > Project Settings > Player > Active Input Handling = Input Manager (Old)** (or Both).

## Controls
| Action | Windows | Android |
|---|---|---|
| Left / Right lane | A / D, Left / Right arrows | Swipe left / right |
| Jump | W, Up arrow, Space | Swipe up |
| Slide | S, Down arrow | Swipe down |
| Pause | Esc or P | Pause button |

## Gameplay
- 3 lanes, auto-run, speed ramps up gradually (11 -> 30 units/s).
- Obstacles: wooden boxes, ice barriers, fallen trees (jump), big rocks, ice blocks (change lane), snowmobiles, ice arches (slide under).
- Power-ups: **Magnet** (10 s), **Shield** (absorbs 1 hit), **Speed Boost** (6 s, smashes obstacles), **Score x2** (10 s), **Coin Bonus** (+50 coins).
- Score = distance (x multiplier) + coin bonus. Best score, total coins and sound setting persist via PlayerPrefs.
- Game Over: Revive (50 coins, up to 3 per run), Retry, Home.

## Project layout
```
Assets/
  Editor/SnowRunnerProjectSetup.cs   scene creation, build settings, player settings, build menu
  Scenes/                            MainMenu + Gameplay (generated on first open)
  Scripts/
    Core/        GameBootstrap, Mats, ObjectPool, MeshFactory, SaveData
    Player/      PlayerController, PlayerAnimation, InputHandler
    Environment/ TrackGenerator, EnvironmentSpawner, ObstacleSpawner, Obstacle
    Gameplay/    GameManager, ScoreManager, CoinManager, PowerUpManager, GameOverManager, Pickup
    UI/          MainMenuUI, GameplayUI, PauseMenuUI, GameOverUI, UIFactory
    Camera/      CameraFollow
    Audio/       AudioManager (synthesised SFX + music)
  Prefabs/ Materials/ Textures/ Models/ Audio/ Particles/ Settings/   (placeholders; content is built at runtime and pooled)
Packages/manifest.json
ProjectSettings/ProjectVersion.txt
```
Note: prefabs/materials are created at runtime through code and object pools rather than saved as asset files.

## Build
**Windows:** menu **Snow Runner > Build Windows** (output `Builds/Windows/`), or File > Build Settings > PC > Build.

**Android:** install Android Build Support (SDK/NDK/OpenJDK) in Unity Hub, then **Snow Runner > Build Android APK**
(output `Builds/Android/`), or File > Build Settings > Android > Switch Platform > Build. Orientation is landscape,
package `com.snowrunnerstudio.snowendlessrunner`, min API 24, IL2CPP ARM64/ARMv7. Enable Developer Options + USB debugging to install.

## Known limitations
- Primitive-based art (no imported models/animation clips); animation is procedural.
- Not compiled or tested in Unity yet; see status above.
