using DV;
using DV.ThingTypes;
using SMShared;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityModManagerNet;

namespace SkinManagerMod
{
    public class SkinProvider
    {
        public static Skin LastSteamerSkin { get; set; }

        /// <summary>Emitted when skin(s) are reloaded from disk
        public static event Action SkinsLoaded;

        public static event Action<SkinConfig> SkinDisabled;
        public static event Action<SkinConfig> SkinUpdated;

        private static readonly LinkedList<ModSkinCollection> skinConfigs = new LinkedList<ModSkinCollection>();

        /// <summary>Livery ID to SkinGroup mapping</summary>
        private static readonly Dictionary<string, SkinGroup> skinGroups = new Dictionary<string, SkinGroup>();
        public static IEnumerable<SkinGroup> AllSkinGroups => skinGroups.Values;

        /// <summary>Livery ID to default skin mapping</summary>
        private static readonly Dictionary<string, Skin> defaultSkins = new Dictionary<string, Skin>();

        private static readonly Dictionary<TrainCarLivery, Dictionary<string, string>> cachedCarTextures =
            new Dictionary<TrainCarLivery, Dictionary<string, string>>();


        #region Provider Methods

        public static bool TryGetDefaultSkin(string carId, out Skin skin) => defaultSkins.TryGetValue(carId, out skin);
        public static Skin GetDefaultSkin(string carId) => defaultSkins[carId];

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
            if (CarTypes.IsTender(carType) && (LastSteamerSkin != null))
            {
                if (FindSkinByName(carType, LastSteamerSkin.Name) is Skin matchingTenderSkin)
                {
                    return matchingTenderSkin;
                }
            }

            // random skin
            if (skinGroups.TryGetValue(carType.id, out var group) && (group.Skins.Count > 0))
            {
                bool allowRandomDefault =
                    (Main.Settings.defaultSkinsMode == DefaultSkinsMode.AllowForAllCars);
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

        #endregion

        //====================================================================================================
        #region Skin Loading

        public static bool Initialize()
        {
            foreach (var livery in Globals.G.Types.Liveries)
            {
                var defaultSkin = CreateDefaultSkin(livery);
                defaultSkins.Add(livery.id, defaultSkin);

                skinGroups[livery.id] = new SkinGroup(livery);
            }

            ReloadAllSkins();
            LoadLegacySkins();

            UnityModManager.toggleModsListen += HandleSkinModToggle;

            SkinsLoaded?.Invoke();

            return true;
        }

        public static void ReloadAllSkins(bool forceSync = false)
        {
            foreach (var mod in UnityModManager.modEntries)
            {
                ReloadSkinMod(mod, forceSync);
            }
        }

        /// <summary>
        /// Load all skin config files belonging to the given mod
        /// </summary>
        private static ModSkinCollection GetSkinConfigs(UnityModManager.ModEntry mod)
        {
            var result = new ModSkinCollection(mod);

            foreach (string file in Directory.EnumerateFiles(mod.Path, Constants.SKIN_CONFIG_FILE, SearchOption.AllDirectories))
            {
                if (SkinConfig.LoadFromFile(file) is SkinConfig config)
                {
                    result.Configs.Add(config);
                }
            }

            return result;
        }

        private static void HandleSkinModToggle(UnityModManager.ModEntry mod, bool nowActive)
        {
            if (nowActive)
            {
                ReloadSkinMod(mod, WorldStreamingInit.IsStreamingDone); // force synchronous if in-game
                SkinsLoaded?.Invoke();
            }
            else
            {
                var currentConfig = skinConfigs.FirstOrDefault(m => m.modEntry.Info == mod.Info);

                if (currentConfig != null)
                {
                    skinConfigs.Remove(currentConfig);

                    foreach (var config in currentConfig)
                    {
                        skinGroups[config.CarId].Skins.Remove(config.Skin);
                        SkinDisabled?.Invoke(config);
                    }
                }
            }
        }

        /// <summary>
        /// Load or reload all skins belonging to the given mod
        /// </summary>
        private static void ReloadSkinMod(UnityModManager.ModEntry mod, bool forceSync = false)
        {
            // get currently enabled skins
            var removedSkins = new LinkedList<SkinConfig>();
            var currentConfig = skinConfigs.FirstOrDefault(m => m.modEntry.Info == mod.Info);

            if (currentConfig != null)
            {
                skinConfigs.Remove(currentConfig);

                foreach (var config in currentConfig)
                {
                    skinGroups[config.CarId].Skins.Remove(config.Skin);
                    removedSkins.AddLast(config);
                }
            }

            // get newly enabled skins
            var updatedSkins = new List<SkinConfig>();

            if (mod.Active)
            {
                var newConfig = GetSkinConfigs(mod);

                foreach (var config in newConfig)
                {
                    BeginLoadSkin(config, forceSync);

                    // check if not removed, but updated
                    if (removedSkins.Remove(config))
                    {
                        updatedSkins.Add(config);
                    }
                }

                skinConfigs.AddLast(newConfig);
            }

            // emit events
            foreach (var config in removedSkins)
            {
                SkinDisabled?.Invoke(config);
            }

            foreach (var config in updatedSkins)
            {
                SkinUpdated?.Invoke(config);
            }
        }

        private static Dictionary<string, string> GetCarTextureDictionary(TrainCarLivery carType)
        {
            if (!cachedCarTextures.TryGetValue(carType, out var textures))
            {
                var renderers = TextureUtility.GetAllCarRenderers(carType);
                textures = TextureUtility.GetRendererTextureNames(renderers);
                cachedCarTextures.Add(carType, textures);
            }
            return textures;
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

        /// <summary>
        /// Try to find the car texture/material property that matches the given file
        /// </summary>
        /// <param name="liveryId">Used to find remaps for legacy texture names</param>
        /// <param name="filename">Original image file name, will be replaced with updated name if remap is found</param>
        /// <param name="textureNames">Dict of car texture names and material properties</param>
        /// <param name="textureProp">Resultant material property for this file</param>
        /// <returns>True if a matching texture was found</returns>
        private static bool TryGetTextureForFilename(string liveryId, ref string filename, Dictionary<string, string> textureNames, out string textureProp)
        {
            if (textureNames.TryGetValue(filename, out textureProp))
            {
                return true;
            }

            if (Remaps.TryGetUpdatedTextureName(liveryId, filename, out string newName))
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
        /// <param name="forceSync">Force loading of texture files to finish before returning</param>
        internal static void BeginLoadSkin(SkinConfig config, bool forceSync = false)
        {
            if (forceSync || !Main.Settings.parallelLoading)
            {
                Main.Log($"Synchronous loading {config.Name} @ {config.FolderPath}");
            }
            else
            {
                Main.Log($"Async loading {config.Name} @ {config.FolderPath}");
            }

            var skin = new Skin(config.Name, config.FolderPath);
            config.Skin = skin;

            // find correct group, remove existing skin
            var skinGroup = skinGroups[config.Livery.id];
            if (skinGroup.Skins.Find(s => s.Name == config.Name) is Skin existingSkin)
            {
                skinGroup.Skins.Remove(existingSkin);
            }

            var textureNames = GetCarTextureDictionary(config.Livery);

            var loading = new HashSet<string>();

            // load skin files from directory
            var files = Directory.EnumerateFiles(config.FolderPath);
            foreach (var texturePath in files)
            {
                if (!Constants.IsSupportedExtension(Path.GetExtension(texturePath)))
                    continue;

                string fileName = Path.GetFileNameWithoutExtension(texturePath);

                if (!loading.Contains(fileName) && TryGetTextureForFilename(skinGroup.TrainCarType.id, ref fileName, textureNames, out string textureProp))
                {
                    var linear = textureProp == "_BumpMap";
                    loading.Add(fileName);

                    if (!forceSync && Main.Settings.parallelLoading)
                    {
                        skin.SkinTextures.Add(new SkinTexture(fileName, TextureLoader.LoadAsync(config, texturePath, linear)));
                    }
                    else
                    {
                        TextureLoader.BustCache(config, texturePath);
                        skin.SkinTextures.Add(new SkinTexture(fileName, TextureLoader.LoadSync(config, texturePath, linear)));
                    }
                }
            }

            skinGroup.Skins.Add(skin);
        }

        #endregion


        //====================================================================================================
        #region Legacy Skins

        private static string OverhauledSkinFolder => Path.Combine(Main.Instance.Path, Constants.SKIN_FOLDER_NAME);
        private static string BepInExSkinFolder => Path.Combine(Environment.CurrentDirectory, "BepInEx", "content", "skins");

        private static void LoadLegacySkins()
        {
            var result = new ModSkinCollection(Main.Instance);

            foreach (var livery in Globals.G.Types.Liveries)
            {
                result.Configs.AddRange(LoadAllSkinsForType(OverhauledSkinFolder, livery));
                result.Configs.AddRange(LoadAllSkinsForType(BepInExSkinFolder, livery));
            }

            skinConfigs.AddLast(result);
        }

        private static IEnumerable<SkinConfig> LoadAllSkinsForType(string parentFolder, TrainCarLivery livery)
        {
            string folderPath = Path.Combine(parentFolder, livery.id);

            if (Directory.Exists(folderPath))
            {
                foreach (var skin in LoadSkinsFromFolder(folderPath, livery))
                {
                    yield return skin;
                }
            }

            if (Remaps.TryGetOldTrainCarId(livery.id, out string overhauledId))
            {
                folderPath = Path.Combine(parentFolder, overhauledId);

                if (Directory.Exists(folderPath))
                {
                    foreach (var skin in LoadSkinsFromFolder(folderPath, livery))
                    {
                        yield return skin;
                    }
                }
            }
        }

        private static IEnumerable<SkinConfig> LoadSkinsFromFolder(string skinsFolder, TrainCarLivery carType)
        {
            foreach (string subDir in Directory.EnumerateDirectories(skinsFolder))
            {
                string name = Path.GetFileName(subDir);
                var config = new SkinConfig(name, subDir, carType);
                BeginLoadSkin(config, false);

                yield return config;
            }
        }

        #endregion
    }
}
