using HelicopterCombat.Combat;
using UnityEngine;

namespace HelicopterCombat.Weapons
{
    public sealed class MissileProjectile : ExplosiveProjectile
    {
        [SerializeField, Min(0f)] private float speed = 65f;
        [SerializeField, Min(0f)] private float inheritOwnerVelocityMultiplier = 0.35f;

        private Vector3 launchDirection;

        public void Configure(float configuredSpeed, float configuredInheritOwnerVelocityMultiplier)
        {
            speed = configuredSpeed;
            inheritOwnerVelocityMultiplier = configuredInheritOwnerVelocityMultiplier;
        }

        protected override void OnOwnerInitialized()
        {
            launchDirection = transform.forward.normalized;

            Vector3 ownerVelocity = OwnerRigidbody != null
                ? OwnerRigidbody.linearVelocity * inheritOwnerVelocityMultiplier
                : Vector3.zero;

            ProjectileRigidbody.useGravity = false;
            ProjectileRigidbody.linearVelocity = launchDirection * speed + ownerVelocity;
        }

        private void FixedUpdate()
        {
            if (ProjectileRigidbody.linearVelocity.sqrMagnitude > 0.01f)
            {
                transform.rotation = Quaternion.LookRotation(
                    ProjectileRigidbody.linearVelocity.normalized,
                    Vector3.up);
            }
        }
    }
}
