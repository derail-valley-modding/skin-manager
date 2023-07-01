using DV;
using DV.CabControls.Spec;
using DV.JObjectExtstensions;
using DV.ThingTypes;
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
        //private static Dictionary<TrainCarType, string> CustomCarTypes;

        /// <summary>Livery ID to SkinGroup mapping</summary>
        private static readonly Dictionary<string, SkinGroup> skinGroups = new Dictionary<string, SkinGroup>();
        public static IEnumerable<SkinGroup> AllSkinGroups => skinGroups.Values;

        /// <summary>Livery ID to default skin mapping</summary>
        private static readonly Dictionary<string, Skin> defaultSkins = new Dictionary<string, Skin>();

        private static readonly Dictionary<string, string> carGuidToAppliedSkinMap = new Dictionary<string, string>();

        private static Skin lastSteamerSkin;

        public static event Action<TrainCarLivery> SkinsLoaded;

        public static Skin FindSkinByName(TrainCarLivery carType, string name)
        {
            if (skinGroups.TryGetValue(carType.id, out var group))
            {
                return group.GetSkin(name);
            }
            return null;
        }

        public static List<Skin> GetSkinsForType(TrainCarLivery carType, bool includeDefault = true)
        {
            if (skinGroups.TryGetValue(carType.id, out var group))
            {
                var result = group.Skins;
                return includeDefault ? result.Append(defaultSkins[carType.id]).ToList() : result;
            }
            return includeDefault ? new List<Skin>() { defaultSkins[carType.id] } : new List<Skin>();
        }

        public static Skin GetNewSkin(TrainCarLivery carType)
        {
            if (CarTypes.IsTender(carType) && (lastSteamerSkin != null))
            {
                if (FindSkinByName(carType, lastSteamerSkin.Name) is Skin matchingTenderSkin)
                {
                    return matchingTenderSkin;
                }
            }

            // random skin
            if (skinGroups.TryGetValue(carType.id, out var group) && (group.Skins.Count > 0))
            {
                bool allowRandomDefault =
                    (Main.Settings.DefaultSkinsUsage.Value == DefaultSkinsMode.AllowForAllCars);
                    // || (CustomCarTypes.ContainsKey(carType) && (Main.Settings.defaultSkinsMode == SkinManagerSettings.DefaultSkinsMode.AllowForCustomCars));

                int nChoices = allowRandomDefault ? group.Skins.Count + 1 : group.Skins.Count;
                int choice = UnityEngine.Random.Range(0, nChoices);
                if (choice < group.Skins.Count)
                {
                    return group.Skins[choice];
                }
            }

            // fall back to default skin
            if (defaultSkins.TryGetValue(carType.id, out Skin skin))
            {
                return skin;
            }
            return null;
        }

        /// <summary>Get the currently assigned skin for given car, or a new one if none is assigned</summary>
        public static Skin GetCurrentCarSkin(TrainCar car)
        {
            if (carGuidToAppliedSkinMap.TryGetValue(car.CarGUID, out var skinName))
            {
                if (defaultSkins.TryGetValue(car.carLivery.id, out Skin defaultSkin) && (skinName == defaultSkin.Name))
                {
                    return defaultSkin;
                }

                if (FindSkinByName(car.carLivery, skinName) is Skin result)
                {
                    return result;
                }
            }

            return GetNewSkin(car.carLivery);
        }

        /// <summary>Save the </summary>
        private static void SetAppliedCarSkin(TrainCar car, Skin skin)
        {
            carGuidToAppliedSkinMap[car.CarGUID] = skin.Name;

            // TODO: support for CCL steam locos (this method only checks if == locosteamheavy)
            if (CarTypes.IsSteamLocomotive(car.carType))
            {
                lastSteamerSkin = skin;
            }
            else
            {
                lastSteamerSkin = null;
            }
        }

        //====================================================================================================
        #region Skin Loading

        public static bool Initialize()
        {
            //CustomCarTypes = new Dictionary<TrainCarType, string>(CCLPatch.CarList);
            LoadSkins();
            return true;
        }

        private static void LoadSkins()
        {
            foreach (var livery in Globals.G.Types.Liveries)
            {
                Skin defaultSkin = CreateDefaultSkin(livery);
                defaultSkins.Add(livery.id, defaultSkin);

                skinGroups[livery.id] = new SkinGroup(livery);

                LoadAllSkinsForType(livery);
            }

            SkinsLoaded?.Invoke(null);
        }

        /*
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
        */

        public static void ReloadSkins(TrainCarLivery livery)
        {
            skinGroups[livery.id] = new SkinGroup(livery);

            LoadAllSkinsForType(livery, true);

            /*
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
            */

            SkinsLoaded?.Invoke(livery);

            // reapply skins to any cars of this type
            var carsInScene = UnityEngine.Object.FindObjectsOfType<TrainCar>().Where(tc => tc.carLivery == livery);
            foreach (var car in carsInScene)
            {
                var toApply = GetCurrentCarSkin(car);

                if (toApply != null)
                {
                    ApplySkin(car, toApply);

                    if (car.IsInteriorLoaded)
                    {
                        ApplySkinToInterior(car, toApply);
                    }
                }
            }
        }

        private static void LoadAllSkinsForType(TrainCarLivery livery, bool forceSync = false)
        {
            string folderPath = Main.GetSkinFolder(livery.id);

            if (Directory.Exists(folderPath))
            {
                LoadSkinsFromFolder(folderPath, livery, forceSync);
            }
            else
            {
                // create default directories if not exist
                Directory.CreateDirectory(folderPath);
            }
            
            if (Remaps.OldCarTypeIDs.TryGetValue(livery.v1, out string overhauledId))
            {
                folderPath = Main.GetSkinFolder(overhauledId);

                if (Directory.Exists(folderPath))
                {
                    LoadSkinsFromFolder(folderPath, livery, forceSync);
                }
            }
        }

        private static void LoadSkinsFromFolder(string skinsFolder, TrainCarLivery carType, bool forceSync = false)
        {
            var skinGroup = skinGroups[carType.id];
            var renderers = TextureUtility.GetAllCarRenderers(carType);
            var textures = TextureUtility.GetRendererTextureNames(renderers);

            foreach (string subDir in Directory.GetDirectories(skinsFolder))
            {
                BeginLoadSkin(skinGroup, textures, subDir, forceSync);
            }
        }

        /// <summary>
        /// Create a skin containing the default/starting textures of a car
        /// </summary>
        private static Skin CreateDefaultSkin(TrainCarLivery carType)
        {
            GameObject carPrefab = carType.prefab;
            if (carPrefab == null) return null;

            string skinDir = null;
            //if (CCLPatch.IsCustomCarType(carType))
            //{
            //    skinDir = CCLPatch.GetCarFolder(carType);
            //}

            var defaultSkin = new Skin($"Default_{carType.id}", skinDir, isDefault: true);

            foreach (var texture in TextureUtility.EnumerateTextures(carType))
            {
                if (!defaultSkin.ContainsTexture(texture.name))
                {
                    defaultSkin.SkinTextures.Add(new SkinTexture(texture.name, texture));
                }
            }

            return defaultSkin;
        }

        private static bool TryGetTextureForFilename(TrainCarType carType, ref string filename, Dictionary<string, string> textureNames, out string textureProp)
        {
            if (textureNames.TryGetValue(filename, out textureProp))
            {
                return true;
            }
            
            if ((carType != TrainCarType.NotSet) && Remaps.TryGetUpdatedTextureName(carType, filename, out string newName))
            {
                if (textureNames.TryGetValue(newName, out textureProp))
                {
                    filename = newName;
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Create a skin from the given directory, load textures, and add it to the given group
        /// </summary>
        private static void BeginLoadSkin(SkinGroup skinGroup, Dictionary<string, string> textureNames, string subDir, bool forceSync = false)
        {
            var dirInfo = new DirectoryInfo(subDir);
            var files = dirInfo.GetFiles();
            var skin = new Skin(dirInfo.Name, subDir);

            var loading = new HashSet<string>();

            foreach (var file in files)
            {
                if (!StbImage.IsSupportedExtension(file.Extension))
                    continue;
                string fileName = Path.GetFileNameWithoutExtension(file.Name);

                if (!loading.Contains(fileName) && TryGetTextureForFilename(skinGroup.TrainCarType.v1, ref fileName, textureNames, out string textureProp))
                {
                    var linear = textureProp == "_BumpMap";
                    loading.Add(fileName);

                    if (!forceSync && Main.Settings.ParallelLoading.Value)
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

            skinGroup.Skins.Add(skin);
        }

        #endregion

        //====================================================================================================
        #region Skin Manipulation

        public static void ApplySkin(TrainCar trainCar, Skin skin)
        {
            if (skin == null) return;

            //Main.ModEntry.Logger.Log($"Applying skin {skin.Name} to car {trainCar.ID}");

            ApplySkin(trainCar.gameObject.transform, skin, defaultSkins[trainCar.carLivery.id]);
            if (trainCar.IsInteriorLoaded)
            {
                ApplySkinToInterior(trainCar, skin);
            }

            SetAppliedCarSkin(trainCar, skin);
        }

        public static void ApplySkinToInterior(TrainCar trainCar, Skin skin)
        {
            if (skin == null) return;

            ApplySkin(trainCar.interior, skin, defaultSkins[trainCar.carLivery.id]);
        }

        private static void ApplySkin(Transform objectRoot, Skin skin, Skin defaultSkin)
        {
            foreach (var renderer in objectRoot.GetComponentsInChildren<MeshRenderer>())
            {
                if (!renderer.material)
                {
                    continue;
                }

                TextureUtility.ApplyTextures(renderer, skin, defaultSkin);
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
