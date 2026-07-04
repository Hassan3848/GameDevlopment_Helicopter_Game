using HelicopterCombat.Combat;
using HelicopterCombat.Core;
using UnityEngine;

namespace HelicopterCombat.Audio
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(AudioSource))]
    public sealed class HelicopterEngineAudio : MonoBehaviour
    {
        [SerializeField] private GameAudioSettings audioSettings;
        [SerializeField] private AudioClip engineClip;
        [SerializeField] private Rigidbody targetRigidbody;
        [SerializeField] private Health health;
        [SerializeField] private GameFlowController gameFlowController;
        [SerializeField] private bool playerHelicopter;
        [SerializeField] private float basePitchOffset;
        [SerializeField, Min(0.01f)] private float fadeSpeed = 1.8f;
        [SerializeField, Min(0.01f)] private float speedForMaxResponse = 28f;

        private AudioSource audioSource;

        public void Configure(
            GameAudioSettings configuredAudioSettings,
            AudioClip configuredEngineClip,
            Rigidbody configuredRigidbody,
            Health configuredHealth,
            GameFlowController configuredGameFlowController,
            bool configuredPlayerHelicopter,
            float configuredPitchOffset)
        {
            audioSettings = configuredAudioSettings;
            engineClip = configuredEngineClip;
            targetRigidbody = configuredRigidbody;
            health = configuredHealth;
            gameFlowController = configuredGameFlowController;
            playerHelicopter = configuredPlayerHelicopter;
            basePitchOffset = configuredPitchOffset;
        }

        private void Awake()
        {
            audioSource = GetComponent<AudioSource>();
            if (targetRigidbody == null)
            {
                targetRigidbody = GetComponent<Rigidbody>();
            }

            if (health == null)
            {
                health = GetComponent<Health>();
            }

            if (playerHelicopter && gameFlowController == null)
            {
                gameFlowController = FindAnyObjectByType<GameFlowController>();
            }

            ConfigureSource();
        }

        private void Update()
        {
            if (audioSource == null || engineClip == null)
            {
                return;
            }

            float targetVolume = 0f;
            float speedRatio = targetRigidbody != null
                ? Mathf.Clamp01(targetRigidbody.linearVelocity.magnitude / speedForMaxResponse)
                : 0f;

            if (ShouldPlay())
            {
                if (playerHelicopter)
                {
                    targetVolume = GetScaledVolume(Mathf.Lerp(0.2f, 0.4f, speedRatio));
                    audioSource.pitch = Mathf.Lerp(0.82f, 1.18f, speedRatio) + basePitchOffset;
                }
                else
                {
                    targetVolume = GetScaledVolume(Mathf.Lerp(0.08f, 0.22f, speedRatio));
                    audioSource.pitch = Mathf.Lerp(0.88f, 1.08f, speedRatio) + basePitchOffset;
                }
            }

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

        private bool ShouldPlay()
        {
            if (health != null && health.IsDead)
            {
                return false;
            }

            if (playerHelicopter)
            {
                return gameFlowController != null && gameFlowController.State == GameFlowController.GameFlowState.Playing;
            }

            return true;
        }

        private void ConfigureSource()
        {
            audioSource.playOnAwake = false;
            audioSource.loop = true;
            audioSource.clip = engineClip;
            audioSource.dopplerLevel = 0f;
            audioSource.ignoreListenerPause = true;

            if (playerHelicopter)
            {
                audioSource.spatialBlend = 0f;
                audioSource.rolloffMode = AudioRolloffMode.Logarithmic;
                audioSource.minDistance = 1f;
                audioSource.maxDistance = 20f;
            }
            else
            {
                audioSource.spatialBlend = 1f;
                audioSource.rolloffMode = AudioRolloffMode.Logarithmic;
                audioSource.minDistance = 8f;
                audioSource.maxDistance = 135f;
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
