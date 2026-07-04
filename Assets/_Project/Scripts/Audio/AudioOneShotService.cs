using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HelicopterCombat.Audio
{
    [DisallowMultipleComponent]
    public sealed class AudioOneShotService : MonoBehaviour
    {
        [SerializeField] private GameAudioSettings audioSettings;
        [SerializeField, Min(1)] private int initialPoolSize = 10;
        [SerializeField, Min(1)] private int maximumPoolSize = 28;

        private readonly Queue<AudioSource> availableSources = new Queue<AudioSource>();
        private readonly HashSet<AudioSource> leasedSources = new HashSet<AudioSource>();
        private readonly Dictionary<string, int> activeGroupCounts = new Dictionary<string, int>();

        public static AudioOneShotService Instance { get; private set; }

        public void Configure(GameAudioSettings configuredAudioSettings)
        {
            audioSettings = configuredAudioSettings;
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            PrewarmPool();
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        public void Play2D(AudioClip clip, AudioCategory category, float volume, float pitch = 1f, string groupKey = null, int maxConcurrent = int.MaxValue)
        {
            PlayInternal(clip, category, volume, pitch, transform.position, 0f, AudioRolloffMode.Logarithmic, 1f, 500f, groupKey, maxConcurrent);
        }

        public void Play3D(
            AudioClip clip,
            Vector3 worldPosition,
            AudioCategory category,
            float volume,
            float pitch = 1f,
            float spatialBlend = 1f,
            AudioRolloffMode rolloffMode = AudioRolloffMode.Logarithmic,
            float minDistance = 5f,
            float maxDistance = 100f,
            string groupKey = null,
            int maxConcurrent = int.MaxValue)
        {
            PlayInternal(clip, category, volume, pitch, worldPosition, spatialBlend, rolloffMode, minDistance, maxDistance, groupKey, maxConcurrent);
        }

        private void PrewarmPool()
        {
            for (int index = 0; index < initialPoolSize; index++)
            {
                availableSources.Enqueue(CreateSource(index));
            }
        }

        private void PlayInternal(
            AudioClip clip,
            AudioCategory category,
            float volume,
            float pitch,
            Vector3 worldPosition,
            float spatialBlend,
            AudioRolloffMode rolloffMode,
            float minDistance,
            float maxDistance,
            string groupKey,
            int maxConcurrent)
        {
            if (clip == null)
            {
                return;
            }

            int activeCount = 0;
            if (!string.IsNullOrEmpty(groupKey) &&
                activeGroupCounts.TryGetValue(groupKey, out activeCount) &&
                activeCount >= maxConcurrent)
            {
                return;
            }

            AudioSource source = AcquireSource();
            if (source == null)
            {
                return;
            }

            if (!string.IsNullOrEmpty(groupKey))
            {
                activeGroupCounts[groupKey] = activeCount + 1;
            }

            source.transform.position = worldPosition;
            source.clip = clip;
            source.loop = false;
            source.pitch = pitch;
            source.volume = audioSettings != null ? audioSettings.GetScaledVolume(category, volume) : Mathf.Clamp01(volume);
            source.spatialBlend = spatialBlend;
            source.rolloffMode = rolloffMode;
            source.minDistance = minDistance;
            source.maxDistance = maxDistance;
            source.ignoreListenerPause = true;
            source.gameObject.SetActive(true);
            source.Play();

            float duration = clip.length / Mathf.Max(0.01f, Mathf.Abs(pitch));
            StartCoroutine(ReturnAfterPlayback(source, duration, groupKey));
        }

        private AudioSource AcquireSource()
        {
            AudioSource source = null;

            if (availableSources.Count > 0)
            {
                source = availableSources.Dequeue();
            }
            else if (leasedSources.Count + availableSources.Count < maximumPoolSize)
            {
                source = CreateSource(leasedSources.Count + availableSources.Count);
            }

            if (source != null)
            {
                leasedSources.Add(source);
            }

            return source;
        }

        private IEnumerator ReturnAfterPlayback(AudioSource source, float duration, string groupKey)
        {
            yield return new WaitForSecondsRealtime(duration + 0.05f);

            if (source != null)
            {
                source.Stop();
                source.clip = null;
                source.gameObject.SetActive(false);
            }

            if (source != null && leasedSources.Remove(source))
            {
                availableSources.Enqueue(source);
            }

            if (!string.IsNullOrEmpty(groupKey) && activeGroupCounts.TryGetValue(groupKey, out int count))
            {
                if (count <= 1)
                {
                    activeGroupCounts.Remove(groupKey);
                }
                else
                {
                    activeGroupCounts[groupKey] = count - 1;
                }
            }
        }

        private AudioSource CreateSource(int index)
        {
            GameObject child = new GameObject("OneShotAudio_" + index);
            child.transform.SetParent(transform, false);
            AudioSource source = child.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = false;
            source.dopplerLevel = 0f;
            child.SetActive(false);
            return source;
        }
    }
}
