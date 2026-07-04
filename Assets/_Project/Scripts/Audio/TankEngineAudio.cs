using HelicopterCombat.Combat;
using HelicopterCombat.Enemies;
using UnityEngine;

namespace HelicopterCombat.Audio
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(AudioSource))]
    public sealed class TankEngineAudio : MonoBehaviour
    {
        [SerializeField] private GameAudioSettings audioSettings;
        [SerializeField] private AudioClip engineClip;
        [SerializeField] private EnemyTankMovement movement;
        [SerializeField] private Health health;
        [SerializeField, Min(0.01f)] private float fadeSpeed = 1.8f;
        [SerializeField, Min(0f)] private float movementThreshold = 0.25f;
        [SerializeField, Min(0.01f)] private float maxSpeedReference = 8f;

        private AudioSource audioSource;

        public void Configure(
            GameAudioSettings configuredAudioSettings,
            AudioClip configuredEngineClip,
            EnemyTankMovement configuredMovement,
            Health configuredHealth)
        {
            audioSettings = configuredAudioSettings;
            engineClip = configuredEngineClip;
            movement = configuredMovement;
            health = configuredHealth;
        }

        private void Awake()
        {
            audioSource = GetComponent<AudioSource>();
            if (movement == null)
            {
                movement = GetComponent<EnemyTankMovement>();
            }

            if (health == null)
            {
                health = GetComponent<Health>();
            }

            audioSource.playOnAwake = false;
            audioSource.loop = true;
            audioSource.clip = engineClip;
            audioSource.spatialBlend = 1f;
            audioSource.rolloffMode = AudioRolloffMode.Logarithmic;
            audioSource.minDistance = 9f;
            audioSource.maxDistance = 120f;
            audioSource.dopplerLevel = 0f;
            audioSource.ignoreListenerPause = true;
        }

        private void Update()
        {
            if (audioSource == null || engineClip == null)
            {
                return;
            }

            float targetVolume = 0f;
            float targetPitch = 0.86f;

            if (health == null || !health.IsDead)
            {
                float speed = movement != null ? movement.CurrentSpeed : 0f;
                float speedRatio = Mathf.Clamp01(speed / maxSpeedReference);

                if (speed > movementThreshold)
                {
                    targetVolume = GetScaledVolume(Mathf.Lerp(0.08f, 0.24f, speedRatio));
                    targetPitch = Mathf.Lerp(0.82f, 1.05f, speedRatio);
                }
            }

            audioSource.pitch = Mathf.MoveTowards(audioSource.pitch, targetPitch, Time.deltaTime * 1.2f);
            audioSource.volume = Mathf.MoveTowards(audioSource.volume, targetVolume, fadeSpeed * Time.deltaTime);

            if (!audioSource.isPlaying && targetVolume > 0.001f)
            {
                audioSource.Play();
            }

            if (audioSource.isPlaying && audioSource.volume <= 0.001f && targetVolume <= 0.001f)
            {
                audioSource.Stop();
            }
        }

        private float GetScaledVolume(float rawVolume)
        {
            return audioSettings != null
                ? audioSettings.GetScaledVolume(AudioCategory.Vehicles, rawVolume)
                : rawVolume;
        }
    }
}
