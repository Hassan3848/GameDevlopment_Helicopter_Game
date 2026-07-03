using HelicopterCombat.Weapons;
using UnityEngine;

namespace HelicopterCombat.Enemies
{
    [DisallowMultipleComponent]
    public sealed class EnemyHelicopterWeaponController : MonoBehaviour
    {
        [SerializeField] private EnemyHelicopterTargeting targeting;
        [SerializeField] private MissileLauncher missileLauncher;
        [SerializeField] private Transform aimOrigin;
        [SerializeField] private Rigidbody targetRigidbody;
        [SerializeField, Min(0f)] private float attackRange = 115f;
        [SerializeField, Min(0f)] private float minimumAttackRange = 30f;
        [SerializeField, Min(0f)] private float requiredAimAngle = 18f;
        [SerializeField, Min(0f)] private float initialFireDelay = 1.5f;
        [SerializeField, Min(0f)] private float leadTime = 0.20f;

        private float enabledTime;

        public void Configure(EnemyHelicopterTargeting configuredTargeting, MissileLauncher configuredLauncher, Transform configuredAimOrigin, Rigidbody configuredTargetRigidbody)
        {
            targeting = configuredTargeting;
            missileLauncher = configuredLauncher;
            aimOrigin = configuredAimOrigin;
            targetRigidbody = configuredTargetRigidbody;
        }

        private void OnEnable()
        {
            enabledTime = Time.time;
        }

        public void TickWeapons(bool canFire)
        {
            if (!canFire ||
                Time.time - enabledTime < initialFireDelay ||
                targeting == null ||
                missileLauncher == null ||
                aimOrigin == null ||
                !targeting.HasCombatTarget)
            {
                return;
            }

            float distance = targeting.DistanceToTarget;

            if (distance < minimumAttackRange || distance > attackRange)
            {
                return;
            }

            Vector3 predictedPosition = targeting.TargetPosition;

            if (targetRigidbody != null)
            {
                predictedPosition += targetRigidbody.linearVelocity * leadTime;
            }

            Vector3 aimDirection = predictedPosition - aimOrigin.position;

            if (aimDirection.sqrMagnitude < 0.01f)
            {
                return;
            }

            float angle = Vector3.Angle(aimOrigin.forward, aimDirection.normalized);

            if (angle <= requiredAimAngle)
            {
                missileLauncher.TryFire();
            }
        }
    }
}
