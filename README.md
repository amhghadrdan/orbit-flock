# Orbit Flock

Orbit Flock is a Unity 2D vertical shooter built for the third Computer Game Design assignment. The campaign spans Earth Orbit, Jupiter Belt and Deep Galaxy across 22 rounds, with a boss at the end of each sector.

- [Download and controls](https://amhghadrdan.github.io/orbit-flock/)
- [Windows and Linux releases](https://github.com/amhghadrdan/orbit-flock/releases/latest)
- [Persian project report](Report/OrbitFlock-Report.pdf)

## Play

- **Windows x64** — run `OrbitFlock-Setup-x64.exe` and launch Orbit Flock from the installer or Start menu.
- **Linux x64** — extract `OrbitFlock-Linux-x64.tar.gz` and run `./OrbitFlock-Linux-x64/OrbitFlock.x86_64`. Keep the extracted directory together.

| Action | Input |
| --- | --- |
| Move | WASD or arrow keys |
| Laser | Space |
| Missile | X |
| Pause or resume | Esc |

## Source and builds

Open the project in Unity **2022.3.62f1**. Start in `Assets/ChickenShooter/Scenes/MainMenu.unity`. All four scenes are listed in Build Settings. Use **Orbit Flock → Build Linux x64** or **Orbit Flock → Build Windows x64**. The Windows installer is built from the full player folder with NSIS using `Installer/OrbitFlock.nsi`.

Run the seven PlayMode tests in **Window → General → Test Runner**. The suite covers the 22-round campaign, boss milestones, checkpoint restoration, pause, retry, weapons and collisions.

## Architecture

| Responsibility | Code |
| --- | --- |
| Persistent campaign and scene navigation | `GameManager` |
| Player state and change events | `FlightProgress` |
| Save schema and JSON file lifecycle | `FlightCheckpoint`, `CheckpointStore` |
| Stage flow and round schedule | `StageController`, `WaveRun` |
| Combat and prefab reuse | `CombatWorld`, `ActorPool`, `CombatActor`, `BossFight` |
| Input, HUD, audio and effects | `PlayerShip`, `FlightHud`, `FlightAudio`, `FlightEffects` |

`LevelDataSO`, `WaveDataSO` and `WeaponDataSO` define the campaign, enemy waves and weapons. The game writes `savegame.json` to Unity's `Application.persistentDataPath` at each round boundary. Defeat replaces the checkpoint with round one of the current sector and three lives. Final victory clears the checkpoint.

The GitHub Pages download page source is in `Deployment/`.
