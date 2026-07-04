# Helicopter Strike

Unity 6 helicopter combat project built on the Built-in Render Pipeline.

You pilot a helicopter through a terrain combat space, destroy two enemy helicopters and two enemy tanks, survive incoming missiles, and complete the mission through a full menu/HUD/game-over/victory flow.

## What is in this project

- Milestone 1: player helicopter flight, camera follow, input system
- Milestone 2: terrain, vegetation, lighting, atmosphere
- Milestone 3: player combat systems, missiles, bombs, explosions, health
- Milestone 4 / 4.1: enemy helicopters, persistence fixes, mission-state stabilization
- Milestone 5 / 5.1: enemy tanks, tank combat, ground pursuit
- Milestone 6: main menu, pause, HUD, game over, victory flow
- Milestone 7: audio, VFX, camera shake, polish

## Quick start

1. Open the project in Unity 6.
2. Wait for import and compile to finish.
3. Open [`Assets/_Project/Scenes/Game.unity`](Assets/_Project/Scenes/Game.unity).
4. Press Play.
5. In the main menu, click `START MISSION`.

## Controls

These are the current bindings from [`Assets/_Project/Input/HelicopterControls.inputactions`](Assets/_Project/Input/HelicopterControls.inputactions):

- `W / S`: forward / backward move input
- `A / D`: strafe left / right move input
- `R / F`: altitude up / down
- `Q / E`: yaw left / right
- `Mouse Move`: camera look
- `Left Mouse` or `Space`: fire missile
- `Right Mouse` or `B`: drop bomb
- On-screen `PAUSE` button: pause menu

## Game flow

At runtime the project uses [`GameFlowController`](Assets/_Project/Scripts/Core/GameFlowController.cs) and [`GameUIController`](Assets/_Project/Scripts/UI/GameUIController.cs) for the player-facing state machine.

Flow:

1. Startup opens on Main Menu.
2. `START MISSION` enters gameplay.
3. HUD shows health, missiles, bombs, objective, and enemies remaining.
4. `PAUSE` opens the pause panel.
5. Player death triggers Game Over.
6. Destroying all four required enemy units triggers Victory.
7. Replay / Retry reloads the scene.

## Objective

Final victory requires destroying all four combat units tracked by [`CombatMissionController`](Assets/_Project/Scripts/Core/CombatMissionController.cs):

- `Enemy Helicopter Alpha`
- `Enemy Helicopter Bravo`
- `Tank Alpha`
- `Tank Bravo`

Victory does not trigger after helicopters alone. Tanks must also be destroyed.

## Player systems

Main player components live under:

- [`Assets/_Project/Scripts/Player`](Assets/_Project/Scripts/Player)
- [`Assets/_Project/Scripts/Weapons`](Assets/_Project/Scripts/Weapons)
- [`Assets/_Project/Scripts/Audio`](Assets/_Project/Scripts/Audio)

Key systems:

- `HelicopterFlightController`: movement
- `ThirdPersonHelicopterCamera`: follow camera
- `PlayerWeaponController`: weapon input bridge
- `MissileLauncher`: missile firing
- `BombLauncher`: bomb dropping
- `Health`: player health
- `PlayerDeathHandler`: defeat path
- `HelicopterEngineAudio`: player helicopter loop
- `RotorSpinner`: decorative rotor rotation for imported visuals

## Enemy systems

### Enemy helicopters

Enemy helicopter logic is split into modular scripts in [`Assets/_Project/Scripts/Enemies`](Assets/_Project/Scripts/Enemies):

- `EnemyHelicopterBrain`
- `EnemyHelicopterMovement`
- `EnemyHelicopterTargeting`
- `EnemyHelicopterSeparation`
- `EnemyHelicopterWeaponController`
- `EnemyHelicopterVisualController`

Behavior:

- pursue the player
- keep combat spacing
- orbit and separate
- fire missiles
- return to combat space instead of disappearing
- stay alive until actual health reaches zero

### Enemy tanks

Tank systems are also modular:

- `EnemyTankBrain`
- `EnemyTankMovement`
- `EnemyTankTargeting`
- `TankTurretAimer`
- `TankWeaponController`

Behavior:

- detect the player
- chase the player's ground-projected position
- remain grounded on terrain
- hold combat distance
- keep turret aiming independent from hull movement
- fire missiles upward
- return toward home position when disengaged

## UI, audio, and polish

### UI

UI scripts:

- [`Assets/_Project/Scripts/UI/HUDController.cs`](Assets/_Project/Scripts/UI/HUDController.cs)
- [`Assets/_Project/Scripts/UI/ObjectiveUIController.cs`](Assets/_Project/Scripts/UI/ObjectiveUIController.cs)
- [`Assets/_Project/Scripts/UI/EndScreenController.cs`](Assets/_Project/Scripts/UI/EndScreenController.cs)

Main menu background image:

- [`Assets/_Project/Art/UI/MainMenuBackground.jpg`](Assets/_Project/Art/UI/MainMenuBackground.jpg)

### Audio

Audio clips are organized in:

- [`Assets/_Project/Audio/Clips`](Assets/_Project/Audio/Clips)

Categories:

- `Ambient`
- `Combat`
- `Mission`
- `UI`
- `Vehicles`
- `Weapons`

### VFX

Generated VFX prefabs are in:

- [`Assets/_Project/Prefabs/VFX`](Assets/_Project/Prefabs/VFX)

These include:

- missile muzzle flash
- bomb release puff
- bomb impact dust
- death fire/smoke
- upgraded explosion prefab

## Builder commands

This repo uses editor builders to generate and rebuild major milestone content.

Available Unity menu commands:

- `Tools > Helicopter Combat > Rebuild Milestone 2 Environment`
- `Tools > Helicopter Combat > Rebuild Milestone 3 Combat`
- `Tools > Helicopter Combat > Rebuild Milestone 4 Enemy Helicopters`
- `Tools > Helicopter Combat > Rebuild Milestone 5 Enemy Tanks`
- `Tools > Helicopter Combat > Rebuild Milestone 6 UI and Game Flow`
- `Tools > Helicopter Combat > Rebuild Milestone 7 Audio, VFX and Polish`

Recommended rebuild order if you need to regenerate everything:

1. Milestone 2
2. Milestone 3
3. Milestone 4
4. Milestone 5
5. Milestone 6
6. Milestone 7

If you only change menu/UI presentation, rebuild Milestone 6, then optionally Milestone 7 as a safety pass for UI audio hookups.

## Project layout

Core gameplay content is under [`Assets/_Project`](Assets/_Project):

- `Art`: generated and imported materials/UI art
- `Audio`: clips and third-party license notes
- `Input`: Input System asset
- `Prefabs`: player, enemies, projectiles, UI, VFX
- `Scenes`: playable scene
- `Scripts`: gameplay/editor/UI/audio/VFX code
- `Settings`: generated runtime settings assets

Imported external source assets currently present in the repo include:

- `Assets/Low Poly Helicopters Pack Free`
- `Assets/Isle of Assets/Tank 3D Model`
- `Assets/_Project/Art/External/QuaterniusUltimateNature` for environment content

## Testing checklist

Use this as a fast end-to-end validation pass:

- [ ] Project compiles with zero red console errors
- [ ] `Game.unity` opens correctly
- [ ] Main menu appears with background image
- [ ] `START MISSION` begins gameplay
- [ ] Helicopter flies with keyboard/mouse controls
- [ ] Rotor visuals rotate
- [ ] Player helicopter loop audio plays in gameplay
- [ ] Terrain, grass, trees, and atmosphere render correctly
- [ ] Missiles fire
- [ ] Bombs drop
- [ ] Enemy helicopters pursue and fire
- [ ] Enemy tanks pursue on the ground and fire
- [ ] HUD health and enemy count update correctly
- [ ] Pause menu works
- [ ] Game Over triggers on player death
- [ ] Victory triggers only after both helicopters and both tanks are destroyed
- [ ] Replay / Retry / Main Menu buttons work

## Troubleshooting

### Menu/UI changes do not appear

Run:

- `Tools > Helicopter Combat > Rebuild Milestone 6 UI and Game Flow`

Then reopen `Game.unity` if needed.

### Audio/VFX links seem missing after a rebuild

Run:

- `Tools > Helicopter Combat > Rebuild Milestone 7 Audio, VFX and Polish`

### Terrain or environment needs regeneration

Run:

- `Tools > Helicopter Combat > Rebuild Milestone 2 Environment`

### Enemy scene setup looks wrong

Rebuild the related milestone in order:

- Milestone 4 for helicopters
- Milestone 5 for tanks
- Milestone 6 for UI
- Milestone 7 for audio/VFX/polish

## License notes

Audio license/source notes are stored in:

- [`Assets/_Project/Audio/ThirdPartyLicenses/ASSET_MANIFEST.md`](Assets/_Project/Audio/ThirdPartyLicenses/ASSET_MANIFEST.md)

Additional license notes:

- [`Assets/_Project/Audio/ThirdPartyLicenses/Kenney_Interface_Sounds_LICENSE.txt`](Assets/_Project/Audio/ThirdPartyLicenses/Kenney_Interface_Sounds_LICENSE.txt)
- [`Assets/_Project/Audio/ThirdPartyLicenses/Kenney_Impact_Sounds_LICENSE.txt`](Assets/_Project/Audio/ThirdPartyLicenses/Kenney_Impact_Sounds_LICENSE.txt)
- [`Assets/_Project/Audio/ThirdPartyLicenses/Mixkit_Asset_Notes.txt`](Assets/_Project/Audio/ThirdPartyLicenses/Mixkit_Asset_Notes.txt)
- [`Assets/_Project/Audio/ThirdPartyLicenses/Pixabay_Asset_Notes.txt`](Assets/_Project/Audio/ThirdPartyLicenses/Pixabay_Asset_Notes.txt)

Environment pack licensing was previously captured under the imported Quaternius asset folder.

## Current status

The repo currently contains a playable end-to-end vertical slice with:

- terrain and environment
- player helicopter combat
- enemy helicopters
- enemy tanks
- mission tracking
- main menu and HUD
- pause / game over / victory flow
- audio and VFX polish

Milestone 8 is not included here.
