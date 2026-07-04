# Unity Assignment 2 — Milestone 4.1 Stabilization Specification

## Purpose

This is a corrective patch for Milestone 4. It must be completed before starting Milestone 5.

### Problem being fixed

Enemy helicopters can disappear, drift away, be disabled, or otherwise stop participating before the player destroys them. That is invalid game behavior.

### Required final behavior

- Enemy helicopters remain in the scene and remain active until their own `Health` reaches zero from valid player damage.
- Losing sight of the player, moving too far away, leaving an intended combat area, visibility callbacks, physics behavior, or timed cleanup must **never** delete or disable an enemy helicopter root.
- If an enemy moves too far from its combat area, it returns safely to its spawn/home area and resumes searching/engaging. It must not despawn.
- A direct enemy missile hit on the player is a one-hit defeat for this assignment build: player health reaches zero, controls stop, and the mission enters a non-UI defeat state. The Game Over screen is added in Milestone 6.
- In the final game, victory requires all enemy helicopters **and** all enemy tanks to be destroyed. Milestone 4.1 creates the reusable mission-state foundation; Milestone 5 registers tanks; Milestone 6 shows the Victory/Game Over UI.

## Project boundaries

- Unity 6, Built-in Render Pipeline, Windows PC, New Input System.
- Milestones 1–4 already exist.
- Do not add external assets, packages, input actions, UI screens, menus, tanks, audio, or Milestone 5+ gameplay.
- Do not manually edit `.unity`, `.meta`, `ProjectSettings`, `Packages`, or imported Asset Store source files.
- Do not commit or push to GitHub.

## Required audit before changes

Inspect the actual current source before writing code. Do not assume APIs from older specifications.

At minimum inspect:

```text
Assets/_Project/Scripts/Enemies/EnemyHelicopterBrain.cs
Assets/_Project/Scripts/Enemies/EnemyHelicopterMovement.cs
Assets/_Project/Scripts/Enemies/EnemyHelicopterTargeting.cs
Assets/_Project/Scripts/Enemies/EnemyHelicopterWeaponController.cs
Assets/_Project/Scripts/Combat/EnemyUnit.cs
Assets/_Project/Scripts/Player/PlayerDeathHandler.cs
Assets/_Project/Scripts/Combat/Health.cs
Assets/_Project/Scripts/Combat/DestroyOnDeath.cs
Assets/_Project/Scripts/Combat/ExplosiveProjectile.cs
Assets/_Project/Scripts/Combat/MissileProjectile.cs
Assets/_Project/Scripts/Weapons/MissileLauncher.cs
Assets/_Project/Scripts/Editor/Milestone4EnemyHelicopterBuilder.cs
```

Search the complete project for these patterns and identify any code that can affect an enemy helicopter root:

```text
Destroy(
SetActive(false)
OnBecameInvisible
OnBecameVisible
OnTriggerExit
OnCollisionExit
out of bounds
height limit
lifetime
self-destruct
scene reload
```

### Mandatory destruction rule

For `EnemyHelicopterAlpha` and `EnemyHelicopterBravo` roots, the only allowed destruction path is:

```text
valid player damage -> Health reaches zero -> Health death event -> DestroyOnDeath/explosion -> destroy enemy root
```

Do not destroy, deactivate, pool, hide, or replace enemy helicopter roots for any other reason.

Projectile lifetime cleanup remains allowed for projectile objects only, never helicopter roots.

## Required code changes

### 1. Persistent enemy lifecycle

Update `EnemyHelicopterBrain` and related behavior so the state model includes:

```text
Idle
Pursue
Engage
ReturnToCombatZone
Defeated
```

Rules:

- `Defeated` may be entered only after that enemy's real `Health` death event fires.
- A missing/distant target must never set `Defeated`, destroy the GameObject, or disable its core components.
- When the player is temporarily outside awareness range, switch to `ReturnToCombatZone` or `Idle` at the home location.
- While returning, weapons must not fire.
- Once the player returns within the detection/awareness range, the enemy must reacquire and resume `Pursue`/`Engage`.
- Do not use `Find`, `FindWithTag`, or allocations every frame.

### 2. Home position and combat boundary

Update `EnemyHelicopterMovement` or add a focused `EnemyHelicopterCombatBoundary` component. Keep one responsibility per class.

Every enemy must have a builder-assigned persistent home position based on its original spawn position.

Use these Inspector-friendly values:

```text
Home Position:                         set by builder from initial scene spawn
Return-To-Home Distance:               225
Hard Combat Boundary Distance:         280
Return Arrival Distance:                18
Return Cruise Speed:                    22
Maximum Height Above Terrain:          155
Minimum Terrain Clearance:              24
```

Behavior:

- At distance <= 225 from home, ordinary pursue/engage behavior is allowed.
- At distance > 225 from home, prefer returning toward home; do not keep orbiting farther away.
- At distance >= 280 from home, force `ReturnToCombatZone`, stop weapons, and use stable movement toward home.
- When within 18 metres of home, hover or resume target acquisition.
- Keep the root upright and above terrain. Do not teleport except as an emergency safety recovery if the Rigidbody becomes invalid/NaN or falls below the terrain. Any recovery must place the enemy at home + safe altitude and write a single clear warning.
- Never disable/destroy an enemy to enforce the boundary.

### 3. Safe target acquisition

Update targeting semantics so a long distance only changes behavior; it does not remove the enemy from the game.

Recommended values:

```text
Detection Range:                       220
Awareness/Return Range:                340
Lose-Target Range:                     340
```

Requirements:

- Keep the assigned player `Transform` reference. Do not permanently clear it merely because the player is far away.
- Use `HasCombatTarget` or equivalent only for active pursuit/fire decisions.
- If the player is beyond 340 metres, the enemy returns/holds at home and waits.
- If the player comes back within 220 metres, it engages again.

### 4. Projectile ownership and friendly-fire protection

Audit player and enemy projectile collision/damage behavior.

Required result:

- A projectile must ignore all colliders under its launcher/owner root from spawn until destruction.
- Enemy missiles must not damage their own enemy helicopter.
- Enemy missiles must not damage the other enemy helicopter.
- Player missiles and bombs must not damage `PlayerHelicopter`.
- Enemy missile direct damage must be able to damage only the player; player projectiles must be able to damage only enemies.

Prefer a reusable, small faction/team solution only if the current projectile implementation does not already have a safe owner/friendly-fire filter:

```text
CombatTeam enum: Player, Enemy, Neutral
TeamMember component on player and enemy roots
```

If introducing it, keep it generic and use it in projectile direct-hit and explosion-radius damage filtering. Do not put faction rules inside AI code.

Do not alter the visible behavior of player weapons except preventing self/friendly damage.

### 5. One-hit player defeat

Keep the existing player health architecture, but configure the current values so one valid direct enemy missile hit defeats the player.

```text
Player Health Max Health:              250
Player Health Destroy When Dead:       false
Enemy Missile direct/explosion Damage: 250
```

If the current projectile uses different field names or separates direct and explosion damage, configure equivalent values.

`PlayerDeathHandler` must:

1. Run once when player `Health` dies.
2. Disable player flight input/movement and weapon input/firing.
3. Zero/neutralize the player Rigidbody safely.
4. Keep the camera active.
5. Raise a reusable defeat event for the later UI.
6. Log exactly one clear message such as: `GAME OVER: Player defeated. Game Over screen will be added in Milestone 6.`
7. Never reload the scene or destroy the player in this patch.

### 6. Mission-state foundation

Create this new focused component:

```text
Assets/_Project/Scripts/Core/CombatMissionController.cs
```

Namespace:

```text
HelicopterCombat.Core
```

Responsibilities:

- Track player defeat from `PlayerDeathHandler` / player health event.
- Track required enemy units through `EnemyUnit` death events.
- Expose an Inspector-readable mission state such as:

```text
Active
HelicopterObjectiveCleared
Defeat
FinalVictoryReady
```

- Expose events for future Milestone 6 UI.
- Do not create UI, load scenes, or contain weapon/AI physics code.

Milestone 4.1 behavior:

- Register exactly the two active enemy helicopter scene instances as required enemies.
- When both are destroyed and the player is still alive, enter `HelicopterObjectiveCleared` and log once:

```text
HELICOPTER OBJECTIVE CLEARED: Tanks will be added before final victory.
```

- When player dies first, enter `Defeat`, stop further mission progression, and do not raise helicopter-clear/victory.
- Do not call this final Victory yet, because Milestone 5 has not created tanks.

Milestone 5 must extend this same controller by registering tanks. Milestone 6 will connect its events to Victory/Game Over screens.

### 7. Builder update

Update `Milestone4EnemyHelicopterBuilder` so it remains idempotent and configures the generated scene correctly.

It must:

- Preserve the existing `EnemyHelicopters` root and both scene enemy objects when rebuilding, or rebuild them safely without duplicates.
- Assign each enemy's home position from the initial generated spawn position.
- Set combat boundary/return fields to the values above.
- Configure enemy projectile ownership and faction/team references.
- Set enemy missile damage to 250.
- Add/configure `CombatMissionController` on a single root object named `GameSystems` or an existing suitable manager root. Do not create duplicates.
- Assign the two actual scene enemy `EnemyUnit` references and player death reference explicitly.
- Keep `CombatTestRange` inactive if it exists.
- Save `Assets/_Project/Scenes/Game.unity` only after successful setup.

Menu command remains:

```text
Tools > Helicopter Combat > Rebuild Milestone 4 Enemy Helicopters
```

Do not create a separate builder menu unless a separate patch menu is truly necessary. Prefer updating the original Milestone 4 builder so a full rebuild remains consistent.

## Play Mode acceptance tests

Run these tests manually after Unity compiles and the builder runs:

1. **Persistence test**
   - Start `Game.unity`.
   - Confirm both enemy roots exist under `EnemyHelicopters`.
   - Watch for at least 60 seconds without shooting.
   - Move far away, then return.
   - Both enemy helicopters must remain alive and visible/returnable. Neither may disappear or become disabled.

2. **Boundary test**
   - Lure each enemy away from its initial area.
   - It must turn/return at the configured boundary rather than fly endlessly away or despawn.

3. **Combat test**
   - Return within range.
   - Enemies must pursue, orbit, and fire.
   - They must not collide repeatedly or damage each other with their own missiles.

4. **Enemy death test**
   - Destroy Alpha with player weapons.
   - Confirm only Alpha is destroyed once Health reaches zero.
   - Bravo must remain alive and continue to function.
   - Destroy Bravo.
   - Confirm one `HELICOPTER OBJECTIVE CLEARED` log and no final Victory UI yet.

5. **Player defeat test**
   - Restart scene.
   - Let one enemy missile hit the player.
   - Player health must become zero; flight and weapons must stop; camera must remain active; exactly one Game Over log must appear.
   - No scene reload and no player destruction.

6. **Console test**
   - Console must have zero red errors.
   - No repeated warnings/errors while flying, returning, or after death.

## Required Codex report

At the end, report:

1. Every created or modified file.
2. Every code path found that could destroy/deactivate enemy helicopter roots, and how it was removed or corrected.
3. The exact current enemy boundary, return, player-health, and enemy-missile-damage values.
4. How projectile self/friendly damage is prevented.
5. Builder result and latest compile result.
6. Which Play Mode tests still need manual confirmation.
7. Stop after Milestone 4.1. Do not start Milestone 5.
