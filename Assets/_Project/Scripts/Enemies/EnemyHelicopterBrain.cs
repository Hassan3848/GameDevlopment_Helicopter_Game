using HelicopterCombat.Combat;
using UnityEngine;

namespace HelicopterCombat.Enemies
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(EnemyHelicopterTargeting))]
    [RequireComponent(typeof(EnemyHelicopterMovement))]
    [RequireComponent(typeof(EnemyHelicopterWeaponController))]
    [RequireComponent(typeof(Health))]
    public sealed class EnemyHelicopterBrain : MonoBehaviour
    {
        public enum EnemyHelicopterState
        {
            Idle,
            Pursue,
            Engage,
            ReturnToCombatZone,
            Defeated
        }

        [SerializeField] private bool startActive = true;
        [SerializeField] private EnemyHelicopterTargeting targeting;
        [SerializeField] private EnemyHelicopterMovement movement;
        [SerializeField] private EnemyHelicopterWeaponController weaponController;
        [SerializeField] private Health health;

        public EnemyHelicopterState State { get; private set; }
        public bool IsDefeated => State == EnemyHelicopterState.Defeated || (health != null && health.IsDead);

        public void Configure(
            EnemyHelicopterTargeting configuredTargeting,
            EnemyHelicopterMovement configuredMovement,
            EnemyHelicopterWeaponController configuredWeaponController,
            Health configuredHealth)
        {
            targeting = configuredTargeting;
            movement = configuredMovement;
            weaponController = configuredWeaponController;
            health = configuredHealth;
        }

        private void Awake()
        {
            if (targeting == null)
            {
                targeting = GetComponent<EnemyHelicopterTargeting>();
            }

            if (movement == null)
            {
                movement = GetComponent<EnemyHelicopterMovement>();
            }

            if (weaponController == null)
            {
                weaponController = GetComponent<EnemyHelicopterWeaponController>();
            }

            if (health == null)
            {
                health = GetComponent<Health>();
            }
        }

        private void OnEnable()
        {
            if (health != null)
            {
                health.Died += HandleDied;
            }
        }

        private void OnDisable()
        {
            if (health != null)
            {
                health.Died -= HandleDied;
            }
        }

        private void Update()
        {
            if (!startActive || IsDefeated)
            {
                State = IsDefeated ? EnemyHelicopterState.Defeated : EnemyHelicopterState.Idle;
                movement?.SetTarget(transform.position, false, false);
                weaponController?.TickWeapons(false);
                return;
            }

            if (targeting == null || movement == null)
            {
                State = EnemyHelicopterState.Idle;
                movement?.SetTarget(transform.position, false, false);
                weaponController?.TickWeapons(false);
                return;
            }

            if (movement.IsBeyondHardBoundary || movement.IsBeyondSoftBoundary)
            {
                State = EnemyHelicopterState.ReturnToCombatZone;
                movement.SetTarget(movement.HomePosition, false, true);
                weaponController?.TickWeapons(false);
                return;
            }

            if (!targeting.IsWithinAwarenessRange)
            {
                State = movement.IsNearHome ? EnemyHelicopterState.Idle : EnemyHelicopterState.ReturnToCombatZone;
                movement.SetTarget(movement.HomePosition, false, !movement.IsNearHome);
                weaponController?.TickWeapons(false);
                return;
            }

            if (!targeting.HasCombatTarget)
            {
                State = movement.IsNearHome ? EnemyHelicopterState.Idle : EnemyHelicopterState.ReturnToCombatZone;
                movement.SetTarget(movement.HomePosition, false, !movement.IsNearHome);
                weaponController?.TickWeapons(false);
                return;
            }

            float distance = targeting.DistanceToTarget;
            bool inCombatBand = distance <= movement.CombatDistance + movement.CombatDistanceTolerance;
            State = inCombatBand ? EnemyHelicopterState.Engage : EnemyHelicopterState.Pursue;

            movement.SetTarget(targeting.TargetPosition, true, false);
            weaponController?.TickWeapons(State == EnemyHelicopterState.Engage);
        }

        private void HandleDied(Health deadHealth)
        {
            State = EnemyHelicopterState.Defeated;
            movement?.SetTarget(transform.position, false, false);
        }
    }
}
