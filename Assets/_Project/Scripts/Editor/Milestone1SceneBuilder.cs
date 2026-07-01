using System.Collections.Generic;
using System.IO;
using HelicopterCombat.CameraSystem;
using HelicopterCombat.Player;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace HelicopterCombat.EditorTools
{
    /// <summary>
    /// Creates or refreshes the complete Milestone 1 flight-test scene.
    /// This is an editor-only tool; it never runs in a Windows game build.
    /// </summary>
    public static class Milestone1SceneBuilder
    {
        private const string ProjectRoot = "Assets/_Project";
        private const string InputActionsPath =
            ProjectRoot + "/Input/HelicopterControls.inputactions";
        private const string GroundMaterialPath =
            ProjectRoot + "/Art/Materials/M_FlightTestGround.mat";
        private const string BodyMaterialPath =
            ProjectRoot + "/Art/Materials/M_HelicopterBody.mat";
        private const string CockpitMaterialPath =
            ProjectRoot + "/Art/Materials/M_HelicopterCockpit.mat";
        private const string RotorMaterialPath =
            ProjectRoot + "/Art/Materials/M_HelicopterRotor.mat";
        private const string PlayerPrefabPath =
            ProjectRoot + "/Prefabs/Player/PlayerHelicopter.prefab";
        private const string GameScenePath =
            ProjectRoot + "/Scenes/Game.unity";

        private static readonly string[] RequiredFolders =
        {
            ProjectRoot,
            ProjectRoot + "/Art",
            ProjectRoot + "/Art/Materials",
            ProjectRoot + "/Art/Models",
            ProjectRoot + "/Art/Textures",
            ProjectRoot + "/Art/VFX",
            ProjectRoot + "/Audio",
            ProjectRoot + "/Input",
            ProjectRoot + "/Prefabs",
            ProjectRoot + "/Prefabs/Player",
            ProjectRoot + "/Prefabs/Enemies",
            ProjectRoot + "/Prefabs/Projectiles",
            ProjectRoot + "/Prefabs/Environment",
            ProjectRoot + "/Prefabs/UI",
            ProjectRoot + "/Scenes",
            ProjectRoot + "/Scripts",
            ProjectRoot + "/Scripts/Audio",
            ProjectRoot + "/Scripts/Camera",
            ProjectRoot + "/Scripts/Combat",
            ProjectRoot + "/Scripts/Core",
            ProjectRoot + "/Scripts/Editor",
            ProjectRoot + "/Scripts/Enemies",
            ProjectRoot + "/Scripts/Player",
            ProjectRoot + "/Scripts/Terrain",
            ProjectRoot + "/Scripts/UI",
            ProjectRoot + "/Scripts/Utilities",
            ProjectRoot + "/Scripts/Weapons",
            ProjectRoot + "/Settings"
        };

        [MenuItem("Tools/Helicopter Combat/Rebuild Milestone 1 Flight Test", false, 10)]
        public static void RebuildMilestoneOne()
        {
            const string title = "Rebuild Milestone 1";
            const string message =
                "This recreates the Milestone 1 scene, input asset, player prefab, " +
                "and four generated materials. It overwrites only the known Milestone 1 assets. Continue?";

            if (!EditorUtility.DisplayDialog(title, message, "Rebuild", "Cancel"))
            {
                return;
            }

            EnsureFoldersExist();
            CreateOrReplaceInputActions();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            InputActionAsset inputActions =
                AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputActionsPath);

            if (inputActions == null)
            {
                Debug.LogError(
                    "Milestone 1 setup failed: the helicopter Input Action asset could not be imported.");
                return;
            }

            Material groundMaterial = CreateOrUpdateMaterial(
                GroundMaterialPath,
                new Color(0.21f, 0.36f, 0.15f, 1f),
                0f,
                0.22f);

            Material bodyMaterial = CreateOrUpdateMaterial(
                BodyMaterialPath,
                new Color(0.18f, 0.28f, 0.16f, 1f),
                0.08f,
                0.30f);

            Material cockpitMaterial = CreateOrUpdateMaterial(
                CockpitMaterialPath,
                new Color(0.05f, 0.10f, 0.13f, 1f),
                0.65f,
                0.55f);

            Material rotorMaterial = CreateOrUpdateMaterial(
                RotorMaterialPath,
                new Color(0.08f, 0.08f, 0.08f, 1f),
                0.15f,
                0.20f);

            BuildScene(
                inputActions,
                groundMaterial,
                bodyMaterial,
                cockpitMaterial,
                rotorMaterial);
        }

        private static void EnsureFoldersExist()
        {
            foreach (string folder in RequiredFolders)
            {
                Directory.CreateDirectory(Path.GetFullPath(folder));
            }

            AssetDatabase.Refresh();
        }

        private static void CreateOrReplaceInputActions()
        {
            AssetDatabase.DeleteAsset(InputActionsPath);

            InputActionAsset inputAsset = ScriptableObject.CreateInstance<InputActionAsset>();
            inputAsset.name = "HelicopterControls";

            InputActionMap helicopterMap = inputAsset.AddActionMap("Helicopter");

            InputAction move = helicopterMap.AddAction(
                "Move",
                InputActionType.Value);

            move.expectedControlType = "Vector2";

            move.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/w")
                .With("Down", "<Keyboard>/s")
                .With("Left", "<Keyboard>/a")
                .With("Right", "<Keyboard>/d");

            InputAction altitude = helicopterMap.AddAction(
                "Altitude",
                InputActionType.Value);

            altitude.expectedControlType = "Axis";

            altitude.AddCompositeBinding("1DAxis")
                .With("Negative", "<Keyboard>/f")
                .With("Positive", "<Keyboard>/r");

            InputAction yaw = helicopterMap.AddAction(
                "Yaw",
                InputActionType.Value);

            yaw.expectedControlType = "Axis";

            yaw.AddCompositeBinding("1DAxis")
                .With("Negative", "<Keyboard>/q")
                .With("Positive", "<Keyboard>/e");

            InputAction look = helicopterMap.AddAction(
                "Look",
                InputActionType.Value,
                binding: "<Mouse>/delta");

            look.expectedControlType = "Vector2";

            inputAsset.AddControlScheme("Keyboard&Mouse")
                .WithRequiredDevice("<Keyboard>")
                .WithRequiredDevice("<Mouse>");

            string absolutePath = Path.GetFullPath(InputActionsPath);
            File.WriteAllText(absolutePath, inputAsset.ToJson());
            Object.DestroyImmediate(inputAsset);

            AssetDatabase.ImportAsset(
                InputActionsPath,
                ImportAssetOptions.ForceSynchronousImport);
        }

        private static Material CreateOrUpdateMaterial(
            string assetPath,
            Color color,
            float metallic,
            float smoothness)
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(assetPath);

            if (material == null)
            {
                Shader standardShader = Shader.Find("Standard");

                if (standardShader == null)
                {
                    throw new System.InvalidOperationException(
                        "The Built-in Render Pipeline Standard shader was not found.");
                }

                material = new Material(standardShader);
                AssetDatabase.CreateAsset(material, assetPath);
            }

            material.color = color;
            material.SetFloat("_Metallic", metallic);
            material.SetFloat("_Glossiness", smoothness);
            EditorUtility.SetDirty(material);
            return material;
        }

        private static void BuildScene(
            InputActionAsset inputActions,
            Material groundMaterial,
            Material bodyMaterial,
            Material cockpitMaterial,
            Material rotorMaterial)
        {
            AssetDatabase.DeleteAsset(GameScenePath);
            AssetDatabase.DeleteAsset(PlayerPrefabPath);

            Scene scene = EditorSceneManager.NewScene(
                NewSceneSetup.DefaultGameObjects,
                NewSceneMode.Single);

            scene.name = "Game";

            ConfigureRenderSettings();
            Camera mainCamera = ConfigureMainCamera();
            ConfigureDirectionalLight();
            CreateFlightTestGround(groundMaterial);

            GameObject player = CreatePlayerHelicopter(
                inputActions,
                bodyMaterial,
                cockpitMaterial,
                rotorMaterial,
                out HelicopterInputReader inputReader,
                out Transform cameraFocus);

            ThirdPersonHelicopterCamera cameraController =
                mainCamera.GetComponent<ThirdPersonHelicopterCamera>();

            if (cameraController == null)
            {
                cameraController = mainCamera.gameObject
                    .AddComponent<ThirdPersonHelicopterCamera>();
            }

            cameraController.Configure(cameraFocus, inputReader);
            mainCamera.transform.position = new Vector3(0f, 6f, -12f);
            mainCamera.transform.LookAt(cameraFocus.position);

            PrefabUtility.SaveAsPrefabAssetAndConnect(
                player,
                PlayerPrefabPath,
                InteractionMode.AutomatedAction);

            EditorSceneManager.SaveScene(scene, GameScenePath);
            AddGameSceneToBuildSettings();

            Selection.activeGameObject = player;
            EditorGUIUtility.PingObject(player);
            Debug.Log(
                "Milestone 1 built successfully. Open Game.unity and enter Play Mode to test the helicopter.");
        }

        private static void ConfigureRenderSettings()
        {
            RenderSettings.ambientMode = AmbientMode.Skybox;
            RenderSettings.fog = false;
            DynamicGI.UpdateEnvironment();
        }

        private static Camera ConfigureMainCamera()
        {
            Camera mainCamera = Camera.main;

            if (mainCamera == null)
            {
                GameObject cameraObject = new GameObject("Main Camera");
                cameraObject.tag = "MainCamera";
                mainCamera = cameraObject.AddComponent<Camera>();
            }

            mainCamera.gameObject.name = "Main Camera";
            mainCamera.tag = "MainCamera";
            mainCamera.clearFlags = CameraClearFlags.Skybox;
            mainCamera.nearClipPlane = 0.1f;
            mainCamera.farClipPlane = 1000f;
            mainCamera.fieldOfView = 60f;
            return mainCamera;
        }

        private static void ConfigureDirectionalLight()
        {
            Light directionalLight = Object.FindAnyObjectByType<Light>();

            if (directionalLight == null)
            {
                GameObject lightObject = new GameObject("Directional Light");
                directionalLight = lightObject.AddComponent<Light>();
            }

            directionalLight.gameObject.name = "Directional Light";
            directionalLight.type = LightType.Directional;
            directionalLight.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            directionalLight.intensity = 1.15f;
            directionalLight.color = Color.white;
            directionalLight.shadows = LightShadows.Soft;
        }

        private static void CreateFlightTestGround(Material groundMaterial)
        {
            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "FlightTestGround";
            ground.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            ground.transform.localScale = new Vector3(20f, 1f, 20f);
            ground.GetComponent<Renderer>().sharedMaterial = groundMaterial;
        }

        private static GameObject CreatePlayerHelicopter(
            InputActionAsset inputActions,
            Material bodyMaterial,
            Material cockpitMaterial,
            Material rotorMaterial,
            out HelicopterInputReader inputReader,
            out Transform cameraFocus)
        {
            GameObject player = new GameObject("PlayerHelicopter");
            player.transform.SetPositionAndRotation(
                new Vector3(0f, 8f, 0f),
                Quaternion.identity);

            Rigidbody rigidbody = player.AddComponent<Rigidbody>();
            rigidbody.mass = 1f;
            rigidbody.useGravity = false;
            rigidbody.isKinematic = false;
            rigidbody.interpolation = RigidbodyInterpolation.Interpolate;
            rigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            rigidbody.linearDamping = 0.15f;
            rigidbody.angularDamping = 0.50f;
            rigidbody.constraints =
                RigidbodyConstraints.FreezeRotationX |
                RigidbodyConstraints.FreezeRotationZ;

            BoxCollider boxCollider = player.AddComponent<BoxCollider>();
            boxCollider.center = Vector3.zero;
            boxCollider.size = new Vector3(2.4f, 1.2f, 5.8f);
            boxCollider.isTrigger = false;

            PlayerInput playerInput = player.AddComponent<PlayerInput>();
            playerInput.actions = inputActions;
            playerInput.defaultActionMap = "Helicopter";
            playerInput.defaultControlScheme = string.Empty;
            playerInput.notificationBehavior = PlayerNotifications.InvokeUnityEvents;

            inputReader = player.AddComponent<HelicopterInputReader>();
            inputReader.Configure(playerInput);

            HelicopterFlightController flightController =
                player.AddComponent<HelicopterFlightController>();
            flightController.Configure(inputReader, rigidbody);

            CreateHelicopterVisual(
                player.transform,
                inputReader,
                bodyMaterial,
                cockpitMaterial,
                rotorMaterial);

            GameObject focusObject = new GameObject("CameraFocus");
            focusObject.transform.SetParent(player.transform, false);
            focusObject.transform.localPosition = new Vector3(0f, 1.5f, 0f);
            cameraFocus = focusObject.transform;

            return player;
        }

        private static void CreateHelicopterVisual(
            Transform playerRoot,
            HelicopterInputReader inputReader,
            Material bodyMaterial,
            Material cockpitMaterial,
            Material rotorMaterial)
        {
            GameObject visualRoot = new GameObject("HelicopterVisual");
            visualRoot.transform.SetParent(playerRoot, false);

            HelicopterVisualTilt visualTilt =
                visualRoot.AddComponent<HelicopterVisualTilt>();
            visualTilt.Configure(inputReader);

            CreateVisualPrimitive(
                PrimitiveType.Capsule,
                "Body",
                visualRoot.transform,
                new Vector3(0f, 0f, 0f),
                Vector3.zero,
                new Vector3(1.1f, 0.55f, 2.1f),
                bodyMaterial);

            CreateVisualPrimitive(
                PrimitiveType.Sphere,
                "Cockpit",
                visualRoot.transform,
                new Vector3(0f, 0.12f, 1.3f),
                Vector3.zero,
                new Vector3(0.85f, 0.55f, 0.90f),
                cockpitMaterial);

            CreateVisualPrimitive(
                PrimitiveType.Cube,
                "TailBoom",
                visualRoot.transform,
                new Vector3(0f, 0.10f, -3.0f),
                Vector3.zero,
                new Vector3(0.35f, 0.25f, 3.0f),
                bodyMaterial);

            GameObject mainRotorPivot = new GameObject("MainRotorPivot");
            mainRotorPivot.transform.SetParent(visualRoot.transform, false);
            mainRotorPivot.transform.localPosition = new Vector3(0f, 0.80f, 0f);
            mainRotorPivot.AddComponent<HelicopterRotorSpinner>()
                .Configure(Vector3.up, 1800f);

            CreateVisualPrimitive(
                PrimitiveType.Cube,
                "MainRotorBlade_A",
                mainRotorPivot.transform,
                Vector3.zero,
                Vector3.zero,
                new Vector3(5.2f, 0.05f, 0.14f),
                rotorMaterial);

            CreateVisualPrimitive(
                PrimitiveType.Cube,
                "MainRotorBlade_B",
                mainRotorPivot.transform,
                Vector3.zero,
                new Vector3(0f, 90f, 0f),
                new Vector3(5.2f, 0.05f, 0.14f),
                rotorMaterial);

            GameObject tailRotorPivot = new GameObject("TailRotorPivot");
            tailRotorPivot.transform.SetParent(visualRoot.transform, false);
            tailRotorPivot.transform.localPosition = new Vector3(0f, 0.28f, -4.4f);
            tailRotorPivot.AddComponent<HelicopterRotorSpinner>()
                .Configure(Vector3.forward, 2200f);

            CreateVisualPrimitive(
                PrimitiveType.Cube,
                "TailRotorBlade_A",
                tailRotorPivot.transform,
                Vector3.zero,
                Vector3.zero,
                new Vector3(1.20f, 0.04f, 0.10f),
                rotorMaterial);

            CreateVisualPrimitive(
                PrimitiveType.Cube,
                "TailRotorBlade_B",
                tailRotorPivot.transform,
                Vector3.zero,
                new Vector3(0f, 90f, 0f),
                new Vector3(1.20f, 0.04f, 0.10f),
                rotorMaterial);
        }

        private static GameObject CreateVisualPrimitive(
            PrimitiveType primitiveType,
            string objectName,
            Transform parent,
            Vector3 localPosition,
            Vector3 localEulerAngles,
            Vector3 localScale,
            Material material)
        {
            GameObject visualObject = GameObject.CreatePrimitive(primitiveType);
            visualObject.name = objectName;
            visualObject.transform.SetParent(parent, false);
            visualObject.transform.localPosition = localPosition;
            visualObject.transform.localRotation = Quaternion.Euler(localEulerAngles);
            visualObject.transform.localScale = localScale;
            visualObject.GetComponent<Renderer>().sharedMaterial = material;

            Collider primitiveCollider = visualObject.GetComponent<Collider>();

            if (primitiveCollider != null)
            {
                Object.DestroyImmediate(primitiveCollider);
            }

            return visualObject;
        }

        private static void AddGameSceneToBuildSettings()
        {
            List<EditorBuildSettingsScene> scenes =
                new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);

            scenes.RemoveAll(scene => scene.path == GameScenePath);
            scenes.Insert(0, new EditorBuildSettingsScene(GameScenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }
}
