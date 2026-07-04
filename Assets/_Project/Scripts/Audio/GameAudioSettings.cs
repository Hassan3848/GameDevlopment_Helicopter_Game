using UnityEngine;

namespace HelicopterCombat.Audio
{
    public enum AudioCategory
    {
        Ambient,
        Combat,
        Mission,
        UI,
        Vehicles,
        Weapons
    }

    [CreateAssetMenu(fileName = "M7_GameAudioSettings", menuName = "Helicopter Combat/Audio Settings")]
    public sealed class GameAudioSettings : ScriptableObject
    {
        [SerializeField, Range(0f, 1f)] private float masterVolume = 1f;
        [SerializeField, Range(0f, 1f)] private float ambientVolume = 1f;
        [SerializeField, Range(0f, 1f)] private float combatVolume = 1f;
        [SerializeField, Range(0f, 1f)] private float missionVolume = 1f;
        [SerializeField, Range(0f, 1f)] private float uiVolume = 1f;
        [SerializeField, Range(0f, 1f)] private float vehiclesVolume = 1f;
        [SerializeField, Range(0f, 1f)] private float weaponsVolume = 1f;

        public float GetScaledVolume(AudioCategory category, float baseVolume)
        {
            return Mathf.Clamp01(baseVolume * masterVolume * GetCategoryMultiplier(category));
        }

        public float GetCategoryMultiplier(AudioCategory category)
        {
            switch (category)
            {
                case AudioCategory.Ambient:
                    return ambientVolume;
                case AudioCategory.Combat:
                    return combatVolume;
                case AudioCategory.Mission:
                    return missionVolume;
                case AudioCategory.UI:
                    return uiVolume;
                case AudioCategory.Vehicles:
                    return vehiclesVolume;
                case AudioCategory.Weapons:
                    return weaponsVolume;
                default:
                    return 1f;
            }
        }
    }
}
