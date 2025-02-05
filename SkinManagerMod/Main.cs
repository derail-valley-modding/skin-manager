using UnityModManagerNet;
using DV;
using DV.Localization;
using DV.ThingTypes;
using HarmonyLib;
using System.ComponentModel;
using System.IO;
using System.Reflection;
using UnityEngine;
using SMShared;
using DVLangHelper.Runtime;
using SkinManagerMod.Items;
using System.Collections;
using SkinManagerMod.Patches;

namespace SkinManagerMod
{
    public static class Main
    {
#nullable disable
        public static UnityModManager.ModEntry Instance { get; private set; }
        public static SkinManagerSettings Settings { get; private set; }
        public static TranslationInjector TranslationInjector { get; private set; }
        public static Harmony Harmony { get; private set; }
#nullable restore

        public static string ExportFolderPath => Path.Combine(Instance.Path, Constants.EXPORT_FOLDER_NAME);
        public static string GetExportFolderForCar(string carId)
        {
            return Path.Combine(ExportFolderPath, carId);
        }

        public static string CacheFolderPath => Path.Combine(Instance.Path, Constants.CACHE_FOLDER_NAME);

        public static bool Load(UnityModManager.ModEntry modEntry)
        {
            Instance = modEntry;
            Settings = UnityModManager.ModSettings.Load<SkinManagerSettings>(modEntry);

            TranslationInjector = new TranslationInjector(Constants.MOD_ID);
            TranslationInjector.AddTranslationsFromCsv(Path.Combine(Instance.Path, "translations.csv"));
            TranslationInjector.AddTranslationsFromWebCsv("https://docs.google.com/spreadsheets/d/1TrI4RuUgCijOuCjxM_WsOO9AV0BO4noTIZIzal3HbnY/export?format=csv&gid=1691364666");

            SkinProvider.CacheDefaultThemes();
            CarMaterialData.Initialize();
            if (!SkinProvider.Initialize())
            {
                Error("Failed to initialize skin manager");
                return false;
            }
            SkinManager.Initialize();

            UnloadWatcher.UnloadRequested += PaintFactory.DestroyInjectedShopData;

            Harmony = new Harmony(Constants.MOD_ID);
            Harmony.PatchAll(Assembly.GetExecutingAssembly());

            Instance.OnGUI = OnGUI;
            Instance.OnSaveGUI = OnSaveGUI;
            Instance.OnHideGUI = OnHideGUI;

            QualitySettings.anisotropicFiltering = AnisotropicFiltering.ForceEnable;
            return true;
        }

        #region Settings

        static Vector2 scrollViewVector = Vector2.zero;
        static TrainCarLivery? trainCarSelected = null;
        static bool showDropdown = false;

        private static string? _guiMessage;

        static void OnGUI(UnityModManager.ModEntry modEntry)
        {
            GUILayout.BeginVertical();

            Settings.alwaysAllowRadioReskin = GUILayout.Toggle(Settings.alwaysAllowRadioReskin, Translations.Settings.AlwaysAllowRadioReskin);
            Settings.allowPaintingUnowned = GUILayout.Toggle(Settings.allowPaintingUnowned, Translations.Settings.AllowPaintingUnowned);
            Settings.allowDE6SkinsForSlug = GUILayout.Toggle(Settings.allowDE6SkinsForSlug, Translations.Settings.AllowSlugDE6Skins);
            GUILayout.Space(5);

            bool newAniso = GUILayout.Toggle(Settings.aniso5, Translations.Settings.IncreaseAniso);
            if (newAniso != Settings.aniso5)
            {
                Settings.aniso5 = newAniso;
            }
            Settings.parallelLoading = GUILayout.Toggle(Settings.parallelLoading, Translations.Settings.ParallelLoading);
            Settings.verboseLogging = GUILayout.Toggle(Settings.verboseLogging, Translations.Settings.VerboseLogging);

            GUILayout.Label(Translations.Settings.DefaultSkinMode);

            string[] defaultSkinModeTexts = new string[]
            {
                Translations.DefaultSkinMode.PreferReskins,
                Translations.DefaultSkinMode.AllowForCustomCars,
                Translations.DefaultSkinMode.AllowForAllCars,
                Translations.DefaultSkinMode.PreferDefaults,
            };

            Settings.defaultSkinsMode = (DefaultSkinsMode)GUILayout.SelectionGrid((int)Settings.defaultSkinsMode, defaultSkinModeTexts, 1, "toggle");
            GUILayout.Space(5);

            // disable texture tools while exporting
            if (_exportAllCoro != null)
            {
                GUI.enabled = false;
            }

            GUILayout.Label(Translations.Settings.TextureTools);

            GUILayout.BeginVertical();

            string typeLabel = (trainCarSelected != null) ? LocalizationAPI.L(trainCarSelected.localizationKey) : Translations.Settings.SelectCarType;
            if (GUILayout.Button(typeLabel, GUILayout.Width(320)))
            {
                showDropdown = !showDropdown;
            }

            if (showDropdown)
            {
                scrollViewVector = GUILayout.BeginScrollView(scrollViewVector, GUILayout.Height(350));

                foreach (var livery in Globals.G.Types.Liveries)
                {
                    if (GUILayout.Button(LocalizationAPI.L(livery.localizationKey), GUILayout.Width(320)))
                    {
                        showDropdown = false;
                        trainCarSelected = livery;
                    }
                }

                GUILayout.EndScrollView();
            }

            GUILayout.EndVertical();

            if (trainCarSelected != null)
            {
                if (GUILayout.Button(Translations.Settings.ExportTextures, GUILayout.Width(400)))
                {
                    TextureUtility.DumpTextures(trainCarSelected);
                }

                if (GUILayout.Button(Translations.Settings.ReloadTextures, GUILayout.Width(400)))
                {
                    int reloadedCount = SkinProvider.ReloadSkinsForType(trainCarSelected);
                    _guiMessage = Translations.Settings.ReloadedCarType(reloadedCount, trainCarSelected.localizationKey);
                }
            }

            GUILayout.Space(5);
            if (GUILayout.Button(Translations.Settings.ExportAll, GUILayout.Width(400)))
            {
                _exportAllCoro = CoroutineManager.Instance.StartCoroutine(PerformMassExport());
            }

            if (GUILayout.Button(Translations.Settings.ReloadAll, GUILayout.Width(400)))
            {
                int reloadedCount = SkinProvider.ReloadAllSkins(true);
                _guiMessage = Translations.Settings.ReloadedAll(reloadedCount);
            }
            GUI.enabled = true;

            if (!string.IsNullOrEmpty(_guiMessage))
            {
                GUILayout.Space(2);
                GUILayout.Label(_guiMessage);
            }

            GUILayout.EndVertical();
        }

        private static Coroutine? _exportAllCoro = null;
        private static int _completedLiveryCount = 0;
        private static int _totalLiveryCount = 0;

        private static IEnumerator PerformMassExport()
        {
            _completedLiveryCount = 0;
            _totalLiveryCount = Globals.G.Types.Liveries.Count;

            foreach (var livery in Globals.G.Types.Liveries)
            {
                yield return null;
                TextureUtility.DumpTextures(livery);

                _completedLiveryCount++;
                _guiMessage = Translations.Settings.ExportedAll(_completedLiveryCount, _totalLiveryCount);
            }

            _exportAllCoro = null;
        }

        static void OnSaveGUI(UnityModManager.ModEntry modEntry)
        {
            Settings.Save(modEntry);
        }

        static void OnHideGUI(UnityModManager.ModEntry modEntry)
        {
            _guiMessage = null;
        }

        #endregion


        #region Logging

        public static void Log(string message)
        {
            Instance.Logger.Log(message);
        }

        public static void LogVerbose(string message)
        {
            if (Settings.verboseLogging)
            {
                Instance.Logger.Log(message);
            }
        }

        public static void Warning(string message)
        {
            Instance.Logger.Warning(message);
        }

        public static void Error(string message)
        {
            Instance.Logger.Error(message);
        }

        #endregion
    }

    public enum DefaultSkinsMode
    {
        PreferReplacements,
        AllowForCustomCars,
        AllowForAllCars,
        PreferDefaults,
    }

    public class SkinManagerSettings : UnityModManager.ModSettings
    {
        public bool alwaysAllowRadioReskin = true;
        public bool allowPaintingUnowned = true;
        public bool allowDE6SkinsForSlug = true;

        public bool aniso5 = true;
        public bool parallelLoading = true;
        public DefaultSkinsMode defaultSkinsMode = DefaultSkinsMode.AllowForCustomCars;
        public bool verboseLogging = false;

        public override void Save(UnityModManager.ModEntry modEntry)
        {
            Save(this, modEntry);
        }
    }
}
