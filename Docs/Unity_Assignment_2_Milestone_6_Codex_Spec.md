# Unity Assignment 2 — Milestone 6 Codex Specification
## UI, Main Menu, HUD, End Screens, and Final Game Flow

**Project:** Helicopter combat game  
**Engine:** Unity 6  
**Render pipeline:** Built-in Render Pipeline  
**Input:** New Input System  
**Target:** Windows PC  
**Scope:** Milestone 6 only

---

## 1. Purpose

Milestones 1–5.1 already provide flight, terrain, player weapons, enemy helicopters, enemy tanks, health, explosions, team filtering, and mission logic.

Milestone 6 adds:

1. Main Menu
2. In-game HUD
3. Game Over screen
4. Victory screen
5. Enemy/objective progress display
6. Restart, return-to-menu, and quit actions
7. Correct final win/lose flow

Do not change the feel of flight controls, combat physics, enemy AI, terrain, or imported models.

---

## 2. Required final behavior

### Victory

Victory occurs **only** after all required enemy units are destroyed:

- Enemy Helicopter Alpha
- Enemy Helicopter Bravo
- Tank Alpha
- Tank Bravo

Do not show victory after only the helicopters are destroyed.

### Defeat

Defeat occurs when the player Health reaches zero and the existing `PlayerDeathHandler` raises its defeat event.

### Initial state

At Play Mode startup:

- Main Menu is visible.
- HUD, Victory, and Game Over panels are hidden.
- Cursor is unlocked and visible.
- `Time.timeScale = 0f`.
- Player, enemies, projectiles, terrain physics, and AI do not advance.

### Start Game

After Start Game:

- Main Menu hides.
- HUD and optional crosshair appear.
- `Time.timeScale = 1f`.
- Cursor locks and hides.
- Existing gameplay resumes without resetting or respawning units.

### End screens

After Victory or Game Over:

- Set `Time.timeScale = 0f`.
- Unlock/show cursor.
- Hide HUD and crosshair.
- Show exactly one correct end panel.
- Do not destroy player or unload the scene merely to show an end screen.
- Buttons must still work at `Time.timeScale == 0`.

---

## 3. UI technology and style

Use Unity **uGUI** with TextMeshPro if it is already available. If TMP essentials are unavailable and cannot be created safely, use a Unity UI text fallback; do not change packages.

Create a generated Canvas:

- Render Mode: Screen Space - Overlay
- Canvas Scaler: Scale With Screen Size
- Reference Resolution: `1920 x 1080`
- Screen Match Mode: Match Width Or Height
- Match: `0.5`
- Graphic Raycaster: enabled

Create `EventSystem` with `InputSystemUIInputModule`, never `StandaloneInputModule`. Configure it for the installed New Input System using verified APIs. Mouse button clicks must work while time is paused.

Visual style:

- Dark translucent military UI
- Cyan/blue normal UI accent
- Yellow objective/ammo accent
- Red Game Over accent
- Green Victory accent
- No downloaded UI pack, image, icon, audio, or new VFX
- Optional small generated crosshair

---

## 4. Required folders and deliverables

Create folders only if missing:

```text
Assets/_Project/Prefabs/UI/
Assets/_Project/Scripts/UI/
Assets/_Project/Scripts/Core/
Assets/_Project/Scripts/Editor/
Assets/_Project/Art/Materials/UI/
```

Create or update only these Milestone 6 deliverables as needed:

```text
Assets/_Project/Scripts/Core/GameFlowController.cs
Assets/_Project/Scripts/Core/SceneLoader.cs
Assets/_Project/Scripts/UI/GameUIController.cs
Assets/_Project/Scripts/UI/HUDController.cs
Assets/_Project/Scripts/UI/ObjectiveUIController.cs
Assets/_Project/Scripts/UI/EndScreenController.cs
Assets/_Project/Scripts/Editor/Milestone6UIBuilder.cs
Assets/_Project/Prefabs/UI/GameUI.prefab
```

Small additional scripts are permitted only when they improve single responsibility. Do not create one giant UI script.

`Milestone6UIBuilder.cs` is the sole approved mechanism for generated UI prefab and scene-object setup.

---

## 5. Mandatory pre-edit audit

Before editing, inspect actual APIs and serialized fields. Do not guess names, events, signatures, namespaces, or prefab hierarchy.

At minimum inspect:

```text
Assets/_Project/Scripts/Core/CombatMissionController.cs
Assets/_Project/Scripts/Core/MissionOutcomeTracker.cs              (if present)
Assets/_Project/Scripts/Combat/Health.cs
Assets/_Project/Scripts/Combat/DestroyOnDeath.cs
Assets/_Project/Scripts/Enemies/EnemyUnit.cs
Assets/_Project/Scripts/Player/PlayerDeathHandler.cs
Assets/_Project/Scripts/Weapons/MissileLauncher.cs
Assets/_Project/Scripts/Weapons/BombLauncher.cs                   (or actual bomb launcher name)
Assets/_Project/Scripts/Player/HelicopterInputReader.cs
Assets/_Project/Scripts/Player/HelicopterFlightController.cs
Assets/_Project/Scripts/Editor/Milestone4EnemyHelicopterBuilder.cs
Assets/_Project/Scripts/Editor/Milestone5TankBuilder.cs
Assets/_Project/Scenes/Game.unity
Assets/_Project/Prefabs/Player/PlayerHelicopter.prefab
Assets/_Project/Input/HelicopterControls.inputactions
```

Integration must be adapted to the actual project codebase.

---

## 6. Architecture

### 6.1 `GameFlowController`

Use the established project namespace convention, expected `HelicopterCombat.Core`.

Responsibility: game-flow state machine only.

Recommended states:

```csharp
public enum GameFlowState
{
    MainMenu,
    Playing,
    GameOver,
    Victory
}
```

Required behavior:

- Enter MainMenu at scene startup.
- Subscribe safely to `CombatMissionController` state changes.
- Subscribe to `PlayerDeathHandler` only if required by actual mission-controller API.
- Route state display to `GameUIController`.
- Expose public methods for UI buttons:
  - `StartGame()`
  - `RestartGame()`
  - `ReturnToMainMenu()`
  - `QuitGame()`
- Prevent duplicate state transitions.
- Keep UI component manipulation out of this class.
- Do not contain AI, health calculations, combat code, or scene-construction code.

State rules:

```csharp
// MainMenu
Time.timeScale = 0f;
Cursor.lockState = CursorLockMode.None;
Cursor.visible = true;

// Playing
Time.timeScale = 1f;
Cursor.lockState = CursorLockMode.Locked;
Cursor.visible = false;

// GameOver / Victory
Time.timeScale = 0f;
Cursor.lockState = CursorLockMode.None;
Cursor.visible = true;
```

In `OnDisable` and/or `OnDestroy`, restore `Time.timeScale = 1f` so Unity Editor does not remain paused after Play Mode stops.

### 6.2 `SceneLoader`

Responsibility: scene loading/restart/application exit only.

Required public methods:

```csharp
public void ReloadCurrentScene();
public void QuitApplication();
```

Requirements:

- Restore `Time.timeScale = 1f` before reload.
- Reload active gameplay scene robustly with valid runtime `SceneManager` API.
- In player builds call `Application.Quit()`.
- In Unity Editor log a clear quit message instead of using editor-only code in runtime classes.

### 6.3 `GameUIController`

Responsibility: panel visibility and button routing only.

Serialized references:

- Main Menu panel root
- HUD panel root
- Game Over panel root
- Victory panel root
- Optional crosshair root
- Start/Quit/Retry/Main Menu/Replay buttons
- `GameFlowController`

Public display methods:

```csharp
public void ShowMainMenu();
public void ShowGameplayUI();
public void ShowGameOver();
public void ShowVictory();
```

Requirements:

- Only one major state panel active at a time.
- HUD/crosshair only while Playing.
- Attach button listeners once; never accumulate duplicate listeners after multiple screen changes.
- No mission or health logic inside this class.

### 6.4 `HUDController`

Responsibility: player health and weapon ammo display only.

Show:

```text
HEALTH 250 / 250
MISSILES: 30       (or MISSILES: ∞ only if weapon remains unlimited)
BOMBS: 12          (or BOMBS: ∞ only if weapon remains unlimited)
```

- Subscribe to actual Health change events if present.
- If needed, add a minimal non-breaking health event/read-only property to the existing `Health` class; do not replace death behavior.
- Use actual launcher APIs.
- If finite ammo does not exist, add it cleanly to existing launcher scripts:
  - Missile maximum default: `30`
  - Bomb maximum default: `12`
  - Decrement only after a successful launch/drop
  - Block firing at zero
  - Expose public read-only count/max/unlimited information
- Do not add reloads, pickups, or other ammo gameplay.

### 6.5 `ObjectiveUIController`

Responsibility: objective and progress presentation only.

Display at start:

```text
OBJECTIVE: DESTROY ALL ENEMY UNITS
ENEMIES REMAINING: 4
MISSION ACTIVE
```

- Update after valid `EnemyUnit.Destroyed` events.
- Do not hardcode the number `4` inside UI logic; read it from `CombatMissionController`.
- When all targets are destroyed, display `MISSION COMPLETE` briefly before Victory panel opens.

### 6.6 `EndScreenController`

Responsibility: end-screen text only.

Victory:

```text
MISSION COMPLETE
All enemy helicopters and tanks have been destroyed.
```

Game Over:

```text
MISSION FAILED
Your helicopter was destroyed.
```

No scene loading, state machine, or button logic in this class.

---

## 7. Mission controller integration — critical

`CombatMissionController` is the authoritative mission source.

Audit its current code and extend it without breaking prior Milestones.

It must:

1. Track all required scene `EnemyUnit` instances: two helicopters + two tanks.
2. Subscribe to each valid unit’s destruction event exactly once.
3. Track destroyed count and remaining count.
4. Expose data/events needed by UI:
   - `RequiredEnemyCount`
   - `DestroyedEnemyCount`
   - `RemainingEnemyCount`
   - Mission state or mission-state-changed event
5. Trigger final Victory exactly once after all required units die.
6. Trigger Defeat exactly once when player death lifecycle reports defeat.
7. Never trigger victory after defeat.
8. Never trigger victory only because helicopters are destroyed.
9. Never destroy/reload scene itself.
10. Preserve valid enemy death path: player damage -> Health zero -> death event -> DestroyOnDeath.

Compatibility requirements:

- Keep any prior `HelicopterObjectiveCleared` message as optional intermediate information only; it must never display final Victory.
- The M6 builder must discover valid active scene `EnemyUnit` components after previous builders have created helicopters/tanks.
- Configure controller with exactly the valid required units in `Game.unity`.
- Do not count combat test targets, inactive prefabs, editor-only objects, or projectiles.
- Prefer robust discovery/configuration over brittle GameObject name dependence.
- If M4/M5 builders are rerun later, mission flow must still work.

---

## 8. Exact generated UI hierarchy

Create/update this hierarchy in `Game.unity`:

```text
GameUI
├── Canvas
│   ├── HUDPanel
│   │   ├── HealthCard
│   │   │   ├── Label
│   │   │   ├── HealthBarBackground
│   │   │   │   └── HealthBarFill
│   │   │   └── HealthValueText
│   │   ├── AmmoCard
│   │   │   ├── MissileCountText
│   │   │   └── BombCountText
│   │   ├── ObjectiveText
│   │   ├── EnemyCountText
│   │   ├── StatusText
│   │   └── Crosshair (optional)
│   ├── MainMenuPanel
│   │   ├── Title
│   │   ├── Subtitle
│   │   ├── StartButton
│   │   └── QuitButton
│   ├── GameOverPanel
│   │   ├── Title
│   │   ├── Description
│   │   ├── RetryButton
│   │   ├── MainMenuButton
│   │   └── QuitButton
│   └── VictoryPanel
│       ├── Title
│       ├── Description
│       ├── ReplayButton
│       ├── MainMenuButton
│       └── QuitButton
└── EventSystem
```

Use anchor-based layouts, not only fixed absolute coordinates.

### Layout targets

**Main Menu**
- Full-screen dark translucent background
- Center card about `720 x 500`
- Title: `HELICOPTER STRIKE`
- Subtitle: `Eliminate all hostile helicopters and ground armor.`
- Buttons: `START MISSION`, `QUIT GAME`

**HUD**
- Health card top-left: around `380 x 125`
- Ammo card below: around `300 x 100`
- Objective/status top-center
- Enemy count below objective
- Crosshair centered at `14–24` pixels

**End screens**
- Full-screen translucent overlay
- Center card with readable buttons
- Victory green accent
- Defeat red accent

---

## 9. `Milestone6UIBuilder` requirements

Create:

```text
Assets/_Project/Scripts/Editor/Milestone6UIBuilder.cs
```

Use project editor namespace convention, expected `HelicopterCombat.EditorTools`.

Add Unity menu item:

```text
Tools > Helicopter Combat > Rebuild Milestone 6 UI and Game Flow
```

Optional command-line entry point:

```csharp
public static void RebuildFromCommandLine()
```

Builder must:

1. Ensure required folders.
2. Locate/open `Assets/_Project/Scenes/Game.unity`.
3. Locate player root, existing player Health, PlayerDeathHandler, CombatMissionController, EnemyUnit instances, and actual weapon launcher components.
4. Build/update `GameUI`, Canvas, EventSystem, all panels, and controls.
5. Build/update `GameUI.prefab`.
6. Create/configure GameFlow/UI references.
7. Create/update mission controller references/counts for actual scene units.
8. Set initial state: main menu visible; HUD/end panels hidden.
9. Avoid duplicates after repeated builder runs:
   - one GameUI root
   - one Canvas
   - one EventSystem
   - one GameFlowController
   - no duplicate button listeners
10. Mark scene dirty and save assets/scene.
11. Add `Assets/_Project/Scenes/Game.unity` to the enabled Editor Build Settings list through Unity’s editor API only if missing. This is explicitly allowed because Retry/Replay must work in Windows build.
12. Do not raw-edit `.unity`, `.meta`, ProjectSettings, or package files.
13. Do not change imported Asset Store source files.
14. Do not rebuild terrain/enemy/tank models/combat prefabs.
15. Do not add audio.

### New Input System UI rule

Use `InputSystemUIInputModule`.

- Never create a legacy `StandaloneInputModule`.
- Use verified module default actions or create/configure a correct UI action map.
- Ensure left mouse click works on buttons while `Time.timeScale = 0`.
- Avoid gameplay interference by keeping simulation paused when Menu/end panels are open.

---

## 10. Preservation and scope limits

Do not regress:

- Helicopter controls and camera
- Terrain/environment
- Missile/bomb physics
- Explosions/damage
- Enemy helicopter home boundaries
- Tank ground-pursuit behavior
- Team/friendly-fire behavior
- Existing player and enemy death paths

Do not add:

- New downloads/assets
- Audio/music
- New VFX
- New enemies
- New terrain
- NavMesh
- Settings menu, multiplayer, score system, pickups, checkpoints
- Milestone 7 polish

Do not commit or push to GitHub.

---

## 11. Verification

After coding:

1. Static review for Unity 6 compatibility, namespaces, using directives, stale call sites, duplicate events, null handling, and time-scale restoration.
2. Compile in Unity.
3. Run:

```text
Tools > Helicopter Combat > Rebuild Milestone 6 UI and Game Flow
```

4. Open `Assets/_Project/Scenes/Game.unity`.
5. Clear Console and test Play Mode:
   - Main Menu visible at launch
   - Start button works
   - HUD displays true health
   - missile/bomb count is true
   - objective reports all enemies initially
   - destroying one enemy updates count
   - destroying all four shows Victory once
   - player death shows Game Over once
   - Replay/Retry works
   - Main Menu returns to paused menu state in same scene
   - Quit logs in editor / closes standalone build
   - no red Console errors
6. Stop Play Mode and confirm `Time.timeScale` returns to `1`.

---

## 12. Required Codex final report

Report:

1. Created files.
2. Modified files.
3. Actual existing APIs integrated.
4. Mission-controller changes and enemy counting method.
5. Generated UI hierarchy/prefab.
6. Input System/EventSystem configuration.
7. Ammo behavior: existing, finite added, or unlimited.
8. Builder result.
9. Latest Unity compile result.
10. Real deviations and reasons.
11. Manual tests still required.

Stop after Milestone 6. Do not begin Milestone 7.
