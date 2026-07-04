using HelicopterCombat.Combat;
using UnityEngine;

namespace HelicopterCombat.Enemies
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(EnemyTankTargeting))]
    [RequireComponent(typeof(EnemyTankMovement))]
    [RequireComponent(typeof(TankWeaponController))]
    [RequireComponent(typeof(Health))]
    public sealed class EnemyTankBrain : MonoBehaviour
    {
        public enum EnemyTankState
        {
            Idle,
            Pursue,
            Tracking,
            Firing,
            ReturnHome,
            Defeated
        }

        [SerializeField] private EnemyTankTargeting targeting;
        [SerializeField] private EnemyTankMovement movement;
        [SerializeField] private TankWeaponController weaponController;
        [SerializeField] private Health health;

        public EnemyTankState State { get; private set; }
        public bool IsDefeated => State == EnemyTankState.Defeated || (health != null && health.IsDead);

        public void Configure(EnemyTankTargeting configuredTargeting, TankWeaponController configuredWeaponController, Health configuredHealth)
        {
            targeting = configuredTargeting;
            movement = GetComponent<EnemyTankMovement>();
            weaponController = configuredWeaponController;
            health = configuredHealth;
        }

        private void Awake()
        {
            if (targeting == null)
            {
                targeting = GetComponent<EnemyTankTargeting>();
            }

            if (weaponController == null)
            {
                weaponController = GetComponent<TankWeaponController>();
            }

            if (movement == null)
            {
                movement = GetComponent<EnemyTankMovement>();
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
            if (IsDefeated)
            {
                State = EnemyTankState.Defeated;
                movement?.SetMovementMode(EnemyTankMovement.MovementMode.Idle);
                weaponController?.TickWeapons(false);
                return;
            }

            if (targeting == null || movement == null)
            {
                State = EnemyTankState.Idle;
                movement?.SetMovementMode(EnemyTankMovement.MovementMode.Idle);
                weaponController?.TickWeapons(false);
                return;
            }

            if (targeting.IsBeyondReturnThreshold || movement.DistanceToHome > movement.HomeReturnDistance)
            {
                State = EnemyTankState.ReturnHome;
                movement.SetMovementMode(EnemyTankMovement.MovementMode.ReturnHome);
                weaponController?.TickWeapons(false);
                return;
            }

            if (!targeting.HasCombatTarget || !targeting.IsWithinAwarenessRange)
            {
                State = EnemyTankState.ReturnHome;
                movement.SetMovementMode(EnemyTankMovement.MovementMode.ReturnHome);
                weaponController?.TickWeapons(false);
                return;
            }

            if (movement.GroundProjectedDistanceToTarget > movement.ChaseStartDistance ||
                movement.GroundProjectedDistanceToTarget < movement.MinimumCombatDistance)
            {
                State = EnemyTankState.Pursue;
                movement.SetMovementMode(EnemyTankMovement.MovementMode.Pursue);
                weaponController?.TickWeapons(targeting.IsWithinFiringRange && targeting.HasValidFiringAngle);
                return;
            }

            movement.SetMovementMode(EnemyTankMovement.MovementMode.Hold);
            bool canFire = targeting.IsWithinFiringRange && targeting.HasValidFiringAngle;
            State = canFire ? EnemyTankState.Firing : EnemyTankState.Tracking;
            weaponController?.TickWeapons(canFire);
        }

        private void HandleDied(Health deadHealth)
        {
            State = EnemyTankState.Defeated;
            movement?.SetMovementMode(EnemyTankMovement.MovementMode.Idle);
            weaponController?.TickWeapons(false);
        }
    }
}
