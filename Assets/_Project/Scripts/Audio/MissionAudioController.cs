using HelicopterCombat.Core;
using UnityEngine;

namespace HelicopterCombat.Audio
{
    [DisallowMultipleComponent]
    public sealed class MissionAudioController : MonoBehaviour
    {
        [SerializeField] private CombatMissionController missionController;
        [SerializeField] private AudioClip victoryClip;
        [SerializeField] private AudioClip defeatClip;

        private bool roundResolved;

        public void Configure(CombatMissionController configuredMissionController, AudioClip configuredVictoryClip, AudioClip configuredDefeatClip)
        {
            missionController = configuredMissionController;
            victoryClip = configuredVictoryClip;
            defeatClip = configuredDefeatClip;
        }

        private void OnEnable()
        {
            roundResolved = false;

            if (missionController != null)
            {
                missionController.MissionStateChanged -= HandleMissionStateChanged;
                missionController.MissionStateChanged += HandleMissionStateChanged;
            }
        }

        private void OnDisable()
        {
            if (missionController != null)
            {
                missionController.MissionStateChanged -= HandleMissionStateChanged;
            }
        }

        private void HandleMissionStateChanged(CombatMissionController.MissionState state)
        {
            if (roundResolved || AudioOneShotService.Instance == null)
            {
                return;
            }

            switch (state)
            {
                case CombatMissionController.MissionState.Defeat:
                    roundResolved = true;
                    AudioOneShotService.Instance.Play2D(defeatClip, AudioCategory.Mission, 0.85f, 1f, "mission_result", 1);
                    break;
                case CombatMissionController.MissionState.FinalVictoryReady:
                    roundResolved = true;
                    AudioOneShotService.Instance.Play2D(victoryClip, AudioCategory.Mission, 0.9f, 1f, "mission_result", 1);
                    break;
            }
        }
    }
}
