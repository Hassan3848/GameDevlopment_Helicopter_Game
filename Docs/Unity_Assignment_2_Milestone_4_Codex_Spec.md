# Unity Assignment 2 — Milestone 4 Codex Specification
## Imported Helicopter Visual Upgrade, Enemy Helicopters, and Modular Flight AI

## Project context

- **Unity:** Unity 6
- **Render pipeline:** Built-in Render Pipeline
- **Target platform:** Windows PC
- **Input:** New Input System
- **Visual style:** polished low-poly military helicopter combat
- **Completed milestones:**
  - Milestone 1: player Rigidbody flight controller and third-person camera
  - Milestone 2: sculpted terrain and natural environment
  - Milestone 3: player missiles, bombs, `Health`, explosions, combat test range
- **Imported asset:** a free Unity Asset Store helicopter pack has already been imported successfully. Its exact path, model names, prefabs, scale, orientation, and materials must be discovered from the project rather than guessed.

---

## 1. Milestone objective

Implement the first real opponents: **two enemy helicopters** that use imported helicopter models, detect the player, pursue/orbit the player in the air, avoid each other, fire non-homing missiles, take damage from the player, and explode when destroyed.

This milestone also safely upgrades the player’s *visual-only* helicopter model from the imported pack where possible. It must not replace or disturb the `PlayerHelicopter` root, flight controller, camera, input, Rigidbody, collider, weapons, or hardpoints.

The final scene must have two live enemy helicopters:

```text
Enemy Helicopter Alpha — dark red visual treatment
Enemy Helicopter Bravo — charcoal/dark gray visual treatment
```

---

## 2. Strict scope boundaries

### Must implement

1. Discover the imported helicopter pack automatically through `AssetDatabase`.
2. Build a safe imported-model wrapper/prefab and use it as the visual for the player where valid.
3. Create two distinct enemy helicopter prefabs or two clean visual variants if only one valid helicopter model is found.
4. Add modular enemy AI: target detection, approach, combat-distance maintenance, orbiting, turn-to-target, and separation steering.
5. Reuse Milestone 3’s damage, `Health`, explosion, missile, collision, and owner-ignore systems.
6. Create an enemy missile variant with a clearly different color/material.
7. Give the player a real `Health` component and a no-UI, future-ready death handler so enemy missiles can cause a temporary defeat state.
8. Make destruction events available for the future Milestone 6 victory system.
9. Place the two enemies safely above terrain in `Game.unity`.
10. Use a repeatable Unity Editor builder for all generated assets and scene changes.

### Must not implement

- Enemy tanks or tank turrets (Milestone 5)
- Main menu, HUD, victory screen, or Game Over screen (Milestone 6)
- Audio content (Milestone 7)
- Homing missiles
- NavMesh/NavMeshAgents
- Multiplayer
- New paid assets or further external downloads
- New input actions
- Any gameplay beyond the two helicopter enemies
- Manual editing of `.unity`, `.meta`, `ProjectSettings`, or `Packages` files

**Important:** Enemy helicopters fly in 3D. Use force/steering-based aerial movement instead of NavMesh.

---

## 3. Required preservation rules

Do not delete, rename, or change the gameplay behavior of any completed milestone content, including:

```text
Assets/_Project/Input/HelicopterControls.inputactions
Assets/_Project/Prefabs/Player/PlayerHelicopter.prefab
Assets/_Project/Scenes/Game.unity
Assets/_Project/Scripts/Player/HelicopterInputReader.cs
Assets/_Project/Scripts/Player/HelicopterFlightController.cs
Assets/_Project/Scripts/Player/HelicopterVisualTilt.cs
Assets/_Project/Scripts/Camera/ThirdPersonHelicopterCamera.cs
Assets/_Project/Scripts/Combat/*
Assets/_Project/Scripts/Weapons/*
Assets/_Project/Scripts/Player/PlayerCombatInputReader.cs
Assets/_Project/Scripts/Player/PlayerWeaponController.cs
All Milestone 2 terrain/environment scripts and generated objects
```

The player must retain:

```text
W/S        forward/backward
A/D        strafe left/right
R/F        ascend/descend
Q/E        yaw left/right
Mouse      camera orbit
Left Click / Space  fire missile
Right Click / B      drop bomb
```

Do **not** rename existing public APIs from Milestone 3. Before coding, inspect the actual `Health`, `DestroyOnDeath`, `Explosion`, `ExplosiveProjectile`, `MissileProjectile`, and `MissileLauncher` implementations and integrate with their real APIs. Do not assume an event or method name that is not present.

---

## 4. Required folder structure

Create folders only if they do not already exist:

```text
Assets/_Project/
├── Art/
│   ├── Materials/
│   └── External/                         # Imported Asset Store content remains here or at its original import location
├── Prefabs/
│   ├── Player/
│   ├── Enemies/
│   ├── Projectiles/
│   └── VFX/
├── Scenes/
├── Scripts/
│   ├── Combat/
│   ├── Enemies/
│   ├── Player/
│   ├── Utilities/
│   └── Editor/
└── Settings/
```

Never move or rename the Asset Store pack’s original imported folder. Generated prefabs and materials belong only under `Assets/_Project/`.

---

## 5. Asset-discovery and model-selection rules

The builder must discover the imported helicopter pack rather than hardcoding an Asset Store path.

### 5.1 Candidate discovery

Use editor-only `AssetDatabase` searches to locate candidates:

- Search `t:Model` and `t:GameObject` asset paths.
- Prefer `.fbx` models and prefab assets whose file/path/name contains case-insensitive words such as:
  - `helicopter`
  - `heli`
  - `chopper`
- Exclude thumbnails, demo scenes, textures, materials, scripts, and files outside `Assets/`.
- Build a concise resolved-candidate report in the Console and final Codex response.

### 5.2 Candidate ranking

Rank candidate models in this order:

1. Valid prefab/model with at least one enabled `Renderer`.
2. Name/path visibly related to helicopter.
3. Higher renderer/mesh count preferred over a plain empty root.
4. Distinct source model preferred for Alpha and Bravo where available.

Use:

```text
Best resolved candidate  → Player imported visual
Second valid candidate   → Enemy Alpha visual
Third valid candidate    → Enemy Bravo visual
```

If only one usable helicopter model is found, use it for player, Alpha, and Bravo; create enemy visual variation through different materials, rotor settings, and scale only. This is a valid fallback and must be reported.

If no usable model is found, preserve the original player placeholder visual and create simple fallback enemy helicopter silhouettes. Log an error/warning that clearly identifies the discovery failure. Do not fail the entire builder.

### 5.3 Visual normalization

For every imported helicopter visual:

- Instantiate it underneath a generated `VisualPivot`/wrapper object, never directly as the physics root.
- Measure enabled renderers’ combined bounds after instantiation.
- Normalize scale so the largest horizontal visual dimension is approximately **6.5 Unity units**.
- Center the visual around its wrapper/root so the collider and weapon hardpoints are sensible.
- Preserve source materials for the player whenever possible.
- Preserve existing player `HelicopterVisualTilt` by keeping its component and replacing only its child visual content.
- Do not edit the imported source asset itself.
- Do not apply arbitrary rotations unless the source root clearly needs it. Prefer the source model’s default forward orientation and log the selected source asset path.

**Orientation fallback:** If source orientation is visibly reversed after build, do not alter flight code. Put the correction only on the generated wrapper transform and report the wrapper rotation so it can be adjusted later. Default wrapper rotation is `(0, 0, 0)`.

---

## 6. Required new code files and exact responsibilities

Create the following files unless the same class already exists with the same responsibility. Do not merge unrelated responsibilities into one giant script.

### 6.1 `Assets/_Project/Scripts/Combat/EnemyUnit.cs`

**Purpose:** generic future-ready marker and destruction event for any enemy unit, including helicopters now and tanks later.

Requirements:

- Namespace: `HelicopterCombat.Combat`.
- Require or resolve an existing `Health` component.
- Serialized fields:
  - `displayName` default `Enemy Unit`
  - `unitType` enum with at least `Helicopter` and `Tank`
- Public properties:
  - `DisplayName`
  - `UnitType`
  - `IsDestroyed`
- Subscribe to the actual death event exposed by the existing `Health` component.
- Raise an instance event once when the unit dies.
- Raise a static notification/event once when any `EnemyUnit` dies.
- Unsubscribe correctly in `OnDisable`/`OnDestroy`.
- No direct UI, score, scene loading, weapon, or VFX logic.

This is a **future hook** for Milestone 6. It must not try to display victory now.

---

### 6.2 `Assets/_Project/Scripts/Player/PlayerDeathHandler.cs`

**Purpose:** temporary non-UI response when enemy missiles reduce player health to zero.

Requirements:

- Namespace: `HelicopterCombat.Player`.
- Resolve an existing `Health` component.
- Serialized references (assign in builder):
  - `HelicopterInputReader`
  - `HelicopterFlightController`
  - `PlayerCombatInputReader` if present
  - `PlayerWeaponController` if present
  - player `Rigidbody`
- Public property: `IsDefeated`.
- Public event/static notification appropriate for Milestone 6 to listen to later.
- On player death:
  1. Run exactly once.
  2. Disable flight input/movement and combat input/weapons.
  3. Stop or safely neutralize the Rigidbody without destroying the object.
  4. Keep the camera functioning so the scene remains observable.
  5. Write one clear `Debug.Log` message explaining that Game Over UI comes in Milestone 6.
- Do not reload the scene, show UI, or destroy the player here.
- Do not add explosion/VFX logic unless an existing safe visual death effect is already easy to reuse; visual polish belongs later.

Player `Health` exact values when builder adds/configures it:

```text
Max Health: 250
Destroy When Dead: false
```

---

### 6.3 `Assets/_Project/Scripts/Enemies/EnemyHelicopterTargeting.cs`

**Purpose:** owns only target acquisition and target-related measurements.

Requirements:

- Namespace: `HelicopterCombat.Enemies`.
- Serialized fields:
  - `Transform target`
  - `float detectionRange = 220f`
  - `float loseTargetRange = 250f`
  - `float targetHeightOffset = 6f`
- Public properties/methods:
  - `Transform CurrentTarget`
  - `bool HasTarget`
  - `float DistanceToTarget`
  - `Vector3 TargetPosition`
  - `Vector3 DirectionToTarget`
  - `void SetTarget(Transform newTarget)`
- Resolve player through builder assignment. Do not call `FindWithTag` every frame.
- When target is beyond `loseTargetRange`, report no target; when inside detection range, report target acquired.
- Use no movement or weapon logic.

---

### 6.4 `Assets/_Project/Scripts/Enemies/EnemyHelicopterSeparation.cs`

**Purpose:** produce a separation vector that helps enemy helicopters avoid overlapping or colliding.

Requirements:

- Namespace: `HelicopterCombat.Enemies`.
- Serialized fields:
  - `float separationRadius = 18f`
  - `float separationStrength = 1.5f`
  - `LayerMask queryLayers = Physics.DefaultRaycastLayers`
- Use a fixed reusable collider buffer (`Physics.OverlapSphereNonAlloc`) where practical.
- Identify other `EnemyHelicopterBrain` instances from hit colliders/parents.
- Ignore self and disabled/dead enemies.
- Public method/property returns a normalized or weighted local/world separation direction.
- Do not alter Rigidbody movement directly.
- Never allocate a new array every physics frame.

---

### 6.5 `Assets/_Project/Scripts/Enemies/EnemyHelicopterMovement.cs`

**Purpose:** owns Rigidbody flight movement and yaw steering only.

Requirements:

- Namespace: `HelicopterCombat.Enemies`.
- Require `Rigidbody`.
- Serialized references:
  - `Rigidbody helicopterRigidbody`
  - `Terrain terrain`
  - `EnemyHelicopterSeparation separation`
- Serialized tuning fields:

```text
Cruise Speed:              24
Maximum Speed:             30
Acceleration:              16
Braking Acceleration:      20
Yaw Speed:                 115 degrees/second
Yaw Acceleration:          300 degrees/second²
Combat Distance:           58
Minimum Combat Distance:   38
Combat Distance Tolerance: 8
Orbit Strength:            0.55
Minimum Terrain Clearance: 24
Preferred Altitude Offset: 34
Altitude Response:         2.4
Vertical Speed Limit:      13
```

Behavior:

- Use `FixedUpdate` / Rigidbody-compatible force and rotation operations only.
- Do not use NavMesh.
- Maintain flight above terrain:
  - sample active/assigned terrain height where present;
  - desired altitude must never be below terrain height + `Minimum Terrain Clearance`;
  - if player is higher, allow the enemy to track a reasonable height above/near the player.
- When far from player, approach smoothly.
- When inside combat band, maintain approximately `Combat Distance` and blend a horizontal orbit/tangent direction.
- When too close, retreat smoothly while maintaining safe altitude.
- Blend separation direction from `EnemyHelicopterSeparation` before calculating acceleration.
- Rotate yaw toward the desired horizontal flight/aim direction smoothly.
- Keep physics root upright: freeze physical X/Z rotation or otherwise ensure root does not roll from forces.
- Expose read-only velocity/normalized movement values for visual tilt.
- Do not know how missiles fire, which target was selected, or how damage is applied.

---

### 6.6 `Assets/_Project/Scripts/Enemies/EnemyHelicopterWeaponController.cs`

**Purpose:** decide when an enemy can fire; reuse the existing Milestone 3 `MissileLauncher` for actual spawning.

Requirements:

- Namespace: `HelicopterCombat.Enemies`.
- Serialized references:
  - `EnemyHelicopterTargeting targeting`
  - `MissileLauncher missileLauncher`
  - `Transform aimOrigin`
  - optional target `Rigidbody` cache/reference
- Serialized fields:

```text
Attack Range:        115
Minimum Attack Range: 30
Required Aim Angle:  18 degrees
Initial Fire Delay:  1.5 seconds
Lead Time:           0.20 seconds
```

Behavior:

- Do not instantiate a projectile directly if the existing `MissileLauncher` can launch one.
- Use the existing launcher with `EnemyMissile.prefab`, unlimited/practically unlimited ammo, and its own fire cooldown.
- Only request `missileLauncher.TryFire()` when:
  - target exists;
  - target distance is in attack range;
  - enemy is facing the predicted target direction within `Required Aim Angle`;
  - initial fire delay has passed;
  - launcher cooldown/ammo rules permit firing.
- Predict aim with target Rigidbody velocity only when safely available; otherwise aim at target position.
- No homing. Enemy missiles must remain unguided after launch.
- Do not move or rotate the helicopter root directly; movement/brain handles that.

Enemy launcher values set by builder:

```text
Max Ammo:       999
Fire Cooldown:  2.40 seconds
Launch Points:  left and right hardpoints
Owner Root:     enemy helicopter root
Owner Rigidbody: enemy helicopter Rigidbody
```

---

### 6.7 `Assets/_Project/Scripts/Enemies/EnemyHelicopterVisualController.cs`

**Purpose:** visual-only tilt and optional rotor spinning; no physics movement or combat decisions.

Requirements:

- Namespace: `HelicopterCombat.Enemies`.
- Serialized references:
  - `EnemyHelicopterMovement movement`
  - `Transform visualPivot`
  - `Transform[] rotorTransforms`
- Serialized fields:

```text
Forward Tilt Angle:  10
Side Tilt Angle:     14
Tilt Smoothness:      5
Rotor Speed:       1200 degrees/second
Rotor Local Axis:  (0, 1, 0)
```

Behavior:

- Tilt only the visual pivot based on the movement component’s local velocity/intention.
- Keep the Rigidbody physics root stable and upright.
- Spin rotor transforms only when they were safely found by name (`rotor`, `propeller`, `mainrotor`, `tailrotor`).
- It is acceptable for no rotor transforms to be found; no warnings every frame.
- Visual updates may run in `LateUpdate`/`Update`; physics must remain in movement code.

---

### 6.8 `Assets/_Project/Scripts/Enemies/EnemyHelicopterBrain.cs`

**Purpose:** lightweight orchestration/state ownership for one enemy helicopter.

Requirements:

- Namespace: `HelicopterCombat.Enemies`.
- Require or resolve:
  - `EnemyHelicopterTargeting`
  - `EnemyHelicopterMovement`
  - `EnemyHelicopterWeaponController`
  - `Health`
- Serialized field:
  - `bool startActive = true`
- Include readable states such as:

```text
Idle
Pursue
Engage
Defeated
```

Behavior:

- On startup, use the player transform assigned by builder.
- `Idle`: hover safely if no target.
- `Pursue`: target detected but not within preferred combat distance.
- `Engage`: target in active combat range; movement maintains/orbits distance and weapon controller can fire.
- `Defeated`: stop requesting movement and weapons after `Health` death.
- Brain coordinates components; it must not contain projectile spawning, low-level Rigidbody force math, explosion instantiation, or renderer material work.

---

### 6.9 `Assets/_Project/Scripts/Utilities/RotorSpinner.cs`

**Purpose:** optional generic visual-only rotor spinner used by imported player and enemy visuals.

Requirements:

- Namespace: `HelicopterCombat.Utilities`.
- Serialized `Transform[] rotors`, local axis, and speed.
- No dependency on player/enemy AI.
- Safely does nothing when no rotor transforms are supplied.
- Builder may attach it to player `HelicopterVisual` after imported visual replacement and populate it by names where possible.

---

### 6.10 `Assets/_Project/Scripts/Editor/Milestone4EnemyHelicopterBuilder.cs`

**Purpose:** the only editor-side mechanism for generated assets, player visual upgrade, prefabs, and `Game.unity` updates for this milestone.

Required namespace:

```text
HelicopterCombat.EditorTools
```

Required menu command:

```text
Tools > Helicopter Combat > Rebuild Milestone 4 Enemy Helicopters
```

Required command-line entry point:

```csharp
HelicopterCombat.EditorTools.Milestone4EnemyHelicopterBuilder.RebuildFromCommandLine
```

Detailed builder requirements are in Sections 7–10.

---

## 7. Generated assets and exact Inspector values

All generated assets must be placed below `Assets/_Project/`.

### 7.1 Materials

Create or update these Built-in Render Pipeline-compatible materials:

```text
Assets/_Project/Art/Materials/M4_EnemyRed.mat
Assets/_Project/Art/Materials/M4_EnemyCharcoal.mat
Assets/_Project/Art/Materials/M4_EnemyMissile.mat
```

Use the Standard shader when available.

| Material | Albedo/appearance | Metallic | Smoothness |
|---|---:|---:|---:|
| `M4_EnemyRed` | dark military red / muted crimson | 0.25 | 0.35 |
| `M4_EnemyCharcoal` | charcoal/dark gray | 0.35 | 0.30 |
| `M4_EnemyMissile` | dark red with mild emission-like readability if safe | 0.20 | 0.45 |

Enemy visual material handling:

- First try a `MaterialPropertyBlock`/supported `_Color` property to retain imported detail.
- If source shader does not support color tint reliably, assign the generated material to ensure Alpha and Bravo can be clearly distinguished.
- Never modify source materials inside the imported Asset Store pack.

### 7.2 Enemy missile prefab

Create/update:

```text
Assets/_Project/Prefabs/Projectiles/EnemyMissile.prefab
```

Preferred approach:

- Duplicate/build from the existing `PlayerMissile.prefab` through safe Unity Editor prefab APIs.
- Preserve collider, Rigidbody, `MissileProjectile`, smoke, and explosion configuration.
- Change only the visual material and combat tuning where fields are safely accessible.

Exact intended enemy missile values:

```text
Use Gravity:                       false
Rigidbody Interpolation:           Interpolate
Collision Detection:               Continuous Dynamic
Speed:                             58
Inherited Owner Velocity Multiplier: 0.30
Damage:                            28
Explosion Radius:                  5
Explosion Force:                   250
Lifetime:                          5
Arming Delay:                      0.12
```

If current Milestone 3 script field/property names differ, set the equivalent values without changing the public behavior of player missiles.

### 7.3 Enemy helicopter prefabs

Create/update:

```text
Assets/_Project/Prefabs/Enemies/EnemyHelicopterAlpha.prefab
Assets/_Project/Prefabs/Enemies/EnemyHelicopterBravo.prefab
```

Required root hierarchy:

```text
EnemyHelicopterAlpha / EnemyHelicopterBravo
├── VisualPivot
│   └── ImportedHelicopterModel
├── MissileHardpointLeft
├── MissileHardpointRight
└── AimOrigin
```

Required root components:

```text
Rigidbody
BoxCollider or correctly sized generated collider
Health
DestroyOnDeath
EnemyUnit
EnemyHelicopterTargeting
EnemyHelicopterSeparation
EnemyHelicopterMovement
MissileLauncher
EnemyHelicopterWeaponController
EnemyHelicopterVisualController
EnemyHelicopterBrain
```

Root Rigidbody values:

```text
Use Gravity:              false
Mass:                     5
Linear Damping:           0.55
Angular Damping:          1.0
Interpolate:              Interpolate
Collision Detection:      Continuous Dynamic
Freeze Rotation X:        enabled
Freeze Rotation Z:        enabled
```

Root Health values:

```text
Max Health:               150
Destroy When Dead:        false
```

`DestroyOnDeath` values:

```text
Explosion Prefab:         existing Assets/_Project/Prefabs/VFX/Explosion.prefab
Destroy Delay:            0.05
Destroy Root Object:      true
```

`EnemyUnit` values:

```text
Display Name:             Enemy Helicopter Alpha / Enemy Helicopter Bravo
Unit Type:                Helicopter
```

Hardpoint local transforms:

```text
MissileHardpointLeft
Position: (-1.45, -0.35, 1.80)
Rotation: (0, 0, 0)

MissileHardpointRight
Position: (1.45, -0.35, 1.80)
Rotation: (0, 0, 0)

AimOrigin
Position: (0, 0.25, 1.70)
Rotation: (0, 0, 0)
```

If normalized imported visuals clearly have a different scale, hardpoints may be adjusted only enough to lie at the front/under-side of the visual. Report any adjustment.

`MissileLauncher` references and values:

```text
Missile Prefab:           EnemyMissile.prefab
Launch Points:            left then right hardpoint
Owner Root:               enemy root
Owner Rigidbody:          enemy root Rigidbody
Max Ammo:                 999
Fire Cooldown:            2.40
```

Visual differences:

```text
Alpha: use M4_EnemyRed material/tint
Bravo: use M4_EnemyCharcoal material/tint
```

### 7.4 Player visual-only update

Preserve the existing root structure and scripts:

```text
PlayerHelicopter
├── HelicopterVisual                  # Keep this object and its existing tilt component
│   └── M4_ImportedHelicopterModel    # Generated imported model child
├── MissileHardpointLeft               # preserve existing
├── MissileHardpointRight              # preserve existing
└── BombDropPoint                      # preserve existing
```

Rules:

- Do not replace `PlayerHelicopter` root.
- Do not remove existing Rigidbody, collider, flight, camera, input, weapon, or hardpoint components.
- Add the generated imported child under existing `HelicopterVisual` only after confirming a valid imported model was found.
- Remove/disable only the old primitive placeholder model children after the imported child is valid.
- Preserve player-friendly original materials from the imported source where possible.
- Add `RotorSpinner` only if rotor transforms can be safely identified.
- If player visual import cannot be normalized safely, leave the original player visual untouched and report the reason.

---

## 8. Scene setup in `Game.unity`

The builder must open and update:

```text
Assets/_Project/Scenes/Game.unity
```

### 8.1 Existing scene prerequisites

Resolve existing references safely:

```text
PlayerHelicopter
Main Camera
Directional Light
active Terrain / Terrain.activeTerrain if present
```

Never create duplicates of those objects.

### 8.2 Player health setup

On `PlayerHelicopter` root:

- Add `Health` if it is absent.
- Configure exactly:

```text
Max Health: 250
Destroy When Dead: false
```

- Add/configure `PlayerDeathHandler`.
- Assign player flight, combat, and Rigidbody references.
- Do not add `DestroyOnDeath` to the player in this milestone.

### 8.3 Enemy scene root and placements

Create/update one owned root only:

```text
EnemyHelicopters
├── EnemyHelicopterAlpha
└── EnemyHelicopterBravo
```

Compute terrain height through the active terrain when available. Spawn positions relative to player are:

```text
Alpha horizontal offset: (-82, +145)
Bravo horizontal offset: (+88, +175)
```

For each enemy:

```text
Y = max(terrain height + 48, player Y + 26)
```

Then orient each enemy root to face generally toward the player on the Y axis. If terrain is unavailable, use world Y values of `48` for Alpha and `58` for Bravo.

Safety rules:

- Keep enemies at least 75 metres from player at scene start.
- Keep at least 55 metres between Alpha and Bravo at scene start.
- Do not place them below terrain.
- Do not add obstacle objects or alter the environment.
- If the existing `CombatTestRange` root remains in the scene, set it inactive rather than deleting it. This prevents Milestone 3 placeholder targets from being confused with real enemies while preserving the test range for reference.

### 8.4 Enemy references

Builder must assign every prefab/scene reference explicitly:

```text
EnemyHelicopterTargeting.target → PlayerHelicopter transform
EnemyHelicopterMovement.terrain → active terrain where available
EnemyHelicopterMovement.separation → same enemy root separation component
EnemyHelicopterWeaponController.targeting → same root targeting component
EnemyHelicopterWeaponController.missileLauncher → same root MissileLauncher
EnemyHelicopterWeaponController.aimOrigin → AimOrigin
EnemyHelicopterVisualController.movement → same root movement component
EnemyHelicopterVisualController.visualPivot → VisualPivot
EnemyHelicopterBrain → all same-root behavior references
DestroyOnDeath.explosionPrefab → existing Explosion.prefab
```

### 8.5 Repeatability

Builder must be idempotent:

- Remove/rebuild only the root it owns: `EnemyHelicopters`.
- Update/reuse known generated assets rather than endlessly duplicating materials/prefabs.
- Remove/rebuild `M4_ImportedHelicopterModel` only, not unrelated player visual or gameplay objects.
- Do not duplicate `Health`, `PlayerDeathHandler`, enemy components, or hardpoints when run more than once.
- Preserve all terrain/environment objects.
- Save `Game.unity` only after successful setup.
- Log a concise success summary including selected source model paths, prefabs created, and scene objects created.

---

## 9. AI behavior acceptance criteria

During Play Mode, both enemy helicopters must demonstrate these visible behaviors:

1. **Detect player**
   - They activate when player is inside the 220 m detection range.

2. **Follow player**
   - When far away, they move toward the player smoothly instead of teleporting.

3. **Maintain combat distance**
   - They should prefer roughly 58 m distance.
   - If too close, they back away instead of passing through the player.

4. **Orbit / believable aerial behavior**
   - In combat distance, they move with a modest sideways/orbit tendency rather than stopping completely.

5. **Avoid one another**
   - When their separation becomes low, they steer apart.
   - They must not constantly occupy the same point or collide repeatedly.

6. **Aim and fire**
   - They rotate naturally toward the player before firing.
   - They fire an enemy-colored unguided missile every ~2.4 seconds only when in range and aim cone.

7. **Receive player damage**
   - Player missiles/bombs reduce enemy health.
   - Each enemy is destroyed/explodes at zero health.

8. **Damage player**
   - Enemy missiles can reduce player health.
   - At zero health, `PlayerDeathHandler` stops player control and logs the temporary defeat state without UI.

9. **No unrealistic shortcuts**
   - No teleports, no homing missiles, no NavMesh, no direct scene reload, no `Find` calls every frame.

---

## 10. Code quality and Unity rules

1. Use these namespaces:

```text
HelicopterCombat.Combat
HelicopterCombat.Player
HelicopterCombat.Enemies
HelicopterCombat.Utilities
HelicopterCombat.EditorTools
```

2. One class, one responsibility.
3. Use `[SerializeField]` fields for Inspector configuration.
4. Use `RequireComponent` for mandatory same-object dependencies.
5. All physics forces/velocity work must be in `FixedUpdate` or controlled physics methods.
6. Visual interpolation/spinning may be in `Update` or `LateUpdate`.
7. Use `Rigidbody.linearVelocity` consistently with Milestone 1/3 where applicable.
8. Do not instantiate memory-heavy assets repeatedly outside normal missile firing.
9. Avoid per-frame `Find`, `GetComponentsInChildren`, and new arrays/lists where possible.
10. Guard missing references with a clear one-time warning/error, never a per-frame Console spam.
11. Do not add packages, edit manifests, or manually generate `.meta` files.
12. Do not commit or push to GitHub.
13. Do not convert the project to URP/HDRP.
14. Do not alter existing Input Action map/action names.
15. Do not edit imported Asset Store source assets; treat them as read-only.

---

## 11. Required verification

After implementation:

1. Inspect all new and modified scripts for missing namespaces, missing `using`, event-unsubscribe, and Unity 6 API issues.
2. Confirm Milestones 1–3 scripts were not accidentally changed except for an unavoidable, documented compatibility addition.
3. Let Unity compile fully.
4. Run:

```text
Tools > Helicopter Combat > Rebuild Milestone 4 Enemy Helicopters
```

5. Open:

```text
Assets/_Project/Scenes/Game.unity
```

6. Verify in the Hierarchy:

```text
PlayerHelicopter
EnemyHelicopters
  EnemyHelicopterAlpha
  EnemyHelicopterBravo
```

7. In Play Mode, verify:

```text
- Player flight/camera/weapons still work.
- Imported player visual appears without breaking flight root.
- Two imported-model enemies appear over terrain.
- Enemies detect and move toward player.
- Enemies maintain spacing and steer away from each other.
- Enemies fire visible enemy missiles.
- Player missiles/bombs damage and destroy enemies.
- Enemy explosions trigger once per destroyed enemy.
- Enemy missiles damage player.
- Player controls disable at zero player health without UI/scene reload.
- Re-running builder does not duplicate enemy roots, player components, or imported-model children.
- Console has zero red errors.
```

If Codex can use a safe Unity command-line executable for compilation/build execution, it may use the specified builder entry point. It must still clearly state which tests need Unity Play Mode verification by the user.

---

## 12. Final reporting format for Codex

At the end, report exactly:

1. **Imported asset discovery**
   - List the helicopter asset candidates found.
   - Identify the source asset path selected for player, Alpha, and Bravo.
   - State whether different source models or one shared model was used.

2. **Created/changed files**
   - List every created or modified source, prefab, material, and scene file.

3. **Player visual update**
   - Confirm whether imported visual replacement succeeded.
   - Confirm original player flight/camera/weapons were preserved.
   - Report wrapper scale/rotation if adjusted.

4. **Enemy setup**
   - Confirm Enemy Alpha and Bravo prefab paths.
   - Confirm health, missile, spawn, and material values.

5. **Combat integration**
   - Confirm reuse of existing Milestone 3 health/explosion/missile systems.
   - Confirm player health/death-handler setup.
   - Confirm future `EnemyUnit` destruction-event hook.

6. **Verification**
   - Latest compile result.
   - Builder result.
   - Any safe automated checks performed.
   - Unity Play Mode tests still required.

7. **Deviations**
   - List only real deviations from this specification and why they were necessary.

8. **Stop condition**
   - State that Milestone 4 is complete.
   - Do not start Milestone 5.
