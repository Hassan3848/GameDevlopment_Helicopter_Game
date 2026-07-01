using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace HelicopterCombat.EditorTools
{
    /// <summary>
    /// Rebuilds the complete Milestone 2 outdoor combat environment.
    /// This is an editor-only utility. It preserves the Milestone 1 helicopter and camera.
    /// </summary>
    public static class Milestone2EnvironmentBuilder
    {
        private const string ProjectRoot = "Assets/_Project";
        private const string ScenePath = ProjectRoot + "/Scenes/Game.unity";
        private const string TerrainDataPath = ProjectRoot + "/Art/Terrain/TD_CombatArena.asset";
        private const string TerrainLayersPath = ProjectRoot + "/Art/Terrain/Layers";
        private const string TerrainTexturesPath = ProjectRoot + "/Art/Terrain/Textures";
        private const string MaterialsPath = ProjectRoot + "/Art/Materials";
        private const string PrefabsPath = ProjectRoot + "/Prefabs/Environment";
        private const string ExternalNaturePath = ProjectRoot + "/Art/External/QuaterniusUltimateNature";

        private const int Seed = 24062026;
        private const int TerrainWidth = 500;
        private const int TerrainLength = 500;
        private const int TerrainHeight = 90;
        private const int HeightmapResolution = 513;
        private const int AlphamapResolution = 512;
        private const int DetailResolution = 256;

        [MenuItem("Tools/Helicopter Combat/Rebuild Milestone 2 Environment")]
        public static void RebuildMilestone2Environment()
        {
            EnsureFolders();

            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) == null)
            {
                throw new InvalidOperationException(
                    "Milestone 1 scene was not found at " + ScenePath + ". Build Milestone 1 first.");
            }

            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            GameObject player = GameObject.Find("PlayerHelicopter");
            GameObject mainCamera = GameObject.Find("Main Camera");
            GameObject directionalLight = GameObject.Find("Directional Light");

            if (player == null || mainCamera == null || directionalLight == null)
            {
                throw new InvalidOperationException(
                    "Game.unity is missing PlayerHelicopter, Main Camera, or Directional Light. " +
                    "Rebuild Milestone 1 before running Milestone 2.");
            }

            RemoveObjectIfPresent("FlightTestGround");
            RemoveObjectIfPresent("Environment");

            TerrainData terrainData = CreateFreshTerrainData();
            CreateTerrainTexturesAndLayers(terrainData);
            GenerateHeights(terrainData);
            PaintTerrainLayers(terrainData);

            GameObject environmentRoot = new GameObject("Environment");
            GameObject terrainObject = Terrain.CreateTerrainGameObject(terrainData);
            terrainObject.name = "CombatTerrain";
            terrainObject.transform.SetParent(environmentRoot.transform, false);
            terrainObject.transform.localPosition = new Vector3(-TerrainWidth * 0.5f, 0f, -TerrainLength * 0.5f);

            Terrain terrain = terrainObject.GetComponent<Terrain>();
            TerrainCollider terrainCollider = terrainObject.GetComponent<TerrainCollider>();
            terrainCollider.terrainData = terrainData;
            ConfigureTerrainRenderer(terrain);
            CreateGrassDetails(terrainData);

            Transform treesRoot = CreateChildRoot("Trees", environmentRoot.transform);
            Transform rocksRoot = CreateChildRoot("Rocks", environmentRoot.transform);
            Transform bushesRoot = CreateChildRoot("Bushes", environmentRoot.transform);
            CreateChildRoot("DecorativeProps", environmentRoot.transform);

            NaturePrefabs prefabs = CreateOrFindNaturePrefabs();
            PlaceEnvironmentProps(terrain, treesRoot, rocksRoot, bushesRoot, prefabs);

            PositionPlayerSafely(player, terrain);
            ConfigureLightingAndCamera(directionalLight, mainCamera);

            EditorSceneManager.MarkSceneDirty(scene);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorSceneManager.SaveScene(scene);

            Debug.Log("Milestone 2 built successfully. Open Game.unity and enter Play Mode to fly through the combat environment.");
        }

        public static void RebuildFromCommandLine()
        {
            RebuildMilestone2Environment();
        }

        private static void EnsureFolders()
        {
            string[] folders =
            {
                ProjectRoot + "/Art/External",
                ExternalNaturePath,
                ProjectRoot + "/Art/Materials",
                ProjectRoot + "/Art/Terrain",
                TerrainLayersPath,
                TerrainTexturesPath,
                ProjectRoot + "/Prefabs",
                PrefabsPath,
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

        private static void RemoveObjectIfPresent(string objectName)
        {
            GameObject existing = GameObject.Find(objectName);

            if (existing != null)
            {
                UnityEngine.Object.DestroyImmediate(existing);
            }
        }

        private static TerrainData CreateFreshTerrainData()
        {
            if (AssetDatabase.LoadAssetAtPath<TerrainData>(TerrainDataPath) != null)
            {
                AssetDatabase.DeleteAsset(TerrainDataPath);
            }

            TerrainData terrainData = new TerrainData
            {
                heightmapResolution = HeightmapResolution,
                alphamapResolution = AlphamapResolution,
                baseMapResolution = 1024,
                size = new Vector3(TerrainWidth, TerrainHeight, TerrainLength)
            };

            terrainData.SetDetailResolution(DetailResolution, 16);
            AssetDatabase.CreateAsset(terrainData, TerrainDataPath);
            return terrainData;
        }

        private static void GenerateHeights(TerrainData terrainData)
        {
            float[,] heights = new float[HeightmapResolution, HeightmapResolution];

            for (int zIndex = 0; zIndex < HeightmapResolution; zIndex++)
            {
                for (int xIndex = 0; xIndex < HeightmapResolution; xIndex++)
                {
                    float normalizedX = xIndex / (float)(HeightmapResolution - 1);
                    float normalizedZ = zIndex / (float)(HeightmapResolution - 1);
                    heights[zIndex, xIndex] = CalculateTerrainHeight(normalizedX, normalizedZ);
                }
            }

            terrainData.SetHeights(0, 0, heights);
        }

        private static float CalculateTerrainHeight(float normalizedX, float normalizedZ)
        {
            float worldX = (normalizedX - 0.5f) * TerrainWidth;
            float worldZ = (normalizedZ - 0.5f) * TerrainLength;

            float height = 0.060f;
            height += 0.028f * Mathf.PerlinNoise(normalizedX * 4.2f + 14.2f, normalizedZ * 4.2f + 9.1f);
            height += 0.018f * Mathf.PerlinNoise(normalizedX * 10.5f + 42.4f, normalizedZ * 10.5f + 17.7f);

            height += GaussianHill(worldX, worldZ, -155f, 120f, 95f, 0.22f);
            height += GaussianHill(worldX, worldZ, 145f, 95f, 80f, 0.18f);
            height += GaussianHill(worldX, worldZ, -120f, -145f, 85f, 0.20f);
            height += GaussianHill(worldX, worldZ, 155f, -135f, 105f, 0.24f);
            height += GaussianHill(worldX, worldZ, 5f, 205f, 110f, 0.14f);

            float centerDistance = new Vector2(worldX, worldZ).magnitude;
            float clearingBlend = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(45f, 110f, centerDistance));
            height = Mathf.Lerp(0.055f, height, clearingBlend);

            return Mathf.Clamp01(height);
        }

        private static float GaussianHill(
            float x,
            float z,
            float hillX,
            float hillZ,
            float radius,
            float amplitude)
        {
            float deltaX = x - hillX;
            float deltaZ = z - hillZ;
            float squaredDistance = deltaX * deltaX + deltaZ * deltaZ;
            float denominator = 2f * radius * radius;
            return amplitude * Mathf.Exp(-squaredDistance / denominator);
        }

        private static void CreateTerrainTexturesAndLayers(TerrainData terrainData)
        {
            Texture2D grassTexture = CreateTerrainTexture(
                TerrainTexturesPath + "/T_Grass_Generated.png",
                new Color(0.17f, 0.31f, 0.12f),
                new Color(0.36f, 0.50f, 0.20f),
                17.4f,
                101);

            Texture2D dirtTexture = CreateTerrainTexture(
                TerrainTexturesPath + "/T_Dirt_Generated.png",
                new Color(0.23f, 0.15f, 0.08f),
                new Color(0.47f, 0.32f, 0.16f),
                13.8f,
                211);

            Texture2D dryGrassTexture = CreateTerrainTexture(
                TerrainTexturesPath + "/T_DryGrass_Generated.png",
                new Color(0.32f, 0.29f, 0.12f),
                new Color(0.56f, 0.48f, 0.20f),
                15.6f,
                307);

            Texture2D rockTexture = CreateTerrainTexture(
                TerrainTexturesPath + "/T_Rock_Generated.png",
                new Color(0.20f, 0.21f, 0.20f),
                new Color(0.40f, 0.40f, 0.36f),
                10.2f,
                401);

            terrainData.terrainLayers = new[]
            {
                CreateOrUpdateTerrainLayer("TL_Grass", grassTexture, new Vector2(18f, 18f), 0f, 0.05f),
                CreateOrUpdateTerrainLayer("TL_Dirt", dirtTexture, new Vector2(20f, 20f), 0f, 0.02f),
                CreateOrUpdateTerrainLayer("TL_DryGrass", dryGrassTexture, new Vector2(22f, 22f), 0f, 0.03f),
                CreateOrUpdateTerrainLayer("TL_Rock", rockTexture, new Vector2(14f, 14f), 0f, 0.12f)
            };
        }

        private static Texture2D CreateTerrainTexture(
            string assetPath,
            Color darkColor,
            Color lightColor,
            float noiseScale,
            int seedOffset)
        {
            const int textureSize = 256;
            Texture2D generatedTexture = new Texture2D(textureSize, textureSize, TextureFormat.RGBA32, true)
            {
                name = Path.GetFileNameWithoutExtension(assetPath),
                wrapMode = TextureWrapMode.Repeat,
                filterMode = FilterMode.Trilinear
            };

            for (int y = 0; y < textureSize; y++)
            {
                for (int x = 0; x < textureSize; x++)
                {
                    float largeNoise = Mathf.PerlinNoise(
                        (x + seedOffset) / noiseScale,
                        (y + seedOffset * 2) / noiseScale);

                    float fineNoise = Mathf.PerlinNoise(
                        (x + seedOffset * 3) / 4.5f,
                        (y + seedOffset * 5) / 4.5f);

                    float blend = Mathf.Clamp01(largeNoise * 0.76f + fineNoise * 0.24f);
                    generatedTexture.SetPixel(x, y, Color.Lerp(darkColor, lightColor, blend));
                }
            }

            generatedTexture.Apply(true, false);
            WriteTextureToAsset(assetPath, generatedTexture, false);
            UnityEngine.Object.DestroyImmediate(generatedTexture);
            return AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
        }

        private static Texture2D CreateGrassDetailTexture()
        {
            const string assetPath = TerrainTexturesPath + "/T_GrassDetail_Generated.png";
            const int size = 128;
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, true)
            {
                name = Path.GetFileNameWithoutExtension(assetPath),
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
            };

            Color transparent = new Color(0f, 0f, 0f, 0f);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    texture.SetPixel(x, y, transparent);
                }
            }

            System.Random random = new System.Random(Seed + 702);

            for (int blade = 0; blade < 24; blade++)
            {
                int baseX = random.Next(4, size - 4);
                int bladeHeight = random.Next(size / 3, size - 8);
                int lean = random.Next(-10, 11);

                for (int y = 0; y < bladeHeight; y++)
                {
                    float t = y / (float)bladeHeight;
                    int x = Mathf.RoundToInt(baseX + lean * t);
                    int width = Mathf.Max(1, Mathf.RoundToInt((1f - t) * 1.8f));
                    Color bladeColor = Color.Lerp(
                        new Color(0.16f, 0.32f, 0.08f, 1f),
                        new Color(0.48f, 0.62f, 0.16f, 1f),
                        t);

                    for (int offset = -width; offset <= width; offset++)
                    {
                        int pixelX = x + offset;
                        if (pixelX >= 0 && pixelX < size)
                        {
                            texture.SetPixel(pixelX, y, bladeColor);
                        }
                    }
                }
            }

            texture.Apply(true, false);
            WriteTextureToAsset(assetPath, texture, true);
            UnityEngine.Object.DestroyImmediate(texture);
            return AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
        }

        private static void WriteTextureToAsset(string assetPath, Texture2D texture, bool alphaIsTransparency)
        {
            string fullPath = Path.GetFullPath(Path.Combine(Application.dataPath, "..", assetPath));
            string directory = Path.GetDirectoryName(fullPath);

            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllBytes(fullPath, texture.EncodeToPNG());
            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);

            TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Default;
                importer.wrapMode = alphaIsTransparency ? TextureWrapMode.Clamp : TextureWrapMode.Repeat;
                importer.filterMode = FilterMode.Trilinear;
                importer.mipmapEnabled = true;
                importer.alphaIsTransparency = alphaIsTransparency;
                importer.sRGBTexture = true;
                importer.SaveAndReimport();
            }
        }

        private static TerrainLayer CreateOrUpdateTerrainLayer(
            string layerName,
            Texture2D diffuseTexture,
            Vector2 tileSize,
            float metallic,
            float smoothness)
        {
            string assetPath = TerrainLayersPath + "/" + layerName + ".terrainlayer";
            TerrainLayer layer = AssetDatabase.LoadAssetAtPath<TerrainLayer>(assetPath);

            if (layer == null)
            {
                layer = new TerrainLayer();
                AssetDatabase.CreateAsset(layer, assetPath);
            }

            layer.name = layerName;
            layer.diffuseTexture = diffuseTexture;
            layer.tileSize = tileSize;
            layer.tileOffset = Vector2.zero;
            layer.metallic = metallic;
            layer.smoothness = smoothness;
            EditorUtility.SetDirty(layer);
            return layer;
        }

        private static void PaintTerrainLayers(TerrainData terrainData)
        {
            float[,,] alphaMaps = new float[AlphamapResolution, AlphamapResolution, 4];

            for (int zIndex = 0; zIndex < AlphamapResolution; zIndex++)
            {
                for (int xIndex = 0; xIndex < AlphamapResolution; xIndex++)
                {
                    float normalizedX = xIndex / (float)(AlphamapResolution - 1);
                    float normalizedZ = zIndex / (float)(AlphamapResolution - 1);
                    float worldX = (normalizedX - 0.5f) * TerrainWidth;
                    float worldZ = (normalizedZ - 0.5f) * TerrainLength;
                    float terrainHeight = terrainData.GetInterpolatedHeight(normalizedX, normalizedZ) / TerrainHeight;
                    float slope = terrainData.GetSteepness(normalizedX, normalizedZ);
                    float centerDistance = new Vector2(worldX, worldZ).magnitude;
                    float noise = Mathf.PerlinNoise(normalizedX * 9.7f + 4.2f, normalizedZ * 9.7f + 15.8f);

                    float rockWeight = Mathf.Clamp01((slope - 24f) / 24f) * 0.80f +
                                       Mathf.Clamp01((terrainHeight - 0.19f) / 0.18f) * 0.38f;

                    float clearingWeight = 1f - Mathf.SmoothStep(35f, 115f, centerDistance);
                    float dirtWeight = clearingWeight * 0.86f + Mathf.Clamp01((noise - 0.72f) / 0.28f) * 0.18f;
                    float dryGrassWeight = Mathf.Clamp01((terrainHeight - 0.12f) / 0.25f) * 0.34f +
                                           Mathf.Clamp01((noise - 0.54f) / 0.46f) * 0.16f;
                    float grassWeight = 1.00f - rockWeight * 0.88f - dirtWeight * 0.60f - dryGrassWeight * 0.30f;

                    float totalWeight = Mathf.Max(0.0001f, grassWeight + dirtWeight + dryGrassWeight + rockWeight);
                    alphaMaps[zIndex, xIndex, 0] = Mathf.Max(0f, grassWeight) / totalWeight;
                    alphaMaps[zIndex, xIndex, 1] = Mathf.Max(0f, dirtWeight) / totalWeight;
                    alphaMaps[zIndex, xIndex, 2] = Mathf.Max(0f, dryGrassWeight) / totalWeight;
                    alphaMaps[zIndex, xIndex, 3] = Mathf.Max(0f, rockWeight) / totalWeight;
                }
            }

            terrainData.SetAlphamaps(0, 0, alphaMaps);
        }

        private static void CreateGrassDetails(TerrainData terrainData)
        {
            Texture2D grassTexture = CreateGrassDetailTexture();
            DetailPrototype grassPrototype = new DetailPrototype
            {
                prototypeTexture = grassTexture,
                renderMode = DetailRenderMode.GrassBillboard,
                minWidth = 0.55f,
                maxWidth = 1.10f,
                minHeight = 0.65f,
                maxHeight = 1.45f,
                healthyColor = new Color(0.31f, 0.49f, 0.16f, 1f),
                dryColor = new Color(0.52f, 0.46f, 0.20f, 1f),
                noiseSpread = 0.35f,
                positionJitter = 0.90f,
                useDensityScaling = true
            };

            terrainData.detailPrototypes = new[] { grassPrototype };

            int[,] densityMap = new int[DetailResolution, DetailResolution];

            for (int zIndex = 0; zIndex < DetailResolution; zIndex++)
            {
                for (int xIndex = 0; xIndex < DetailResolution; xIndex++)
                {
                    float normalizedX = xIndex / (float)(DetailResolution - 1);
                    float normalizedZ = zIndex / (float)(DetailResolution - 1);
                    float worldX = (normalizedX - 0.5f) * TerrainWidth;
                    float worldZ = (normalizedZ - 0.5f) * TerrainLength;
                    float centerDistance = new Vector2(worldX, worldZ).magnitude;
                    float slope = terrainData.GetSteepness(normalizedX, normalizedZ);
                    float noise = Mathf.PerlinNoise(normalizedX * 20f + 7.3f, normalizedZ * 20f + 2.1f);

                    bool canPlaceGrass = centerDistance > 55f && slope < 28f && noise > 0.32f;
                    densityMap[xIndex, zIndex] = canPlaceGrass ? Mathf.RoundToInt(Mathf.Lerp(1f, 4f, noise)) : 0;
                }
            }

            terrainData.SetDetailLayer(0, 0, 0, densityMap);
        }

        private static void ConfigureTerrainRenderer(Terrain terrain)
        {
            terrain.materialType = Terrain.MaterialType.BuiltInStandard;
            terrain.heightmapPixelError = 6f;
            terrain.basemapDistance = 650f;
            terrain.treeDistance = 240f;
            terrain.treeBillboardDistance = 70f;
            terrain.treeCrossFadeLength = 8f;
            terrain.treeMaximumFullLODCount = 400;
            terrain.detailObjectDistance = 75f;
            terrain.detailObjectDensity = 0.62f;
            terrain.drawHeightmap = true;
            terrain.drawTreesAndFoliage = true;
            terrain.reflectionProbeUsage = ReflectionProbeUsage.BlendProbes;
            terrain.shadowCastingMode = ShadowCastingMode.On;
        }

        private static Transform CreateChildRoot(string name, Transform parent)
        {
            GameObject root = new GameObject(name);
            root.transform.SetParent(parent, false);
            return root.transform;
        }

        private static NaturePrefabs CreateOrFindNaturePrefabs()
        {
            Material bark = CreateOrUpdateMaterial("M_FallbackBark", new Color(0.24f, 0.13f, 0.06f), 0f, 0.05f);
            Material foliage = CreateOrUpdateMaterial("M_FallbackFoliage", new Color(0.16f, 0.34f, 0.10f), 0f, 0.03f);
            Material foliageDark = CreateOrUpdateMaterial("M_FallbackFoliageDark", new Color(0.08f, 0.24f, 0.08f), 0f, 0.03f);
            Material rock = CreateOrUpdateMaterial("M_FallbackRock", new Color(0.28f, 0.29f, 0.27f), 0f, 0.16f);

            GameObject fallbackTreeTall = CreateFallbackTreeTall(bark, foliageDark);
            GameObject fallbackTreeRound = CreateFallbackTreeRound(bark, foliage);
            GameObject fallbackRockLarge = CreateFallbackRock("P_FallbackRockLarge", rock, new Vector3(3.8f, 2.4f, 3.1f));
            GameObject fallbackRockSmall = CreateFallbackRock("P_FallbackRockSmall", rock, new Vector3(1.8f, 1.2f, 1.5f));
            GameObject fallbackBush = CreateFallbackBush(foliage);

            GameObject importedTreeA = FindImportedNatureModel("tree", "pine", "conifer");
            GameObject importedTreeB = FindImportedNatureModel("tree", "oak", "birch", "deciduous");
            GameObject importedRock = FindImportedNatureModel("rock", "stone", "boulder");
            GameObject importedBush = FindImportedNatureModel("bush", "plant", "fern", "shrub");

            bool importedAssetsFound = importedTreeA != null || importedTreeB != null || importedRock != null || importedBush != null;
            if (!importedAssetsFound)
            {
                Debug.LogWarning(
                    "Milestone 2 used fallback low-poly vegetation because no compatible imported models were found in " +
                    ExternalNaturePath + ". Import the free nature pack there before rebuilding to use external models.");
            }

            return new NaturePrefabs
            {
                TreeA = importedTreeA != null ? importedTreeA : fallbackTreeTall,
                TreeB = importedTreeB != null ? importedTreeB : fallbackTreeRound,
                RockA = importedRock != null ? importedRock : fallbackRockLarge,
                RockB = importedRock != null ? importedRock : fallbackRockSmall,
                Bush = importedBush != null ? importedBush : fallbackBush
            };
        }

        private static Material CreateOrUpdateMaterial(string name, Color color, float metallic, float smoothness)
        {
            string assetPath = MaterialsPath + "/" + name + ".mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(assetPath);

            if (material == null)
            {
                Shader standardShader = Shader.Find("Standard");
                material = new Material(standardShader);
                AssetDatabase.CreateAsset(material, assetPath);
            }

            material.name = name;
            material.color = color;
            material.SetFloat("_Metallic", metallic);
            material.SetFloat("_Glossiness", smoothness);
            EditorUtility.SetDirty(material);
            return material;
        }

        private static GameObject CreateFallbackTreeTall(Material barkMaterial, Material foliageMaterial)
        {
            const string assetPath = PrefabsPath + "/P_FallbackTreeTall.prefab";
            GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
            if (existing != null)
            {
                return existing;
            }

            GameObject root = new GameObject("P_FallbackTreeTall");
            CapsuleCollider collider = root.AddComponent<CapsuleCollider>();
            collider.center = new Vector3(0f, 4.7f, 0f);
            collider.radius = 1.3f;
            collider.height = 9.4f;

            CreatePrimitive(PrimitiveType.Cylinder, "Trunk", root.transform, new Vector3(0f, 2f, 0f), new Vector3(0.65f, 2f, 0.65f), barkMaterial, false);
            CreatePrimitive(PrimitiveType.Sphere, "FoliageLower", root.transform, new Vector3(0f, 4.7f, 0f), new Vector3(3.6f, 2.6f, 3.6f), foliageMaterial, false);
            CreatePrimitive(PrimitiveType.Sphere, "FoliageMiddle", root.transform, new Vector3(0f, 6.8f, 0f), new Vector3(2.9f, 2.5f, 2.9f), foliageMaterial, false);
            CreatePrimitive(PrimitiveType.Sphere, "FoliageTop", root.transform, new Vector3(0f, 8.5f, 0f), new Vector3(2.0f, 2.0f, 2.0f), foliageMaterial, false);

            return SavePrefab(root, assetPath);
        }

        private static GameObject CreateFallbackTreeRound(Material barkMaterial, Material foliageMaterial)
        {
            const string assetPath = PrefabsPath + "/P_FallbackTreeRound.prefab";
            GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
            if (existing != null)
            {
                return existing;
            }

            GameObject root = new GameObject("P_FallbackTreeRound");
            CapsuleCollider collider = root.AddComponent<CapsuleCollider>();
            collider.center = new Vector3(0f, 3.8f, 0f);
            collider.radius = 1.8f;
            collider.height = 7.6f;

            CreatePrimitive(PrimitiveType.Cylinder, "Trunk", root.transform, new Vector3(0f, 1.7f, 0f), new Vector3(0.55f, 1.7f, 0.55f), barkMaterial, false);
            CreatePrimitive(PrimitiveType.Sphere, "Foliage", root.transform, new Vector3(0f, 5.3f, 0f), new Vector3(4.8f, 3.8f, 4.8f), foliageMaterial, false);
            CreatePrimitive(PrimitiveType.Sphere, "FoliageSide", root.transform, new Vector3(1.2f, 4.5f, 0.3f), new Vector3(3.1f, 2.7f, 3.1f), foliageMaterial, false);

            return SavePrefab(root, assetPath);
        }

        private static GameObject CreateFallbackRock(string prefabName, Material rockMaterial, Vector3 scale)
        {
            string assetPath = PrefabsPath + "/" + prefabName + ".prefab";
            GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
            if (existing != null)
            {
                return existing;
            }

            GameObject root = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            root.name = prefabName;
            root.transform.localScale = scale;
            root.GetComponent<Renderer>().sharedMaterial = rockMaterial;
            return SavePrefab(root, assetPath);
        }

        private static GameObject CreateFallbackBush(Material foliageMaterial)
        {
            const string assetPath = PrefabsPath + "/P_FallbackBush.prefab";
            GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
            if (existing != null)
            {
                return existing;
            }

            GameObject root = new GameObject("P_FallbackBush");
            SphereCollider collider = root.AddComponent<SphereCollider>();
            collider.center = new Vector3(0f, 0.7f, 0f);
            collider.radius = 1.25f;

            CreatePrimitive(PrimitiveType.Sphere, "BushLeft", root.transform, new Vector3(-0.7f, 0.7f, 0f), new Vector3(1.4f, 1.3f, 1.2f), foliageMaterial, false);
            CreatePrimitive(PrimitiveType.Sphere, "BushCenter", root.transform, new Vector3(0f, 0.9f, 0.2f), new Vector3(1.6f, 1.6f, 1.4f), foliageMaterial, false);
            CreatePrimitive(PrimitiveType.Sphere, "BushRight", root.transform, new Vector3(0.7f, 0.6f, -0.1f), new Vector3(1.2f, 1.1f, 1.1f), foliageMaterial, false);

            return SavePrefab(root, assetPath);
        }

        private static void CreatePrimitive(
            PrimitiveType primitiveType,
            string name,
            Transform parent,
            Vector3 localPosition,
            Vector3 localScale,
            Material material,
            bool keepCollider)
        {
            GameObject primitive = GameObject.CreatePrimitive(primitiveType);
            primitive.name = name;
            primitive.transform.SetParent(parent, false);
            primitive.transform.localPosition = localPosition;
            primitive.transform.localRotation = Quaternion.identity;
            primitive.transform.localScale = localScale;
            primitive.GetComponent<Renderer>().sharedMaterial = material;

            if (!keepCollider)
            {
                Collider primitiveCollider = primitive.GetComponent<Collider>();
                if (primitiveCollider != null)
                {
                    UnityEngine.Object.DestroyImmediate(primitiveCollider);
                }
            }
        }

        private static GameObject SavePrefab(GameObject root, string assetPath)
        {
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, assetPath);
            UnityEngine.Object.DestroyImmediate(root);
            return prefab;
        }

        private static GameObject FindImportedNatureModel(params string[] keywords)
        {
            if (!AssetDatabase.IsValidFolder(ExternalNaturePath))
            {
                return null;
            }

            string[] guids = AssetDatabase.FindAssets("t:GameObject", new[] { ExternalNaturePath });

            foreach (string guid in guids)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                string lowerPath = assetPath.ToLowerInvariant();

                foreach (string keyword in keywords)
                {
                    if (!lowerPath.Contains(keyword.ToLowerInvariant()))
                    {
                        continue;
                    }

                    GameObject candidate = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
                    if (candidate != null)
                    {
                        return candidate;
                    }
                }
            }

            return null;
        }

        private static void PlaceEnvironmentProps(
            Terrain terrain,
            Transform treesRoot,
            Transform rocksRoot,
            Transform bushesRoot,
            NaturePrefabs prefabs)
        {
            System.Random random = new System.Random(Seed);

            PlaceProps(terrain, treesRoot, new[] { prefabs.TreeA, prefabs.TreeB }, 90, 70f, 27f, 0.80f, 1.35f, random, "Tree");
            PlaceProps(terrain, rocksRoot, new[] { prefabs.RockA, prefabs.RockB }, 130, 45f, 42f, 0.65f, 1.55f, random, "Rock");
            PlaceProps(terrain, bushesRoot, new[] { prefabs.Bush }, 100, 65f, 25f, 0.70f, 1.30f, random, "Bush");
        }

        private static void PlaceProps(
            Terrain terrain,
            Transform parent,
            IReadOnlyList<GameObject> candidates,
            int targetCount,
            float minimumCenterDistance,
            float maximumSlope,
            float minimumScale,
            float maximumScale,
            System.Random random,
            string objectPrefix)
        {
            int placedCount = 0;

            for (int targetIndex = 0; targetIndex < targetCount; targetIndex++)
            {
                if (!TryGetValidPosition(terrain, minimumCenterDistance, maximumSlope, random, out Vector3 position))
                {
                    continue;
                }

                GameObject prefab = candidates[random.Next(candidates.Count)];
                GameObject instance = PrefabUtility.InstantiatePrefab(prefab, parent) as GameObject;

                if (instance == null)
                {
                    continue;
                }

                float scale = Mathf.Lerp(minimumScale, maximumScale, (float)random.NextDouble());
                instance.name = objectPrefix + "_" + placedCount.ToString("000");
                instance.transform.position = position;
                instance.transform.rotation = Quaternion.Euler(0f, (float)random.NextDouble() * 360f, 0f);
                instance.transform.localScale = Vector3.one * scale;
                GameObjectUtility.SetStaticEditorFlags(instance, StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccluderStatic);
                placedCount++;
            }
        }

        private static bool TryGetValidPosition(
            Terrain terrain,
            float minimumCenterDistance,
            float maximumSlope,
            System.Random random,
            out Vector3 position)
        {
            TerrainData terrainData = terrain.terrainData;
            Vector3 terrainOrigin = terrain.transform.position;

            for (int attempt = 0; attempt < 150; attempt++)
            {
                float normalizedX = 0.035f + (float)random.NextDouble() * 0.93f;
                float normalizedZ = 0.035f + (float)random.NextDouble() * 0.93f;
                float worldX = terrainOrigin.x + normalizedX * terrainData.size.x;
                float worldZ = terrainOrigin.z + normalizedZ * terrainData.size.z;
                float centerDistance = new Vector2(worldX, worldZ).magnitude;
                float slope = terrainData.GetSteepness(normalizedX, normalizedZ);

                if (centerDistance < minimumCenterDistance || slope > maximumSlope)
                {
                    continue;
                }

                float worldY = terrain.SampleHeight(new Vector3(worldX, 0f, worldZ)) + terrainOrigin.y;
                position = new Vector3(worldX, worldY, worldZ);
                return true;
            }

            position = default;
            return false;
        }

        private static void PositionPlayerSafely(GameObject player, Terrain terrain)
        {
            Vector3 spawnPosition = Vector3.zero;
            spawnPosition.y = terrain.SampleHeight(spawnPosition) + terrain.transform.position.y + 22f;
            player.transform.position = spawnPosition;
            player.transform.rotation = Quaternion.identity;

            Rigidbody playerRigidbody = player.GetComponent<Rigidbody>();
            if (playerRigidbody != null)
            {
                playerRigidbody.position = spawnPosition;
                playerRigidbody.linearVelocity = Vector3.zero;
                playerRigidbody.angularVelocity = Vector3.zero;
            }
        }

        private static void ConfigureLightingAndCamera(GameObject directionalLightObject, GameObject mainCameraObject)
        {
            Light directionalLight = directionalLightObject.GetComponent<Light>();
            if (directionalLight != null)
            {
                directionalLight.type = LightType.Directional;
                directionalLight.transform.rotation = Quaternion.Euler(52f, -35f, 0f);
                directionalLight.intensity = 1.15f;
                directionalLight.color = new Color(1f, 0.93f, 0.82f, 1f);
                directionalLight.shadows = LightShadows.Soft;
                directionalLight.shadowStrength = 0.80f;
                directionalLight.shadowBias = 0.05f;
                directionalLight.shadowNormalBias = 0.40f;
            }

            Camera mainCamera = mainCameraObject.GetComponent<Camera>();
            if (mainCamera != null)
            {
                mainCamera.clearFlags = CameraClearFlags.Skybox;
                mainCamera.fieldOfView = 65f;
                mainCamera.nearClipPlane = 0.20f;
                mainCamera.farClipPlane = 900f;
            }

            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = new Color(0.69f, 0.76f, 0.78f, 1f);
            RenderSettings.fogStartDistance = 180f;
            RenderSettings.fogEndDistance = 540f;
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.56f, 0.66f, 0.78f, 1f);
            RenderSettings.ambientEquatorColor = new Color(0.42f, 0.47f, 0.37f, 1f);
            RenderSettings.ambientGroundColor = new Color(0.22f, 0.24f, 0.20f, 1f);
            RenderSettings.reflectionIntensity = 0.55f;
        }

        private struct NaturePrefabs
        {
            public GameObject TreeA;
            public GameObject TreeB;
            public GameObject RockA;
            public GameObject RockB;
            public GameObject Bush;
        }
    }
}
