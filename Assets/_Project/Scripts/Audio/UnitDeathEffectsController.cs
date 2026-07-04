using HelicopterCombat.CameraSystem;
using HelicopterCombat.Combat;
using UnityEngine;

namespace HelicopterCombat.Audio
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Health))]
    public sealed class UnitDeathEffectsController : MonoBehaviour
    {
        [SerializeField] private Health health;
        [SerializeField] private GameObject deathEffectPrefab;
        [SerializeField] private AudioClip deathExplosionClip;

        private bool triggered;

        public void Configure(Health configuredHealth, GameObject configuredDeathEffectPrefab, AudioClip configuredDeathExplosionClip)
        {
            health = configuredHealth;
            deathEffectPrefab = configuredDeathEffectPrefab;
            deathExplosionClip = configuredDeathExplosionClip;
        }

        private void Awake()
        {
            if (health == null)
            {
                health = GetComponent<Health>();
            }
        }

        private void OnEnable()
        {
            if (health != null)
            {
                health.Died -= HandleDied;
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

        private void HandleDied(Health deadHealth)
        {
            if (triggered)
            {
                return;
            }

            triggered = true;
            Vector3 center = CalculateVisualCenter();

            if (deathEffectPrefab != null)
            {
                Instantiate(deathEffectPrefab, center, Quaternion.identity);
            }

            AudioOneShotService.Instance?.Play3D(
                deathExplosionClip,
                center,
                AudioCategory.Combat,
                1f,
                1f,
                1f,
                AudioRolloffMode.Logarithmic,
                10f,
                180f,
                "large_explosion",
                4);

            CameraShakeController.Instance?.ShakeExplosion(center, 58f, 0.18f, 1.1f, 0.35f);
        }

        private Vector3 CalculateVisualCenter()
        {
            Renderer[] renderers = GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0)
            {
                return transform.position;
            }

            Bounds bounds = renderers[0].bounds;

            for (int index = 1; index < renderers.Length; index++)
            {
                bounds.Encapsulate(renderers[index].bounds);
            }

            return bounds.center;
        }
    }
}
