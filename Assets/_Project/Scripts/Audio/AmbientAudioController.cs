using HelicopterCombat.Core;
using UnityEngine;

namespace HelicopterCombat.Audio
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(AudioSource))]
    public sealed class AmbientAudioController : MonoBehaviour
    {
        [SerializeField] private GameAudioSettings audioSettings;
        [SerializeField] private AudioClip ambientLoop;
        [SerializeField] private GameFlowController gameFlowController;
        [SerializeField, Min(0.01f)] private float fadeSpeed = 0.45f;

        private AudioSource audioSource;

        public void Configure(GameAudioSettings configuredAudioSettings, AudioClip configuredAmbientLoop, GameFlowController configuredGameFlowController)
        {
            audioSettings = configuredAudioSettings;
            ambientLoop = configuredAmbientLoop;
            gameFlowController = configuredGameFlowController;
        }

        private void Awake()
        {
            audioSource = GetComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.loop = true;
            audioSource.clip = ambientLoop;
            audioSource.spatialBlend = 0f;
            audioSource.dopplerLevel = 0f;
            audioSource.ignoreListenerPause = true;
        }

        private void Update()
        {
            if (audioSource == null || ambientLoop == null)
            {
                return;
            }

            float targetVolume = gameFlowController != null && gameFlowController.State == GameFlowController.GameFlowState.Playing
                ? GetScaledVolume(0.12f)
                : 0f;

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
                ? audioSettings.GetScaledVolume(AudioCategory.Ambient, rawVolume)
                : rawVolume;
        }
    }
}
