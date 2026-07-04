using HelicopterCombat.Core;
using UnityEngine;
using UnityEngine.UI;

namespace HelicopterCombat.UI
{
    [DisallowMultipleComponent]
    public sealed class ObjectiveUIController : MonoBehaviour
    {
        [SerializeField] private CombatMissionController missionController;
        [SerializeField] private Text objectiveText;
        [SerializeField] private Text enemyCountText;
        [SerializeField] private Text statusText;

        public void Configure(
            CombatMissionController configuredMissionController,
            Text configuredObjectiveText,
            Text configuredEnemyCountText,
            Text configuredStatusText)
        {
            missionController = configuredMissionController;
            objectiveText = configuredObjectiveText;
            enemyCountText = configuredEnemyCountText;
            statusText = configuredStatusText;
        }

        private void OnEnable()
        {
            if (missionController != null)
            {
                missionController.ObjectiveProgressChanged -= Refresh;
                missionController.ObjectiveProgressChanged += Refresh;
                missionController.MissionStateChanged -= HandleMissionStateChanged;
                missionController.MissionStateChanged += HandleMissionStateChanged;
            }

            Refresh();
        }

        private void OnDisable()
        {
            if (missionController != null)
            {
                missionController.ObjectiveProgressChanged -= Refresh;
                missionController.MissionStateChanged -= HandleMissionStateChanged;
            }
        }

        private void Start()
        {
            Refresh();
        }

        private void HandleMissionStateChanged(CombatMissionController.MissionState state)
        {
            Refresh();
        }

        private void Refresh()
        {
            if (objectiveText != null)
            {
                objectiveText.text = "OBJECTIVE: DESTROY ALL ENEMY UNITS";
            }

            if (enemyCountText != null && missionController != null)
            {
                enemyCountText.text = $"ENEMIES REMAINING: {missionController.RemainingEnemyCount}";
            }

            if (statusText != null)
            {
                statusText.text = GetStatusText();
            }
        }

        private string GetStatusText()
        {
            if (missionController == null)
            {
                return "MISSION ACTIVE";
            }

            switch (missionController.State)
            {
                case CombatMissionController.MissionState.HelicopterObjectiveCleared:
                    return "DESTROY REMAINING TANKS";
                case CombatMissionController.MissionState.FinalVictoryReady:
                    return "MISSION COMPLETE";
                case CombatMissionController.MissionState.Defeat:
                    return "MISSION FAILED";
                default:
                    return "MISSION ACTIVE";
            }
        }
    }
}
