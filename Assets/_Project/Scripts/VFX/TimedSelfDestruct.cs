using UnityEngine;

namespace HelicopterCombat.VFX
{
    [DisallowMultipleComponent]
    public sealed class TimedSelfDestruct : MonoBehaviour
    {
        [SerializeField, Min(0.01f)] private float lifetime = 2f;

        public void Configure(float configuredLifetime)
        {
            lifetime = Mathf.Max(0.01f, configuredLifetime);
        }

        private void OnEnable()
        {
            CancelInvoke(nameof(DestroySelf));
            Invoke(nameof(DestroySelf), lifetime);
        }

        private void OnDisable()
        {
            CancelInvoke(nameof(DestroySelf));
        }

        private void DestroySelf()
        {
            Destroy(gameObject);
        }
    }
}
