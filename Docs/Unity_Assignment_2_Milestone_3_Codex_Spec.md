# Unity Assignment 2 — Milestone 3 Codex Specification
## Player Weapons, Damage, Explosions, and Combat Test Range

**Project context**
- Unity version: Unity 6
- Render pipeline: Built-in Render Pipeline
- Platform: Windows PC
- Input: New Input System
- Visual style: polished low-poly / beginner-to-intermediate military helicopter game
- Existing work: Milestone 1 flight/camera and Milestone 2 terrain/environment are complete and working.

---

## 1. Milestone objective

Implement a modular combat foundation for the player helicopter.

The completed milestone must let the player:
- Fire unguided missiles from helicopter hardpoints.
- Drop gravity-driven bombs from the helicopter.
- Damage destructible test targets.
- Trigger visible explosions and smoke trails.
- Verify that missile and bomb logic works safely in the terrain environment.

This milestone is **not** for enemy AI, tanks, UI, audio, or final VFX polish.

---

## 2. Strict scope boundaries

### Must implement
1. Reusable damage interface and health component.
2. Reusable explosion effect and radial damage component.
3. Missile projectile, launcher, spawn hardpoints, smoke trail, impact explosion.
4. Bomb projectile, launcher, gravity, impact explosion.
5. Player combat input reader and weapon controller.
6. Generated placeholder projectile models and particle effects.
7. A test range with airborne missile targets and ground bomb targets.
8. Input action additions for firing missiles and dropping bombs.
9. A Unity Editor builder script that creates/updates all required generated assets and scene changes.

### Must not implement
- Enemy helicopter AI
- Tanks with AI/turret behavior
- Homing missiles
- HUD or menu
- Audio
- Inventory/reload pickups
- Multiplayer
- Mission system
- Paid assets
- Milestone 4 or later work

---

## 3. Existing project assets that must be preserved

Do not remove, rename, or alter gameplay behavior of:
- `Assets/_Project/Input/HelicopterControls.inputactions`
- `Assets/_Project/Prefabs/Player/PlayerHelicopter.prefab`
- `Assets/_Project/Scenes/Game.unity`
- All existing Milestone 1 player and camera scripts
- All existing Milestone 2 terrain/environment assets and builder scripts

The player helicopter must retain:
- Smooth Rigidbody flight.
- W/S/A/D movement.
- R/F altitude.
- Q/E yaw.
- Mouse camera orbit.
- Existing third-person camera behavior.

Use Unity Editor APIs through the builder for scene and asset updates. Do not manually write `.unity` or `.meta` files.

---

## 4. Required folder structure

Create folders only if missing:

```text
Assets/_Project/
├── Art/
│   ├── Materials/
│   └── VFX/
├── Input/
├── Prefabs/
│   ├── Player/
│   ├── Projectiles/
│   ├── VFX/
│   └── TestTargets/
├── Scenes/
├── Scripts/
│   ├── Combat/
│   ├── Player/
│   ├── Weapons/
│   ├── VFX/
│   └── Editor/
└── Settings/
```

---

## 5. Required source files and responsibilities

Create exactly these scripts unless a file with the same responsibility already exists and can safely be extended.

### `Assets/_Project/Scripts/Combat/IDamageable.cs`
Purpose: minimal reusable contract for objects that can receive damage.

Required public API:
```csharp
void ApplyDamage(float damage, GameObject source);
```

Rules:
- No Unity lifecycle code.
- No references to UI, weapons, enemies, or VFX.

---

### `Assets/_Project/Scripts/Combat/Health.cs`
Purpose: hold health state and process damage.

Requirements:
- `MonoBehaviour`.
- Inspector-friendly serialized fields:
  - `maxHealth` default `100f`.
  - `destroyWhenDead` default `false`.
- Public read-only `CurrentHealth`, `MaxHealth`, and `IsDead`.
- Public event raised when health changes.
- Public event raised once when death happens.
- Clamp health from `0` to `maxHealth`.
- Ignore non-positive damage and ignore damage after death.
- Do not know anything about missiles, bombs, UI, or enemy types.
- Do not directly spawn particles.
- If `destroyWhenDead` is true, destroy the root object after a short safe delay; otherwise leave destruction to a separate component.

---

### `Assets/_Project/Scripts/Combat/DestroyOnDeath.cs`
Purpose: visual/deletion response to a `Health` component dying.

Requirements:
- Requires or resolves `Health`.
- Serialized fields:
  - `explosionPrefab`
  - `destroyDelay` default `0.05f`
  - `destroyRootObject` default `true`
- On death:
  - Instantiate explosion prefab at the target’s visual center.
  - Destroy target root after delay when enabled.
- Must not re-trigger after death.
- Keep this component separate from `Health`.

---

### `Assets/_Project/Scripts/Combat/Explosion.cs`
Purpose: reusable explosion effect with radial damage and optional physical force.

Required serialized fields:
- `damage` default `60f`
- `radius` default `7f`
- `force` default `450f`
- `upwardForce` default `0.75f`
- `lifeTime` default `2.5f`
- `damageLayers` default all layers
- `sourceRoot` runtime-settable, not required in Inspector

Required behavior:
- Method: `Initialize(float damage, float radius, float force, GameObject source)` or equivalent clear initialization method.
- On activation/initialization, use `Physics.OverlapSphereNonAlloc` where practical; otherwise avoid repeated allocations.
- Find `IDamageable` on each hit collider or parent.
- Damage each unique damageable once per explosion.
- Do not damage the source object or any of its children.
- Add explosion force to nearby non-kinematic rigidbodies.
- Destroy itself after `lifeTime`.
- Must work for projectiles now and enemy objects later.

---

### `Assets/_Project/Scripts/Combat/ExplosiveProjectile.cs`
Purpose: shared impact/lifetime/owner logic for explosive projectiles.

Requirements:
- Abstract `MonoBehaviour`.
- Requires `Rigidbody` and `Collider`.
- Shared serialized fields:
  - `explosionPrefab`
  - `damage`
  - `explosionRadius`
  - `explosionForce`
  - `lifeTime`
  - `armingDelay`
- Runtime owner setup method receives owner root and optional owner rigidbody.
- On spawn, ignore collisions between projectile collider(s) and all owner colliders.
- On valid collision after arming delay, create explosion then destroy projectile.
- On timeout, create explosion then destroy projectile.
- Do not trigger multiple explosions.
- Projectile collision detection must be continuous/dynamic.

---

### `Assets/_Project/Scripts/Weapons/MissileProjectile.cs`
Purpose: forward-moving missile behavior.

Requirements:
- Inherits `ExplosiveProjectile`.
- Inspector fields:
  - `speed` default `65f`
  - `inheritOwnerVelocityMultiplier` default `0.35f`
- Launch in the initial forward direction.
- Use Rigidbody velocity/linear velocity so the projectile travels consistently.
- Keep visual model aligned to velocity direction.
- No homing behavior.
- Add a TrailRenderer or lightweight ParticleSystem smoke trail.
- Default combat values:
  - damage `60`
  - radius `7`
  - force `450`
  - lifetime `5`
  - arming delay `0.08`

---

### `Assets/_Project/Scripts/Weapons/BombProjectile.cs`
Purpose: gravity-driven bomb behavior.

Requirements:
- Inherits `ExplosiveProjectile`.
- Rigidbody gravity enabled.
- Inspector fields:
  - `initialForwardSpeed` default `8f`
  - `inheritOwnerVelocityMultiplier` default `1f`
  - `gravityMultiplier` default `1f`
- Bomb must spawn below the helicopter and fall naturally.
- Optional small visual spin is acceptable only if it does not affect physics realism.
- Default combat values:
  - damage `120`
  - radius `10`
  - force `800`
  - lifetime `8`
  - arming delay `0.15`

---

### `Assets/_Project/Scripts/Weapons/MissileLauncher.cs`
Purpose: missile spawning, ammo, cooldown, and alternating hardpoints.

Requirements:
- `MonoBehaviour`.
- Inspector fields:
  - `missilePrefab`
  - `launchPoints` array
  - `ownerRoot`
  - `ownerRigidbody`
  - `maxAmmo` default `20`
  - `fireCooldown` default `0.25f`
- Public read-only `CurrentAmmo`.
- Public method `TryFire()`.
- Use alternating left/right hardpoints when more than one exists.
- Return safely without errors when no ammo, no prefab, no hardpoint, or cooldown active.
- Do not read input directly.

---

### `Assets/_Project/Scripts/Weapons/BombLauncher.cs`
Purpose: bomb spawning, ammo, and cooldown.

Requirements:
- `MonoBehaviour`.
- Inspector fields:
  - `bombPrefab`
  - `dropPoint`
  - `ownerRoot`
  - `ownerRigidbody`
  - `maxAmmo` default `8`
  - `dropCooldown` default `0.55f`
- Public read-only `CurrentAmmo`.
- Public method `TryDrop()`.
- Spawn bomb clear of helicopter collision volume.
- Do not read input directly.

---

### `Assets/_Project/Scripts/Player/PlayerCombatInputReader.cs`
Purpose: input-only adapter for weapon actions.

Requirements:
- Must not modify existing `HelicopterInputReader` unless necessary.
- Serialized `InputActionAsset` reference.
- Resolve these actions from action map `Helicopter`:
  - `FireMissile`
  - `DropBomb`
- Enable and disable only its own actions safely.
- Expose one-shot events or boolean polling methods for button presses.
- No launcher references and no projectile behavior.

---

### `Assets/_Project/Scripts/Player/PlayerWeaponController.cs`
Purpose: route player combat input to launchers.

Requirements:
- Serialized references:
  - `PlayerCombatInputReader`
  - `MissileLauncher`
  - `BombLauncher`
- On missile input, call `MissileLauncher.TryFire()`.
- On bomb input, call `BombLauncher.TryDrop()`.
- Must not contain movement, projectile, damage, or VFX logic.
- Must unsubscribe from input events in `OnDisable`.

---

### `Assets/_Project/Scripts/Editor/Milestone3CombatBuilder.cs`
Purpose: single editor-only setup mechanism for all Milestone 3 generated content.

Menu command:
```text
Tools > Helicopter Combat > Rebuild Milestone 3 Combat
```

Optional command-line entry point:
```csharp
HelicopterCombat.EditorTools.Milestone3CombatBuilder.RebuildFromCommandLine
```

Use an editor-only namespace such as:
```csharp
HelicopterCombat.EditorTools
```

Detailed responsibilities are in Section 8.

---

## 6. Input Actions requirements

Update the existing asset:

```text
Assets/_Project/Input/HelicopterControls.inputactions
```

Action map: `Helicopter`

Add these actions if missing:

| Action | Type | Primary binding | Optional backup binding |
|---|---|---|---|
| `FireMissile` | Button | `<Mouse>/leftButton` | `<Keyboard>/space` |
| `DropBomb` | Button | `<Mouse>/rightButton` | `<Keyboard>/b` |

Important:
- Preserve all existing movement, altitude, yaw, and look actions.
- Do not delete or rename existing action maps/actions.
- Do not create duplicate actions if they already exist.
- Update the asset through Unity-compatible APIs and save/import it correctly.
- Existing camera cursor-lock behavior may continue using left-click when cursor is unlocked. When the cursor is locked, left-click must fire missiles normally.

---

## 7. Generated assets and exact prefab requirements

All generated assets must be placed under `Assets/_Project/`.

### Materials
Create or update these materials in:
```text
Assets/_Project/Art/Materials/
```

Required materials:
- `M3_Missile.mat`: dark military gray or olive.
- `M3_Bomb.mat`: dark gray.
- `M3_ExplosionFire.mat`: warm orange/yellow emission-like visual if supported.
- `M3_ExplosionSmoke.mat`: dark translucent smoke.
- `M3_TestHelicopterTarget.mat`: clearly visible red/orange.
- `M3_TestTankTarget.mat`: clearly visible yellow/olive.

Use simple Built-in pipeline compatible shaders. No paid or external assets are needed.

### Explosion prefab
Path:
```text
Assets/_Project/Prefabs/VFX/Explosion.prefab
```

Requirements:
- Root name: `Explosion`.
- Add `Explosion` script.
- At least two Particle Systems:
  1. fire/flash burst
  2. smoke burst
- Optional short Point Light allowed.
- Auto-destroys after approximately 2.5 seconds.
- Make it readable and lightweight; do not create excessive particle counts.

Suggested visual values:
- Fire burst: 30–50 particles, 0.5–0.9 sec lifetime, start size 1.5–4.5.
- Smoke: 12–24 particles, 1.5–2.5 sec lifetime, start size 2–5.
- Play on Awake enabled.
- No looping.

### Missile prefab
Path:
```text
Assets/_Project/Prefabs/Projectiles/PlayerMissile.prefab
```

Requirements:
- Root name: `PlayerMissile`.
- Visual: procedural cylinder/capsule-like low-poly missile; small nose cone/fins optional.
- Rigidbody:
  - Use Gravity: false
  - Interpolate: Interpolate
  - Collision Detection: Continuous Dynamic
  - Mass: 1
- Collider: CapsuleCollider or SphereCollider sized closely to visual.
- Add `MissileProjectile`.
- Add TrailRenderer or child smoke particle system.
- Assign `Explosion.prefab`.
- Configure values from Section 5.

### Bomb prefab
Path:
```text
Assets/_Project/Prefabs/Projectiles/PlayerBomb.prefab
```

Requirements:
- Root name: `PlayerBomb`.
- Visual: procedural bomb/capsule with simple tail fins optional.
- Rigidbody:
  - Use Gravity: true
  - Interpolate: Interpolate
  - Collision Detection: Continuous Dynamic
  - Mass: 2
- Collider: CapsuleCollider or SphereCollider sized closely to visual.
- Add `BombProjectile`.
- Assign `Explosion.prefab`.
- Configure values from Section 5.

### Test target prefabs
Create:
```text
Assets/_Project/Prefabs/TestTargets/TestHelicopterTarget.prefab
Assets/_Project/Prefabs/TestTargets/TestTankTarget.prefab
```

`TestHelicopterTarget.prefab`
- Simple visible helicopter-like primitive silhouette.
- `Health`: max health `120`.
- `DestroyOnDeath`: assign `Explosion.prefab`, destroy root true.
- Collider(s) appropriate to visual.
- Optional Rigidbody kinematic true.
- Clearly distinguishable color.

`TestTankTarget.prefab`
- Simple visible tank-like primitive silhouette.
- `Health`: max health `120`.
- `DestroyOnDeath`: assign `Explosion.prefab`, destroy root true.
- Collider(s) appropriate to visual.
- Rigidbody kinematic true or no Rigidbody.
- Clearly distinguishable color.

Test targets are placeholders only. Real enemy helicopters and tanks will replace them in later milestones.

---

## 8. Required scene and player setup

The builder must open and update:

```text
Assets/_Project/Scenes/Game.unity
```

### Player prefab and/or scene instance

Ensure the existing player helicopter receives this hierarchy:

```text
PlayerHelicopter
├── HelicopterVisual                         (existing)
├── MissileHardpointLeft
├── MissileHardpointRight
└── BombDropPoint
```

Hardpoint local transforms:
- `MissileHardpointLeft`
  - local position: `(-1.35, -0.30, 1.70)`
  - local rotation: `(0, 0, 0)`
- `MissileHardpointRight`
  - local position: `(1.35, -0.30, 1.70)`
  - local rotation: `(0, 0, 0)`
- `BombDropPoint`
  - local position: `(0, -0.90, 0.10)`
  - local rotation: `(0, 0, 0)`

Where player GameObject/prefab references are safe to update, add:
- `PlayerCombatInputReader`
- `MissileLauncher`
- `BombLauncher`
- `PlayerWeaponController`

Inspector references:
- `PlayerCombatInputReader.inputActions` → `HelicopterControls.inputactions`
- `MissileLauncher.missilePrefab` → `PlayerMissile.prefab`
- `MissileLauncher.launchPoints` → left and right hardpoints
- `MissileLauncher.ownerRoot` → PlayerHelicopter
- `MissileLauncher.ownerRigidbody` → PlayerHelicopter Rigidbody
- `BombLauncher.bombPrefab` → `PlayerBomb.prefab`
- `BombLauncher.dropPoint` → BombDropPoint
- `BombLauncher.ownerRoot` → PlayerHelicopter
- `BombLauncher.ownerRigidbody` → PlayerHelicopter Rigidbody
- `PlayerWeaponController` → required reader and both launchers

Do not add `Health` to the player in this milestone unless needed as an inert future-ready component; player game-over logic belongs to Milestone 6.

### Combat test range
Create root:
```text
CombatTestRange
```

Place targets relative to the player/terrain while keeping them clear of trees, rocks, and the player spawn valley:
- Two airborne helicopter targets:
  - approximately 60–95 meters ahead of player
  - at least 18 meters above nearby terrain
  - offset left/right so they are not overlapping
- Three tank targets:
  - approximately 45–100 meters ahead of player
  - aligned to terrain height plus a small offset
  - spaced far enough for individual bomb testing

The builder should determine terrain height using the active terrain when available. If no terrain is found, use safe fixed Y positions.

Add a small scene label/comment root or a non-gameplay marker if useful, but no final UI.

### Safe builder behavior
The builder must be repeatable:
- Re-running it must update/reuse generated assets rather than endlessly duplicating objects.
- Remove/rebuild only the root it owns, such as `CombatTestRange`.
- Do not delete terrain/environment roots created by Milestone 2.
- Do not delete the player, camera, or directional light.
- Save the updated scene when successful.
- Log a concise success summary.

---

## 9. Quality, code, and Unity rules

1. Use meaningful namespaces. Recommended:
```text
HelicopterCombat.Combat
HelicopterCombat.Weapons
HelicopterCombat.Player
HelicopterCombat.EditorTools
```

2. Keep each class focused on one responsibility.
3. Use `[SerializeField]` for Inspector setup fields.
4. Avoid magic values outside of serialized defaults.
5. Use `RequireComponent` where a component is mandatory.
6. Guard missing references with clear warnings/errors, but do not spam Console every frame.
7. Use Unity 6 APIs compatible with the existing project.
8. Use `Rigidbody.linearVelocity` consistently if the project already uses it.
9. Do not use `Find` calls every frame.
10. Do not use `Update` for physics movement; use `FixedUpdate` when physics updates are needed.
11. Avoid runtime allocations in explosion physics where reasonable.
12. Provide concise XML or regular comments only where behavior is not obvious.
13. Do not introduce compilation warnings/errors.
14. Do not add external packages or modify package manifests.
15. Do not manually generate `.meta` files.
16. Do not commit or push to GitHub.

---

## 10. Required verification

After code generation:
1. Inspect all generated scripts for namespace/API/import errors.
2. Let Unity compile.
3. Run:
```text
Tools > Helicopter Combat > Rebuild Milestone 3 Combat
```
4. Open:
```text
Assets/_Project/Scenes/Game.unity
```
5. Enter Play Mode and verify:
   - Flight controls still work.
   - Left mouse or Space fires missiles.
   - Right mouse or B drops bombs.
   - Missile launches from alternating hardpoints.
   - Missile moves forward and hits an airborne test target.
   - Bomb falls naturally and explodes on a ground target.
   - Targets take damage and disappear/explode at zero health.
   - Projectiles do not instantly collide with the player.
   - No red Console errors occur.
   - Re-running the builder does not duplicate test ranges/hardpoints/components.

---

## 11. Final reporting format for Codex

At the end, report exactly:

1. **Created/changed files**  
   List every created or modified file.

2. **Input changes**  
   Confirm the two added actions and their bindings.

3. **Generated assets**  
   List materials, prefabs, and scene roots created.

4. **Player setup**  
   Confirm hardpoints and weapon components/references.

5. **Verification**  
   - Latest compile result
   - Builder result
   - Any tests Codex could perform
   - Unity Editor tests still required

6. **Deviations**  
   List only real deviations from this specification, with clear reasons.

7. **Stop condition**  
   State that Milestone 3 is complete and do not start Milestone 4.
