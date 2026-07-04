using HelicopterCombat.Combat;
using HelicopterCombat.Enemies;
using UnityEngine;

namespace HelicopterCombat.Audio
{
    [DisallowMultipleComponent]
    public sealed class TankDustVfxController : MonoBehaviour
    {
        [SerializeField] private EnemyTankMovement movement;
        [SerializeField] private Health health;
        [SerializeField] private ParticleSystem leftDust;
        [SerializeField] private ParticleSystem rightDust;
        [SerializeField, Min(0f)] private float activationSpeed = 0.35f;

        private ParticleSystem.EmissionModule leftEmission;
        private ParticleSystem.EmissionModule rightEmission;

        public void Configure(EnemyTankMovement configuredMovement, Health configuredHealth, ParticleSystem configuredLeftDust, ParticleSystem configuredRightDust)
        {
            movement = configuredMovement;
            health = configuredHealth;
            leftDust = configuredLeftDust;
            rightDust = configuredRightDust;
            CacheEmission();
        }

        private void Awake()
        {
            if (movement == null)
            {
                movement = GetComponent<EnemyTankMovement>();
            }

            if (health == null)
            {
                health = GetComponent<Health>();
            }

            CacheEmission();
        }

        private void Update()
        {
            float rate = 0f;

            if ((health == null || !health.IsDead) && movement != null && movement.CurrentSpeed > activationSpeed)
            {
                float normalized = Mathf.Clamp01(movement.CurrentSpeed / 8f);
                rate = Mathf.Lerp(4f, 18f, normalized);
            }

            leftEmission.rateOverTime = rate;
            rightEmission.rateOverTime = rate;
        }

        private void CacheEmission()
        {
            if (leftDust != null)
            {
                leftEmission = leftDust.emission;
            }

            if (rightDust != null)
            {
                rightEmission = rightDust.emission;
            }
        }
    }
}
