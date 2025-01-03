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

namespace SkinManagerMod
{
    public static class Main
    {
        public static UnityModManager.ModEntry Instance { get; private set; }
        public static SkinManagerSettings Settings { get; private set; }
        public static TranslationInjector Translations { get; private set; }

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
            Translations = new TranslationInjector(Constants.MOD_ID);

            //CCLPatch.Initialize();
            CarMaterialData.Initialize();
            if (!SkinProvider.Initialize())
            {
                Error("Failed to initialize skin manager");
                return false;
            }

            var harmony = new Harmony(Constants.MOD_ID);
            harmony.PatchAll(Assembly.GetExecutingAssembly());

            Instance.OnGUI = OnGUI;
            Instance.OnSaveGUI = OnSaveGUI;
            Instance.OnHideGUI = OnHideGUI;

            QualitySettings.anisotropicFiltering = AnisotropicFiltering.ForceEnable;
            return true;
        }

        static Vector2 scrollViewVector = Vector2.zero;
        static TrainCarLivery trainCarSelected = null;
        static bool showDropdown = false;

        private static readonly string[] defaultSkinModeTexts = new[]
        {
            "Prefer Reskins",
            "Random For Custom Cars",
            "Random For All Cars"
        };

        private static string _guiMessage;

        static void OnGUI(UnityModManager.ModEntry modEntry)
        {
            GUILayout.BeginVertical();

            bool newAniso = GUILayout.Toggle(Settings.aniso5, "Increase Anisotropic Filtering (Requires Manual Game Restart)");
            if (newAniso != Settings.aniso5)
            {
                Settings.aniso5 = newAniso;
            }
            Settings.parallelLoading = GUILayout.Toggle(Settings.parallelLoading, "Multi-threaded texture loading");
            Settings.allowDE6SkinsForSlug = GUILayout.Toggle(Settings.allowDE6SkinsForSlug, "Allow DE6 skins to be applied to slug");
            Settings.verboseLogging = GUILayout.Toggle(Settings.verboseLogging, "Log extra skin loading information");

            GUILayout.Label("Default skin usage:");
            Settings.defaultSkinsMode = (DefaultSkinsMode)GUILayout.SelectionGrid((int)Settings.defaultSkinsMode, defaultSkinModeTexts, 1, "toggle");
            GUILayout.Space(5);

            GUILayout.Label("Texture Utility");

            GUILayout.BeginHorizontal(GUILayout.Width(250));

            GUILayout.BeginVertical();

            string typeLabel = (trainCarSelected != null) ? LocalizationAPI.L(trainCarSelected.localizationKey) : "Select Train Car";
            if (GUILayout.Button(typeLabel, GUILayout.Width(220)))
            {
                showDropdown = !showDropdown;
            }

            if (showDropdown)
            {
                scrollViewVector = GUILayout.BeginScrollView(scrollViewVector, GUILayout.Height(350));

                foreach (var livery in Globals.G.Types.Liveries)
                {
                    if (GUILayout.Button(LocalizationAPI.L(livery.localizationKey), GUILayout.Width(220)))
                    {
                        showDropdown = false;
                        trainCarSelected = livery;
                    }
                }

                GUILayout.EndScrollView();
            }

            GUILayout.EndVertical();

            GUILayout.EndHorizontal();

            if (trainCarSelected != null)
            {
                if (GUILayout.Button("Export Textures", GUILayout.Width(300)))
                {
                    TextureUtility.DumpTextures(trainCarSelected);
                }

                if (GUILayout.Button("Reload Skins for Selected Type", GUILayout.Width(300)))
                {
                    int reloadedCount = SkinProvider.ReloadSkinsForType(trainCarSelected);
                    _guiMessage = $"Reloaded {reloadedCount} skins for car {trainCarSelected.id}";
                }
            }

            GUILayout.Space(5);
            if (GUILayout.Button("Export All Textures (Warning: Slow!)", GUILayout.Width(300)))
            {
                foreach (var livery in Globals.G.Types.Liveries)
                {
                    TextureUtility.DumpTextures(livery);
                }
            }

            if (GUILayout.Button("Reload All Skins (Warning: Slow!)", GUILayout.Width(300)))
            {
                int reloadedCount = SkinProvider.ReloadAllSkins(true);
                _guiMessage = $"Reloaded {reloadedCount} skins";
            }

            if (!string.IsNullOrEmpty(_guiMessage))
            {
                GUILayout.Space(2);
                GUILayout.Label(_guiMessage);
            }

            GUILayout.EndVertical();
        }

        static void OnSaveGUI(UnityModManager.ModEntry modEntry)
        {
            Settings.Save(modEntry);
        }

        static void OnHideGUI(UnityModManager.ModEntry modEntry)
        {
            _guiMessage = null;
        }

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
    }

    public enum DefaultSkinsMode
    {
        [Description("Prefer Reskins")]
        PreferReplacements,

        [Description("Random For Custom Cars")]
        AllowForCustomCars,

        [Description("Random For All Cars")]
        AllowForAllCars
    }

    public class SkinManagerSettings : UnityModManager.ModSettings
    {
        public bool aniso5 = true;
        public bool parallelLoading = true;
        public DefaultSkinsMode defaultSkinsMode = DefaultSkinsMode.AllowForCustomCars;
        public bool allowDE6SkinsForSlug = true;
        public bool verboseLogging = false;

        public override void Save(UnityModManager.ModEntry modEntry)
        {
            Save(this, modEntry);
        }
    }
}
