using HelicopterCombat.Player;
using UnityEngine;

namespace HelicopterCombat.Combat
{
    [DisallowMultipleComponent]
    public sealed class MissionOutcomeTracker : MonoBehaviour
    {
        [SerializeField, Min(1)] private int requiredEnemyDestroyedCount = 2;

        public bool MissionSucceeded { get; private set; }
        public bool MissionFailed { get; private set; }

        private int destroyedEnemyCount;

        public void Configure(int configuredRequiredEnemyDestroyedCount)
        {
            requiredEnemyDestroyedCount = Mathf.Max(1, configuredRequiredEnemyDestroyedCount);
        }

        private void OnEnable()
        {
            EnemyUnit.AnyEnemyDestroyed += HandleEnemyDestroyed;
            PlayerDeathHandler.PlayerDefeated += HandlePlayerDefeated;
        }

        private void OnDisable()
        {
            EnemyUnit.AnyEnemyDestroyed -= HandleEnemyDestroyed;
            PlayerDeathHandler.PlayerDefeated -= HandlePlayerDefeated;
        }

        private void HandleEnemyDestroyed(EnemyUnit enemyUnit)
        {
            if (MissionSucceeded || MissionFailed)
            {
                return;
            }

            destroyedEnemyCount++;

            if (destroyedEnemyCount >= requiredEnemyDestroyedCount)
            {
                MissionSucceeded = true;
                Debug.Log("Mission successful. Both enemy helicopters have been destroyed. Victory UI belongs to a later milestone.", this);
            }
        }

        private void HandlePlayerDefeated(PlayerDeathHandler playerDeathHandler)
        {
            if (MissionSucceeded || MissionFailed)
            {
                return;
            }

            MissionFailed = true;
            Debug.Log("Game over. Enemy missile destroyed the player helicopter. Game Over UI belongs to a later milestone.", this);
        }
    }
}
