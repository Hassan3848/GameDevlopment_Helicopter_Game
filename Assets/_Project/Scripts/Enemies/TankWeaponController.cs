using HelicopterCombat.Player;
using HelicopterCombat.Weapons;
using UnityEngine;

namespace HelicopterCombat.Enemies
{
    [DisallowMultipleComponent]
    public sealed class TankWeaponController : MonoBehaviour
    {
        [SerializeField] private EnemyTankTargeting targeting;
        [SerializeField] private TankTurretAimer turretAimer;
        [SerializeField] private MissileLauncher missileLauncher;
        [SerializeField] private PlayerDeathHandler playerDeathHandler;
        [SerializeField, Min(0f)] private float fireCooldown = 3.5f;
        [SerializeField, Min(0f)] private float acquireTargetDelay = 0.45f;
        [SerializeField] private bool requireValidFiringAngle = true;

        private float enabledTime;
        private float nextFireTime;

        public void Configure(
            EnemyTankTargeting configuredTargeting,
            TankTurretAimer configuredTurretAimer,
            MissileLauncher configuredMissileLauncher,
            PlayerDeathHandler configuredPlayerDeathHandler)
        {
            targeting = configuredTargeting;
            turretAimer = configuredTurretAimer;
            missileLauncher = configuredMissileLauncher;
            playerDeathHandler = configuredPlayerDeathHandler;
        }

        public void ConfigureTiming(float configuredFireCooldown, float configuredAcquireTargetDelay, bool configuredRequireValidFiringAngle)
        {
            fireCooldown = Mathf.Max(0f, configuredFireCooldown);
            acquireTargetDelay = Mathf.Max(0f, configuredAcquireTargetDelay);
            requireValidFiringAngle = configuredRequireValidFiringAngle;
        }

        private void OnEnable()
        {
            enabledTime = Time.time;
            nextFireTime = enabledTime + acquireTargetDelay;
        }

        public void TickWeapons(bool canFire)
        {
            if (!canFire ||
                Time.time < nextFireTime ||
                targeting == null ||
                turretAimer == null ||
                missileLauncher == null ||
                playerDeathHandler == null ||
                playerDeathHandler.IsDefeated ||
                !targeting.HasCombatTarget ||
                !targeting.IsWithinFiringRange ||
                !turretAimer.IsAligned)
            {
                return;
            }

            if (requireValidFiringAngle && !targeting.HasValidFiringAngle)
            {
                return;
            }

            if (missileLauncher.TryFire())
            {
                nextFireTime = Time.time + fireCooldown;
            }
        }
    }
}
