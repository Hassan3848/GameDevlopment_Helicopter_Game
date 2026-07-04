using HelicopterCombat.CameraSystem;
using UnityEngine;

namespace HelicopterCombat.Audio
{
    [DisallowMultipleComponent]
    public sealed class ExplosionAudioController : MonoBehaviour
    {
        [SerializeField] private AudioClip explosionClip;
        [SerializeField, Min(0f)] private float baseVolume = 0.9f;

        private bool played;

        public void Configure(AudioClip configuredExplosionClip, float configuredBaseVolume)
        {
            explosionClip = configuredExplosionClip;
            baseVolume = Mathf.Max(0f, configuredBaseVolume);
        }

        private void OnEnable()
        {
            if (played)
            {
                return;
            }

            played = true;
            AudioOneShotService.Instance?.Play3D(
                explosionClip,
                transform.position,
                AudioCategory.Combat,
                baseVolume,
                1f,
                1f,
                AudioRolloffMode.Logarithmic,
                8f,
                160f,
                "small_explosion",
                6);

            CameraShakeController.Instance?.ShakeExplosion(transform.position, 42f, 0.12f, 0.75f, 0.22f);
        }
    }
}
