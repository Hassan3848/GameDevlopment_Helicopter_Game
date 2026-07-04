using System.Collections;
using HelicopterCombat.Player;
using HelicopterCombat.UI;
using UnityEngine;
using System;

namespace HelicopterCombat.Core
{
    [DisallowMultipleComponent]
    public sealed class GameFlowController : MonoBehaviour
    {
        public enum GameFlowState
        {
            MainMenu,
            Playing,
            Paused,
            GameOver,
            Victory
        }

        [SerializeField] private CombatMissionController missionController;
        [SerializeField] private GameUIController gameUIController;
        [SerializeField] private SceneLoader sceneLoader;
        [SerializeField] private float victoryDelaySeconds = 1.2f;

        private Coroutine pendingVictoryRoutine;
        private bool stateApplied;

        public event Action<GameFlowState> StateChanged;

        public GameFlowState State { get; private set; }

        public void Configure(
            CombatMissionController configuredMissionController,
            GameUIController configuredGameUIController,
            SceneLoader configuredSceneLoader)
        {
            missionController = configuredMissionController;
            gameUIController = configuredGameUIController;
            sceneLoader = configuredSceneLoader;
        }

        private void Awake()
        {
            ApplyState(GameFlowState.MainMenu);
        }

        private void OnEnable()
        {
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

            if (pendingVictoryRoutine != null)
            {
                StopCoroutine(pendingVictoryRoutine);
                pendingVictoryRoutine = null;
            }

            Time.timeScale = 1f;
        }

        private void OnDestroy()
        {
            Time.timeScale = 1f;
        }

        public void StartGame()
        {
            if (State == GameFlowState.Playing)
            {
                return;
            }

            ApplyState(GameFlowState.Playing);
        }

        public void TogglePause()
        {
            if (State == GameFlowState.Playing)
            {
                ApplyState(GameFlowState.Paused);
                return;
            }

            if (State == GameFlowState.Paused)
            {
                ApplyState(GameFlowState.Playing);
            }
        }

        public void ResumeGame()
        {
            if (State == GameFlowState.Paused)
            {
                ApplyState(GameFlowState.Playing);
            }
        }

        public void RestartGame()
        {
            sceneLoader?.ReloadCurrentScene();
        }

        public void ReturnToMainMenu()
        {
            sceneLoader?.ReloadCurrentScene();
        }

        public void QuitGame()
        {
            sceneLoader?.QuitApplication();
        }

        private void HandleMissionStateChanged(CombatMissionController.MissionState missionState)
        {
            if (State == GameFlowState.GameOver || State == GameFlowState.Victory)
            {
                return;
            }

            switch (missionState)
            {
                case CombatMissionController.MissionState.Defeat:
                    ApplyState(GameFlowState.GameOver);
                    break;
                case CombatMissionController.MissionState.FinalVictoryReady:
                    if (pendingVictoryRoutine == null)
                    {
                        pendingVictoryRoutine = StartCoroutine(ShowVictoryAfterDelay());
                    }
                    break;
            }
        }

        private IEnumerator ShowVictoryAfterDelay()
        {
            yield return new WaitForSecondsRealtime(victoryDelaySeconds);
            pendingVictoryRoutine = null;

            if (missionController != null &&
                missionController.State == CombatMissionController.MissionState.FinalVictoryReady &&
                State == GameFlowState.Playing)
            {
                ApplyState(GameFlowState.Victory);
            }
        }

        private void ApplyState(GameFlowState newState)
        {
            if (stateApplied && State == newState)
            {
                return;
            }

            State = newState;
            stateApplied = true;
            StateChanged?.Invoke(State);

            switch (State)
            {
                case GameFlowState.MainMenu:
                    Time.timeScale = 0f;
                    Cursor.lockState = CursorLockMode.None;
                    Cursor.visible = true;
                    gameUIController?.ShowMainMenu();
                    break;
                case GameFlowState.Playing:
                    Time.timeScale = 1f;
                    Cursor.lockState = CursorLockMode.Locked;
                    Cursor.visible = false;
                    gameUIController?.ShowGameplayUI();
                    break;
                case GameFlowState.Paused:
                    Time.timeScale = 0f;
                    Cursor.lockState = CursorLockMode.None;
                    Cursor.visible = true;
                    gameUIController?.ShowPauseMenu();
                    break;
                case GameFlowState.GameOver:
                    Time.timeScale = 0f;
                    Cursor.lockState = CursorLockMode.None;
                    Cursor.visible = true;
                    gameUIController?.ShowGameOver();
                    break;
                case GameFlowState.Victory:
                    Time.timeScale = 0f;
                    Cursor.lockState = CursorLockMode.None;
                    Cursor.visible = true;
                    gameUIController?.ShowVictory();
                    break;
            }
        }
    }
}
