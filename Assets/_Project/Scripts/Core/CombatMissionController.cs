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
        [SerializeField] private EnemyUnit[] helicopterUnits = Array.Empty<EnemyUnit>();
        [SerializeField] private EnemyUnit[] tankUnits = Array.Empty<EnemyUnit>();

        private readonly HashSet<EnemyUnit> destroyedEnemies = new HashSet<EnemyUnit>();
        private EnemyUnit[] subscribedUnits = Array.Empty<EnemyUnit>();
        private bool helicopterClearLogged;
        private bool finalVictoryLogged;

        public event Action<MissionState> MissionStateChanged;
        public event Action ObjectiveProgressChanged;

        public MissionState State { get; private set; } = MissionState.Active;
        public int RequiredEnemyCount => subscribedUnits.Length;
        public int DestroyedEnemyCount => destroyedEnemies.Count;
        public int RemainingEnemyCount => Mathf.Max(0, RequiredEnemyCount - DestroyedEnemyCount);

        public void Configure(PlayerDeathHandler configuredPlayerDeathHandler, EnemyUnit[] configuredRequiredEnemyUnits)
        {
            Configure(configuredPlayerDeathHandler, configuredRequiredEnemyUnits, Array.Empty<EnemyUnit>());
        }

        public void Configure(
            PlayerDeathHandler configuredPlayerDeathHandler,
            EnemyUnit[] configuredHelicopterUnits,
            EnemyUnit[] configuredTankUnits)
        {
            Unsubscribe();
            playerDeathHandler = configuredPlayerDeathHandler;
            helicopterUnits = FilterNullUnits(configuredHelicopterUnits);
            tankUnits = FilterNullUnits(configuredTankUnits);
            RebuildRuntimeState();
            Subscribe();
            RaiseObjectiveProgressChanged();
        }

        private void Awake()
        {
            RebuildRuntimeState();
        }

        private void OnEnable()
        {
            RebuildRuntimeState();
            Subscribe();
            RaiseObjectiveProgressChanged();
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

            foreach (EnemyUnit enemyUnit in subscribedUnits)
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

            foreach (EnemyUnit enemyUnit in subscribedUnits)
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
            RaiseObjectiveProgressChanged();
            bool helicoptersCleared = AreAllDestroyed(helicopterUnits);
            bool tanksCleared = AreAllDestroyed(tankUnits);
            bool hasTanks = tankUnits.Length > 0;

            if (helicoptersCleared && hasTanks && !tanksCleared)
            {
                SetState(MissionState.HelicopterObjectiveCleared);

                if (!helicopterClearLogged)
                {
                    helicopterClearLogged = true;
                    Debug.Log("HELICOPTER OBJECTIVE CLEARED: Destroy remaining tanks.", this);
                }
            }

            if (helicoptersCleared && (!hasTanks || tanksCleared))
            {
                SetState(MissionState.FinalVictoryReady);

                if (!finalVictoryLogged)
                {
                    finalVictoryLogged = true;
                    Debug.Log("MISSION COMPLETE: All enemy helicopters and tanks destroyed. Victory Screen will be added in Milestone 6.", this);
                }
            }
        }

        private void HandlePlayerDefeated(PlayerDeathHandler defeatedPlayer)
        {
            if (State == MissionState.Defeat || State == MissionState.FinalVictoryReady)
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
            RaiseObjectiveProgressChanged();
        }

        private void RaiseObjectiveProgressChanged()
        {
            ObjectiveProgressChanged?.Invoke();
        }

        private void RebuildRuntimeState()
        {
            helicopterUnits = FilterNullUnits(helicopterUnits);
            tankUnits = FilterNullUnits(tankUnits);
            subscribedUnits = MergeUnits(helicopterUnits, tankUnits);
            destroyedEnemies.Clear();
            helicopterClearLogged = false;
            finalVictoryLogged = false;
            State = MissionState.Active;

            foreach (EnemyUnit enemyUnit in subscribedUnits)
            {
                if (enemyUnit != null && enemyUnit.IsDestroyed)
                {
                    destroyedEnemies.Add(enemyUnit);
                }
            }
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

        private bool AreAllDestroyed(EnemyUnit[] units)
        {
            if (units == null || units.Length == 0)
            {
                return true;
            }

            foreach (EnemyUnit unit in units)
            {
                if (unit != null && !destroyedEnemies.Contains(unit))
                {
                    return false;
                }
            }

            return true;
        }

        private static EnemyUnit[] MergeUnits(EnemyUnit[] first, EnemyUnit[] second)
        {
            HashSet<EnemyUnit> uniqueUnits = new HashSet<EnemyUnit>();

            AddUnits(uniqueUnits, first);
            AddUnits(uniqueUnits, second);

            EnemyUnit[] merged = new EnemyUnit[uniqueUnits.Count];
            uniqueUnits.CopyTo(merged);
            return merged;
        }

        private static void AddUnits(HashSet<EnemyUnit> uniqueUnits, EnemyUnit[] sourceUnits)
        {
            if (sourceUnits == null)
            {
                return;
            }

            foreach (EnemyUnit unit in sourceUnits)
            {
                if (unit != null)
                {
                    uniqueUnits.Add(unit);
                }
            }
        }
    }
}
