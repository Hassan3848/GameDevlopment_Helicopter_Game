using System;
using HelicopterCombat.Audio;
using HelicopterCombat.CameraSystem;
using HelicopterCombat.Combat;
using HelicopterCombat.Core;
using HelicopterCombat.Enemies;
using HelicopterCombat.Player;
using HelicopterCombat.VFX;
using HelicopterCombat.Weapons;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace HelicopterCombat.EditorTools
{
    public static class Milestone7PolishBuilder
    {
        private const string ProjectRoot = "Assets/_Project";
        private const string ScenePath = ProjectRoot + "/Scenes/Game.unity";
        private const string PlayerPrefabPath = ProjectRoot + "/Prefabs/Player/PlayerHelicopter.prefab";
        private const string EnemyHelicopterAlphaPath = ProjectRoot + "/Prefabs/Enemies/EnemyHelicopterAlpha.prefab";
        private const string EnemyHelicopterBravoPath = ProjectRoot + "/Prefabs/Enemies/EnemyHelicopterBravo.prefab";
        private const string TankAlphaPath = ProjectRoot + "/Prefabs/Enemies/TankAlpha.prefab";
        private const string TankBravoPath = ProjectRoot + "/Prefabs/Enemies/TankBravo.prefab";
        private const string PlayerMissilePath = ProjectRoot + "/Prefabs/Projectiles/PlayerMissile.prefab";
        private const string PlayerBombPath = ProjectRoot + "/Prefabs/Projectiles/PlayerBomb.prefab";
        private const string EnemyMissilePath = ProjectRoot + "/Prefabs/Projectiles/EnemyMissile.prefab";
        private const string TankMissilePath = ProjectRoot + "/Prefabs/Enemies/TankMissile.prefab";
        private const string ExplosionPrefabPath = ProjectRoot + "/Prefabs/VFX/Explosion.prefab";
        private const string GameUiPrefabPath = ProjectRoot + "/Prefabs/UI/GameUI.prefab";
        private const string MaterialsPath = ProjectRoot + "/Art/Materials";
        private const string VfxPrefabFolder = ProjectRoot + "/Prefabs/VFX";
        private const string SettingsPath = ProjectRoot + "/Settings/M7_GameAudioSettings.asset";

        [MenuItem("Tools/Helicopter Combat/Rebuild Milestone 7 Audio, VFX and Polish")]
        public static void RebuildMilestone7AudioVfxAndPolish()
        {
            EnsureFolders();

            AudioClip windClip = LoadClip(ProjectRoot + "/Audio/Clips/Ambient/WindAmbienceLoop.wav");
            AudioClip explosionSmallClip = LoadClip(ProjectRoot + "/Audio/Clips/Combat/ExplosionSmall.wav");
            AudioClip explosionLargeClip = LoadClip(ProjectRoot + "/Audio/Clips/Combat/ExplosionLarge.wav");
            AudioClip victoryClip = LoadClip(ProjectRoot + "/Audio/Clips/Mission/VictoryStinger.wav");
            AudioClip defeatClip = LoadClip(ProjectRoot + "/Audio/Clips/Mission/DefeatStinger.wav");
            AudioClip hoverClip = LoadClip(ProjectRoot + "/Audio/Clips/UI/ButtonHover.wav");
            AudioClip clickClip = LoadClip(ProjectRoot + "/Audio/Clips/UI/ButtonClick.wav");
            AudioClip confirmClip = LoadClip(ProjectRoot + "/Audio/Clips/UI/ButtonConfirm.wav");
            AudioClip helicopterEngineClip = LoadClip(ProjectRoot + "/Audio/Clips/Vehicles/HelicopterEngineLoop.wav");
            AudioClip tankEngineClip = LoadClip(ProjectRoot + "/Audio/Clips/Vehicles/TankEngineLoop.wav");
            AudioClip missileLaunchClip = LoadClip(ProjectRoot + "/Audio/Clips/Weapons/MissileLaunch.wav");
            AudioClip bombDropClip = LoadClip(ProjectRoot + "/Audio/Clips/Weapons/BombDrop.wav");

            GameAudioSettings settings = CreateOrUpdateAudioSettings();
            Material flashMaterial = CreateParticleMaterial("M7_Flash", new Color(1f, 0.65f, 0.22f, 0.85f), true);
            Material smokeMaterial = CreateParticleMaterial("M7_Smoke", new Color(0.20f, 0.20f, 0.20f, 0.45f), false);
            Material fireMaterial = CreateParticleMaterial("M7_Fire", new Color(1f, 0.35f, 0.10f, 0.72f), true);
            Material dustMaterial = CreateParticleMaterial("M7_Dust", new Color(0.56f, 0.50f, 0.36f, 0.42f), false);

            GameObject muzzleFlashPrefab = CreateMuzzleFlashPrefab(flashMaterial);
            GameObject bombReleasePrefab = CreateBombReleasePuffPrefab(dustMaterial);
            GameObject bombImpactDustPrefab = CreateBombImpactDustPrefab(dustMaterial);
            GameObject deathEffectPrefab = CreateDeathEffectPrefab(fireMaterial, smokeMaterial);

            UpdateExplosionPrefab(explosionSmallClip, fireMaterial, smokeMaterial, dustMaterial);
            UpdateProjectilePrefabs(smokeMaterial, dustMaterial, bombImpactDustPrefab);
            UpdatePlayerPrefab(settings, helicopterEngineClip, missileLaunchClip, bombDropClip, muzzleFlashPrefab, bombReleasePrefab);
            UpdateEnemyHelicopterPrefab(EnemyHelicopterAlphaPath, settings, helicopterEngineClip, missileLaunchClip, muzzleFlashPrefab, explosionLargeClip, deathEffectPrefab, -0.03f);
            UpdateEnemyHelicopterPrefab(EnemyHelicopterBravoPath, settings, helicopterEngineClip, missileLaunchClip, muzzleFlashPrefab, explosionLargeClip, deathEffectPrefab, 0.04f);
            UpdateTankPrefab(TankAlphaPath, settings, tankEngineClip, missileLaunchClip, muzzleFlashPrefab, explosionLargeClip, deathEffectPrefab, dustMaterial);
            UpdateTankPrefab(TankBravoPath, settings, tankEngineClip, missileLaunchClip, muzzleFlashPrefab, explosionLargeClip, deathEffectPrefab, dustMaterial);
            UpdateGameUiPrefab(hoverClip, clickClip, confirmClip);
            UpdateScene(settings, windClip, victoryClip, defeatClip, helicopterEngineClip, tankEngineClip, missileLaunchClip, bombDropClip, explosionLargeClip, deathEffectPrefab, muzzleFlashPrefab, bombReleasePrefab);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Milestone 7 rebuild complete: audio, VFX, camera shake, UI button audio, and polish references updated.");
        }

        public static void RebuildFromCommandLine()
        {
            RebuildMilestone7AudioVfxAndPolish();
        }

        private static void EnsureFolders()
        {
            string[] folders =
            {
                ProjectRoot + "/Scripts/Audio",
                ProjectRoot + "/Scripts/VFX",
                ProjectRoot + "/Scripts/Camera",
                ProjectRoot + "/Scripts/Editor",
                ProjectRoot + "/Prefabs/VFX",
                ProjectRoot + "/Settings",
                ProjectRoot + "/Art/Materials"
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

        private static AudioClip LoadClip(string path)
        {
            AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
            if (clip == null)
            {
                throw new InvalidOperationException("Required audio clip is missing: " + path);
            }

            return clip;
        }

        private static GameAudioSettings CreateOrUpdateAudioSettings()
        {
            GameAudioSettings settings = AssetDatabase.LoadAssetAtPath<GameAudioSettings>(SettingsPath);

            if (settings == null)
            {
                settings = ScriptableObject.CreateInstance<GameAudioSettings>();
                AssetDatabase.CreateAsset(settings, SettingsPath);
            }

            SerializedObject serialized = new SerializedObject(settings);
            serialized.FindProperty("masterVolume").floatValue = 1f;
            serialized.FindProperty("ambientVolume").floatValue = 1f;
            serialized.FindProperty("combatVolume").floatValue = 0.95f;
            serialized.FindProperty("missionVolume").floatValue = 1f;
            serialized.FindProperty("uiVolume").floatValue = 0.95f;
            serialized.FindProperty("vehiclesVolume").floatValue = 1f;
            serialized.FindProperty("weaponsVolume").floatValue = 1f;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(settings);
            return settings;
        }

        private static Material CreateParticleMaterial(string name, Color color, bool additive)
        {
            string path = MaterialsPath + "/" + name + ".mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);

            if (material == null)
            {
                Shader shader = Shader.Find(additive ? "Legacy Shaders/Particles/Additive" : "Legacy Shaders/Particles/Alpha Blended");
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, path);
            }

            material.name = name;
            material.color = color;
            EditorUtility.SetDirty(material);
            return material;
        }

        private static GameObject CreateMuzzleFlashPrefab(Material flashMaterial)
        {
            GameObject root = new GameObject("M7_MissileMuzzleFlash");
            TimedSelfDestruct cleanup = root.AddComponent<TimedSelfDestruct>();
            cleanup.Configure(0.45f);

            GameObject flash = FindOrCreateChild(root.transform, "Flash");
            ConfigureBurstParticles(
                GetOrAdd<ParticleSystem>(flash),
                flashMaterial,
                12,
                0.12f,
                0.35f,
                1.8f,
                new Color(1f, 0.78f, 0.42f, 1f),
                ParticleSystemShapeType.Cone,
                0.08f);

            return SaveTemporaryPrefab(root, VfxPrefabFolder + "/M7_MissileMuzzleFlash.prefab");
        }

        private static GameObject CreateBombReleasePuffPrefab(Material dustMaterial)
        {
            GameObject root = new GameObject("M7_BombReleasePuff");
            TimedSelfDestruct cleanup = root.AddComponent<TimedSelfDestruct>();
            cleanup.Configure(0.85f);

            GameObject puff = FindOrCreateChild(root.transform, "Puff");
            ConfigureBurstParticles(
                GetOrAdd<ParticleSystem>(puff),
                dustMaterial,
                16,
                0.35f,
                0.22f,
                1.1f,
                new Color(0.62f, 0.58f, 0.46f, 0.45f),
                ParticleSystemShapeType.Sphere,
                0.16f);

            return SaveTemporaryPrefab(root, VfxPrefabFolder + "/M7_BombReleasePuff.prefab");
        }

        private static GameObject CreateBombImpactDustPrefab(Material dustMaterial)
        {
            GameObject root = new GameObject("M7_BombImpactDust");
            TimedSelfDestruct cleanup = root.AddComponent<TimedSelfDestruct>();
            cleanup.Configure(1.2f);

            GameObject dust = FindOrCreateChild(root.transform, "Dust");
            ConfigureBurstParticles(
                GetOrAdd<ParticleSystem>(dust),
                dustMaterial,
                22,
                0.55f,
                0.45f,
                2.4f,
                new Color(0.58f, 0.54f, 0.40f, 0.44f),
                ParticleSystemShapeType.Hemisphere,
                0.35f);

            return SaveTemporaryPrefab(root, VfxPrefabFolder + "/M7_BombImpactDust.prefab");
        }

        private static GameObject CreateDeathEffectPrefab(Material fireMaterial, Material smokeMaterial)
        {
            GameObject root = new GameObject("M7_DeathFireSmoke");
            TimedSelfDestruct cleanup = root.AddComponent<TimedSelfDestruct>();
            cleanup.Configure(3.6f);

            GameObject fire = FindOrCreateChild(root.transform, "Fire");
            ConfigureBurstParticles(
                GetOrAdd<ParticleSystem>(fire),
                fireMaterial,
                28,
                0.85f,
                0.45f,
                2.8f,
                new Color(1f, 0.42f, 0.12f, 0.88f),
                ParticleSystemShapeType.Sphere,
                0.35f);

            GameObject smoke = FindOrCreateChild(root.transform, "Smoke");
            ConfigureBurstParticles(
                GetOrAdd<ParticleSystem>(smoke),
                smokeMaterial,
                24,
                2.4f,
                0.75f,
                4.8f,
                new Color(0.22f, 0.22f, 0.22f, 0.44f),
                ParticleSystemShapeType.Sphere,
                0.40f);

            return SaveTemporaryPrefab(root, VfxPrefabFolder + "/M7_DeathFireSmoke.prefab");
        }

        private static void UpdateExplosionPrefab(AudioClip smallExplosionClip, Material fireMaterial, Material smokeMaterial, Material dustMaterial)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(ExplosionPrefabPath);
            ExplosionAudioController audioController = GetOrAdd<ExplosionAudioController>(root);
            audioController.Configure(smallExplosionClip, 0.90f);

            GameObject flash = FindOrCreateChild(root.transform, "M7_Flash");
            ConfigureBurstParticles(
                GetOrAdd<ParticleSystem>(flash),
                fireMaterial,
                16,
                0.15f,
                0.60f,
                2.0f,
                new Color(1f, 0.62f, 0.22f, 0.95f),
                ParticleSystemShapeType.Sphere,
                0.25f);

            GameObject debris = FindOrCreateChild(root.transform, "M7_Debris");
            ConfigureBurstParticles(
                GetOrAdd<ParticleSystem>(debris),
                dustMaterial,
                14,
                0.55f,
                0.10f,
                0.35f,
                new Color(0.55f, 0.48f, 0.34f, 0.75f),
                ParticleSystemShapeType.Cone,
                0.20f);

            GameObject smoke = FindOrCreateChild(root.transform, "M7_SmokePlume");
            ConfigureBurstParticles(
                GetOrAdd<ParticleSystem>(smoke),
                smokeMaterial,
                18,
                1.9f,
                0.80f,
                4.5f,
                new Color(0.18f, 0.18f, 0.18f, 0.38f),
                ParticleSystemShapeType.Sphere,
                0.40f);

            PrefabUtility.SaveAsPrefabAsset(root, ExplosionPrefabPath);
            PrefabUtility.UnloadPrefabContents(root);
        }

        private static void UpdateProjectilePrefabs(Material smokeMaterial, Material dustMaterial, GameObject bombImpactDustPrefab)
        {
            UpdateMissilePrefab(PlayerMissilePath, smokeMaterial, 0.26f, 0.62f);
            UpdateMissilePrefab(EnemyMissilePath, smokeMaterial, 0.24f, 0.58f);
            UpdateMissilePrefab(TankMissilePath, smokeMaterial, 0.24f, 0.58f);
            UpdateBombPrefab(dustMaterial, bombImpactDustPrefab);
        }

        private static void UpdateMissilePrefab(string path, Material smokeMaterial, float trailWidth, float trailTime)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(path);

            TrailRenderer trail = GetOrAdd<TrailRenderer>(root);
            trail.time = trailTime;
            trail.startWidth = trailWidth;
            trail.endWidth = 0.03f;
            trail.shadowCastingMode = ShadowCastingMode.Off;
            trail.receiveShadows = false;
            trail.sharedMaterial = smokeMaterial;

            GameObject exhaust = FindOrCreateChild(root.transform, "M7_ExhaustTrail");
            exhaust.transform.localPosition = new Vector3(0f, 0f, -0.48f);
            ConfigureLoopParticles(
                GetOrAdd<ParticleSystem>(exhaust),
                smokeMaterial,
                10f,
                0.40f,
                0.08f,
                0.20f,
                new Color(0.42f, 0.42f, 0.42f, 0.40f),
                ParticleSystemShapeType.Cone,
                0.06f);

            PrefabUtility.SaveAsPrefabAsset(root, path);
            PrefabUtility.UnloadPrefabContents(root);
        }

        private static void UpdateBombPrefab(Material dustMaterial, GameObject bombImpactDustPrefab)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(PlayerBombPath);
            BombProjectile bombProjectile = root.GetComponent<BombProjectile>();
            if (bombProjectile != null)
            {
                bombProjectile.ConfigureImpactDustPrefab(bombImpactDustPrefab);
            }

            GameObject trail = FindOrCreateChild(root.transform, "M7_FallingTrail");
            trail.transform.localPosition = new Vector3(0f, 0.34f, -0.06f);
            ConfigureLoopParticles(
                GetOrAdd<ParticleSystem>(trail),
                dustMaterial,
                8f,
                0.55f,
                0.10f,
                0.26f,
                new Color(0.52f, 0.50f, 0.44f, 0.34f),
                ParticleSystemShapeType.Cone,
                0.10f);

            PrefabUtility.SaveAsPrefabAsset(root, PlayerBombPath);
            PrefabUtility.UnloadPrefabContents(root);
        }

        private static void UpdatePlayerPrefab(GameAudioSettings settings, AudioClip helicopterEngineClip, AudioClip missileLaunchClip, AudioClip bombDropClip, GameObject muzzleFlashPrefab, GameObject bombReleasePrefab)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(PlayerPrefabPath);
            ConfigurePlayerRoot(root, settings, helicopterEngineClip, missileLaunchClip, bombDropClip, null, muzzleFlashPrefab, bombReleasePrefab);
            PrefabUtility.SaveAsPrefabAsset(root, PlayerPrefabPath);
            PrefabUtility.UnloadPrefabContents(root);
        }

        private static void UpdateEnemyHelicopterPrefab(string path, GameAudioSettings settings, AudioClip engineClip, AudioClip missileLaunchClip, GameObject muzzleFlashPrefab, AudioClip largeExplosionClip, GameObject deathEffectPrefab, float pitchOffset)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(path);
            Health health = root.GetComponent<Health>();
            MissileLauncher missileLauncher = root.GetComponent<MissileLauncher>();

            HelicopterEngineAudio engineAudio = GetOrAdd<HelicopterEngineAudio>(root);
            engineAudio.Configure(settings, engineClip, root.GetComponent<Rigidbody>(), health, null, false, pitchOffset);

            WeaponAudioController weaponAudio = GetOrAdd<WeaponAudioController>(root);
            weaponAudio.Configure(missileLauncher, null, missileLaunchClip, null);

            WeaponVfxController weaponVfx = GetOrAdd<WeaponVfxController>(root);
            weaponVfx.Configure(missileLauncher, null, muzzleFlashPrefab, null);

            UnitDeathEffectsController deathEffects = GetOrAdd<UnitDeathEffectsController>(root);
            deathEffects.Configure(health, deathEffectPrefab, largeExplosionClip);

            PrefabUtility.SaveAsPrefabAsset(root, path);
            PrefabUtility.UnloadPrefabContents(root);
        }

        private static void UpdateTankPrefab(string path, GameAudioSettings settings, AudioClip tankEngineClip, AudioClip missileLaunchClip, GameObject muzzleFlashPrefab, AudioClip largeExplosionClip, GameObject deathEffectPrefab, Material dustMaterial)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(path);
            Health health = root.GetComponent<Health>();
            MissileLauncher missileLauncher = root.GetComponent<MissileLauncher>();
            EnemyTankMovement movement = root.GetComponent<EnemyTankMovement>();

            TankEngineAudio tankAudio = GetOrAdd<TankEngineAudio>(root);
            tankAudio.Configure(settings, tankEngineClip, movement, health);

            WeaponAudioController weaponAudio = GetOrAdd<WeaponAudioController>(root);
            weaponAudio.Configure(missileLauncher, null, missileLaunchClip, null);

            WeaponVfxController weaponVfx = GetOrAdd<WeaponVfxController>(root);
            weaponVfx.Configure(missileLauncher, null, muzzleFlashPrefab, null);

            UnitDeathEffectsController deathEffects = GetOrAdd<UnitDeathEffectsController>(root);
            deathEffects.Configure(health, deathEffectPrefab, largeExplosionClip);

            ParticleSystem leftDust = EnsureTankDust(root.transform, "M7_DustLeft", new Vector3(-0.95f, 0.15f, -1.4f), dustMaterial);
            ParticleSystem rightDust = EnsureTankDust(root.transform, "M7_DustRight", new Vector3(0.95f, 0.15f, -1.4f), dustMaterial);
            TankDustVfxController dustController = GetOrAdd<TankDustVfxController>(root);
            dustController.Configure(movement, health, leftDust, rightDust);

            PrefabUtility.SaveAsPrefabAsset(root, path);
            PrefabUtility.UnloadPrefabContents(root);
        }

        private static void UpdateGameUiPrefab(AudioClip hoverClip, AudioClip clickClip, AudioClip confirmClip)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(GameUiPrefabPath);
            ConfigureUiButtons(root.transform, hoverClip, clickClip, confirmClip);
            PrefabUtility.SaveAsPrefabAsset(root, GameUiPrefabPath);
            PrefabUtility.UnloadPrefabContents(root);
        }

        private static void UpdateScene(
            GameAudioSettings settings,
            AudioClip windClip,
            AudioClip victoryClip,
            AudioClip defeatClip,
            AudioClip helicopterEngineClip,
            AudioClip tankEngineClip,
            AudioClip missileLaunchClip,
            AudioClip bombDropClip,
            AudioClip largeExplosionClip,
            GameObject deathEffectPrefab,
            GameObject muzzleFlashPrefab,
            GameObject bombReleasePrefab)
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            GameObject player = GameObject.Find("PlayerHelicopter");
            GameObject systems = GameObject.Find("GameSystems");
            GameObject cameraObject = GameObject.Find("Main Camera");
            GameObject gameUi = GameObject.Find("GameUI");
            GameFlowController gameFlow = gameUi != null ? gameUi.GetComponent<GameFlowController>() : null;
            CombatMissionController missionController = systems != null ? systems.GetComponent<CombatMissionController>() : null;

            if (player == null || systems == null || cameraObject == null || gameUi == null || gameFlow == null || missionController == null)
            {
                throw new InvalidOperationException("Milestone 7 requires PlayerHelicopter, GameSystems, Main Camera, GameUI, GameFlowController, and CombatMissionController in Game.unity.");
            }

            GameObject testRange = GameObject.Find("CombatTestRange");
            if (testRange != null)
            {
                testRange.SetActive(false);
            }

            AudioOneShotService service = GetOrAdd<AudioOneShotService>(systems);
            service.Configure(settings);

            AmbientAudioController ambient = GetOrAdd<AmbientAudioController>(systems);
            ambient.Configure(settings, windClip, gameFlow);

            MissionAudioController missionAudio = GetOrAdd<MissionAudioController>(systems);
            missionAudio.Configure(missionController, victoryClip, defeatClip);

            Health playerHealth = player.GetComponent<Health>();
            CameraShakeController cameraShake = GetOrAdd<CameraShakeController>(cameraObject);
            cameraShake.Configure(gameFlow, playerHealth);

            ConfigurePlayerRoot(player, settings, helicopterEngineClip, missileLaunchClip, bombDropClip, gameFlow, muzzleFlashPrefab, bombReleasePrefab);
            ConfigureUiButtons(gameUi.transform, LoadClip(ProjectRoot + "/Audio/Clips/UI/ButtonHover.wav"), LoadClip(ProjectRoot + "/Audio/Clips/UI/ButtonClick.wav"), LoadClip(ProjectRoot + "/Audio/Clips/UI/ButtonConfirm.wav"));

            ConfigureSceneEnemies(settings, tankEngineClip, helicopterEngineClip, missileLaunchClip, largeExplosionClip, deathEffectPrefab, muzzleFlashPrefab);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        private static void ConfigureSceneEnemies(GameAudioSettings settings, AudioClip tankEngineClip, AudioClip helicopterEngineClip, AudioClip missileLaunchClip, AudioClip largeExplosionClip, GameObject deathEffectPrefab, GameObject muzzleFlashPrefab)
        {
            EnemyUnit[] enemyUnits = UnityEngine.Object.FindObjectsByType<EnemyUnit>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);

            foreach (EnemyUnit unit in enemyUnits)
            {
                if (unit == null || unit.name.Contains("Test"))
                {
                    continue;
                }

                Health health = unit.GetComponent<Health>();
                MissileLauncher missileLauncher = unit.GetComponent<MissileLauncher>();

                if (unit.UnitType == EnemyUnit.EnemyUnitType.Helicopter)
                {
                    float pitchOffset = unit.name.IndexOf("Bravo", StringComparison.OrdinalIgnoreCase) >= 0 ? 0.04f : -0.03f;
                    HelicopterEngineAudio engineAudio = GetOrAdd<HelicopterEngineAudio>(unit.gameObject);
                    engineAudio.Configure(settings, helicopterEngineClip, unit.GetComponent<Rigidbody>(), health, null, false, pitchOffset);

                    WeaponAudioController audioController = GetOrAdd<WeaponAudioController>(unit.gameObject);
                    audioController.Configure(missileLauncher, null, missileLaunchClip, null);

                    WeaponVfxController vfxController = GetOrAdd<WeaponVfxController>(unit.gameObject);
                    vfxController.Configure(missileLauncher, null, muzzleFlashPrefab, null);
                }
                else if (unit.UnitType == EnemyUnit.EnemyUnitType.Tank)
                {
                    EnemyTankMovement movement = unit.GetComponent<EnemyTankMovement>();
                    TankEngineAudio tankAudio = GetOrAdd<TankEngineAudio>(unit.gameObject);
                    tankAudio.Configure(settings, tankEngineClip, movement, health);

                    WeaponAudioController audioController = GetOrAdd<WeaponAudioController>(unit.gameObject);
                    audioController.Configure(missileLauncher, null, missileLaunchClip, null);

                    WeaponVfxController vfxController = GetOrAdd<WeaponVfxController>(unit.gameObject);
                    vfxController.Configure(missileLauncher, null, muzzleFlashPrefab, null);
                }

                UnitDeathEffectsController deathEffects = GetOrAdd<UnitDeathEffectsController>(unit.gameObject);
                deathEffects.Configure(health, deathEffectPrefab, largeExplosionClip);
            }
        }

        private static void ConfigurePlayerRoot(
            GameObject root,
            GameAudioSettings settings,
            AudioClip helicopterEngineClip,
            AudioClip missileLaunchClip,
            AudioClip bombDropClip,
            GameFlowController gameFlow,
            GameObject muzzleFlashPrefab,
            GameObject bombReleasePrefab)
        {
            Health health = root.GetComponent<Health>();
            Rigidbody rigidbody = root.GetComponent<Rigidbody>();
            MissileLauncher missileLauncher = root.GetComponent<MissileLauncher>();
            BombLauncher bombLauncher = root.GetComponent<BombLauncher>();

            HelicopterEngineAudio engineAudio = GetOrAdd<HelicopterEngineAudio>(root);
            engineAudio.Configure(settings, helicopterEngineClip, rigidbody, health, gameFlow, true, 0f);

            WeaponAudioController audioController = GetOrAdd<WeaponAudioController>(root);
            audioController.Configure(missileLauncher, bombLauncher, missileLaunchClip, bombDropClip);

            WeaponVfxController vfxController = GetOrAdd<WeaponVfxController>(root);
            vfxController.Configure(missileLauncher, bombLauncher, muzzleFlashPrefab, bombReleasePrefab);
        }

        private static void ConfigureUiButtons(Transform root, AudioClip hoverClip, AudioClip clickClip, AudioClip confirmClip)
        {
            ConfigureButtonAudio(root, "StartButton", hoverClip, clickClip, confirmClip, true);
            ConfigureButtonAudio(root, "ResumeButton", hoverClip, clickClip, confirmClip, true);
            ConfigureButtonAudio(root, "RetryButton", hoverClip, clickClip, confirmClip, true);
            ConfigureButtonAudio(root, "ReplayButton", hoverClip, clickClip, confirmClip, true);
            ConfigureButtonAudio(root, "PauseButton", hoverClip, clickClip, confirmClip, false);
            ConfigureButtonAudio(root, "QuitButton", hoverClip, clickClip, confirmClip, false);
            ConfigureButtonAudio(root, "PauseQuitButton", hoverClip, clickClip, confirmClip, false);

            foreach (Transform candidate in root.GetComponentsInChildren<Transform>(true))
            {
                if (candidate.name == "MainMenuButton" || candidate.name == "PauseMainMenuButton")
                {
                    UIButtonAudio buttonAudio = GetOrAdd<UIButtonAudio>(candidate.gameObject);
                    buttonAudio.Configure(hoverClip, clickClip, confirmClip, false);
                }
            }
        }

        private static void ConfigureButtonAudio(Transform root, string name, AudioClip hoverClip, AudioClip clickClip, AudioClip confirmClip, bool confirmOnClick)
        {
            Transform transform = FindDeepChild(root, name);
            if (transform == null || transform.GetComponent<Button>() == null)
            {
                return;
            }

            UIButtonAudio buttonAudio = GetOrAdd<UIButtonAudio>(transform.gameObject);
            buttonAudio.Configure(hoverClip, clickClip, confirmClip, confirmOnClick);
        }

        private static Transform FindDeepChild(Transform root, string name)
        {
            foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
            {
                if (child.name == name)
                {
                    return child;
                }
            }

            return null;
        }

        private static ParticleSystem EnsureTankDust(Transform root, string name, Vector3 localPosition, Material dustMaterial)
        {
            GameObject child = FindOrCreateChild(root, name);
            child.transform.localPosition = localPosition;
            ConfigureLoopParticles(
                GetOrAdd<ParticleSystem>(child),
                dustMaterial,
                0f,
                0.85f,
                0.18f,
                0.55f,
                new Color(0.54f, 0.50f, 0.40f, 0.35f),
                ParticleSystemShapeType.Cone,
                0.12f);
            return child.GetComponent<ParticleSystem>();
        }

        private static void ConfigureBurstParticles(
            ParticleSystem particles,
            Material material,
            int particleCount,
            float lifetime,
            float minSize,
            float maxSize,
            Color color,
            ParticleSystemShapeType shapeType,
            float shapeRadius)
        {
            ParticleSystem.MainModule main = particles.main;
            main.playOnAwake = true;
            main.loop = false;
            main.duration = 0.25f;
            main.startLifetime = lifetime;
            main.startSpeed = new ParticleSystem.MinMaxCurve(2.5f, 7f);
            main.startSize = new ParticleSystem.MinMaxCurve(minSize, maxSize);
            main.startColor = color;
            main.maxParticles = particleCount;

            ParticleSystem.EmissionModule emission = particles.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)particleCount) });

            ParticleSystem.ShapeModule shape = particles.shape;
            shape.shapeType = shapeType;
            shape.radius = shapeRadius;

            ParticleSystemRenderer renderer = particles.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = material;
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
        }

        private static void ConfigureLoopParticles(
            ParticleSystem particles,
            Material material,
            float rateOverTime,
            float lifetime,
            float minSize,
            float maxSize,
            Color color,
            ParticleSystemShapeType shapeType,
            float shapeRadius)
        {
            ParticleSystem.MainModule main = particles.main;
            main.playOnAwake = true;
            main.loop = true;
            main.duration = 1f;
            main.startLifetime = lifetime;
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.3f, 1.6f);
            main.startSize = new ParticleSystem.MinMaxCurve(minSize, maxSize);
            main.startColor = color;
            main.maxParticles = 64;

            ParticleSystem.EmissionModule emission = particles.emission;
            emission.rateOverTime = rateOverTime;

            ParticleSystem.ShapeModule shape = particles.shape;
            shape.shapeType = shapeType;
            shape.radius = shapeRadius;

            ParticleSystemRenderer renderer = particles.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = material;
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
        }

        private static GameObject FindOrCreateChild(Transform parent, string name)
        {
            Transform child = parent.Find(name);
            if (child == null)
            {
                child = new GameObject(name).transform;
                child.SetParent(parent, false);
            }

            return child.gameObject;
        }

        private static GameObject SaveTemporaryPrefab(GameObject root, string path)
        {
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            UnityEngine.Object.DestroyImmediate(root);
            return prefab;
        }

        private static T GetOrAdd<T>(GameObject target) where T : Component
        {
            T component = target.GetComponent<T>();
            return component != null ? component : target.AddComponent<T>();
        }
    }
}
