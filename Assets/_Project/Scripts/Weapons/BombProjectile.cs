using HelicopterCombat.Combat;
using UnityEngine;

namespace HelicopterCombat.Weapons
{
    public sealed class BombProjectile : ExplosiveProjectile
    {
        [SerializeField, Min(0f)] private float initialForwardSpeed = 8f;
        [SerializeField, Min(0f)] private float inheritOwnerVelocityMultiplier = 1f;
        [SerializeField, Min(0f)] private float gravityMultiplier = 1f;
        [SerializeField, Min(0f)] private float visualSpinSpeed = 90f;

        public void Configure(float configuredInitialForwardSpeed, float configuredInheritOwnerVelocityMultiplier, float configuredGravityMultiplier)
        {
            initialForwardSpeed = configuredInitialForwardSpeed;
            inheritOwnerVelocityMultiplier = configuredInheritOwnerVelocityMultiplier;
            gravityMultiplier = configuredGravityMultiplier;
        }

        protected override void OnOwnerInitialized()
        {
            Vector3 ownerVelocity = OwnerRigidbody != null
                ? OwnerRigidbody.linearVelocity * inheritOwnerVelocityMultiplier
                : Vector3.zero;

            ProjectileRigidbody.useGravity = true;
            ProjectileRigidbody.linearVelocity = transform.forward * initialForwardSpeed + ownerVelocity;
        }

        private void FixedUpdate()
        {
            if (gravityMultiplier > 1f)
            {
                ProjectileRigidbody.AddForce(
                    Physics.gravity * (gravityMultiplier - 1f),
                    ForceMode.Acceleration);
            }

            transform.Rotate(Vector3.right, visualSpinSpeed * Time.fixedDeltaTime, Space.Self);
        }
    }
}
