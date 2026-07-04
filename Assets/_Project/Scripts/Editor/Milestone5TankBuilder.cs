using System;
using System.Collections.Generic;
using System.IO;
using HelicopterCombat.Combat;
using HelicopterCombat.Core;
using HelicopterCombat.Enemies;
using HelicopterCombat.Player;
using HelicopterCombat.Weapons;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace HelicopterCombat.EditorTools
{
    public static class Milestone5TankBuilder
    {
        private const string ProjectRoot = "Assets/_Project";
        private const string ScenePath = ProjectRoot + "/Scenes/Game.unity";
        private const string PlayerPrefabPath = ProjectRoot + "/Prefabs/Player/PlayerHelicopter.prefab";
        private const string MaterialsPath = ProjectRoot + "/Art/Materials";
        private const string EnemyMissilePath = ProjectRoot + "/Prefabs/Projectiles/EnemyMissile.prefab";
        private const string TankMissilePath = ProjectRoot + "/Prefabs/Enemies/TankMissile.prefab";
        private const string ExplosionPrefabPath = ProjectRoot + "/Prefabs/VFX/Explosion.prefab";
        private const string TankAlphaPrefabPath = ProjectRoot + "/Prefabs/Enemies/TankAlpha.prefab";
        private const string TankBravoPrefabPath = ProjectRoot + "/Prefabs/Enemies/TankBravo.prefab";

        [MenuItem("Tools/Helicopter Combat/Rebuild Milestone 5 Enemy Tanks")]
        public static void RebuildMilestone5EnemyTanks()
        {
            EnsureFolders();

            TankCandidate selectedTank = DiscoverTankCandidate();
            Debug.Log("Milestone 5 selected tank source asset: " + CandidatePath(selectedTank));

            Material olive = CreateOrUpdateMaterial("TankOlive", new Color(0.37f, 0.41f, 0.24f), 0.20f, 0.25f);
            Material desert = CreateOrUpdateMaterial("TankDesert", new Color(0.56f, 0.47f, 0.31f), 0.18f, 0.22f);
            Material barrelDark = CreateOrUpdateMaterial("TankBarrelDark", new Color(0.15f, 0.15f, 0.16f), 0.25f, 0.30f);

            GameObject explosionPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ExplosionPrefabPath);
            GameObject tankMissile = CreateTankMissilePrefab(explosionPrefab);
            GameObject tankAlphaPrefab = CreateTankPrefab("TankAlpha", "Tank Alpha", selectedTank, olive, barrelDark, tankMissile, explosionPrefab, TankAlphaPrefabPath);
            GameObject tankBravoPrefab = CreateTankPrefab("TankBravo", "Tank Bravo", selectedTank, desert, barrelDark, tankMissile, explosionPrefab, TankBravoPrefabPath);

            UpdatePlayerPrefab();
            UpdateScene(tankAlphaPrefab, tankBravoPrefab);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Milestone 5 tank rebuild complete. Selected source: " + CandidatePath(selectedTank));
        }

        public static void RebuildFromCommandLine()
        {
            RebuildMilestone5EnemyTanks();
        }

        private static void EnsureFolders()
        {
            string[] folders =
            {
                ProjectRoot + "/Art/Materials",
                ProjectRoot + "/Prefabs/Enemies",
                ProjectRoot + "/Scripts/Enemies",
                ProjectRoot + "/Scripts/Editor"
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

        private static TankCandidate DiscoverTankCandidate()
        {
            Dictionary<string, TankCandidate> byPath = new Dictionary<string, TankCandidate>(StringComparer.OrdinalIgnoreCase);

            AddTankCandidates(byPath, AssetDatabase.FindAssets("t:GameObject"));
            AddTankCandidates(byPath, AssetDatabase.FindAssets("t:Model"));

            List<TankCandidate> candidates = new List<TankCandidate>(byPath.Values);
            candidates.Sort((left, right) => right.Score.CompareTo(left.Score));

            for (int index = 0; index < Mathf.Min(6, candidates.Count); index++)
            {
                Debug.Log("Milestone 5 tank candidate " + (index + 1) + ": " + candidates[index].Path + " score=" + candidates[index].Score);
            }

            return candidates.Count > 0 ? candidates[0] : default;
        }

        private static void AddTankCandidates(Dictionary<string, TankCandidate> byPath, string[] guids)
        {
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);

                if (string.IsNullOrEmpty(path) ||
                    !path.StartsWith("Assets/", StringComparison.Ordinal) ||
                    path.StartsWith("Assets/_Project/", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                string lowerPath = path.ToLowerInvariant();

                if (!lowerPath.Contains("tank") ||
                    lowerPath.Contains("/scenes/") ||
                    lowerPath.EndsWith(".unity", StringComparison.Ordinal) ||
                    lowerPath.EndsWith(".mat", StringComparison.Ordinal) ||
                    lowerPath.EndsWith(".png", StringComparison.Ordinal))
                {
                    continue;
                }

                GameObject asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);

                if (asset == null)
                {
                    continue;
                }

                Renderer[] renderers = asset.GetComponentsInChildren<Renderer>(true);

                if (renderers.Length == 0)
                {
                    continue;
                }

                int score = 1000 + renderers.Length * 20;

                if (lowerPath.EndsWith(".prefab", StringComparison.Ordinal))
                {
                    score += 180;
                }

                if (lowerPath.EndsWith(".fbx", StringComparison.Ordinal))
                {
                    score += 140;
                }

                if (lowerPath.Contains("/prefabs/"))
                {
                    score += 100;
                }

                byPath[path] = new TankCandidate(path, asset, score);
            }
        }

        private static GameObject CreateTankMissilePrefab(GameObject explosionPrefab)
        {
            GameObject sourcePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(EnemyMissilePath);

            if (sourcePrefab == null)
            {
                throw new InvalidOperationException("EnemyMissile prefab was not found. Run Milestone 4 first.");
            }

            GameObject root = PrefabUtility.InstantiatePrefab(sourcePrefab) as GameObject;

            if (root == null)
            {
                throw new InvalidOperationException("Could not instantiate EnemyMissile prefab.");
            }

            root.name = "TankMissile";
            MissileProjectile missile = root.GetComponent<MissileProjectile>();

            if (missile != null)
            {
                missile.Configure(48f, 0f);
                missile.ConfigureExplosion(explosionPrefab, 250f, 5f, 250f, 7f, 0.12f);
            }

            GameObject prefab = SavePrefab(root, TankMissilePath);
            UnityEngine.Object.DestroyImmediate(root);
            return prefab;
        }

        private static GameObject CreateTankPrefab(
            string prefabName,
            string displayName,
            TankCandidate candidate,
            Material bodyMaterial,
            Material barrelMaterial,
            GameObject tankMissile,
            GameObject explosionPrefab,
            string prefabPath)
        {
            GameObject root = new GameObject(prefabName);
            Rigidbody rigidbody = root.AddComponent<Rigidbody>();
            rigidbody.useGravity = false;
            rigidbody.isKinematic = true;
            rigidbody.interpolation = RigidbodyInterpolation.Interpolate;
            rigidbody.collisionDetectionMode = CollisionDetectionMode.Discrete;
            rigidbody.constraints = RigidbodyConstraints.None;

            BoxCollider boxCollider = root.AddComponent<BoxCollider>();
            Health health = root.AddComponent<Health>();
            health.Configure(180f, false);
            DestroyOnDeath destroyOnDeath = root.AddComponent<DestroyOnDeath>();
            destroyOnDeath.Configure(explosionPrefab, 0.05f, true);
            TeamMember teamMember = root.AddComponent<TeamMember>();
            teamMember.Configure(CombatTeam.Enemy);
            EnemyUnit enemyUnit = root.AddComponent<EnemyUnit>();
            enemyUnit.Configure(displayName, EnemyUnit.EnemyUnitType.Tank);
            EnemyTankTargeting targeting = root.AddComponent<EnemyTankTargeting>();
            EnemyTankMovement movement = root.AddComponent<EnemyTankMovement>();
            TankTurretAimer turretAimer = root.AddComponent<TankTurretAimer>();
            TankWeaponController weaponController = root.AddComponent<TankWeaponController>();
            EnemyTankBrain brain = root.AddComponent<EnemyTankBrain>();
            MissileLauncher missileLauncher = root.AddComponent<MissileLauncher>();

            Transform visualRoot = CreateChild(root.transform, "VisualRoot", Vector3.zero);
            Transform colliderRoot = CreateChild(root.transform, "ColliderRoot", Vector3.zero);
            bool importedModelUsed = CreateImportedTankVisual(candidate, visualRoot, bodyMaterial);

            if (!importedModelUsed)
            {
                CreateFallbackTankVisual(visualRoot, bodyMaterial, barrelMaterial);
            }

            Transform turretYawPivot = CreateChild(root.transform, "TurretYawPivot", new Vector3(0f, 1.45f, 0.35f));
            CreatePrimitive(PrimitiveType.Cube, "TurretVisual", turretYawPivot, Vector3.zero, Quaternion.identity, new Vector3(1.9f, 0.65f, 1.9f), bodyMaterial);
            Transform barrelPitchPivot = CreateChild(turretYawPivot, "BarrelPitchPivot", new Vector3(0f, 0.08f, 0.75f));
            CreatePrimitive(PrimitiveType.Cylinder, "BarrelVisual", barrelPitchPivot, new Vector3(0f, 0f, 1.15f), Quaternion.Euler(90f, 0f, 0f), new Vector3(0.13f, 1.2f, 0.13f), barrelMaterial);
            Transform muzzle = CreateChild(barrelPitchPivot, "Muzzle", new Vector3(0f, 0f, 2.35f));

            FitTankCollider(root.transform, boxCollider, colliderRoot);

            targeting.SetAimOrigin(muzzle);
            targeting.ConfigureRanges(180f, 240f, 300f, 25f, 165f, 8f, 65f, 2.5f);
            movement.Configure(rigidbody, boxCollider, null, targeting);
            movement.ConfigureMovement(root.transform.position, 8f, 80f, 9f, 12f, 30f, 0.08f, 90f, 60f, 40f, 260f, 6f);
            turretAimer.Configure(root.transform, turretYawPivot, barrelPitchPivot, muzzle, targeting);
            weaponController.Configure(targeting, turretAimer, missileLauncher, null);
            weaponController.ConfigureTiming(3.5f, 0.45f, true);
            missileLauncher.Configure(tankMissile, new[] { muzzle }, root, rigidbody);
            missileLauncher.ConfigureAmmoAndCooldown(999, 0f);
            brain.Configure(targeting, weaponController, health);

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

        private static bool CreateImportedTankVisual(TankCandidate candidate, Transform visualRoot, Material bodyMaterial)
        {
            if (candidate.Asset == null)
            {
                return false;
            }

            GameObject visual = PrefabUtility.InstantiatePrefab(candidate.Asset, visualRoot) as GameObject;

            if (visual == null)
            {
                return false;
            }

            visual.name = "ImportedTankVisual";
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localRotation = Quaternion.identity;
            visual.transform.localScale = Vector3.one;

            NormalizeVisual(visual.transform, 7.5f);

            foreach (Renderer renderer in visual.GetComponentsInChildren<Renderer>(true))
            {
                renderer.sharedMaterial = bodyMaterial;
            }

            return true;
        }

        private static void CreateFallbackTankVisual(Transform visualRoot, Material bodyMaterial, Material barrelMaterial)
        {
            GameObject visual = new GameObject("FallbackTankVisual");
            visual.transform.SetParent(visualRoot, false);
            CreatePrimitive(PrimitiveType.Cube, "Hull", visual.transform, new Vector3(0f, 0.85f, 0f), Quaternion.identity, new Vector3(4.8f, 1.2f, 6.6f), bodyMaterial);
            CreatePrimitive(PrimitiveType.Cube, "Cabin", visual.transform, new Vector3(0f, 1.55f, 0.1f), Quaternion.identity, new Vector3(2.0f, 0.85f, 2.6f), bodyMaterial);
            CreatePrimitive(PrimitiveType.Cube, "TrackLeft", visual.transform, new Vector3(-2.0f, 0.45f, 0f), Quaternion.identity, new Vector3(0.7f, 0.75f, 6.2f), barrelMaterial);
            CreatePrimitive(PrimitiveType.Cube, "TrackRight", visual.transform, new Vector3(2.0f, 0.45f, 0f), Quaternion.identity, new Vector3(0.7f, 0.75f, 6.2f), barrelMaterial);
        }

        private static void NormalizeVisual(Transform visualRoot, float desiredLength)
        {
            if (!TryGetLocalBounds(visualRoot, out Bounds bounds))
            {
                return;
            }

            float length = Mathf.Max(bounds.size.x, bounds.size.z);

            if (length > 0.001f)
            {
                float scale = desiredLength / length;
                visualRoot.localScale *= scale;
            }

            if (TryGetLocalBounds(visualRoot, out Bounds scaledBounds))
            {
                Vector3 offset = scaledBounds.center;
                offset.y = scaledBounds.min.y;
                visualRoot.localPosition -= offset;
            }
        }

        private static void FitTankCollider(Transform root, BoxCollider boxCollider, Transform colliderRoot)
        {
            if (!TryGetWorldBounds(root, out Bounds bounds))
            {
                boxCollider.center = new Vector3(0f, 1f, 0f);
                boxCollider.size = new Vector3(5f, 2f, 7f);
                return;
            }

            Vector3 localCenter = root.InverseTransformPoint(bounds.center);
            boxCollider.center = localCenter;
            boxCollider.size = bounds.size;
            colliderRoot.localPosition = new Vector3(
                localCenter.x,
                localCenter.y - (boxCollider.size.y * 0.5f),
                localCenter.z);
        }

        private static bool TryGetLocalBounds(Transform root, out Bounds bounds)
        {
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
            bool hasBounds = false;
            bounds = new Bounds(Vector3.zero, Vector3.zero);

            foreach (Renderer renderer in renderers)
            {
                if (renderer == null || !renderer.enabled)
                {
                    continue;
                }

                Bounds rendererBounds = TransformBoundsToLocal(root, renderer.bounds);

                if (!hasBounds)
                {
                    bounds = rendererBounds;
                    hasBounds = true;
                }
                else
                {
                    bounds.Encapsulate(rendererBounds);
                }
            }

            return hasBounds;
        }

        private static Bounds TransformBoundsToLocal(Transform root, Bounds worldBounds)
        {
            Vector3 center = root.InverseTransformPoint(worldBounds.center);
            Vector3 extents = worldBounds.extents;
            return new Bounds(center, extents * 2f);
        }

        private static bool TryGetWorldBounds(Transform root, out Bounds bounds)
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

        private static void UpdatePlayerPrefab()
        {
            GameObject prefabRoot = PrefabUtility.LoadPrefabContents(PlayerPrefabPath);
            EnsureInfinitePlayerMissiles(prefabRoot);
            PrefabUtility.SaveAsPrefabAsset(prefabRoot, PlayerPrefabPath);
            PrefabUtility.UnloadPrefabContents(prefabRoot);
        }

        private static void EnsureInfinitePlayerMissiles(GameObject player)
        {
            MissileLauncher missileLauncher = player != null ? player.GetComponent<MissileLauncher>() : null;

            if (missileLauncher != null)
            {
                missileLauncher.ConfigureAmmoAndCooldown(int.MaxValue, 0.25f);
                EditorUtility.SetDirty(player);
            }
        }

        private static void UpdateScene(GameObject tankAlphaPrefab, GameObject tankBravoPrefab)
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            GameObject player = GameObject.Find("PlayerHelicopter");
            GameObject enemyHelicopters = GameObject.Find("EnemyHelicopters");
            GameObject gameSystems = GameObject.Find("GameSystems");
            CombatMissionController missionController = gameSystems != null ? gameSystems.GetComponent<CombatMissionController>() : null;
            Terrain terrain = Terrain.activeTerrain;

            if (player == null || enemyHelicopters == null || gameSystems == null || missionController == null || terrain == null)
            {
                throw new InvalidOperationException("Milestone 5 preflight failed. Required scene objects were not found.");
            }

            EnsureInfinitePlayerMissiles(player);

            GameObject existingTankRoot = GameObject.Find("EnemyTanks");

            if (existingTankRoot != null)
            {
                UnityEngine.Object.DestroyImmediate(existingTankRoot);
            }

            GameObject tanksRoot = new GameObject("EnemyTanks");
            GameObject tankAlpha = PlaceTank(tankAlphaPrefab, tanksRoot.transform, player.transform, terrain, new Vector2(-95f, 95f), "TankAlpha");
            GameObject tankBravo = PlaceTank(tankBravoPrefab, tanksRoot.transform, player.transform, terrain, new Vector2(110f, 120f), "TankBravo");

            EnemyUnit[] helicopterUnits = enemyHelicopters.GetComponentsInChildren<EnemyUnit>(true);
            EnemyUnit[] tankUnits =
            {
                tankAlpha != null ? tankAlpha.GetComponent<EnemyUnit>() : null,
                tankBravo != null ? tankBravo.GetComponent<EnemyUnit>() : null
            };

            missionController.Configure(
                player.GetComponent<PlayerDeathHandler>(),
                helicopterUnits,
                tankUnits);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        private static GameObject PlaceTank(GameObject prefab, Transform parent, Transform player, Terrain terrain, Vector2 offset, string name)
        {
            GameObject tank = PrefabUtility.InstantiatePrefab(prefab, parent) as GameObject;

            if (tank == null)
            {
                return null;
            }

            tank.name = name;
            BoxCollider boxCollider = tank.GetComponent<BoxCollider>();
            Vector3 position = player.position + new Vector3(offset.x, 0f, offset.y);
            position = ClampToTerrainBounds(position, terrain, boxCollider != null ? Mathf.Max(boxCollider.size.x, boxCollider.size.z) * 0.5f : 4f);

            if (Vector3.Distance(new Vector3(position.x, 0f, position.z), new Vector3(player.position.x, 0f, player.position.z)) < 28f)
            {
                position += new Vector3(18f, 0f, 18f);
                position = ClampToTerrainBounds(position, terrain, 4f);
            }

            foreach (Transform sibling in parent)
            {
                if (sibling == tank.transform)
                {
                    continue;
                }

                Vector3 siblingFlat = new Vector3(sibling.position.x, 0f, sibling.position.z);
                Vector3 positionFlat = new Vector3(position.x, 0f, position.z);

                if (Vector3.Distance(positionFlat, siblingFlat) < 18f)
                {
                    position += new Vector3(14f, 0f, -12f);
                    position = ClampToTerrainBounds(position, terrain, 4f);
                    break;
                }
            }

            float groundHeight = terrain.SampleHeight(position) + terrain.transform.position.y;
            float centerY = boxCollider != null ? boxCollider.center.y : 1f;
            float halfHeight = boxCollider != null ? boxCollider.size.y * 0.5f : 1f;
            position.y = groundHeight + halfHeight - centerY;
            tank.transform.position = position;

            Vector3 flatToPlayer = Vector3.ProjectOnPlane(player.position - position, Vector3.up);

            if (flatToPlayer.sqrMagnitude > 0.01f)
            {
                tank.transform.rotation = Quaternion.LookRotation(flatToPlayer.normalized, Vector3.up);
            }

            ConfigureTankSceneReferences(tank, player.gameObject);
            return tank;
        }

        private static void ConfigureTankSceneReferences(GameObject tank, GameObject player)
        {
            EnemyTankTargeting targeting = tank.GetComponent<EnemyTankTargeting>();
            EnemyTankMovement movement = tank.GetComponent<EnemyTankMovement>();
            TankTurretAimer turretAimer = tank.GetComponent<TankTurretAimer>();
            TankWeaponController weaponController = tank.GetComponent<TankWeaponController>();
            EnemyTankBrain brain = tank.GetComponent<EnemyTankBrain>();
            Health health = tank.GetComponent<Health>();
            MissileLauncher missileLauncher = tank.GetComponent<MissileLauncher>();
            Rigidbody rigidbody = tank.GetComponent<Rigidbody>();
            BoxCollider boxCollider = tank.GetComponent<BoxCollider>();
            Transform muzzle = tank.transform.Find("TurretYawPivot/BarrelPitchPivot/Muzzle");
            PlayerDeathHandler playerDeathHandler = player.GetComponent<PlayerDeathHandler>();

            targeting.SetTarget(player.transform);
            targeting.SetAimOrigin(muzzle);
            targeting.ConfigureRanges(180f, 240f, 300f, 25f, 165f, 8f, 65f, 2.5f);
            movement.Configure(rigidbody, boxCollider, Terrain.activeTerrain, targeting);
            movement.ConfigureMovement(tank.transform.position, 8f, 80f, 9f, 12f, 30f, 0.08f, 90f, 60f, 40f, 260f, 6f);
            turretAimer.Configure(tank.transform, tank.transform.Find("TurretYawPivot"), tank.transform.Find("TurretYawPivot/BarrelPitchPivot"), muzzle, targeting);
            missileLauncher.Configure(AssetDatabase.LoadAssetAtPath<GameObject>(TankMissilePath), new[] { muzzle }, tank, rigidbody);
            missileLauncher.ConfigureAmmoAndCooldown(999, 0f);
            weaponController.Configure(targeting, turretAimer, missileLauncher, playerDeathHandler);
            weaponController.ConfigureTiming(3.5f, 0.45f, true);
            brain.Configure(targeting, weaponController, health);
        }

        private static Vector3 ClampToTerrainBounds(Vector3 position, Terrain terrain, float margin)
        {
            Vector3 terrainPosition = terrain.transform.position;
            Vector3 terrainSize = terrain.terrainData.size;
            position.x = Mathf.Clamp(position.x, terrainPosition.x + margin, terrainPosition.x + terrainSize.x - margin);
            position.z = Mathf.Clamp(position.z, terrainPosition.z + margin, terrainPosition.z + terrainSize.z - margin);
            return position;
        }

        private static Material CreateOrUpdateMaterial(string materialName, Color color, float metallic, float smoothness)
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
            material.DisableKeyword("_EMISSION");
            EditorUtility.SetDirty(material);
            return material;
        }

        private static GameObject SavePrefab(GameObject root, string assetPath)
        {
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, assetPath);
            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
            return prefab;
        }

        private static string CandidatePath(TankCandidate candidate)
        {
            return string.IsNullOrEmpty(candidate.Path) ? "<fallback>" : candidate.Path;
        }

        private readonly struct TankCandidate
        {
            public TankCandidate(string path, GameObject asset, int score)
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
