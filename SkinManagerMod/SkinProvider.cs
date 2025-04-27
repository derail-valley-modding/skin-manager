using DV;
using DV.Customization.Paint;
using DV.ThingTypes;
using DVLangHelper.Data;
using Newtonsoft.Json;
using SMShared;
using SMShared.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityModManagerNet;

namespace SkinManagerMod
{
    public class SkinProvider
    {
        public const string DefaultThemeName = "DVRT";
        public const string DefaultNewThemeName = "DVRT_New";
        public const string DemoThemeName = "Relic";
        public const string DemoRustyThemeName = "Relic_Rusty";
        public const string PrimerThemeName = "Null";

        public static readonly string[] BuiltInThemeNames = { DefaultThemeName, DefaultNewThemeName, DemoThemeName, DemoRustyThemeName, PrimerThemeName };

        private static readonly Dictionary<string, PaintTheme> _builtInThemeDict = new();

        public static IEnumerable<PaintTheme> BuiltInThemes => _builtInThemeDict.Values;
        public static PaintTheme PrimerTheme => _builtInThemeDict[PrimerThemeName];

        public static void CacheDefaultThemes()
        {
            foreach (string themeName in BuiltInThemeNames)
            {
                PaintTheme.TryLoad(themeName, out var builtin);
                _builtInThemeDict.Add(themeName, builtin);
            }
        }

        public static CustomPaintTheme CustomDefaultTheme => _themeDict[DefaultThemeName];

        public static BaseTheme GetThemeTypeByName(string themeName)
        {
            return themeName switch
            {
                DefaultNewThemeName => BaseTheme.Pristine,
                DemoThemeName => BaseTheme.Demonstrator,
                DemoRustyThemeName => BaseTheme.Relic,
                PrimerThemeName => BaseTheme.Primer,
                _ => BaseTheme.DVRT,
            };
        }

        public static CustomPaintTheme GetBaseTheme(BaseTheme themeType)
        {
            string themeName = (themeType & ~BaseTheme.DVRT_NoDetails) switch
            {
                BaseTheme.Pristine => DefaultNewThemeName,
                BaseTheme.Demonstrator => DemoThemeName,
                BaseTheme.Relic => DemoRustyThemeName,
                BaseTheme.Primer => PrimerThemeName,
                _ => DefaultThemeName,
            };

            return _themeDict[themeName];
        }

        public static bool IsBuiltInTheme(string themeName) => BuiltInThemeNames.Contains(themeName);
        public static bool IsBuiltInTheme(PaintTheme theme) => BuiltInThemeNames.Contains(theme.name);

        public static string? LastSteamerSkin { get; set; }
        public static string? LastDE6Skin { get; set; }

        /// <summary>Emitted when skin(s) are reloaded from disk
        public static event Action? SkinsLoaded;

        public static event Action<SkinConfig>? SkinDisabled;
        public static event Action<SkinConfig>? SkinUpdated;

        private static readonly LinkedList<ModSkinCollection> skinConfigs = new();

        /// <summary>Livery ID to SkinGroup mapping</summary>
        private static readonly Dictionary<string, SkinGroup> skinGroups = new();
        public static IEnumerable<SkinGroup> AllSkinGroups => skinGroups.Values;

        /// <summary>Livery ID to default skin mapping</summary>
        private static readonly Dictionary<string, Skin> defaultSkins = new();

        private static readonly Dictionary<TrainCarLivery, Dictionary<string, string>> cachedCarTextures = new();


        // Skin Name to Paint Theme (combined liveries)
        private static PaintTheme[]? _cachedThemeList = null;
        private static readonly Dictionary<string, CustomPaintTheme> _themeDict = new(StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<string, ThemeSettings> _themeSettings = new();
        

        private static readonly HashSet<string> _themeableLiveries = Globals.G.Types.Liveries
            .Where(l => l.prefab.GetComponentInChildren<TrainCarPaint>(true))
            .Select(l => l.id)
            .ToHashSet();

        public static bool IsThemeable(TrainCarLivery livery) => _themeableLiveries.Contains(livery.id);
        public static bool IsThemeable(string liveryId) => _themeableLiveries.Contains(liveryId);

        public static PaintTheme[] ModdedThemes
        {
            get
            {
                _cachedThemeList ??= _themeDict.Values.Where(t => !BuiltInThemeNames.Contains(t.name)).ToArray();
                return _cachedThemeList;
            }
        }

        public static List<PaintTheme> GetRandomizedStoreThemes()
        {
            var arr = ModdedThemes.Where(IsThemeAllowedInStore).ToList();

            // Fisher Yates shuffle
            for (int i = arr.Count - 1; i > 0; i--)
            {
                int j = UnityEngine.Random.Range(0, i);
                (arr[i], arr[j]) = (arr[j], arr[i]);
            }

            return arr;
        }

        public static bool IsThemeAllowedInStore(PaintTheme theme)
        {
            if (TryGetThemeSettings(theme.name, out var settings))
            {
                return !settings.HideFromStores;
            }
            return true;
        }

        public static bool TryGetTheme(string themeName, out CustomPaintTheme theme) => _themeDict.TryGetValue(themeName, out theme);
        public static CustomPaintTheme GetTheme(string themeName) => _themeDict[themeName];

        private static void RegisterNewTheme(CustomPaintTheme theme)
        {
            string key = theme.name.ToLower();
            if (PaintTheme.loadedThemes.ContainsKey(key))
            {
                Main.Error($"Skin \"{theme.name}\" conflicts with an existing or built-in paint theme");
                return;
            }

            _themeDict.Add(theme.name, theme);
            PaintTheme.loadedThemes.Add(key, theme);

            _cachedThemeList = null;
        }

        private static void UnregisterTheme(string themeName)
        {
            if (_themeDict.TryGetValue(themeName, out var theme))
            {
                UnityEngine.Object.Destroy(theme);
            }
            _themeDict.Remove(themeName);
            PaintTheme.loadedThemes.Remove(themeName.ToLower());

            _cachedThemeList = null;
        }

        public static bool TryGetThemeSettings(string themeName, out ThemeSettings themeSettings) =>
            _themeSettings.TryGetValue(themeName, out themeSettings);


        #region Provider Methods

        public static SkinGroup GetSkinGroup(TrainCarLivery livery)
        {
            if (skinGroups.TryGetValue(livery.id, out SkinGroup group))
            {
                return group;
            }
            
            var newGroup = new SkinGroup(livery);
            skinGroups[livery.id] = newGroup;
            return newGroup;
        }

        //public static Skin GetDefaultSkin(string carId, BaseTheme baseTheme = BaseTheme.DVRT)
        //{
        //    string key = $"{carId}_{baseTheme}";
        //    if (defaultSkins.TryGetValue(key, out Skin existing))
        //    {
        //        return existing;
        //    }

        //    var livery = Globals.G.Types.Liveries.First(l => l.id == carId);
        //    var newDefault = CreateDefaultSkin(livery, baseTheme);
        //    defaultSkins[carId] = newDefault;
        //    return newDefault;
        //}

        public static Skin? FindSkinByName(TrainCarLivery carType, string name) => FindSkinByName(carType.id, name);

        public static Skin? FindSkinByName(string carId, string name)
        {
            //if (name == Skin.GetDefaultSkinName(carId))
            //{
            //    return GetDefaultSkin(carId);
            //}

            if (skinGroups.TryGetValue(carId, out var group))
            {
                return group.GetSkin(name);
            }
            return null;
        }

        public static List<CustomPaintTheme> GetSkinsForType(TrainCarLivery carType, bool includeDefault = true, bool sort = true) =>
            GetSkinsForType(carType.id, includeDefault, sort);

        public static List<CustomPaintTheme> GetSkinsForType(string carId, bool includeDefault = true, bool sort = true)
        {
            var result = new List<string>();

            if (skinGroups.TryGetValue(carId, out var group))
            {
                result.AddRange(group.Skins.Select(s => s.Name));
            }

            if (Main.Settings.allowDE6SkinsForSlug && (carId == Constants.SLUG_LIVERY_ID))
            {
                if (skinGroups.TryGetValue(Constants.DE6_LIVERY_ID, out group))
                {
                    result.AddRange(group.Skins.Select(s => s.Name));
                }
            }

            if (includeDefault)
            {
                if (IsThemeable(carId) || (Main.Settings.allowDE6SkinsForSlug && (carId == Constants.SLUG_LIVERY_ID)))
                {
                    result.AddRange(BuiltInThemeNames);
                }
                else
                {
                    result.Add(DefaultThemeName);
                }
            }

            var uniqueThemes = result.Distinct().Select(name => _themeDict[name]).ToList();

            if (sort)
            {
                uniqueThemes.Sort(CompareThemes);
            }
            return uniqueThemes;
        }

        private static int CompareSkins(Skin a, Skin b)
        {
            return a.Name.CompareTo(b.Name);
        }

        private static int CompareThemes(PaintTheme a, PaintTheme b)
        {
            string aName = a.LocalizedName;
            if (string.IsNullOrEmpty(aName)) aName = a.AssetName;

            string bName = b.LocalizedName;
            if (string.IsNullOrEmpty(bName)) bName = b.AssetName;

            return aName.CompareTo(bName);
        }

        private static PaintTheme GetThemeForSkin(Skin skin) => _themeDict[skin.Name];

        public static string GetNewSkin(TrainCarLivery carType)
        {
            if (CarTypes.IsTender(carType) && (LastSteamerSkin != null))
            {
                if (FindSkinByName(carType, LastSteamerSkin) is not null)
                {
                    return LastSteamerSkin;
                }
            }

            if ((carType.id == Constants.SLUG_LIVERY_ID) && (LastDE6Skin != null))
            {
                if (FindSkinByName(carType, LastDE6Skin) is not null || Main.Settings.allowDE6SkinsForSlug)
                {
                    return LastDE6Skin;
                }
            }

            // random skin
            if (Main.Settings.defaultSkinsMode != DefaultSkinsMode.PreferDefaults)
            {
                var available = new List<Skin>();
                if (skinGroups.TryGetValue(carType.id, out var group))
                {
                    available.AddRange(group.Skins);
                }

                if ((carType.id == Constants.SLUG_LIVERY_ID) && Main.Settings.allowDE6SkinsForSlug && skinGroups.TryGetValue(Constants.DE6_LIVERY_ID, out group))
                {
                    var distinct = group.Skins.Where(s => !available.Any(a => a.Name == s.Name)).ToList();
                    available.AddRange(distinct);
                }

                if (available.Count > 0)
                {
                    bool allowRandomDefault = (Main.Settings.defaultSkinsMode == DefaultSkinsMode.AllowForAllCars);

                    var allowedRandom = available.Where(AllowRandomSpawning).ToList();
                    int nChoices = allowRandomDefault ? allowedRandom.Count + 1 : allowedRandom.Count;

                    int choice = UnityEngine.Random.Range(0, nChoices);
                    if (choice < allowedRandom.Count)
                    {
                        return allowedRandom[choice].Name;
                    }
                }
            }

            // fall back to default skin
            return DefaultThemeName;
        }

        private static bool AllowRandomSpawning(Skin skin)
        {
            if (TryGetThemeSettings(skin.Name, out var settings))
            {
                return !settings.PreventRandomSpawning;
            }
            return true;
        }

        #endregion

        //====================================================================================================
        #region Skin Loading

        public static bool Initialize()
        {
            InjectNewDefaultThemes();
            ReloadAllSkins();

            UnityModManager.toggleModsListen += HandleSkinModToggle;

            SkinsLoaded?.Invoke();

            return true;
        }

        private static bool IsPotentialSkinMod(UnityModManager.ModEntry mod)
        {
            return (mod.Info.Id != Constants.MOD_ID) && string.IsNullOrWhiteSpace(mod.Info.EntryMethod);
        }

        /// <summary>
        /// Synchronously reload all skins for all car types
        /// </summary>
        /// <returns>Number of skins reloaded</returns>
        public static int ReloadAllSkins(bool forceSync = false)
        {
            Main.LogVerbose("Reloading all skins");
            int loadedCount = 0;
            foreach (var mod in UnityModManager.modEntries.Where(IsPotentialSkinMod))
            {
                loadedCount += ReloadSkinMod(mod, forceSync);
            }

            loadedCount += LoadLegacySkins();
            return loadedCount;
        }

        /// <summary>
        /// Synchronously reload all skins for given car type
        /// </summary>
        /// <returns>Number of skins reloaded</returns>
        public static int ReloadSkinsForType(TrainCarLivery livery)
        {
            int reloadedCount = 0;

            foreach (var mod in UnityModManager.modEntries.Where(IsPotentialSkinMod))
            {
                var currentConfig = skinConfigs.FirstOrDefault(m => m.modEntry.Info == mod.Info);
                if ((currentConfig != null) && currentConfig.Any(c => c.CarId == livery.id))
                {
                    reloadedCount += ReloadSkinMod(mod, true);
                }
            }

            reloadedCount += ReloadLegacySkinsForType(livery);
            return reloadedCount;
        }

        /// <summary>
        /// Synchronously reload a single skin for a single car type
        /// </summary>
        public static void ReloadSkin(string liveryId, string skinName)
        {
            foreach (var collection in skinConfigs)
            {
                foreach (var config in collection)
                {
                    if (config.CarId == liveryId && config.Name == skinName)
                    {
                        if (config.Resources.Any())
                        {
                            ReloadSkinMod(collection.modEntry, true);
                            return;
                        }

                        UnloadSkin(liveryId, skinName);
                        BeginLoadSkin(config, true);
                        SkinUpdated?.Invoke(config);
                        return;
                    }
                }
            }
        }

        private static void UnloadSkin(string liveryId, string skinName)
        {
            var skinGroup = skinGroups[liveryId];
            if (skinGroup.Skins.Find(s => s.Name == skinName) is Skin existingSkin)
            {
                skinGroup.Skins.Remove(existingSkin);

                if (_themeDict.TryGetValue(skinName, out var theme))
                {
                    theme.RemoveSkin(liveryId);
                }
            }
        }

        private static void HandleSkinModToggle(UnityModManager.ModEntry mod, bool nowActive)
        {
            if (!IsPotentialSkinMod(mod)) return;

            Main.LogVerbose($"Detected mod enabled toggle: {mod.Info.Id} -> {nowActive}");
            if (nowActive)
            {
                ReloadSkinMod(mod, WorldStreamingInit.IsStreamingDone); // force synchronous if in-game
            }
            else
            {
                var currentConfig = skinConfigs.FirstOrDefault(m => m.modEntry.Info == mod.Info);

                if (currentConfig != null)
                {
                    skinConfigs.Remove(currentConfig);

                    foreach (var config in currentConfig)
                    {
                        UnloadSkin(config.CarId, config.Name);
                        SkinDisabled?.Invoke(config);
                    }
                }
            }
        }

        /// <summary>
        /// Load or reload all skins belonging to the given mod
        /// </summary>
        /// <returns>Number of skins loaded</returns>
        private static int ReloadSkinMod(UnityModManager.ModEntry mod, bool forceSync = false)
        {
            // get currently enabled skins
            var removedSkins = new LinkedList<SkinConfig>();
            var currentConfig = skinConfigs.FirstOrDefault(m => m.modEntry.Info == mod.Info);

            if (currentConfig != null)
            {
                skinConfigs.Remove(currentConfig);

                foreach (var config in currentConfig)
                {
                    UnloadSkin(config.CarId, config.Name);
                    removedSkins.AddLast(config);
                }
            }

            // get newly enabled skins
            var updatedSkins = new List<SkinConfig>();

            int loadedCount = 0;
            if (mod.Active)
            {
                var newConfig = new ModSkinCollection(mod);

                string themeConfigPath = Path.Combine(mod.Path, Constants.THEME_CONFIG_FILE);
                if (File.Exists(themeConfigPath))
                {
                    TryLoadThemeConfigFile(themeConfigPath);
                }

                // load common resources first
                foreach (string file in Directory.EnumerateFiles(mod.Path, Constants.SKIN_RESOURCE_FILE, SearchOption.AllDirectories))
                {
                    if (ResourcePack.LoadFromFile(file) is ResourcePack resourceConfig)
                    {
                        BeginLoadResources(resourceConfig, forceSync);
                        newConfig.ResourcePacks.Add(resourceConfig);
                    }
                }

                // then load all skin configs
                foreach (string file in Directory.EnumerateFiles(mod.Path, Constants.SKIN_CONFIG_FILE, SearchOption.AllDirectories))
                {
                    if (SkinConfig.LoadFromFile(file) is SkinConfig config)
                    {
                        newConfig.Configs.Add(config);

                        if (config.ResourceNames != null)
                        {
                            foreach (string name in config.ResourceNames)
                            {
                                if (newConfig.ResourcePacks.Find(p => p.Name == name) is ResourcePack pack)
                                {
                                    config.Resources.Add(pack);
                                }
                                else
                                {
                                    Main.Warning($"Failed to find resource with name \"{name}\" while loading {config.Name} for {config.CarId}");
                                }
                            }
                        }
                    }
                }

                foreach (var config in newConfig)
                {
                    BeginLoadSkin(config, forceSync);

                    // check if not removed, but updated
                    removedSkins.Remove(config);
                    updatedSkins.Add(config);
                }

                skinConfigs.AddLast(newConfig);
                loadedCount = newConfig.Configs.Count;
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

            return loadedCount;
        }

        private static void TryLoadThemeConfigFile(string configPath)
        {
            try
            {
                string contents = File.ReadAllText(configPath);
                var parsedConfig = JsonConvert.DeserializeObject<ThemeConfigJson>(contents);

                if ((parsedConfig!.Themes is null) || (parsedConfig.Themes.Length == 0))
                {
                    Main.Warning($"Found theme config file, but it is empty: {configPath}");
                    return;
                }

                if (string.IsNullOrEmpty(parsedConfig.Version) || !Version.TryParse(parsedConfig.Version, out var version))
                {
                    Main.Warning($"Theme config contains a missing or invalid version: {configPath}");
                    return;
                }

                foreach (var configItem in parsedConfig.Themes)
                {
                    if (string.IsNullOrEmpty(configItem.Name))
                    {
                        Main.Warning($"Theme config contains an invalid theme name: {configPath}");
                        continue;
                    }

                    var newSettings = ThemeSettings.Create(configPath, configItem, version);

                    if (TryGetThemeSettings(configItem.Name!, out var settings))
                    {
                        settings.Merge(newSettings);
                    }
                    else
                    {
                        _themeSettings.Add(configItem.Name!, newSettings);
                    }
                }
            }
            catch
            {
                Main.Error($"Failed to parse theme config, please check the syntax: {configPath}");
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
        private static void InjectNewDefaultThemes()
        {
            var primerTexture = (Texture2D)PrimerTheme.substitutions.First().substitute.mainTexture;

            foreach (string themeName in BuiltInThemeNames)
            {
                BaseTheme themeType = GetThemeTypeByName(themeName);
                var existingTheme = _builtInThemeDict[themeName];

                var customDefaultTheme = ScriptableObject.CreateInstance<CustomPaintTheme>();
                UnityEngine.Object.DontDestroyOnLoad(customDefaultTheme);
                customDefaultTheme.assetName = existingTheme.assetName;
                customDefaultTheme.name = existingTheme.name;
                customDefaultTheme.nameLocalizationKey = existingTheme.nameLocalizationKey;
                customDefaultTheme.isStrippedSurface = existingTheme.isStrippedSurface;
                customDefaultTheme.substitutions = existingTheme.substitutions;

                foreach (var carType in Globals.G.Types.Liveries)
                {
                    if ((themeName == DefaultThemeName) || IsThemeable(carType))
                    {
                        var defaultSkin = Skin.Default(carType.id, themeType);
                        customDefaultTheme.AddSkin(defaultSkin);
                    }
                    else if ((themeName == PrimerThemeName) && !IsThemeable(carType))
                    {
                        // extend primer coverage to all cars
                        var defaultSkin = Skin.Primer(carType.id, themeType);
                        string bodyName = DefaultTextures.GetBodyTextureName(carType.id);
                        defaultSkin.SkinTextures.Add(new SkinTexture(bodyName, primerTexture));
                        customDefaultTheme.AddSkin(defaultSkin);
                    }
                }

                _themeDict[themeName] = customDefaultTheme;
                //PaintTheme.loadedThemes[themeName.ToLower()] = customDefaultTheme;
            }
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

            if (Remaps.TryGetUpdatedTextureName(liveryId, filename, out string? newName))
            {
                if (textureNames.TryGetValue(newName!, out textureProp))
                {
                    filename = newName!;
                    return true;
                }
            }

            return false;
        }

        internal static void BeginLoadResources(ResourcePack config, bool forceSync = false)
        {
            if (forceSync || !Main.Settings.parallelLoading)
            {
                Main.LogVerbose($"Synchronous loading resource {config.Name} @ {config.FolderPath}");
            }
            else
            {
                Main.LogVerbose($"Async loading resource {config.Name} @ {config.FolderPath}");
            }

            var textureNames = GetCarTextureDictionary(config.Livery);

            // load skin files from directory
            var files = Directory.EnumerateFiles(config.FolderPath);
            foreach (var texturePath in files)
            {
                if (!Constants.IsSupportedExtension(Path.GetExtension(texturePath)))
                    continue;

                string fileName = Path.GetFileNameWithoutExtension(texturePath);

                if (TryGetTextureForFilename(config.CarId!, ref fileName, textureNames, out string textureProp))
                {
                    var linear = textureProp == "_BumpMap";

                    config.Textures.Add(BeginLoadTexture(fileName, config, texturePath, linear, forceSync));
                }
            }
        }

        /// <summary>
        /// Create a skin from the given directory, load textures, and add it to the given group
        /// </summary>
        /// <param name="forceSync">Force loading of texture files to finish before returning</param>
        internal static void BeginLoadSkin(SkinConfig config, bool forceSync = false)
        {
            if (forceSync || !Main.Settings.parallelLoading)
            {
                Main.LogVerbose($"Synchronous loading {config.Name} @ {config.FolderPath}");
            }
            else
            {
                Main.LogVerbose($"Async loading {config.Name} @ {config.FolderPath}");
            }

            var skin = Skin.Custom(config);
            config.Skin = skin;

            // find correct group, remove existing skin
            var skinGroup = GetSkinGroup(config.Livery);
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

                if (TryGetTextureForFilename(skinGroup.TrainCarType.id, ref fileName, textureNames, out string textureProp) && !loading.Contains(fileName))
                {
                    var linear = textureProp == "_BumpMap";
                    loading.Add(fileName);

                    skin.SkinTextures.Add(BeginLoadTexture(fileName, config, texturePath, linear, forceSync));
                }
            }

            // add resource files if not already included in skin
            foreach (var texture in config.Resources.SelectMany(x => x.Textures))
            {
                if (!skin.SkinTextures.Any(t => t.Name == texture.Name))
                {
                    skin.SkinTextures.Add(texture);
                }
            }

            skinGroup.Skins.Add(skin);

            if (!_themeDict.TryGetValue(skin.Name, out var theme))
            {
                Main.LogVerbose($"Create new theme {skin.Name}");
                theme = ScriptableObject.CreateInstance<CustomPaintTheme>();
                UnityEngine.Object.DontDestroyOnLoad(theme);
                theme.assetName = skin.Name;
                theme.name = skin.Name;

                theme.nameLocalizationKey = "mod/skins/" + skin.Name.Replace(' ', '_').ToLowerInvariant();
                Main.TranslationInjector.AddTranslation(theme.nameLocalizationKey, DVLanguage.English, skin.Name.Replace('_', ' '));

                RegisterNewTheme(theme);
            }
            else
            {
                Main.LogVerbose($"Merging into theme {skin.Name}");
            }

            theme.AddSkin(skin);
        }

        private static SkinTexture BeginLoadTexture(string fileName, ResourceConfigJson config, string texturePath, bool linear, bool forceSync)
        {
            var fileInfo = new FileInfo(texturePath);
            var modifiedTime = fileInfo.LastWriteTimeUtc;

            if (!forceSync && Main.Settings.parallelLoading)
            {
                return new SkinTexture(fileName, TextureLoader.LoadAsync(config, texturePath, linear), modifiedTime);
            }
            else
            {
                TextureLoader.BustCache(config, texturePath);
                return new SkinTexture(fileName, TextureLoader.LoadSync(config, texturePath, linear), modifiedTime);
            }
        }

        private static void AddSkinTexturesToTheme(Skin skin)
        {
            //var subs = skin.GetSubstitutions();

            if (_themeDict.TryGetValue(skin.Name, out var theme))
            {
                Main.LogVerbose($"Merging into theme {skin.Name}");
                //MergeSubstitutions(theme, subs);
                theme.AddSkin(skin);
            }
            else
            {
                Main.Error($"Couldn't find theme for skin {skin.Name}");
            }
        }

        #endregion


        //====================================================================================================
        #region Legacy Skins

        private static string OverhauledSkinFolder => Path.Combine(Main.Instance.Path, Constants.SKIN_FOLDER_NAME);
        private static string BepInExSkinFolder => Path.Combine(Main.Instance.Path, "..", "..", "BepInEx", "content", "skins");

        private static int LoadLegacySkins()
        {
            if (skinConfigs.FirstOrDefault(mc => mc.modEntry.Info == Main.Instance.Info) is ModSkinCollection existing)
            {
                skinConfigs.Remove(existing);
            }

            var result = new ModSkinCollection(Main.Instance);

            foreach (var livery in Globals.G.Types.Liveries)
            {
                result.Configs.AddRange(LoadAllSkinsForType(OverhauledSkinFolder, livery));
                result.Configs.AddRange(LoadAllSkinsForType(BepInExSkinFolder, livery));
            }

            skinConfigs.AddLast(result);
            return result.Configs.Count;
        }

        private static int ReloadLegacySkinsForType(TrainCarLivery livery)
        {
            var collection = skinConfigs.First(mc => mc.modEntry.Info == Main.Instance.Info);

            collection.Configs.RemoveAll(c => c.CarId == livery.id);
            int initialCount = collection.Configs.Count;

            collection.Configs.AddRange(LoadAllSkinsForType(OverhauledSkinFolder, livery));
            collection.Configs.AddRange(LoadAllSkinsForType(BepInExSkinFolder, livery));

            return collection.Configs.Count - initialCount;
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

            if (Remaps.TryGetOldTrainCarId(livery.id, out string? overhauledId))
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

                SkinUpdated?.Invoke(config);
                yield return config;
            }
        }

        #endregion
    }
}
