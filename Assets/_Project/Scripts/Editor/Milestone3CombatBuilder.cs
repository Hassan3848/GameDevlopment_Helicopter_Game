using System;
using System.IO;
using HelicopterCombat.Combat;
using HelicopterCombat.Player;
using HelicopterCombat.Weapons;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace HelicopterCombat.EditorTools
{
    public static class Milestone3CombatBuilder
    {
        private const string ProjectRoot = "Assets/_Project";
        private const string ScenePath = ProjectRoot + "/Scenes/Game.unity";
        private const string InputActionsPath = ProjectRoot + "/Input/HelicopterControls.inputactions";
        private const string PlayerPrefabPath = ProjectRoot + "/Prefabs/Player/PlayerHelicopter.prefab";
        private const string MaterialsPath = ProjectRoot + "/Art/Materials";
        private const string VfxPrefabPath = ProjectRoot + "/Prefabs/VFX/Explosion.prefab";
        private const string MissilePrefabPath = ProjectRoot + "/Prefabs/Projectiles/PlayerMissile.prefab";
        private const string BombPrefabPath = ProjectRoot + "/Prefabs/Projectiles/PlayerBomb.prefab";
        private const string HelicopterTargetPrefabPath = ProjectRoot + "/Prefabs/TestTargets/TestHelicopterTarget.prefab";
        private const string TankTargetPrefabPath = ProjectRoot + "/Prefabs/TestTargets/TestTankTarget.prefab";

        [MenuItem("Tools/Helicopter Combat/Rebuild Milestone 3 Combat")]
        public static void RebuildMilestone3Combat()
        {
            EnsureFolders();
            UpdateInputActions();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            InputActionAsset inputActions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputActionsPath);
            GameObject explosionPrefab = CreateExplosionPrefab();
            GameObject missilePrefab = CreateMissilePrefab(explosionPrefab);
            GameObject bombPrefab = CreateBombPrefab(explosionPrefab);
            GameObject helicopterTargetPrefab = CreateHelicopterTargetPrefab(explosionPrefab);
            GameObject tankTargetPrefab = CreateTankTargetPrefab(explosionPrefab);

            UpdatePlayerPrefab(inputActions, missilePrefab, bombPrefab);
            UpdateGameScene(inputActions, missilePrefab, bombPrefab, helicopterTargetPrefab, tankTargetPrefab);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Milestone 3 combat setup complete: input actions, weapons, projectile prefabs, hardpoints, and combat test range are ready.");
        }

        public static void RebuildFromCommandLine()
        {
            RebuildMilestone3Combat();
        }

        private static void EnsureFolders()
        {
            string[] folders =
            {
                ProjectRoot + "/Art/Materials",
                ProjectRoot + "/Art/VFX",
                ProjectRoot + "/Input",
                ProjectRoot + "/Prefabs/Player",
                ProjectRoot + "/Prefabs/Projectiles",
                ProjectRoot + "/Prefabs/VFX",
                ProjectRoot + "/Prefabs/TestTargets",
                ProjectRoot + "/Scripts/Combat",
                ProjectRoot + "/Scripts/Player",
                ProjectRoot + "/Scripts/Weapons",
                ProjectRoot + "/Scripts/VFX",
                ProjectRoot + "/Scripts/Editor",
                ProjectRoot + "/Settings"
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

        private static void UpdateInputActions()
        {
            InputActionAsset inputAsset = AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputActionsPath);

            if (inputAsset == null)
            {
                throw new InvalidOperationException("Input Actions asset was not found at " + InputActionsPath);
            }

            InputActionMap helicopterMap = inputAsset.FindActionMap("Helicopter", false);

            if (helicopterMap == null)
            {
                throw new InvalidOperationException("Input Actions asset is missing the Helicopter action map.");
            }

            AddButtonActionIfMissing(helicopterMap, "FireMissile", "<Mouse>/leftButton", "<Keyboard>/space");
            AddButtonActionIfMissing(helicopterMap, "DropBomb", "<Mouse>/rightButton", "<Keyboard>/b");

            File.WriteAllText(InputActionsPath, inputAsset.ToJson());
            AssetDatabase.ImportAsset(InputActionsPath, ImportAssetOptions.ForceUpdate);
        }

        private static void AddButtonActionIfMissing(InputActionMap actionMap, string actionName, string primaryBinding, string backupBinding)
        {
            InputAction action = actionMap.FindAction(actionName, false);

            if (action == null)
            {
                action = actionMap.AddAction(actionName, InputActionType.Button);
            }

            action.expectedControlType = "Button";
            AddBindingIfMissing(action, primaryBinding);
            AddBindingIfMissing(action, backupBinding);
        }

        private static void AddBindingIfMissing(InputAction action, string bindingPath)
        {
            foreach (InputBinding binding in action.bindings)
            {
                if (binding.path == bindingPath)
                {
                    return;
                }
            }

            action.AddBinding(bindingPath);
        }

        private static GameObject CreateExplosionPrefab()
        {
            Material fireMaterial = CreateOrUpdateMaterial("M3_ExplosionFire", new Color(1f, 0.45f, 0.06f), 0f, 0.15f, true);
            Material smokeMaterial = CreateOrUpdateMaterial("M3_ExplosionSmoke", new Color(0.13f, 0.12f, 0.11f, 0.62f), 0f, 0.75f, false);

            GameObject root = new GameObject("Explosion");
            Explosion explosion = root.AddComponent<Explosion>();
            explosion.ConfigureDefaults(60f, 7f, 450f, 0.75f, 2.5f);

            GameObject fire = CreateParticleChild("FireBurst", root.transform, fireMaterial, 45, 0.65f, 1.5f, 4.5f, new Color(1f, 0.42f, 0.05f, 1f));
            GameObject smoke = CreateParticleChild("SmokeBurst", root.transform, smokeMaterial, 18, 2.1f, 2.0f, 5.0f, new Color(0.16f, 0.15f, 0.13f, 0.72f));

            ParticleSystem.ShapeModule fireShape = fire.GetComponent<ParticleSystem>().shape;
            fireShape.radius = 0.6f;
            ParticleSystem.ShapeModule smokeShape = smoke.GetComponent<ParticleSystem>().shape;
            smokeShape.radius = 0.8f;

            Light pointLight = root.AddComponent<Light>();
            pointLight.type = LightType.Point;
            pointLight.color = new Color(1f, 0.48f, 0.10f);
            pointLight.range = 10f;
            pointLight.intensity = 2.2f;

            GameObject prefab = SavePrefab(root, VfxPrefabPath);
            UnityEngine.Object.DestroyImmediate(root);
            return prefab;
        }

        private static GameObject CreateParticleChild(string name, Transform parent, Material material, int particleCount, float lifetime, float minSize, float maxSize, Color startColor)
        {
            GameObject child = new GameObject(name);
            child.transform.SetParent(parent, false);

            ParticleSystem particles = child.AddComponent<ParticleSystem>();
            ParticleSystem.MainModule main = particles.main;
            main.playOnAwake = true;
            main.loop = false;
            main.duration = 0.2f;
            main.startLifetime = lifetime;
            main.startSpeed = new ParticleSystem.MinMaxCurve(3f, 8f);
            main.startSize = new ParticleSystem.MinMaxCurve(minSize, maxSize);
            main.startColor = startColor;
            main.maxParticles = particleCount;

            ParticleSystem.EmissionModule emission = particles.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)particleCount) });

            ParticleSystem.ShapeModule shape = particles.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;

            ParticleSystemRenderer renderer = child.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = material;
            renderer.renderMode = ParticleSystemRenderMode.Billboard;

            return child;
        }

        private static GameObject CreateMissilePrefab(GameObject explosionPrefab)
        {
            Material missileMaterial = CreateOrUpdateMaterial("M3_Missile", new Color(0.19f, 0.22f, 0.18f), 0.05f, 0.25f, false);
            GameObject root = new GameObject("PlayerMissile");
            Rigidbody rigidbody = root.AddComponent<Rigidbody>();
            rigidbody.useGravity = false;
            rigidbody.mass = 1f;
            rigidbody.interpolation = RigidbodyInterpolation.Interpolate;
            rigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

            CapsuleCollider collider = root.AddComponent<CapsuleCollider>();
            collider.radius = 0.12f;
            collider.height = 1.2f;
            collider.direction = 2;

            MissileProjectile projectile = root.AddComponent<MissileProjectile>();
            projectile.ConfigureExplosion(explosionPrefab, 60f, 7f, 450f, 5f, 0.08f);
            projectile.Configure(65f, 0.35f);

            CreatePrimitive(PrimitiveType.Capsule, "Body", root.transform, Vector3.zero, Quaternion.Euler(90f, 0f, 0f), new Vector3(0.18f, 0.55f, 0.18f), missileMaterial, false);
            CreatePrimitive(PrimitiveType.Cube, "FinTop", root.transform, new Vector3(0f, 0.15f, -0.38f), Quaternion.identity, new Vector3(0.05f, 0.24f, 0.22f), missileMaterial, false);
            CreatePrimitive(PrimitiveType.Cube, "FinLeft", root.transform, new Vector3(-0.15f, 0f, -0.38f), Quaternion.identity, new Vector3(0.24f, 0.05f, 0.22f), missileMaterial, false);

            TrailRenderer trail = root.AddComponent<TrailRenderer>();
            trail.time = 0.55f;
            trail.startWidth = 0.20f;
            trail.endWidth = 0.02f;
            trail.sharedMaterial = CreateOrUpdateMaterial("M3_ExplosionSmoke", new Color(0.13f, 0.12f, 0.11f, 0.62f), 0f, 0.75f, false);

            GameObject prefab = SavePrefab(root, MissilePrefabPath);
            UnityEngine.Object.DestroyImmediate(root);
            return prefab;
        }

        private static GameObject CreateBombPrefab(GameObject explosionPrefab)
        {
            Material bombMaterial = CreateOrUpdateMaterial("M3_Bomb", new Color(0.10f, 0.11f, 0.11f), 0.03f, 0.28f, false);
            GameObject root = new GameObject("PlayerBomb");
            Rigidbody rigidbody = root.AddComponent<Rigidbody>();
            rigidbody.useGravity = true;
            rigidbody.mass = 2f;
            rigidbody.interpolation = RigidbodyInterpolation.Interpolate;
            rigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

            CapsuleCollider collider = root.AddComponent<CapsuleCollider>();
            collider.radius = 0.18f;
            collider.height = 0.9f;
            collider.direction = 1;

            BombProjectile projectile = root.AddComponent<BombProjectile>();
            projectile.ConfigureExplosion(explosionPrefab, 120f, 10f, 800f, 8f, 0.15f);
            projectile.Configure(8f, 1f, 1f);

            CreatePrimitive(PrimitiveType.Capsule, "Body", root.transform, Vector3.zero, Quaternion.identity, new Vector3(0.30f, 0.55f, 0.30f), bombMaterial, false);
            CreatePrimitive(PrimitiveType.Cube, "TailFinA", root.transform, new Vector3(0.18f, 0.38f, 0f), Quaternion.identity, new Vector3(0.05f, 0.22f, 0.28f), bombMaterial, false);
            CreatePrimitive(PrimitiveType.Cube, "TailFinB", root.transform, new Vector3(0f, 0.38f, 0.18f), Quaternion.identity, new Vector3(0.28f, 0.22f, 0.05f), bombMaterial, false);

            GameObject prefab = SavePrefab(root, BombPrefabPath);
            UnityEngine.Object.DestroyImmediate(root);
            return prefab;
        }

        private static GameObject CreateHelicopterTargetPrefab(GameObject explosionPrefab)
        {
            Material material = CreateOrUpdateMaterial("M3_TestHelicopterTarget", new Color(1f, 0.28f, 0.08f), 0f, 0.35f, false);
            GameObject root = new GameObject("TestHelicopterTarget");

            Health health = root.AddComponent<Health>();
            health.Configure(120f, false);
            DestroyOnDeath destroyOnDeath = root.AddComponent<DestroyOnDeath>();
            destroyOnDeath.Configure(explosionPrefab, 0.05f, true);

            Rigidbody rigidbody = root.AddComponent<Rigidbody>();
            rigidbody.isKinematic = true;

            BoxCollider collider = root.AddComponent<BoxCollider>();
            collider.size = new Vector3(4f, 1.4f, 3.2f);

            CreatePrimitive(PrimitiveType.Capsule, "Fuselage", root.transform, Vector3.zero, Quaternion.Euler(90f, 0f, 0f), new Vector3(0.70f, 1.80f, 0.70f), material, false);
            CreatePrimitive(PrimitiveType.Cube, "MainRotor", root.transform, new Vector3(0f, 0.65f, 0f), Quaternion.identity, new Vector3(4.5f, 0.08f, 0.18f), material, false);
            CreatePrimitive(PrimitiveType.Cube, "Tail", root.transform, new Vector3(0f, 0f, -1.85f), Quaternion.identity, new Vector3(0.25f, 0.22f, 2.0f), material, false);

            GameObject prefab = SavePrefab(root, HelicopterTargetPrefabPath);
            UnityEngine.Object.DestroyImmediate(root);
            return prefab;
        }

        private static GameObject CreateTankTargetPrefab(GameObject explosionPrefab)
        {
            Material material = CreateOrUpdateMaterial("M3_TestTankTarget", new Color(0.72f, 0.62f, 0.20f), 0f, 0.38f, false);
            GameObject root = new GameObject("TestTankTarget");

            Health health = root.AddComponent<Health>();
            health.Configure(120f, false);
            DestroyOnDeath destroyOnDeath = root.AddComponent<DestroyOnDeath>();
            destroyOnDeath.Configure(explosionPrefab, 0.05f, true);

            Rigidbody rigidbody = root.AddComponent<Rigidbody>();
            rigidbody.isKinematic = true;

            BoxCollider collider = root.AddComponent<BoxCollider>();
            collider.size = new Vector3(3.8f, 1.6f, 4.2f);

            CreatePrimitive(PrimitiveType.Cube, "Hull", root.transform, new Vector3(0f, 0.45f, 0f), Quaternion.identity, new Vector3(3.6f, 0.9f, 4.0f), material, false);
            CreatePrimitive(PrimitiveType.Cube, "Turret", root.transform, new Vector3(0f, 1.15f, 0.25f), Quaternion.identity, new Vector3(1.6f, 0.7f, 1.6f), material, false);
            CreatePrimitive(PrimitiveType.Cylinder, "Barrel", root.transform, new Vector3(0f, 1.18f, 1.65f), Quaternion.Euler(90f, 0f, 0f), new Vector3(0.16f, 1.2f, 0.16f), material, false);

            GameObject prefab = SavePrefab(root, TankTargetPrefabPath);
            UnityEngine.Object.DestroyImmediate(root);
            return prefab;
        }

        private static GameObject CreatePrimitive(PrimitiveType type, string name, Transform parent, Vector3 localPosition, Quaternion localRotation, Vector3 localScale, Material material, bool keepCollider)
        {
            GameObject primitive = GameObject.CreatePrimitive(type);
            primitive.name = name;
            primitive.transform.SetParent(parent, false);
            primitive.transform.localPosition = localPosition;
            primitive.transform.localRotation = localRotation;
            primitive.transform.localScale = localScale;

            Renderer renderer = primitive.GetComponent<Renderer>();
            renderer.sharedMaterial = material;

            if (!keepCollider)
            {
                Collider collider = primitive.GetComponent<Collider>();

                if (collider != null)
                {
                    UnityEngine.Object.DestroyImmediate(collider);
                }
            }

            return primitive;
        }

        private static void UpdatePlayerPrefab(InputActionAsset inputActions, GameObject missilePrefab, GameObject bombPrefab)
        {
            GameObject prefabRoot = PrefabUtility.LoadPrefabContents(PlayerPrefabPath);
            ConfigurePlayer(prefabRoot, inputActions, missilePrefab, bombPrefab);
            PrefabUtility.SaveAsPrefabAsset(prefabRoot, PlayerPrefabPath);
            PrefabUtility.UnloadPrefabContents(prefabRoot);
        }

        private static void UpdateGameScene(InputActionAsset inputActions, GameObject missilePrefab, GameObject bombPrefab, GameObject helicopterTargetPrefab, GameObject tankTargetPrefab)
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) == null)
            {
                throw new InvalidOperationException("Game scene was not found at " + ScenePath);
            }

            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            GameObject player = GameObject.Find("PlayerHelicopter");

            if (player == null)
            {
                throw new InvalidOperationException("PlayerHelicopter was not found in Game.unity.");
            }

            ConfigurePlayer(player, inputActions, missilePrefab, bombPrefab);
            RebuildCombatTestRange(player.transform, helicopterTargetPrefab, tankTargetPrefab);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        private static void ConfigurePlayer(GameObject player, InputActionAsset inputActions, GameObject missilePrefab, GameObject bombPrefab)
        {
            Transform left = CreateOrUpdateChild(player.transform, "MissileHardpointLeft", new Vector3(-1.35f, -0.30f, 1.70f));
            Transform right = CreateOrUpdateChild(player.transform, "MissileHardpointRight", new Vector3(1.35f, -0.30f, 1.70f));
            Transform bombDrop = CreateOrUpdateChild(player.transform, "BombDropPoint", new Vector3(0f, -0.90f, 0.10f));
            Rigidbody playerRigidbody = player.GetComponent<Rigidbody>();

            PlayerCombatInputReader combatInput = GetOrAdd<PlayerCombatInputReader>(player);
            MissileLauncher missileLauncher = GetOrAdd<MissileLauncher>(player);
            BombLauncher bombLauncher = GetOrAdd<BombLauncher>(player);
            PlayerWeaponController weaponController = GetOrAdd<PlayerWeaponController>(player);

            combatInput.Configure(inputActions);
            missileLauncher.Configure(missilePrefab, new[] { left, right }, player, playerRigidbody);
            missileLauncher.ConfigureAmmoAndCooldown(int.MaxValue, 0.25f);
            bombLauncher.Configure(bombPrefab, bombDrop, player, playerRigidbody);
            weaponController.Configure(combatInput, missileLauncher, bombLauncher);

            EditorUtility.SetDirty(player);
        }

        private static Transform CreateOrUpdateChild(Transform parent, string name, Vector3 localPosition)
        {
            Transform child = parent.Find(name);

            if (child == null)
            {
                child = new GameObject(name).transform;
                child.SetParent(parent, false);
            }

            child.localPosition = localPosition;
            child.localRotation = Quaternion.identity;
            child.localScale = Vector3.one;
            return child;
        }

        private static T GetOrAdd<T>(GameObject target) where T : Component
        {
            T component = target.GetComponent<T>();
            return component != null ? component : target.AddComponent<T>();
        }

        private static void RebuildCombatTestRange(Transform player, GameObject helicopterTargetPrefab, GameObject tankTargetPrefab)
        {
            GameObject existing = GameObject.Find("CombatTestRange");

            if (existing != null)
            {
                UnityEngine.Object.DestroyImmediate(existing);
            }

            Terrain terrain = Terrain.activeTerrain;
            GameObject root = new GameObject("CombatTestRange");
            Vector3 forward = Vector3.ProjectOnPlane(player.forward, Vector3.up).normalized;

            if (forward.sqrMagnitude < 0.01f)
            {
                forward = Vector3.forward;
            }

            Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;
            PlaceTarget(helicopterTargetPrefab, root.transform, CalculatePosition(player.position, forward, right, terrain, 72f, -18f, 24f), Quaternion.LookRotation(-forward, Vector3.up), "AirborneTarget_Left");
            PlaceTarget(helicopterTargetPrefab, root.transform, CalculatePosition(player.position, forward, right, terrain, 92f, 22f, 28f), Quaternion.LookRotation(-forward, Vector3.up), "AirborneTarget_Right");
            PlaceTarget(tankTargetPrefab, root.transform, CalculatePosition(player.position, forward, right, terrain, 50f, -20f, 0.8f), Quaternion.LookRotation(-forward, Vector3.up), "BombTarget_Left");
            PlaceTarget(tankTargetPrefab, root.transform, CalculatePosition(player.position, forward, right, terrain, 72f, 0f, 0.8f), Quaternion.LookRotation(-forward, Vector3.up), "BombTarget_Center");
            PlaceTarget(tankTargetPrefab, root.transform, CalculatePosition(player.position, forward, right, terrain, 96f, 24f, 0.8f), Quaternion.LookRotation(-forward, Vector3.up), "BombTarget_Right");
        }

        private static Vector3 CalculatePosition(Vector3 origin, Vector3 forward, Vector3 right, Terrain terrain, float forwardDistance, float sideOffset, float heightOffset)
        {
            Vector3 position = origin + forward * forwardDistance + right * sideOffset;
            float groundHeight = terrain != null ? terrain.SampleHeight(position) + terrain.transform.position.y : 0f;
            position.y = groundHeight + heightOffset;
            return position;
        }

        private static void PlaceTarget(GameObject prefab, Transform parent, Vector3 position, Quaternion rotation, string name)
        {
            GameObject target = PrefabUtility.InstantiatePrefab(prefab, parent) as GameObject;

            if (target == null)
            {
                return;
            }

            target.name = name;
            target.transform.position = position;
            target.transform.rotation = rotation;
        }

        private static Material CreateOrUpdateMaterial(string materialName, Color color, float metallic, float smoothness, bool emission)
        {
            string path = MaterialsPath + "/" + materialName + ".mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);

            if (material == null)
            {
                material = new Material(Shader.Find("Standard"));
                AssetDatabase.CreateAsset(material, path);
            }

            material.name = materialName;
            material.color = color;
            material.SetFloat("_Metallic", metallic);
            material.SetFloat("_Glossiness", smoothness);

            if (emission)
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", color * 1.5f);
            }
            else
            {
                material.DisableKeyword("_EMISSION");
            }

            if (color.a < 0.99f)
            {
                material.SetFloat("_Mode", 3f);
                material.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
                material.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
                material.SetInt("_ZWrite", 0);
                material.DisableKeyword("_ALPHATEST_ON");
                material.EnableKeyword("_ALPHABLEND_ON");
                material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                material.renderQueue = (int)RenderQueue.Transparent;
            }

            EditorUtility.SetDirty(material);
            return material;
        }

        private static GameObject SavePrefab(GameObject root, string assetPath)
        {
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, assetPath);
            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
            return prefab;
        }
    }
}
