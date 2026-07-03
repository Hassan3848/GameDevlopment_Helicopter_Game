using System;
using System.Collections.Generic;
using HelicopterCombat.Combat;
using HelicopterCombat.Player;
using UnityEngine;

namespace HelicopterCombat.Core
{
    [DisallowMultipleComponent]
    public sealed class CombatMissionController : MonoBehaviour
    {
        public enum MissionState
        {
            Active,
            HelicopterObjectiveCleared,
            Defeat,
            FinalVictoryReady
        }

        [SerializeField] private PlayerDeathHandler playerDeathHandler;
        [SerializeField] private EnemyUnit[] requiredEnemyUnits = Array.Empty<EnemyUnit>();

        private readonly HashSet<EnemyUnit> destroyedEnemies = new HashSet<EnemyUnit>();
        private bool clearLogged;

        public event Action<MissionState> MissionStateChanged;

        public MissionState State { get; private set; } = MissionState.Active;

        public void Configure(PlayerDeathHandler configuredPlayerDeathHandler, EnemyUnit[] configuredRequiredEnemyUnits)
        {
            Unsubscribe();
            playerDeathHandler = configuredPlayerDeathHandler;
            requiredEnemyUnits = FilterNullUnits(configuredRequiredEnemyUnits);
            destroyedEnemies.Clear();
            clearLogged = false;
            State = MissionState.Active;
            Subscribe();
        }

        private void OnEnable()
        {
            Subscribe();
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        private void Subscribe()
        {
            if (playerDeathHandler != null)
            {
                playerDeathHandler.Defeated -= HandlePlayerDefeated;
                playerDeathHandler.Defeated += HandlePlayerDefeated;
            }

            if (requiredEnemyUnits == null)
            {
                return;
            }

            foreach (EnemyUnit enemyUnit in requiredEnemyUnits)
            {
                if (enemyUnit == null)
                {
                    continue;
                }

                enemyUnit.Destroyed -= HandleEnemyDestroyed;
                enemyUnit.Destroyed += HandleEnemyDestroyed;

                if (enemyUnit.IsDestroyed)
                {
                    destroyedEnemies.Add(enemyUnit);
                }
            }
        }

        private void Unsubscribe()
        {
            if (playerDeathHandler != null)
            {
                playerDeathHandler.Defeated -= HandlePlayerDefeated;
            }

            if (requiredEnemyUnits == null)
            {
                return;
            }

            foreach (EnemyUnit enemyUnit in requiredEnemyUnits)
            {
                if (enemyUnit != null)
                {
                    enemyUnit.Destroyed -= HandleEnemyDestroyed;
                }
            }
        }

        private void HandleEnemyDestroyed(EnemyUnit enemyUnit)
        {
            if (State == MissionState.Defeat || enemyUnit == null)
            {
                return;
            }

            destroyedEnemies.Add(enemyUnit);

            if (requiredEnemyUnits == null || destroyedEnemies.Count < requiredEnemyUnits.Length)
            {
                return;
            }

            SetState(MissionState.HelicopterObjectiveCleared);

            if (!clearLogged)
            {
                clearLogged = true;
                Debug.Log("HELICOPTER OBJECTIVE CLEARED: Tanks will be added before final victory.", this);
            }
        }

        private void HandlePlayerDefeated(PlayerDeathHandler defeatedPlayer)
        {
            if (State != MissionState.Active)
            {
                return;
            }

            SetState(MissionState.Defeat);
        }

        private void SetState(MissionState newState)
        {
            if (State == newState)
            {
                return;
            }

            State = newState;
            MissionStateChanged?.Invoke(State);
        }

        private static EnemyUnit[] FilterNullUnits(EnemyUnit[] sourceUnits)
        {
            if (sourceUnits == null || sourceUnits.Length == 0)
            {
                return Array.Empty<EnemyUnit>();
            }

            List<EnemyUnit> filteredUnits = new List<EnemyUnit>(sourceUnits.Length);

            foreach (EnemyUnit unit in sourceUnits)
            {
                if (unit != null)
                {
                    filteredUnits.Add(unit);
                }
            }

            return filteredUnits.ToArray();
        }
    }
}
