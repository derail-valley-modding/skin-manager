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

namespace SkinManagerMod
{
    public static class Main
    {
        public static UnityModManager.ModEntry Instance { get; private set; }
        public static SkinManagerSettings Settings { get; private set; }
        

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
            
            //CCLPatch.Initialize();
            if (!SkinProvider.Initialize())
            {
                Error("Failed to initialize skin manager");
                return false;
            }
            SkinManager.Initialize();

            var harmony = new Harmony(Constants.MOD_ID);
            harmony.PatchAll(Assembly.GetExecutingAssembly());

            Instance.OnGUI = OnGUI;
            Instance.OnSaveGUI = OnSaveGUI;

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

        static void OnGUI(UnityModManager.ModEntry modEntry)
        {
            GUILayout.BeginVertical();

            bool newAniso = GUILayout.Toggle(Settings.aniso5, "Increase Anisotropic Filtering (Requires Manual Game Restart)");
            if (newAniso != Settings.aniso5)
            {
                Settings.aniso5 = newAniso;
            }
            Settings.parallelLoading = GUILayout.Toggle(Settings.parallelLoading, "Multi-threaded texture loading");

            GUILayout.Label("Default skin usage:");
            Settings.defaultSkinsMode = (DefaultSkinsMode)GUILayout.SelectionGrid((int)Settings.defaultSkinsMode, defaultSkinModeTexts, 1, "toggle");
            GUILayout.Space(2);

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
                if (GUILayout.Button("Export Textures", GUILayout.Width(180)))
                {
                    TextureUtility.DumpTextures(trainCarSelected);
                }
            }

            if (GUILayout.Button("Reload Skins (Warning: Slow!)", GUILayout.Width(220)))
            {
                SkinProvider.ReloadAllSkins(true);
            }

            GUILayout.EndVertical();
        }

        static void OnSaveGUI(UnityModManager.ModEntry modEntry)
        {
            Settings.Save(modEntry);
        }

        public static void Log(string message)
        {
            Instance.Logger.Log(message);
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

        public override void Save(UnityModManager.ModEntry modEntry)
        {
            Save(this, modEntry);
        }
    }
}
