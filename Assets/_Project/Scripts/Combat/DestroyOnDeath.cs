using UnityEngine;

namespace HelicopterCombat.Combat
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Health))]
    public sealed class DestroyOnDeath : MonoBehaviour
    {
        [SerializeField] private GameObject explosionPrefab;
        [SerializeField, Min(0f)] private float destroyDelay = 0.05f;
        [SerializeField] private bool destroyRootObject = true;

        private Health health;
        private bool triggered;

        public void Configure(GameObject configuredExplosionPrefab, float configuredDestroyDelay, bool configuredDestroyRootObject)
        {
            explosionPrefab = configuredExplosionPrefab;
            destroyDelay = configuredDestroyDelay;
            destroyRootObject = configuredDestroyRootObject;
        }

        private void Awake()
        {
            health = GetComponent<Health>();
        }

        private void OnEnable()
        {
            if (health == null)
            {
                health = GetComponent<Health>();
            }

            health.Died += HandleDied;
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

            if (explosionPrefab != null)
            {
                Instantiate(explosionPrefab, center, Quaternion.identity);
            }

            if (destroyRootObject)
            {
                Destroy(deadHealth.gameObject, destroyDelay);
            }
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
