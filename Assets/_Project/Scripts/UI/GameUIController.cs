using HelicopterCombat.Core;
using UnityEngine;
using UnityEngine.UI;

namespace HelicopterCombat.UI
{
    [DisallowMultipleComponent]
    public sealed class GameUIController : MonoBehaviour
    {
        [SerializeField] private GameObject mainMenuPanel;
        [SerializeField] private GameObject hudPanel;
        [SerializeField] private GameObject pausePanel;
        [SerializeField] private GameObject gameOverPanel;
        [SerializeField] private GameObject victoryPanel;
        [SerializeField] private GameObject crosshairRoot;
        [SerializeField] private Button startButton;
        [SerializeField] private Button quitButton;
        [SerializeField] private Button pauseButton;
        [SerializeField] private Button resumeButton;
        [SerializeField] private Button pauseMainMenuButton;
        [SerializeField] private Button pauseQuitButton;
        [SerializeField] private Button retryButton;
        [SerializeField] private Button replayButton;
        [SerializeField] private Button gameOverMainMenuButton;
        [SerializeField] private Button victoryMainMenuButton;
        [SerializeField] private Button gameOverQuitButton;
        [SerializeField] private Button victoryQuitButton;
        [SerializeField] private GameFlowController gameFlowController;

        public void Configure(
            GameObject configuredMainMenuPanel,
            GameObject configuredHudPanel,
            GameObject configuredPausePanel,
            GameObject configuredGameOverPanel,
            GameObject configuredVictoryPanel,
            GameObject configuredCrosshairRoot,
            Button configuredStartButton,
            Button configuredQuitButton,
            Button configuredPauseButton,
            Button configuredResumeButton,
            Button configuredPauseMainMenuButton,
            Button configuredPauseQuitButton,
            Button configuredRetryButton,
            Button configuredReplayButton,
            Button configuredGameOverMainMenuButton,
            Button configuredVictoryMainMenuButton,
            Button configuredGameOverQuitButton,
            Button configuredVictoryQuitButton,
            GameFlowController configuredGameFlowController)
        {
            mainMenuPanel = configuredMainMenuPanel;
            hudPanel = configuredHudPanel;
            pausePanel = configuredPausePanel;
            gameOverPanel = configuredGameOverPanel;
            victoryPanel = configuredVictoryPanel;
            crosshairRoot = configuredCrosshairRoot;
            startButton = configuredStartButton;
            quitButton = configuredQuitButton;
            pauseButton = configuredPauseButton;
            resumeButton = configuredResumeButton;
            pauseMainMenuButton = configuredPauseMainMenuButton;
            pauseQuitButton = configuredPauseQuitButton;
            retryButton = configuredRetryButton;
            replayButton = configuredReplayButton;
            gameOverMainMenuButton = configuredGameOverMainMenuButton;
            victoryMainMenuButton = configuredVictoryMainMenuButton;
            gameOverQuitButton = configuredGameOverQuitButton;
            victoryQuitButton = configuredVictoryQuitButton;
            gameFlowController = configuredGameFlowController;
        }

        private void Awake()
        {
            BindButton(startButton, () => gameFlowController?.StartGame());
            BindButton(quitButton, () => gameFlowController?.QuitGame());
            BindButton(pauseButton, () => gameFlowController?.TogglePause());
            BindButton(resumeButton, () => gameFlowController?.ResumeGame());
            BindButton(pauseMainMenuButton, () => gameFlowController?.ReturnToMainMenu());
            BindButton(pauseQuitButton, () => gameFlowController?.QuitGame());
            BindButton(retryButton, () => gameFlowController?.RestartGame());
            BindButton(replayButton, () => gameFlowController?.RestartGame());
            BindButton(gameOverMainMenuButton, () => gameFlowController?.ReturnToMainMenu());
            BindButton(victoryMainMenuButton, () => gameFlowController?.ReturnToMainMenu());
            BindButton(gameOverQuitButton, () => gameFlowController?.QuitGame());
            BindButton(victoryQuitButton, () => gameFlowController?.QuitGame());
        }

        public void ShowMainMenu()
        {
            SetState(true, false, false, false, false, false, false);
        }

        public void ShowGameplayUI()
        {
            SetState(false, true, false, false, false, true, true);
        }

        public void ShowPauseMenu()
        {
            SetState(false, true, true, false, false, false, false);
        }

        public void ShowGameOver()
        {
            SetState(false, false, false, true, false, false, false);
        }

        public void ShowVictory()
        {
            SetState(false, false, false, false, true, false, false);
        }

        private void SetState(bool showMainMenu, bool showHud, bool showPause, bool showGameOver, bool showVictory, bool showCrosshair, bool showPauseButton)
        {
            SetActive(mainMenuPanel, showMainMenu);
            SetActive(hudPanel, showHud);
            SetActive(pausePanel, showPause);
            SetActive(gameOverPanel, showGameOver);
            SetActive(victoryPanel, showVictory);
            SetActive(crosshairRoot, showCrosshair);
            SetButtonVisible(pauseButton, showPauseButton);
        }

        private static void SetActive(GameObject target, bool value)
        {
            if (target != null)
            {
                target.SetActive(value);
            }
        }

        private static void SetButtonVisible(Button button, bool value)
        {
            if (button != null)
            {
                button.gameObject.SetActive(value);
            }
        }

        private static void BindButton(Button button, UnityEngine.Events.UnityAction action)
        {
            if (button == null)
            {
                return;
            }

            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(action);
        }
    }
}
