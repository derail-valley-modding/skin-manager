﻿using DV.JObjectExtstensions;
using HarmonyLib;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace SkinManagerMod
{
    public static class SkinManager
    {
        private static readonly string[] standardShaderUniqueTextures = new[] { "_MainTex", "_BumpMap", "_MetallicGlossMap", "_EmissionMap" };
        private static readonly string[] standardShaderAllTextures = new[] { "_MainTex", "_BumpMap", "_MetallicGlossMap", "_EmissionMap", "_OcclusionMap" };
        private const string METAL_GLOSS_TEXTURE = "_MetallicGlossMap";
        private const string OCCLUSION_TEXTURE = "_OcclusionMap";

        private static readonly Dictionary<string, string> textureAliases = new Dictionary<string, string>
        {
            { "exterior_d", "body" },
            { "LocoDiesel_exterior_d", "body" },
            { "SH_exterior_d", "body" },
            { "SH_tender_01d", "body" }
        };

        public static readonly TrainCarType[] DisabledCarTypes = new[]
        {
            TrainCarType.LocoSteamHeavyBlue,
            TrainCarType.TenderBlue,
            TrainCarType.LocoRailbus
        };

        public static Dictionary<TrainCarType, string> EnabledCarTypes { get; private set; }
        private static Dictionary<TrainCarType, string> CustomCarTypes;

        private static readonly Dictionary<TrainCarType, SkinGroup> skinGroups = new Dictionary<TrainCarType, SkinGroup>();
        public static IEnumerable<SkinGroup> AllSkinGroups => skinGroups.Values;

        private static readonly Dictionary<TrainCarType, Skin> defaultSkins = new Dictionary<TrainCarType, Skin>();

        private static readonly Dictionary<string, string> carGuidToAppliedSkinMap = new Dictionary<string, string>();

        private static Skin lastSteamerSkin;

        public static event Action<TrainCarType?> SkinsLoaded;

        public static Skin FindSkinByName(TrainCarType carType, string name)
        {
            if (skinGroups.TryGetValue(carType, out var group))
            {
                return group.GetSkin(name);
            }
            return null;
        }

        public static List<Skin> GetSkinsForType(TrainCarType carType, bool includeDefault = true)
        {
            if (skinGroups.TryGetValue(carType, out var group))
            {
                var result = group.Skins;
                return includeDefault ? result.Append(defaultSkins[carType]).ToList() : result;
            }
            return includeDefault ? new List<Skin>() { defaultSkins[carType] } : new List<Skin>();
        }

        public static Skin GetNewSkin(TrainCarType carType)
        {
            if (CarTypes.IsTender(carType) && (lastSteamerSkin != null))
            {
                if (FindSkinByName(carType, lastSteamerSkin.Name) is Skin matchingTenderSkin)
                {
                    return matchingTenderSkin;
                }
            }

            // random skin
            if (skinGroups.TryGetValue(carType, out var group) && (group.Skins.Count > 0))
            {
                bool allowRandomDefault =
                    (Main.Settings.defaultSkinsMode == SkinManagerSettings.DefaultSkinsMode.AllowForAllCars) ||
                    (CustomCarTypes.ContainsKey(carType) && (Main.Settings.defaultSkinsMode == SkinManagerSettings.DefaultSkinsMode.AllowForCustomCars));

                int nChoices = allowRandomDefault ? group.Skins.Count + 1 : group.Skins.Count;
                int choice = UnityEngine.Random.Range(0, nChoices);
                if (choice < group.Skins.Count)
                {
                    return group.Skins[choice];
                }
            }

            // fall back to default skin
            return defaultSkins[carType];
        }

        public static Skin GetCurrentCarSkin(TrainCar car)
        {
            if (carGuidToAppliedSkinMap.TryGetValue(car.CarGUID, out var skinName))
            {
                if (defaultSkins.TryGetValue(car.carType, out Skin defaultSkin) && (skinName == defaultSkin.Name))
                {
                    return defaultSkin;
                }

                if (FindSkinByName(car.carType, skinName) is Skin result)
                {
                    return result;
                }
            }
            return GetNewSkin(car.carType);
        }

        private static void SetAppliedCarSkin(TrainCar car, Skin skin)
        {
            carGuidToAppliedSkinMap[car.CarGUID] = skin.Name;

            if (CarTypes.IsSteamLocomotive(car.carType))
            {
                lastSteamerSkin = skin;
            }
            else
            {
                lastSteamerSkin = null;
            }
        }

        public static bool Initialize()
        {
            var fieldInfo = AccessTools.Field(typeof(CarTypes), "prefabMap");
            if (fieldInfo?.GetValue(null) is Dictionary<TrainCarType, string> defaultMap)
            {
                EnabledCarTypes = new Dictionary<TrainCarType, string>(
                    defaultMap.Where(t => !DisabledCarTypes.Contains(t.Key)).Concat(CCLPatch.CarList)
                );
                CustomCarTypes = new Dictionary<TrainCarType, string>(CCLPatch.CarList);
                LoadSkins();
                return true;
            }
            return false;
        }

        //====================================================================================================
        #region Skin Loading

        private static void LoadSkins()
        {
            foreach ((TrainCarType carType, string carName) in EnabledCarTypes)
            {
                Skin defaultSkin = CreateDefaultSkin(carType, carType.DisplayName());
                defaultSkins.Add(carType, defaultSkin);

                skinGroups[carType] = new SkinGroup(carType);

                var dir = Path.Combine(Main.ModEntry.Path, "Skins", carName);

                if (Directory.Exists(dir))
                {
                    LoadSkinsForType(dir, carType);
                }
            }

            LoadCCLEmbeddedSkins();

            SkinsLoaded?.Invoke(null);
        }

        /// <summary>
        /// Load any skins included in CCL car folders
        /// </summary>
        private static void LoadCCLEmbeddedSkins()
        {
            if (!CCLPatch.Enabled) return;

            var idToCarType = CustomCarTypes.ToDictionary(kvp => kvp.Value, kvp => kvp.Key);

            string carsDir = Path.Combine(CCLPatch.ModEntry.Path, "Cars");
            if (Directory.Exists(carsDir))
            {
                foreach (string carDir in Directory.GetDirectories(carsDir))
                {
                    string jsonFile = Path.Combine(carDir, "car.json");
                    string skinsFolder = Path.Combine(carDir, "Skins");

                    if (File.Exists(jsonFile) && Directory.Exists(skinsFolder))
                    {
                        try
                        {
                            string json = File.ReadAllText(jsonFile);
                            var carInfo = JObject.Parse(json);
                            string identifier = carInfo.Value<string>("identifier");

                            var carType = idToCarType[identifier];

                            LoadSkinsForType(skinsFolder, carType);
                        }
                        catch (Exception ex)
                        {
                            Main.ModEntry.Logger.LogException(ex);
                        }
                    }
                }
            }
        }

        public static void ReloadSkins(TrainCarType carType)
        {
            string carName = EnabledCarTypes[carType];
            skinGroups[carType] = new SkinGroup(carType);

            var dir = Path.Combine(Main.ModEntry.Path, "Skins", carName);

            if (Directory.Exists(dir))
            {
                LoadSkinsForType(dir, carType, true);
            }

            if (CustomCarTypes.ContainsKey(carType))
            {
                // also reload CCL embedded skins
                string carsDir = Path.Combine(CCLPatch.ModEntry.Path, "Cars");
                foreach (string carDir in Directory.GetDirectories(carsDir))
                {
                    string jsonFile = Path.Combine(carDir, "car.json");
                    string skinsFolder = Path.Combine(carDir, "Skins");

                    if (File.Exists(jsonFile) && Directory.Exists(skinsFolder))
                    {
                        try
                        {
                            string json = File.ReadAllText(jsonFile);
                            var carInfo = JObject.Parse(json);
                            string identifier = carInfo.Value<string>("identifier");

                            if (identifier == carName)
                            {
                                LoadSkinsForType(skinsFolder, carType);
                            }
                        }
                        catch (Exception ex)
                        {
                            Main.ModEntry.Logger.LogException(ex);
                        }
                    }
                }
            }

            SkinsLoaded?.Invoke(carType);

            // reapply skins to any cars of this type
            var carsInScene = UnityEngine.Object.FindObjectsOfType<TrainCar>().Where(tc => tc.carType == carType);
            foreach (var car in carsInScene)
            {
                var toApply = GetCurrentCarSkin(car);
                ApplySkin(car, toApply);

                if (car.IsInteriorLoaded)
                {
                    ApplySkinToInterior(car, toApply);
                }
            }
        }

        private static void LoadSkinsForType(string skinsFolder, TrainCarType carType, bool forceSync = false)
        {
            var skinGroup = skinGroups[carType];
            var renderers = GetAllCarRenderers(carType);

            foreach (string subDir in Directory.GetDirectories(skinsFolder))
            {
                BeginLoadSkin(skinGroup, renderers, subDir, forceSync);
            }
        }

        private static IEnumerable<MeshRenderer> GetAllCarRenderers(TrainCarType carType)
        {
            var carPrefab = CarTypes.GetCarPrefab(carType);
            IEnumerable<MeshRenderer> cmps = carPrefab.gameObject.GetComponentsInChildren<MeshRenderer>();

            var trainCar = carPrefab.GetComponent<TrainCar>();

            if (trainCar.interiorPrefab != null)
            {
                var interiorCmps = trainCar.interiorPrefab.GetComponentsInChildren<MeshRenderer>();
                cmps = cmps.Concat(interiorCmps);
            }

            return cmps;
        }

        /// <summary>
        /// Create a skin containing the default/starting textures of a car
        /// </summary>
        private static Skin CreateDefaultSkin(TrainCarType carType, string typeName)
        {
            GameObject carPrefab = CarTypes.GetCarPrefab(carType);
            if (carPrefab == null) return null;

            string skinDir = null;
            if (CCLPatch.IsCustomCarType(carType))
            {
                skinDir = CCLPatch.GetCarFolder(carType);
            }

            Skin defSkin = new Skin($"Default_{typeName}", skinDir, isDefault: true);

            var renderers = carPrefab.gameObject.GetComponentsInChildren<MeshRenderer>();
            foreach (var renderer in renderers)
            {
                if (!renderer.material) continue;

                foreach (string textureName in standardShaderUniqueTextures)
                {
                    if (TextureUtility.GetMaterialTexture(renderer, textureName) is Texture2D texture)
                    {
                        defSkin.SkinTextures.Add(new SkinTexture(texture.name, texture));
                    }
                }
            }

            var trainCar = carPrefab.GetComponent<TrainCar>();

            if (trainCar?.interiorPrefab)
            {
                foreach (var renderer in trainCar.interiorPrefab.GetComponentsInChildren<MeshRenderer>())
                {
                    if (!renderer.material) continue;

                    foreach (string textureName in standardShaderUniqueTextures)
                    {
                        if (TextureUtility.GetMaterialTexture(renderer, textureName) is Texture2D texture)
                        {
                            defSkin.SkinTextures.Add(new SkinTexture(texture.name, texture));
                        }
                    }
                }
            }

            return defSkin;
        }

        /// <summary>
        /// Create a skin from the given directory, load textures, and add it to the given group
        /// </summary>
        private static void BeginLoadSkin(SkinGroup skinGroup, IEnumerable<MeshRenderer> renderers, string subDir, bool forceSync = false)
        {
            var dirInfo = new DirectoryInfo(subDir);
            var files = dirInfo.GetFiles();
            var skin = new Skin(dirInfo.Name, subDir);

            var loading = new HashSet<string>();

            bool Matches(Texture2D texture, string fileName)
            {
                return texture != null &&
                    !loading.Contains(texture.name) &&
                    (texture.name == fileName ||
                        (textureAliases.TryGetValue(texture.name, out var alias) && alias == fileName));
            }

            foreach (var file in files)
            {
                if (!StbImage.IsSupportedExtension(file.Extension))
                    continue;
                string fileName = Path.GetFileNameWithoutExtension(file.Name);

                foreach (var renderer in renderers)
                {
                    foreach (var textureName in standardShaderUniqueTextures)
                    {
                        var texture = TextureUtility.GetMaterialTexture(renderer, textureName);
                        if (Matches(texture, fileName))
                        {
                            var linear = textureName == "_BumpMap";
                            loading.Add(texture.name);

                            if (!forceSync && Main.Settings.parallelLoading)
                            {
                                skin.SkinTextures.Add(new SkinTexture(fileName, TextureLoader.Add(file, linear)));
                            }
                            else
                            {
                                TextureLoader.BustCache(file);
                                var tex = new Texture2D(0, 0, textureFormat: TextureFormat.RGBA32, mipChain: true, linear: linear);
                                tex.LoadImage(File.ReadAllBytes(file.FullName));
                                skin.SkinTextures.Add(new SkinTexture(fileName, tex));
                            }
                        }
                    }
                }
            }

            skinGroup.Skins.Add(skin);
        }

        #endregion

        //====================================================================================================
        #region Skin Manipulation

        public static void ApplySkin(TrainCar trainCar, Skin skin)
        {
            if (skin == null) return;

            //Main.ModEntry.Logger.Log($"Applying skin {skin.Name} to car {trainCar.ID}");

            ApplySkin(trainCar.gameObject.transform, skin, defaultSkins[trainCar.carType]);
            if (trainCar.IsInteriorLoaded)
            {
                ApplySkinToInterior(trainCar, skin);
            }

            SetAppliedCarSkin(trainCar, skin);
        }

        public static void ApplySkinToInterior(TrainCar trainCar, Skin skin)
        {
            if (skin == null) return;

            ApplySkin(trainCar.interior, skin, defaultSkins[trainCar.carType]);
        }

        private static void ApplySkin(Transform objectRoot, Skin skin, Skin defaultSkin)
        {
            foreach (var renderer in objectRoot.GetComponentsInChildren<MeshRenderer>())
            {
                if (!renderer.material)
                {
                    continue;
                }

                foreach (string textureID in standardShaderAllTextures)
                {
                    var currentTexture = TextureUtility.GetMaterialTexture(renderer, textureID);

                    if (currentTexture != null)
                    {
                        if (skin.ContainsTexture(currentTexture.name))
                        {
                            var skinTexture = skin.GetTexture(currentTexture.name);
                            renderer.material.SetTexture(textureID, skinTexture.TextureData);

                            if (textureID == METAL_GLOSS_TEXTURE)
                            {
                                if (!TextureUtility.GetMaterialTexture(renderer, OCCLUSION_TEXTURE))
                                {
                                    renderer.material.SetTexture(OCCLUSION_TEXTURE, skinTexture.TextureData);
                                }
                            }
                        }
                        else if ((defaultSkin != null) && defaultSkin.ContainsTexture(currentTexture.name))
                        {
                            var skinTexture = defaultSkin.GetTexture(currentTexture.name);
                            renderer.material.SetTexture(textureID, skinTexture.TextureData);

                            if (textureID == METAL_GLOSS_TEXTURE)
                            {
                                if (!TextureUtility.GetMaterialTexture(renderer, OCCLUSION_TEXTURE))
                                {
                                    renderer.material.SetTexture(OCCLUSION_TEXTURE, skinTexture.TextureData);
                                }
                            }
                        }
                    }
                }
            }
        }

        #endregion

        //====================================================================================================
        #region Save/Load Methods

        public static JObject GetCarsSaveData()
        {
            JObject carsSaveData = new JObject();

            JObject[] array = new JObject[carGuidToAppliedSkinMap.Count];

            int i = 0;

            foreach ((string guid, string skinName) in carGuidToAppliedSkinMap)
            {
                JObject dataObject = new JObject();

                dataObject.SetString("guid", guid);
                dataObject.SetString("name", skinName);

                array[i] = dataObject;

                i++;
            }

            carsSaveData.SetJObjectArray("carsData", array);

            return carsSaveData;
        }

        public static void LoadCarsSaveData(JObject carsSaveData)
        {
            JObject[] jobjectArray = carsSaveData.GetJObjectArray("carsData");

            if (jobjectArray != null)
            {
                foreach (JObject jobject in jobjectArray)
                {
                    var guid = jobject.GetString("guid");
                    var name = jobject.GetString("name");

                    if (!carGuidToAppliedSkinMap.ContainsKey(guid))
                    {
                        carGuidToAppliedSkinMap.Add(guid, name);
                    }
                }
            }
        }

        #endregion
    }
}
