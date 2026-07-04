using System;
using System.Collections.Generic;
using HelicopterCombat.Combat;
using HelicopterCombat.Core;
using HelicopterCombat.Player;
using HelicopterCombat.UI;
using HelicopterCombat.Weapons;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace HelicopterCombat.EditorTools
{
    public static class Milestone6UIBuilder
    {
        private const string ProjectRoot = "Assets/_Project";
        private const string ScenePath = ProjectRoot + "/Scenes/Game.unity";
        private const string UiPrefabPath = ProjectRoot + "/Prefabs/UI/GameUI.prefab";
        private const string BuiltInFontPath = "LegacyRuntime.ttf";
        private const string MainMenuBackgroundPath = ProjectRoot + "/Art/UI/MainMenuBackground.jpg";

        [MenuItem("Tools/Helicopter Combat/Rebuild Milestone 6 UI and Game Flow")]
        public static void RebuildMilestone6UiAndGameFlow()
        {
            EnsureFolders();

            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            EnsureSceneInBuildSettings();

            PlayerDeathHandler playerDeathHandler = FindRequired<PlayerDeathHandler>();
            Health playerHealth = FindRequired<Health>(playerDeathHandler.gameObject);
            MissileLauncher missileLauncher = FindRequired<MissileLauncher>(playerDeathHandler.gameObject);
            BombLauncher bombLauncher = FindRequired<BombLauncher>(playerDeathHandler.gameObject);
            CombatMissionController missionController = FindRequired<CombatMissionController>();
            EnemyUnit[] enemyUnits = FindSceneEnemyUnits();

            ConfigureMissionController(playerDeathHandler, missionController, enemyUnits);

            GameObject existingRoot = GameObject.Find("GameUI");
            if (existingRoot != null)
            {
                UnityEngine.Object.DestroyImmediate(existingRoot);
            }

            DestroyStrayEventSystems();

            GameObject gameUiRoot = new GameObject("GameUI");
            GameFlowController gameFlowController = gameUiRoot.AddComponent<GameFlowController>();
            SceneLoader sceneLoader = gameUiRoot.AddComponent<SceneLoader>();
            GameUIController gameUiController = gameUiRoot.AddComponent<GameUIController>();
            HUDController hudController = gameUiRoot.AddComponent<HUDController>();
            ObjectiveUIController objectiveUiController = gameUiRoot.AddComponent<ObjectiveUIController>();

            Canvas canvas = CreateCanvas(gameUiRoot.transform);
            GameObject hudPanel = CreatePanel(canvas.transform, "HUDPanel", new Color(0f, 0f, 0f, 0f));
            BuildHud(hudPanel.transform, out Text healthValueText, out Image healthBarFill, out Text missileCountText, out Text bombCountText, out Text objectiveText, out Text enemyCountText, out Text statusText, out GameObject crosshair, out Button pauseButton);

            GameObject mainMenuPanel = CreateFullScreenPanel(canvas.transform, "MainMenuPanel", new Color(0.04f, 0.06f, 0.08f, 0.86f));
            BuildMainMenu(mainMenuPanel.transform, out Button startButton, out Button quitButton);

            GameObject pausePanel = CreateFullScreenPanel(canvas.transform, "PausePanel", new Color(0.04f, 0.06f, 0.08f, 0.72f));
            BuildEndPanel(pausePanel.transform, "PAUSED", "Resume the mission or leave to the main menu.", new Color(0.55f, 0.86f, 0.96f), "ResumeButton", "RESUME", "PauseMainMenuButton", "MAIN MENU", "PauseQuitButton", "QUIT GAME", out _, out _, out Button resumeButton, out Button pauseMainMenuButton, out Button pauseQuitButton);

            GameObject gameOverPanel = CreateFullScreenPanel(canvas.transform, "GameOverPanel", new Color(0.10f, 0.02f, 0.02f, 0.88f));
            BuildEndPanel(gameOverPanel.transform, "MISSION FAILED", "Your helicopter was destroyed.", new Color(0.75f, 0.18f, 0.18f), "RetryButton", "Retry", "MainMenuButton", "Main Menu", "QuitButton", "Quit Game", out Text gameOverTitle, out Text gameOverDescription, out Button retryButton, out Button gameOverMainMenuButton, out Button gameOverQuitButton);

            GameObject victoryPanel = CreateFullScreenPanel(canvas.transform, "VictoryPanel", new Color(0.02f, 0.09f, 0.04f, 0.88f));
            BuildEndPanel(victoryPanel.transform, "MISSION COMPLETE", "All enemy helicopters and tanks have been destroyed.", new Color(0.20f, 0.72f, 0.35f), "ReplayButton", "Replay", "MainMenuButton", "Main Menu", "QuitButton", "Quit Game", out Text victoryTitle, out Text victoryDescription, out Button replayButton, out Button victoryMainMenuButton, out Button victoryQuitButton);

            EventSystem eventSystem = CreateEventSystem(gameUiRoot.transform);

            EndScreenController gameOverController = gameOverPanel.AddComponent<EndScreenController>();
            EndScreenController victoryController = victoryPanel.AddComponent<EndScreenController>();
            gameOverController.Configure(gameOverTitle, gameOverDescription);
            victoryController.Configure(victoryTitle, victoryDescription);
            gameOverController.ShowGameOverText();
            victoryController.ShowVictoryText();

            gameFlowController.Configure(missionController, gameUiController, sceneLoader);
            gameUiController.Configure(
                mainMenuPanel,
                hudPanel,
                pausePanel,
                gameOverPanel,
                victoryPanel,
                crosshair,
                startButton,
                quitButton,
                pauseButton,
                resumeButton,
                pauseMainMenuButton,
                pauseQuitButton,
                retryButton,
                replayButton,
                gameOverMainMenuButton,
                victoryMainMenuButton,
                gameOverQuitButton,
                victoryQuitButton,
                gameFlowController);
            hudController.Configure(playerHealth, missileLauncher, bombLauncher, healthValueText, missileCountText, bombCountText, healthBarFill);
            objectiveUiController.Configure(missionController, objectiveText, enemyCountText, statusText);

            PrefabUtility.SaveAsPrefabAsset(gameUiRoot, UiPrefabPath);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("Milestone 6 UI and game flow rebuilt. EventSystem: " + eventSystem.name + ", enemies tracked: " + enemyUnits.Length + ".");
        }

        public static void RebuildFromCommandLine()
        {
            RebuildMilestone6UiAndGameFlow();
        }

        private static void EnsureFolders()
        {
            string[] folders =
            {
                ProjectRoot + "/Prefabs/UI",
                ProjectRoot + "/Scripts/UI",
                ProjectRoot + "/Scripts/Core",
                ProjectRoot + "/Scripts/Editor",
                ProjectRoot + "/Art/Materials/UI",
                ProjectRoot + "/Art/UI"
            };

            foreach (string folder in folders)
            {
                CreateFolderRecursively(folder);
            }
        }

        private static void CreateFolderRecursively(string assetFolder)
        {
            string[] parts = assetFolder.Split('/');
            string current = parts[0];

            for (int index = 1; index < parts.Length; index++)
            {
                string next = current + "/" + parts[index];
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, parts[index]);
                }

                current = next;
            }
        }

        private static void EnsureSceneInBuildSettings()
        {
            List<EditorBuildSettingsScene> scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);

            foreach (EditorBuildSettingsScene buildScene in scenes)
            {
                if (string.Equals(buildScene.path, ScenePath, StringComparison.OrdinalIgnoreCase))
                {
                    if (!buildScene.enabled)
                    {
                        buildScene.enabled = true;
                        EditorBuildSettings.scenes = scenes.ToArray();
                    }

                    return;
                }
            }

            scenes.Add(new EditorBuildSettingsScene(ScenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }

        private static void ConfigureMissionController(PlayerDeathHandler playerDeathHandler, CombatMissionController missionController, EnemyUnit[] enemyUnits)
        {
            List<EnemyUnit> helicopters = new List<EnemyUnit>();
            List<EnemyUnit> tanks = new List<EnemyUnit>();

            foreach (EnemyUnit enemyUnit in enemyUnits)
            {
                if (enemyUnit == null)
                {
                    continue;
                }

                if (enemyUnit.UnitType == EnemyUnit.EnemyUnitType.Helicopter)
                {
                    helicopters.Add(enemyUnit);
                }
                else if (enemyUnit.UnitType == EnemyUnit.EnemyUnitType.Tank)
                {
                    tanks.Add(enemyUnit);
                }
            }

            missionController.Configure(playerDeathHandler, helicopters.ToArray(), tanks.ToArray());
        }

        private static EnemyUnit[] FindSceneEnemyUnits()
        {
            List<EnemyUnit> units = new List<EnemyUnit>();
            EnemyUnit[] allUnits = UnityEngine.Object.FindObjectsByType<EnemyUnit>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);

            foreach (EnemyUnit enemyUnit in allUnits)
            {
                if (enemyUnit == null || !enemyUnit.gameObject.scene.IsValid())
                {
                    continue;
                }

                string lowerName = enemyUnit.name.ToLowerInvariant();

                if (lowerName.Contains("testtarget") || lowerName.Contains("combattestrange"))
                {
                    continue;
                }

                units.Add(enemyUnit);
            }

            return units.ToArray();
        }

        private static T FindRequired<T>() where T : Component
        {
            T[] matches = UnityEngine.Object.FindObjectsByType<T>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            if (matches.Length == 0)
            {
                throw new InvalidOperationException("Required component was not found in the scene: " + typeof(T).Name);
            }

            return matches[0];
        }

        private static T FindRequired<T>(GameObject onObject) where T : Component
        {
            T component = onObject != null ? onObject.GetComponent<T>() : null;
            if (component == null)
            {
                throw new InvalidOperationException("Required component was not found on " + (onObject != null ? onObject.name : "<null>") + ": " + typeof(T).Name);
            }

            return component;
        }

        private static void DestroyStrayEventSystems()
        {
            EventSystem[] eventSystems = UnityEngine.Object.FindObjectsByType<EventSystem>(FindObjectsInactive.Include, FindObjectsSortMode.None);

            foreach (EventSystem eventSystem in eventSystems)
            {
                if (eventSystem != null)
                {
                    UnityEngine.Object.DestroyImmediate(eventSystem.gameObject);
                }
            }
        }

        private static Canvas CreateCanvas(Transform parent)
        {
            GameObject canvasObject = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(parent, false);

            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            return canvas;
        }

        private static EventSystem CreateEventSystem(Transform parent)
        {
            GameObject eventSystemObject = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            eventSystemObject.transform.SetParent(parent, false);
            InputSystemUIInputModule uiModule = eventSystemObject.GetComponent<InputSystemUIInputModule>();
            uiModule.AssignDefaultActions();
            return eventSystemObject.GetComponent<EventSystem>();
        }

        private static GameObject CreatePanel(Transform parent, string name, Color color)
        {
            GameObject panel = new GameObject(name, typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(parent, false);
            RectTransform rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            panel.GetComponent<Image>().color = color;
            return panel;
        }

        private static GameObject CreateFullScreenPanel(Transform parent, string name, Color color)
        {
            return CreatePanel(parent, name, color);
        }

        private static void BuildHud(
            Transform parent,
            out Text healthValueText,
            out Image healthBarFill,
            out Text missileCountText,
            out Text bombCountText,
            out Text objectiveText,
            out Text enemyCountText,
            out Text statusText,
            out GameObject crosshair,
            out Button pauseButton)
        {
            GameObject healthCard = CreateBox(parent, "HealthCard", new Vector2(26f, -26f), new Vector2(380f, 125f), new Vector2(0f, 1f), new Color(0.04f, 0.08f, 0.11f, 0.82f));
            CreateText(healthCard.transform, "Label", "PLAYER STATUS", 24, TextAnchor.UpperLeft, new Color(0.55f, 0.86f, 0.96f), new Vector2(18f, -14f), new Vector2(220f, 30f), new Vector2(0f, 1f));
            GameObject barBackground = CreateBox(healthCard.transform, "HealthBarBackground", new Vector2(18f, -54f), new Vector2(344f, 22f), new Vector2(0f, 1f), new Color(0.08f, 0.10f, 0.12f, 1f));
            GameObject fill = CreateBox(barBackground.transform, "HealthBarFill", Vector2.zero, Vector2.zero, new Vector2(0f, 0.5f), new Color(0.26f, 0.84f, 0.44f, 1f));
            RectTransform fillRect = fill.GetComponent<RectTransform>();
            fillRect.anchorMin = new Vector2(0f, 0f);
            fillRect.anchorMax = new Vector2(1f, 1f);
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;
            healthBarFill = fill.GetComponent<Image>();
            healthBarFill.type = Image.Type.Filled;
            healthBarFill.fillMethod = Image.FillMethod.Horizontal;
            healthBarFill.fillOrigin = 0;
            healthValueText = CreateText(healthCard.transform, "HealthValueText", "HEALTH 250 / 250", 28, TextAnchor.UpperLeft, Color.white, new Vector2(18f, -82f), new Vector2(320f, 34f), new Vector2(0f, 1f));

            GameObject ammoCard = CreateBox(parent, "AmmoCard", new Vector2(26f, -164f), new Vector2(300f, 100f), new Vector2(0f, 1f), new Color(0.07f, 0.08f, 0.10f, 0.82f));
            missileCountText = CreateText(ammoCard.transform, "MissileCountText", "MISSILES: INF", 24, TextAnchor.UpperLeft, new Color(0.96f, 0.84f, 0.35f), new Vector2(18f, -18f), new Vector2(250f, 28f), new Vector2(0f, 1f));
            bombCountText = CreateText(ammoCard.transform, "BombCountText", "BOMBS: 8", 24, TextAnchor.UpperLeft, new Color(0.96f, 0.84f, 0.35f), new Vector2(18f, -52f), new Vector2(250f, 28f), new Vector2(0f, 1f));

            objectiveText = CreateText(parent, "ObjectiveText", "OBJECTIVE: DESTROY ALL ENEMY UNITS", 28, TextAnchor.UpperCenter, new Color(0.95f, 0.86f, 0.32f), new Vector2(0f, -20f), new Vector2(760f, 34f), new Vector2(0.5f, 1f));
            enemyCountText = CreateText(parent, "EnemyCountText", "ENEMIES REMAINING: 4", 24, TextAnchor.UpperCenter, new Color(0.55f, 0.86f, 0.96f), new Vector2(0f, -58f), new Vector2(520f, 30f), new Vector2(0.5f, 1f));
            statusText = CreateText(parent, "StatusText", "MISSION ACTIVE", 22, TextAnchor.UpperCenter, Color.white, new Vector2(0f, -92f), new Vector2(420f, 28f), new Vector2(0.5f, 1f));
            pauseButton = CreateButton(parent, "PauseButton", "PAUSE", new Vector2(-26f, -26f), new Vector2(150f, 52f), new Color(0.12f, 0.40f, 0.52f, 0.90f), new Vector2(1f, 1f));

            crosshair = CreateCrosshair(parent);
        }

        private static void BuildMainMenu(Transform parent, out Button startButton, out Button quitButton)
        {
            CreateMainMenuBackground(parent);
            GameObject card = CreateCenteredCard(parent, "MenuCard", new Vector2(720f, 500f), new Color(0.07f, 0.09f, 0.12f, 0.92f));
            CreateText(card.transform, "Title", "HELICOPTER STRIKE", 48, TextAnchor.UpperCenter, new Color(0.55f, 0.86f, 0.96f), new Vector2(0f, -46f), new Vector2(620f, 58f), new Vector2(0.5f, 1f));
            CreateText(card.transform, "Subtitle", "Eliminate all hostile helicopters and ground armor.", 26, TextAnchor.UpperCenter, new Color(0.85f, 0.88f, 0.91f), new Vector2(0f, -120f), new Vector2(620f, 60f), new Vector2(0.5f, 1f));
            startButton = CreateButton(card.transform, "StartButton", "START MISSION", new Vector2(0f, -270f), new Vector2(320f, 64f), new Color(0.12f, 0.40f, 0.52f), new Vector2(0.5f, 1f));
            quitButton = CreateButton(card.transform, "QuitButton", "QUIT GAME", new Vector2(0f, -352f), new Vector2(320f, 64f), new Color(0.26f, 0.12f, 0.12f), new Vector2(0.5f, 1f));
        }

        private static void CreateMainMenuBackground(Transform parent)
        {
            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(MainMenuBackgroundPath);
            if (texture != null)
            {
                GameObject background = new GameObject("BackgroundImage", typeof(RectTransform), typeof(RawImage));
                background.transform.SetParent(parent, false);
                background.transform.SetAsFirstSibling();

                RectTransform rect = background.GetComponent<RectTransform>();
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;

                RawImage image = background.GetComponent<RawImage>();
                image.texture = texture;
                image.color = Color.white;
            }

            GameObject overlay = CreateBox(parent, "BackgroundOverlay", Vector2.zero, Vector2.zero, new Vector2(0.5f, 0.5f), new Color(0.03f, 0.05f, 0.07f, 0.5f));
            RectTransform overlayRect = overlay.GetComponent<RectTransform>();
            overlayRect.anchorMin = Vector2.zero;
            overlayRect.anchorMax = Vector2.one;
            overlayRect.offsetMin = Vector2.zero;
            overlayRect.offsetMax = Vector2.zero;
            overlay.transform.SetAsLastSibling();
        }

        private static void BuildEndPanel(
            Transform parent,
            string title,
            string description,
            Color accent,
            string primaryButtonName,
            string primaryButtonLabel,
            string secondaryButtonName,
            string secondaryButtonLabel,
            string tertiaryButtonName,
            string tertiaryButtonLabel,
            out Text titleText,
            out Text descriptionText,
            out Button primaryButton,
            out Button secondaryButton,
            out Button tertiaryButton)
        {
            GameObject card = CreateCenteredCard(parent, "Card", new Vector2(760f, 520f), new Color(0.08f, 0.09f, 0.11f, 0.94f));
            titleText = CreateText(card.transform, "Title", title, 48, TextAnchor.UpperCenter, accent, new Vector2(0f, -48f), new Vector2(660f, 58f), new Vector2(0.5f, 1f));
            descriptionText = CreateText(card.transform, "Description", description, 26, TextAnchor.UpperCenter, Color.white, new Vector2(0f, -126f), new Vector2(660f, 82f), new Vector2(0.5f, 1f));
            primaryButton = CreateButton(card.transform, primaryButtonName, primaryButtonLabel, new Vector2(0f, -286f), new Vector2(320f, 64f), accent * 0.75f, new Vector2(0.5f, 1f));
            secondaryButton = CreateButton(card.transform, secondaryButtonName, secondaryButtonLabel, new Vector2(0f, -368f), new Vector2(320f, 64f), new Color(0.18f, 0.22f, 0.28f), new Vector2(0.5f, 1f));
            tertiaryButton = CreateButton(card.transform, tertiaryButtonName, tertiaryButtonLabel, new Vector2(0f, -450f), new Vector2(320f, 64f), new Color(0.28f, 0.14f, 0.14f), new Vector2(0.5f, 1f));
        }

        private static GameObject CreateCenteredCard(Transform parent, string name, Vector2 size, Color color)
        {
            GameObject card = CreateBox(parent, name, Vector2.zero, size, new Vector2(0.5f, 0.5f), color);
            card.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;
            return card;
        }

        private static GameObject CreateBox(Transform parent, string name, Vector2 anchoredPosition, Vector2 size, Vector2 anchor, Color color)
        {
            GameObject box = new GameObject(name, typeof(RectTransform), typeof(Image));
            box.transform.SetParent(parent, false);
            RectTransform rect = box.GetComponent<RectTransform>();
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = anchor;
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;
            box.GetComponent<Image>().color = color;
            return box;
        }

        private static Text CreateText(Transform parent, string name, string content, int fontSize, TextAnchor alignment, Color color, Vector2 anchoredPosition, Vector2 size, Vector2 anchor)
        {
            GameObject textObject = new GameObject(name, typeof(RectTransform), typeof(Text));
            textObject.transform.SetParent(parent, false);
            RectTransform rect = textObject.GetComponent<RectTransform>();
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = anchor;
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;

            Text text = textObject.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>(BuiltInFontPath);
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.color = color;
            text.text = content;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            return text;
        }

        private static Button CreateButton(Transform parent, string name, string label, Vector2 anchoredPosition, Vector2 size, Color color, Vector2 anchor)
        {
            GameObject buttonObject = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(parent, false);
            RectTransform rect = buttonObject.GetComponent<RectTransform>();
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = anchor;
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;

            Image image = buttonObject.GetComponent<Image>();
            image.color = color;

            Button button = buttonObject.GetComponent<Button>();
            ColorBlock colors = button.colors;
            colors.highlightedColor = color * 1.15f;
            colors.pressedColor = color * 0.85f;
            colors.selectedColor = color * 1.1f;
            button.colors = colors;

            CreateText(buttonObject.transform, "Label", label, 24, TextAnchor.MiddleCenter, Color.white, Vector2.zero, size, new Vector2(0.5f, 0.5f));
            return button;
        }

        private static GameObject CreateCrosshair(Transform parent)
        {
            GameObject crosshair = new GameObject("Crosshair", typeof(RectTransform));
            crosshair.transform.SetParent(parent, false);
            RectTransform rect = crosshair.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(24f, 24f);

            CreateBox(crosshair.transform, "Horizontal", Vector2.zero, new Vector2(20f, 2f), new Vector2(0.5f, 0.5f), new Color(0.55f, 0.86f, 0.96f));
            CreateBox(crosshair.transform, "Vertical", Vector2.zero, new Vector2(2f, 20f), new Vector2(0.5f, 0.5f), new Color(0.55f, 0.86f, 0.96f));
            return crosshair;
        }
    }
}
