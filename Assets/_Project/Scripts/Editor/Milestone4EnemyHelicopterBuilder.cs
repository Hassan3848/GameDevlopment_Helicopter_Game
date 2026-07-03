using System;
using System.Collections.Generic;
using System.IO;
using HelicopterCombat.Combat;
using HelicopterCombat.Core;
using HelicopterCombat.Enemies;
using HelicopterCombat.Player;
using HelicopterCombat.Utilities;
using HelicopterCombat.Weapons;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace HelicopterCombat.EditorTools
{
    public static class Milestone4EnemyHelicopterBuilder
    {
        private const string ProjectRoot = "Assets/_Project";
        private const string ScenePath = ProjectRoot + "/Scenes/Game.unity";
        private const string PlayerPrefabPath = ProjectRoot + "/Prefabs/Player/PlayerHelicopter.prefab";
        private const string PlayerMissilePath = ProjectRoot + "/Prefabs/Projectiles/PlayerMissile.prefab";
        private const string EnemyMissilePath = ProjectRoot + "/Prefabs/Projectiles/EnemyMissile.prefab";
        private const string ExplosionPrefabPath = ProjectRoot + "/Prefabs/VFX/Explosion.prefab";
        private const string EnemyAlphaPrefabPath = ProjectRoot + "/Prefabs/Enemies/EnemyHelicopterAlpha.prefab";
        private const string EnemyBravoPrefabPath = ProjectRoot + "/Prefabs/Enemies/EnemyHelicopterBravo.prefab";
        private const string MaterialsPath = ProjectRoot + "/Art/Materials";

        [MenuItem("Tools/Helicopter Combat/Rebuild Milestone 4 Enemy Helicopters")]
        public static void RebuildMilestone4EnemyHelicopters()
        {
            EnsureFolders();

            List<HelicopterCandidate> candidates = DiscoverHelicopterCandidates();
            HelicopterCandidate playerCandidate = candidates.Count > 0 ? candidates[0] : default;
            HelicopterCandidate alphaCandidate = candidates.Count > 1 ? candidates[1] : playerCandidate;
            HelicopterCandidate bravoCandidate = candidates.Count > 2 ? candidates[2] : playerCandidate;

            Material enemyRed = CreateOrUpdateMaterial("M4_EnemyRed", new Color(0.42f, 0.05f, 0.045f), 0.25f, 0.35f, false);
            Material enemyCharcoal = CreateOrUpdateMaterial("M4_EnemyCharcoal", new Color(0.08f, 0.085f, 0.09f), 0.35f, 0.30f, false);
            Material enemyMissileMaterial = CreateOrUpdateMaterial("M4_EnemyMissile", new Color(0.38f, 0.03f, 0.025f), 0.20f, 0.45f, true);

            GameObject explosionPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ExplosionPrefabPath);
            GameObject enemyMissile = CreateEnemyMissilePrefab(enemyMissileMaterial, explosionPrefab);
            GameObject alphaPrefab = CreateEnemyPrefab("EnemyHelicopterAlpha", alphaCandidate, enemyRed, enemyMissile, explosionPrefab, EnemyAlphaPrefabPath, "Enemy Helicopter Alpha");
            GameObject bravoPrefab = CreateEnemyPrefab("EnemyHelicopterBravo", bravoCandidate, enemyCharcoal, enemyMissile, explosionPrefab, EnemyBravoPrefabPath, "Enemy Helicopter Bravo");

            UpdatePlayerPrefab(playerCandidate);
            UpdateScene(playerCandidate, alphaPrefab, bravoPrefab);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log(
                "Milestone 4 enemy helicopters rebuilt. Candidates: " + candidates.Count +
                ". Player visual: " + CandidatePath(playerCandidate) +
                ", Alpha: " + CandidatePath(alphaCandidate) +
                ", Bravo: " + CandidatePath(bravoCandidate) + ".");
        }

        public static void RebuildFromCommandLine()
        {
            RebuildMilestone4EnemyHelicopters();
        }

        private static void EnsureFolders()
        {
            string[] folders =
            {
                ProjectRoot + "/Art/Materials",
                ProjectRoot + "/Prefabs/Enemies",
                ProjectRoot + "/Prefabs/Player",
                ProjectRoot + "/Prefabs/Projectiles",
                ProjectRoot + "/Prefabs/VFX",
                ProjectRoot + "/Scripts/Combat",
                ProjectRoot + "/Scripts/Core",
                ProjectRoot + "/Scripts/Enemies",
                ProjectRoot + "/Scripts/Player",
                ProjectRoot + "/Scripts/Utilities",
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

        private static List<HelicopterCandidate> DiscoverHelicopterCandidates()
        {
            Dictionary<string, HelicopterCandidate> byPath = new Dictionary<string, HelicopterCandidate>(StringComparer.OrdinalIgnoreCase);
            AddCandidates(byPath, AssetDatabase.FindAssets("t:Model"));
            AddCandidates(byPath, AssetDatabase.FindAssets("t:GameObject"));

            List<HelicopterCandidate> candidates = new List<HelicopterCandidate>(byPath.Values);
            candidates.Sort((left, right) => right.Score.CompareTo(left.Score));

            int reportCount = Mathf.Min(candidates.Count, 8);
            for (int index = 0; index < reportCount; index++)
            {
                Debug.Log("Milestone 4 helicopter candidate " + (index + 1) + ": " + candidates[index].Path + " score=" + candidates[index].Score);
            }

            if (candidates.Count == 0)
            {
                Debug.LogWarning("Milestone 4 could not find imported helicopter candidates. Fallback silhouettes will be used for enemies and player visual will be preserved.");
            }

            return candidates;
        }

        private static void AddCandidates(Dictionary<string, HelicopterCandidate> byPath, string[] guids)
        {
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);

                if (string.IsNullOrEmpty(path) || !path.StartsWith("Assets/", StringComparison.Ordinal))
                {
                    continue;
                }

                string lowerPath = path.ToLowerInvariant();

                if (!(lowerPath.Contains("helicopter") || lowerPath.Contains("heli") || lowerPath.Contains("chopper")))
                {
                    continue;
                }

                if (lowerPath.Contains("thumbnail") ||
                    lowerPath.Contains("/textures/") ||
                    lowerPath.Contains("/materials/") ||
                    lowerPath.EndsWith(".mat", StringComparison.Ordinal) ||
                    lowerPath.EndsWith(".png", StringComparison.Ordinal) ||
                    lowerPath.EndsWith(".cs", StringComparison.Ordinal) ||
                    lowerPath.EndsWith(".unity", StringComparison.Ordinal))
                {
                    continue;
                }

                GameObject asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);

                if (asset == null)
                {
                    continue;
                }

                Renderer[] renderers = asset.GetComponentsInChildren<Renderer>(true);
                int enabledRendererCount = 0;

                foreach (Renderer renderer in renderers)
                {
                    if (renderer != null && renderer.enabled)
                    {
                        enabledRendererCount++;
                    }
                }

                if (enabledRendererCount == 0)
                {
                    continue;
                }

                int score = 1000 + enabledRendererCount * 20;

                if (lowerPath.EndsWith(".fbx", StringComparison.Ordinal))
                {
                    score += 150;
                }

                if (lowerPath.EndsWith(".prefab", StringComparison.Ordinal))
                {
                    score += 120;
                }

                if (Path.GetFileNameWithoutExtension(lowerPath).StartsWith("heli", StringComparison.Ordinal))
                {
                    score += 100;
                }

                byPath[path] = new HelicopterCandidate(path, asset, score);
            }
        }

        private static GameObject CreateEnemyMissilePrefab(Material enemyMissileMaterial, GameObject explosionPrefab)
        {
            GameObject sourcePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerMissilePath);

            if (sourcePrefab == null)
            {
                throw new InvalidOperationException("PlayerMissile.prefab was not found. Run Milestone 3 first.");
            }

            GameObject root = PrefabUtility.InstantiatePrefab(sourcePrefab) as GameObject;

            if (root == null)
            {
                throw new InvalidOperationException("Could not instantiate PlayerMissile prefab for enemy missile generation.");
            }

            root.name = "EnemyMissile";

            Rigidbody rigidbody = root.GetComponent<Rigidbody>();
            if (rigidbody != null)
            {
                rigidbody.useGravity = false;
                rigidbody.mass = 1f;
                rigidbody.interpolation = RigidbodyInterpolation.Interpolate;
                rigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            }

            MissileProjectile missile = root.GetComponent<MissileProjectile>();
            if (missile != null)
            {
                missile.Configure(58f, 0.30f);
                missile.ConfigureExplosion(explosionPrefab, 250f, 5f, 250f, 5f, 0.12f);
            }

            foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                renderer.sharedMaterial = enemyMissileMaterial;
            }

            GameObject prefab = SavePrefab(root, EnemyMissilePath);
            UnityEngine.Object.DestroyImmediate(root);
            return prefab;
        }

        private static GameObject CreateEnemyPrefab(
            string rootName,
            HelicopterCandidate candidate,
            Material visualMaterial,
            GameObject enemyMissile,
            GameObject explosionPrefab,
            string prefabPath,
            string displayName)
        {
            GameObject root = new GameObject(rootName);
            Rigidbody rigidbody = root.AddComponent<Rigidbody>();
            rigidbody.useGravity = false;
            rigidbody.mass = 5f;
            rigidbody.linearDamping = 0.55f;
            rigidbody.angularDamping = 1.0f;
            rigidbody.interpolation = RigidbodyInterpolation.Interpolate;
            rigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            rigidbody.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;

            BoxCollider collider = root.AddComponent<BoxCollider>();
            collider.center = new Vector3(0f, 0.15f, 0f);
            collider.size = new Vector3(5.8f, 2.0f, 6.4f);

            Health health = root.AddComponent<Health>();
            health.Configure(150f, false);
            DestroyOnDeath destroyOnDeath = root.AddComponent<DestroyOnDeath>();
            destroyOnDeath.Configure(explosionPrefab, 0.05f, true);
            TeamMember teamMember = root.AddComponent<TeamMember>();
            teamMember.Configure(CombatTeam.Enemy);
            EnemyUnit enemyUnit = root.AddComponent<EnemyUnit>();
            enemyUnit.Configure(displayName, EnemyUnit.EnemyUnitType.Helicopter);

            Transform visualPivot = CreateChild(root.transform, "VisualPivot", Vector3.zero);
            CreateImportedVisual(candidate, visualPivot, "ImportedHelicopterModel", visualMaterial, true, out Transform[] rotors);

            Transform left = CreateChild(root.transform, "MissileHardpointLeft", new Vector3(-1.45f, -0.35f, 1.80f));
            Transform right = CreateChild(root.transform, "MissileHardpointRight", new Vector3(1.45f, -0.35f, 1.80f));
            Transform aimOrigin = CreateChild(root.transform, "AimOrigin", new Vector3(0f, 0.25f, 1.70f));

            EnemyHelicopterTargeting targeting = root.AddComponent<EnemyHelicopterTargeting>();
            EnemyHelicopterSeparation separation = root.AddComponent<EnemyHelicopterSeparation>();
            EnemyHelicopterMovement movement = root.AddComponent<EnemyHelicopterMovement>();
            MissileLauncher missileLauncher = root.AddComponent<MissileLauncher>();
            EnemyHelicopterWeaponController weaponController = root.AddComponent<EnemyHelicopterWeaponController>();
            EnemyHelicopterVisualController visualController = root.AddComponent<EnemyHelicopterVisualController>();
            EnemyHelicopterBrain brain = root.AddComponent<EnemyHelicopterBrain>();

            movement.Configure(rigidbody, null, separation);
            movement.ConfigureHomeAndBoundary(root.transform.position, 225f, 280f, 18f, 22f, 155f, 24f);
            missileLauncher.Configure(enemyMissile, new[] { left, right }, root, rigidbody);
            missileLauncher.ConfigureAmmoAndCooldown(999, 2.40f);
            weaponController.Configure(targeting, missileLauncher, aimOrigin, null);
            visualController.Configure(movement, visualPivot, rotors);
            brain.Configure(targeting, movement, weaponController, health);

            GameObject prefab = SavePrefab(root, prefabPath);
            UnityEngine.Object.DestroyImmediate(root);
            return prefab;
        }

        private static Transform CreateChild(Transform parent, string name, Vector3 localPosition)
        {
            GameObject child = new GameObject(name);
            child.transform.SetParent(parent, false);
            child.transform.localPosition = localPosition;
            child.transform.localRotation = Quaternion.identity;
            child.transform.localScale = Vector3.one;
            return child.transform;
        }

        private static bool CreateImportedVisual(
            HelicopterCandidate candidate,
            Transform parent,
            string childName,
            Material overrideMaterial,
            bool replaceMaterials,
            out Transform[] rotors)
        {
            rotors = new Transform[0];

            if (candidate.Asset == null)
            {
                CreateFallbackHelicopterVisual(parent, overrideMaterial);
                return false;
            }

            GameObject visual = PrefabUtility.InstantiatePrefab(candidate.Asset, parent) as GameObject;

            if (visual == null)
            {
                CreateFallbackHelicopterVisual(parent, overrideMaterial);
                return false;
            }

            visual.name = childName;
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localRotation = Quaternion.identity;
            visual.transform.localScale = Vector3.one;

            NormalizeVisual(visual.transform);

            if (replaceMaterials && overrideMaterial != null)
            {
                foreach (Renderer renderer in visual.GetComponentsInChildren<Renderer>(true))
                {
                    renderer.sharedMaterial = overrideMaterial;
                }
            }

            rotors = FindRotorTransforms(visual.transform);
            return true;
        }

        private static void CreateFallbackHelicopterVisual(Transform parent, Material material)
        {
            GameObject root = new GameObject("ImportedHelicopterModel");
            root.transform.SetParent(parent, false);
            CreatePrimitive(PrimitiveType.Capsule, "FallbackFuselage", root.transform, Vector3.zero, Quaternion.Euler(90f, 0f, 0f), new Vector3(0.75f, 2.4f, 0.75f), material);
            CreatePrimitive(PrimitiveType.Cube, "FallbackRotor", root.transform, new Vector3(0f, 0.75f, 0f), Quaternion.identity, new Vector3(6.2f, 0.08f, 0.22f), material);
            CreatePrimitive(PrimitiveType.Cube, "FallbackTail", root.transform, new Vector3(0f, 0f, -2.45f), Quaternion.identity, new Vector3(0.22f, 0.22f, 2.8f), material);
        }

        private static void NormalizeVisual(Transform visualRoot)
        {
            if (!TryGetBounds(visualRoot, out Bounds bounds))
            {
                return;
            }

            float horizontalSize = Mathf.Max(bounds.size.x, bounds.size.z);

            if (horizontalSize > 0.001f)
            {
                float scale = 6.5f / horizontalSize;
                visualRoot.localScale *= scale;
            }

            if (TryGetBounds(visualRoot, out Bounds scaledBounds))
            {
                visualRoot.localPosition -= visualRoot.parent.InverseTransformPoint(scaledBounds.center);
            }
        }

        private static bool TryGetBounds(Transform root, out Bounds bounds)
        {
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
            bool hasBounds = false;
            bounds = new Bounds(root.position, Vector3.zero);

            foreach (Renderer renderer in renderers)
            {
                if (renderer == null || !renderer.enabled)
                {
                    continue;
                }

                if (!hasBounds)
                {
                    bounds = renderer.bounds;
                    hasBounds = true;
                }
                else
                {
                    bounds.Encapsulate(renderer.bounds);
                }
            }

            return hasBounds;
        }

        private static Transform[] FindRotorTransforms(Transform root)
        {
            List<Transform> rotors = new List<Transform>();
            Transform[] transforms = root.GetComponentsInChildren<Transform>(true);

            foreach (Transform candidate in transforms)
            {
                string lowerName = candidate.name.ToLowerInvariant();

                if (lowerName.Contains("rotor") ||
                    lowerName.Contains("propeller") ||
                    lowerName.Contains("mainrotor") ||
                    lowerName.Contains("tailrotor"))
                {
                    rotors.Add(candidate);
                }
            }

            return rotors.ToArray();
        }

        private static GameObject CreatePrimitive(PrimitiveType type, string name, Transform parent, Vector3 localPosition, Quaternion localRotation, Vector3 localScale, Material material)
        {
            GameObject primitive = GameObject.CreatePrimitive(type);
            primitive.name = name;
            primitive.transform.SetParent(parent, false);
            primitive.transform.localPosition = localPosition;
            primitive.transform.localRotation = localRotation;
            primitive.transform.localScale = localScale;

            Renderer renderer = primitive.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = material;
            }

            Collider collider = primitive.GetComponent<Collider>();
            if (collider != null)
            {
                UnityEngine.Object.DestroyImmediate(collider);
            }

            return primitive;
        }

        private static void UpdatePlayerPrefab(HelicopterCandidate playerCandidate)
        {
            GameObject prefabRoot = PrefabUtility.LoadPrefabContents(PlayerPrefabPath);
            UpgradePlayerVisual(prefabRoot, playerCandidate);
            ConfigurePlayerHealth(prefabRoot);
            PrefabUtility.SaveAsPrefabAsset(prefabRoot, PlayerPrefabPath);
            PrefabUtility.UnloadPrefabContents(prefabRoot);
        }

        private static void UpgradePlayerVisual(GameObject playerRoot, HelicopterCandidate playerCandidate)
        {
            Transform visual = playerRoot.transform.Find("HelicopterVisual");

            if (visual == null || playerCandidate.Asset == null)
            {
                Debug.LogWarning("Milestone 4 left player visual unchanged because HelicopterVisual or a valid imported model was not available.");
                return;
            }

            Transform existingImported = visual.Find("M4_ImportedHelicopterModel");

            if (existingImported != null)
            {
                UnityEngine.Object.DestroyImmediate(existingImported.gameObject);
            }

            GameObject imported = PrefabUtility.InstantiatePrefab(playerCandidate.Asset, visual) as GameObject;

            if (imported == null)
            {
                Debug.LogWarning("Milestone 4 could not instantiate imported player helicopter visual. Existing visual preserved.");
                return;
            }

            imported.name = "M4_ImportedHelicopterModel";
            imported.transform.localPosition = Vector3.zero;
            imported.transform.localRotation = Quaternion.identity;
            imported.transform.localScale = Vector3.one;
            NormalizeVisual(imported.transform);

            for (int index = visual.childCount - 1; index >= 0; index--)
            {
                Transform child = visual.GetChild(index);

                if (child != imported.transform)
                {
                    UnityEngine.Object.DestroyImmediate(child.gameObject);
                }
            }

            Transform[] rotors = FindRotorTransforms(imported.transform);

            if (rotors.Length > 0)
            {
                RotorSpinner spinner = playerRoot.GetComponent<RotorSpinner>();
                if (spinner == null)
                {
                    spinner = playerRoot.AddComponent<RotorSpinner>();
                }

                spinner.Configure(rotors, Vector3.up, 1200f);
            }
        }

        private static void ConfigurePlayerHealth(GameObject playerRoot)
        {
            Health health = GetOrAdd<Health>(playerRoot);
            health.Configure(250f, false);
            TeamMember teamMember = GetOrAdd<TeamMember>(playerRoot);
            teamMember.Configure(CombatTeam.Player);

            PlayerDeathHandler deathHandler = GetOrAdd<PlayerDeathHandler>(playerRoot);
            deathHandler.Configure(
                playerRoot.GetComponent<HelicopterInputReader>(),
                playerRoot.GetComponent<HelicopterFlightController>(),
                playerRoot.GetComponent<PlayerCombatInputReader>(),
                playerRoot.GetComponent<PlayerWeaponController>(),
                playerRoot.GetComponent<Rigidbody>());
        }

        private static void UpdateScene(HelicopterCandidate playerCandidate, GameObject alphaPrefab, GameObject bravoPrefab)
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            GameObject player = GameObject.Find("PlayerHelicopter");

            if (player == null)
            {
                throw new InvalidOperationException("PlayerHelicopter was not found in Game.unity.");
            }

            UpgradePlayerVisual(player, playerCandidate);
            ConfigurePlayerHealth(player);

            GameObject testRange = GameObject.Find("CombatTestRange");
            if (testRange != null)
            {
                testRange.SetActive(false);
            }

            GameObject existingRoot = GameObject.Find("EnemyHelicopters");
            if (existingRoot != null)
            {
                UnityEngine.Object.DestroyImmediate(existingRoot);
            }

            Terrain terrain = Terrain.activeTerrain;
            GameObject enemiesRoot = new GameObject("EnemyHelicopters");
            GameObject alphaEnemy = PlaceEnemy(alphaPrefab, enemiesRoot.transform, player.transform, terrain, new Vector2(-82f, 145f), "EnemyHelicopterAlpha", -1f);
            GameObject bravoEnemy = PlaceEnemy(bravoPrefab, enemiesRoot.transform, player.transform, terrain, new Vector2(88f, 175f), "EnemyHelicopterBravo", 1f);
            ConfigureMissionController(player, alphaEnemy, bravoEnemy);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        private static GameObject PlaceEnemy(GameObject prefab, Transform parent, Transform player, Terrain terrain, Vector2 offset, string name, float orbitSign)
        {
            GameObject enemy = PrefabUtility.InstantiatePrefab(prefab, parent) as GameObject;

            if (enemy == null)
            {
                return null;
            }

            Vector3 position = player.position + new Vector3(offset.x, 0f, offset.y);
            float terrainHeight = terrain != null ? terrain.SampleHeight(position) + terrain.transform.position.y : 0f;
            position.y = terrain != null ? Mathf.Max(terrainHeight + 48f, player.position.y + 26f) : (name.Contains("Alpha") ? 48f : 58f);
            enemy.name = name;
            enemy.transform.position = position;

            Vector3 toPlayer = Vector3.ProjectOnPlane(player.position - position, Vector3.up);
            if (toPlayer.sqrMagnitude > 0.01f)
            {
                enemy.transform.rotation = Quaternion.LookRotation(toPlayer.normalized, Vector3.up);
            }

            ConfigureEnemySceneReferences(enemy, player, terrain, orbitSign);
            return enemy;
        }

        private static void ConfigureEnemySceneReferences(GameObject enemy, Transform player, Terrain terrain, float orbitSign)
        {
            EnemyHelicopterTargeting targeting = enemy.GetComponent<EnemyHelicopterTargeting>();
            EnemyHelicopterSeparation separation = enemy.GetComponent<EnemyHelicopterSeparation>();
            EnemyHelicopterMovement movement = enemy.GetComponent<EnemyHelicopterMovement>();
            MissileLauncher missileLauncher = enemy.GetComponent<MissileLauncher>();
            EnemyHelicopterWeaponController weaponController = enemy.GetComponent<EnemyHelicopterWeaponController>();
            EnemyHelicopterVisualController visualController = enemy.GetComponent<EnemyHelicopterVisualController>();
            EnemyHelicopterBrain brain = enemy.GetComponent<EnemyHelicopterBrain>();
            Health health = enemy.GetComponent<Health>();
            Rigidbody rigidbody = enemy.GetComponent<Rigidbody>();
            Rigidbody playerRigidbody = player.GetComponent<Rigidbody>();
            Transform left = enemy.transform.Find("MissileHardpointLeft");
            Transform right = enemy.transform.Find("MissileHardpointRight");
            Transform aimOrigin = enemy.transform.Find("AimOrigin");
            Transform visualPivot = enemy.transform.Find("VisualPivot");
            Transform[] rotors = visualPivot != null ? FindRotorTransforms(visualPivot) : new Transform[0];

            TeamMember teamMember = GetOrAdd<TeamMember>(enemy);
            teamMember.Configure(CombatTeam.Enemy);
            targeting.SetTarget(player);
            targeting.ConfigureRanges(220f, 340f, 340f);
            movement.Configure(rigidbody, terrain, separation);
            movement.ConfigureOrbit(orbitSign);
            movement.ConfigureHomeAndBoundary(enemy.transform.position, 225f, 280f, 18f, 22f, 155f, 24f);
            missileLauncher.Configure(AssetDatabase.LoadAssetAtPath<GameObject>(EnemyMissilePath), new[] { left, right }, enemy, rigidbody);
            missileLauncher.ConfigureAmmoAndCooldown(999, 2.40f);
            weaponController.Configure(targeting, missileLauncher, aimOrigin, playerRigidbody);
            visualController.Configure(movement, visualPivot, rotors);
            brain.Configure(targeting, movement, weaponController, health);
        }

        private static void ConfigureMissionController(GameObject player, GameObject alphaEnemy, GameObject bravoEnemy)
        {
            GameObject systemsRoot = GameObject.Find("GameSystems");

            if (systemsRoot == null)
            {
                systemsRoot = new GameObject("GameSystems");
            }

            CombatMissionController missionController = GetOrAdd<CombatMissionController>(systemsRoot);
            missionController.Configure(
                player != null ? player.GetComponent<PlayerDeathHandler>() : null,
                new[]
                {
                    alphaEnemy != null ? alphaEnemy.GetComponent<EnemyUnit>() : null,
                    bravoEnemy != null ? bravoEnemy.GetComponent<EnemyUnit>() : null
                });
        }

        private static T GetOrAdd<T>(GameObject target) where T : Component
        {
            T component = target.GetComponent<T>();
            return component != null ? component : target.AddComponent<T>();
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
                material.SetColor("_EmissionColor", color * 1.2f);
            }
            else
            {
                material.DisableKeyword("_EMISSION");
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

        private static string CandidatePath(HelicopterCandidate candidate)
        {
            return string.IsNullOrEmpty(candidate.Path) ? "<fallback>" : candidate.Path;
        }

        private struct HelicopterCandidate
        {
            public HelicopterCandidate(string path, GameObject asset, int score)
            {
                Path = path;
                Asset = asset;
                Score = score;
            }

            public string Path { get; }
            public GameObject Asset { get; }
            public int Score { get; }
        }
    }
}
