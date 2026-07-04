# Unity Assignment 2 — Milestone 5 Codex Specification
## Enemy Tanks, Ground Combat, and Final Mission Progression Foundation

**Project:** Helicopter Combat  
**Unity:** Unity 6 / Built-in Render Pipeline / Windows  
**Scope:** Two persistent enemy tanks, turret aiming, tank missiles, damage/destruction, and final objective tracking.  
**Out of scope:** Tank movement/pathfinding, HUD, menus, Victory/Game Over UI, audio, new downloads, and Milestone 6 work.

---

## 1. Required gameplay result

After this milestone:

1. `TankAlpha` and `TankBravo` exist in `Game.unity`.
2. Tanks remain in game until **their real `Health` reaches zero**. Range loss, visibility, timeouts, collision exit, and distance must never despawn or deactivate a tank.
3. Each tank detects the player helicopter, keeps its chassis planted, smoothly rotates a turret/barrel rig toward the player, and fires an **upward non-homing missile** from the muzzle.
4. Player missiles and bombs damage/destroy tanks.
5. Tank missiles can defeat the player using the existing temporary non-UI Game Over behavior.
6. Enemy missiles cannot damage enemy helicopters, other tanks, or their own launcher.
7. Destroying all helicopters first logs a helicopter-objective-cleared message, but does **not** produce final victory while tanks remain.
8. Destroying all enemy helicopters and tanks sets the mission state to `FinalVictoryReady` and logs a temporary victory-ready message. The visual Victory Screen remains Milestone 6.

---

## 2. Preserve existing milestones

Do not break, rewrite, or remove:

- Milestone 1: `HelicopterControls.inputactions`, player flight, third-person camera, PlayerHelicopter root/prefab.
- Milestone 2: terrain, vegetation, lighting, fog, terrain scene.
- Milestone 3: player missiles, bombs, damage/explosion system, combat input.
- Milestone 4 / 4.1: enemy helicopters, boundary-return behavior, friendly-fire layer, player health/death, mission controller.

Do not change `.unity`, `.meta`, `ProjectSettings`, `Packages`, imported Asset Store source files, or unrelated assets manually. Scene changes must be created via the builder.

---

## 3. Asset rules and discovery

A Unity Asset Store tank model is already imported, but its folder, scale, prefab/model name, hierarchy, and material setup are unknown.

Before implementation, use `AssetDatabase.FindAssets("t:GameObject")` and inspect the actual imported content:

- Prioritize assets whose path/name contains `tank`.
- Prefer an imported asset **outside** `Assets/_Project`.
- Exclude demo scenes, generated prefabs, and unrelated `_Project` content.
- Log selected source asset path/name.
- Do not hardcode guessed package paths.
- Do not move, rename, edit, or overwrite source Asset Store assets.

Create a wrapper prefab with generated root components. Instantiate the discovered model under a `VisualRoot`; use model bounds to produce reliable collider sizing and visual scaling.

If no usable tank model is discovered, create a clean primitive fallback with chassis, turret, barrel, tracks, and muzzle. Report this as a deviation. Do not download anything.

---

## 4. Files to create / modify

Create only these new files:

```text
Assets/_Project/Scripts/Enemies/EnemyTankTargeting.cs
Assets/_Project/Scripts/Enemies/TankTurretAimer.cs
Assets/_Project/Scripts/Enemies/TankWeaponController.cs
Assets/_Project/Scripts/Enemies/EnemyTankBrain.cs
Assets/_Project/Scripts/Editor/Milestone5TankBuilder.cs

Assets/_Project/Art/Materials/TankOlive.mat
Assets/_Project/Art/Materials/TankDesert.mat
Assets/_Project/Art/Materials/TankBarrelDark.mat

Assets/_Project/Prefabs/Enemies/TankAlpha.prefab
Assets/_Project/Prefabs/Enemies/TankBravo.prefab
Assets/_Project/Prefabs/Enemies/TankMissile.prefab
```

Modify only when necessary:

```text
Assets/_Project/Scripts/Core/CombatMissionController.cs
Assets/_Project/Scripts/Editor/Milestone4EnemyHelicopterBuilder.cs
```

Do not duplicate existing `Health`, `DestroyOnDeath`, `CombatTeam`, `TeamMember`, `Explosion`, `ExplosiveProjectile`, `MissileProjectile`, `MissileLauncher`, or `EnemyUnit` classes.

---

## 5. Inspect existing APIs first

Before coding, read the actual public APIs and configuration flow of:

```text
Health
DestroyOnDeath
EnemyUnit
CombatTeam
TeamMember
MissileLauncher
MissileProjectile
ExplosiveProjectile
Explosion
PlayerDeathHandler
CombatMissionController
Milestone4EnemyHelicopterBuilder
```

Adapt builder calls to real existing methods rather than inventing incompatible method signatures.

---

## 6. EnemyTankTargeting.cs

**Single responsibility:** player reference, detection hysteresis, aim data, firing eligibility.

### Inspector defaults

```text
Detection Range: 190
Lose Target Range: 230
Minimum Firing Distance: 25
Maximum Firing Distance: 165
Minimum Elevation Angle: 8
Maximum Elevation Angle: 65
Aim Height Offset: 2.5
```

### Required public API or equivalent

```csharp
Transform AssignedTarget { get; }
Transform CurrentTarget { get; }
bool HasCombatTarget { get; }
bool IsWithinDetectionRange { get; }
bool IsWithinFiringRange { get; }
bool HasValidFiringAngle { get; }
float DistanceToTarget { get; }
Vector3 AimPoint { get; }
Vector3 DirectionToTarget { get; }

void SetTarget(Transform target);
void ConfigureRanges(...);
```

### Behavior

- Assigned target is the PlayerHelicopter transform.
- Use detection/lose-range hysteresis.
- Calculate firing eligibility using distance plus upward elevation angle from muzzle/tank origin to helicopter aim point.
- If player leaves range, tank becomes idle but remains in scene.
- It must never destroy/deactivate itself or modify player state.

---

## 7. TankTurretAimer.cs

**Single responsibility:** smooth turret/barrel aiming only.

### Required references

```text
Tank Root
TurretYawPivot
BarrelPitchPivot
Muzzle
EnemyTankTargeting
```

### Inspector defaults

```text
Yaw Rotation Speed: 75 degrees per second
Pitch Rotation Speed: 55 degrees per second
Minimum Pitch: 0
Maximum Pitch: 68
Target Aim Offset: 2.5
Aim Alignment Tolerance: 6 degrees
```

### Behavior

- Root/chassis stays planted.
- `TurretYawPivot` rotates horizontally to target.
- `BarrelPitchPivot` raises barrel toward helicopter.
- `Muzzle.forward` becomes the launch direction.
- Use smooth rotation.
- If imported visual has a safely discoverable child called turret/tower, optionally make it follow yaw. Otherwise the generated weapon rig is the visual turret.
- Do not rely on unknown source model hierarchy.

---

## 8. TankWeaponController.cs

**Single responsibility:** safe firing cooldown and delegation to existing missile system.

### Inspector defaults

```text
Fire Cooldown: 3.5 seconds
Acquire Target Delay: 0.45 seconds
Require Valid Firing Angle: true
```

### Behavior

- Fire only when player exists, is combat-eligible, in firing range, at valid elevation, turret is aligned, and player is not already defeated.
- Delegate projectile spawning to existing `MissileLauncher` or its actual reusable equivalent after API inspection.
- Tank owner root/rigidbody/team must be propagated using the existing owner initialization logic.
- No homing behavior.
- Does not manually apply health damage; projectile/explosion system does that.
- Does not destroy/deactivate tank.

---

## 9. EnemyTankBrain.cs

**Single responsibility:** high-level state machine.

```text
Idle
Tracking
Firing
Defeated
```

### Behavior

- `Idle`: no valid target in detection range; tank stays in-place.
- `Tracking`: target exists; turret continues tracking.
- `Firing`: tracking plus weapon permission.
- `Defeated`: entered only from this tank root's `Health.Died` event; stop weapon firing.
- Health event is the **only** runtime path to the defeated/destruction route.
- Do not use lifetime, visibility, trigger exit, collision exit, range loss, or scene reload as defeat/despawn signals.

---

## 10. Tank prefab requirements

Generate wrapper prefabs at:

```text
Assets/_Project/Prefabs/Enemies/TankAlpha.prefab
Assets/_Project/Prefabs/Enemies/TankBravo.prefab
```

### Stable hierarchy

```text
TankAlpha / TankBravo
├── VisualRoot
│   └── ImportedTankVisual (or FallbackTankVisual)
├── TurretYawPivot
│   └── BarrelPitchPivot
│       ├── BarrelVisual
│       └── Muzzle
└── ColliderRoot
```

### Root components

```text
Rigidbody
BoxCollider
Health
DestroyOnDeath
TeamMember
EnemyUnit
EnemyTankTargeting
TankTurretAimer
TankWeaponController
EnemyTankBrain
MissileLauncher (or actual existing equivalent)
```

### Rigidbody

```text
Use Gravity: false
Is Kinematic: true
Interpolate: Interpolate
Collision Detection: Discrete
Constraints: Freeze All
```

### Combat values

```text
Health Maximum: 180
Health destroyWhenDead: false
DestroyOnDeath delay: 0.05
DestroyOnDeath destroy root: true
TeamMember Team: Enemy
```

The valid destruction chain is:

```text
player missile/bomb damage
→ Health reaches zero
→ Health.Died
→ EnemyTankBrain enters Defeated
→ DestroyOnDeath destroys tank root
```

Use a generated BoxCollider from renderer bounds; do not rely only on an unknown MeshCollider.

---

## 11. Tank visuals and materials

Create these Built-in Standard Shader materials:

```text
TankOlive.mat
  Base color: muted olive green
  Metallic: 0.20
  Smoothness: 0.25

TankDesert.mat
  Base color: muted desert brown
  Metallic: 0.18
  Smoothness: 0.22

TankBarrelDark.mat
  Base color: dark charcoal
  Metallic: 0.25
  Smoothness: 0.30
```

- Tank Alpha: olive variant.
- Tank Bravo: desert variant.
- Apply generated materials to generated barrel/weapon rig and selectively tint imported model only where safe.
- Never create pink/magenta materials.
- Do not overwrite imported source material assets.

---

## 12. Tank missile

Create:

```text
Assets/_Project/Prefabs/Enemies/TankMissile.prefab
```

Reuse/clone the existing enemy-missile projectile behavior and existing explosion/VFX system. Do not rewrite projectile foundation.

### Values

```text
Speed: 48
Lifetime: 7
Arming Delay: 0.12
Damage: 250
Explosion Radius: 5
Explosion Force: 250
```

### Required behavior

- Spawn at `Muzzle`.
- Launch along `Muzzle.forward`.
- Projectile remains non-homing.
- Ignore launcher owner root.
- Ignore all `CombatTeam.Enemy` colliders and damage targets.
- Valid player hit may defeat player with 250 damage.
- Use existing explosion VFX.
- Self-destruct only at valid collision or lifetime end; no tank cleanup side effect.

---

## 13. Milestone5TankBuilder.cs

All generated scene changes must use:

```text
Tools > Helicopter Combat > Rebuild Milestone 5 Enemy Tanks
```

Also include optional command line method:

```csharp
HelicopterCombat.EditorTools.Milestone5TankBuilder.RebuildFromCommandLine
```

### Builder preflight

Locate safely:

```text
Assets/_Project/Scenes/Game.unity
PlayerHelicopter
Terrain
EnemyHelicopters
GameSystems
CombatMissionController
```

Use discovery, not brittle unverified paths.

### Scene root

Create/rebuild only:

```text
EnemyTanks
├── TankAlpha
└── TankBravo
```

Do not delete/rebuild PlayerHelicopter, Main Camera, terrain, environment, EnemyHelicopters, GameSystems, or unrelated roots.

### Tank placement

Use player XZ plus these initial offsets:

```text
TankAlpha: (-95, +95)
TankBravo: (+110, +120)
```

For each tank:
- Clamp to terrain bounds.
- Sample terrain height.
- Place root on ground using collider half height.
- Keep upright unless a terrain-normal adjustment is demonstrably stable.
- Face generally toward player spawn initially.
- Avoid obvious overlap with player or other tank through an overlap check where practical.
- Do not destructively remove environment trees/props.

### Repeatability

A builder rerun must leave:
- exactly one `EnemyTanks` root
- exactly TankAlpha and TankBravo under it
- no duplicate tank materials/prefabs
- no duplicate GameSystems / EventSystems
- no duplicate mission event subscriptions

---

## 14. Combat mission extension

Extend `CombatMissionController` **backward-compatibly**.

Keep existing call style working:

```csharp
Configure(PlayerDeathHandler playerDeathHandler, EnemyUnit[] requiredEnemyUnits)
```

Add a grouped objective overload or equivalent:

```csharp
Configure(
    PlayerDeathHandler playerDeathHandler,
    EnemyUnit[] helicopterUnits,
    EnemyUnit[] tankUnits)
```

### Required mission logic

- Safely filter null units.
- Subscribe once to all `EnemyUnit.Destroyed` events.
- Subscribe once to player defeat.
- Track destruction once per unit.
- Preserve no-reload temporary defeat behavior.

Messages:

```text
After all helicopters die while tank(s) remain:
HELICOPTER OBJECTIVE CLEARED: Destroy remaining tanks.

After all helicopters and tanks die:
MISSION COMPLETE: All enemy helicopters and tanks destroyed. Victory Screen will be added in Milestone 6.
```

States:

```text
Active
HelicopterObjectiveCleared
Defeat
FinalVictoryReady
```

No UI in this milestone.

### Existing Milestone 4 builder compatibility

If `Milestone4EnemyHelicopterBuilder` later configures `CombatMissionController` with helicopters only, minimally update it so it discovers existing `EnemyTanks` and preserves generic `EnemyUnit` tank references. Do not introduce hard dependency on tank script internals.

---

## 15. Explicit exclusions

Do not implement:

```text
Tank movement / NavMesh / pathfinding
Tracks or wheel animation
HUD health bars
Menus
Victory or Game Over UI
Audio
New external downloads
Paid assets
Homing missiles
Tank respawning
Major pooling refactors
Milestone 6 work
```

Stationary defensive tanks with believable turret/missile behavior are intentional.

---

## 16. Required verification

### Static review
Check for:
- Unity 6 compatibility
- namespaces / missing using directives
- stale method signatures
- no direct manual `.unity` / `.meta` edit
- no Packages / ProjectSettings change
- no alteration of imported source assets
- no accidental changes to flight, camera, terrain, player weapons, helicopter behavior

### Unity Play Mode checklist

1. Scene opens with terrain/environment intact and two tanks present.
2. Wait 60 seconds: tanks do not disappear/fall/deactivate.
3. Fly within detection range: turret/barrel tracks helicopter.
4. Hover 25–165 units away at 8–65° elevation: tank fires upward missile.
5. Tank missile validly defeats player; scene does not reload.
6. Tank missile cannot damage other enemy tanks/helicopters.
7. Player missile/bomb damages and destroys tank only at zero health.
8. Destroy helicopters first: helicopter-cleared Console message appears, no final victory.
9. Destroy both tanks too: final-victory-ready Console message appears.
10. No red Console errors.

---

## 17. Completion report format

At completion report:

1. Created files.
2. Modified files.
3. Actual discovered tank source path/name.
4. Imported-model or fallback result.
5. Generated materials/prefabs/scene roots.
6. Tank values: health, detection, firing range, elevation, cooldown, missile damage.
7. Mission-controller compatibility changes.
8. Compile result.
9. Builder result.
10. Real deviations with reasons.
11. Remaining Unity manual tests.
12. Stop after Milestone 5. Do not start Milestone 6.
