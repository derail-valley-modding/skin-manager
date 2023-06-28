using BepInEx;
using BepInEx.Configuration;
using DV;
using DV.Localization;
using DV.ThingTypes;
using HarmonyLib;
using System.ComponentModel;
using System.IO;
using System.Reflection;
using UnityEngine;

namespace SkinManagerMod
{
    internal static class PluginInfo
    {
        public const string Guid = "SkinManagerMod";
        public const string Name = "Skin Manager";
        public const string Version = "3.0.0";

        public const string ContentFolderName = "content";
        public const string SkinFolderName = "skins";
        public const string DefaultExportFolderName = "skin_export";
    }

    [BepInPlugin(PluginInfo.Guid, PluginInfo.Name, PluginInfo.Version)]
    public class Main : BaseUnityPlugin
    {
        public static Main Instance { get; private set; }
        public static SkinManagerSettings Settings { get; private set; }

        public static string SkinFolderPath { get; private set; }
        public static string GetSkinFolder(string carId)
        {
            return Path.Combine(SkinFolderPath, carId);
        }

        public static string ExportFolderPath => Settings.ExportPath.Value;
        public static string GetExportFolder(string carId)
        {
            return Path.Combine(ExportFolderPath, carId);
        }

        public void Awake()
        {
            Instance = this;
            SkinFolderPath = Path.Combine(Paths.BepInExRootPath, PluginInfo.ContentFolderName, PluginInfo.SkinFolderName);

            // Load the settings
            Settings = new SkinManagerSettings(this);

            //CCLPatch.Initialize();
            if (!SkinManager.Initialize())
            {
                Logger.LogError("Failed to initialize skin manager");
                enabled = false;
                return;
            }

            var harmony = new Harmony(PluginInfo.Guid);
            harmony.PatchAll(Assembly.GetExecutingAssembly());

            QualitySettings.anisotropicFiltering = AnisotropicFiltering.ForceEnable;
        }

        public static void Error(string message)
        {
            Instance.Logger.LogError(message);
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

    public class SkinManagerSettings
    {
        private const string DEFAULT_SECTION = "General";

        public readonly ConfigEntry<bool> ExtraAnisotropic;
        public readonly ConfigEntry<bool> ParallelLoading;
        public readonly ConfigEntry<DefaultSkinsMode> DefaultSkinsUsage;
        public readonly ConfigEntry<string> ExportPath;

        public SkinManagerSettings(Main plugin)
        {
            ExtraAnisotropic = plugin.Config.Bind(
                DEFAULT_SECTION, "IncreasedAniso", true, 
                "Increase Anisotropic Filtering (sharper textures from a distance)");

            ParallelLoading = plugin.Config.Bind(
                DEFAULT_SECTION, "ParallelLoading", true,
                "Multi-threaded texture loading");

            DefaultSkinsUsage = plugin.Config.Bind(
                DEFAULT_SECTION, "DefaultSkinsUsage", DefaultSkinsMode.AllowForCustomCars);

            string defaultExportPath = Path.Combine(Paths.BepInExRootPath, PluginInfo.ContentFolderName, PluginInfo.DefaultExportFolderName);
            var exportDescription = new ConfigDescription(
                "Directory for exported default textures", 
                null,
                new ConfigurationManagerAttributes { CustomDrawer = DrawExporter, Order = 9999 });

            ExportPath = plugin.Config.Bind(DEFAULT_SECTION, "ExportPath", defaultExportPath, exportDescription);
        }

        private static Vector2 scrollViewVector = Vector2.zero;
        private static TrainCarLivery trainCarSelected = null;
        private static bool showDropdown = false;

        private static void DrawExporter(ConfigEntryBase entry)
        {
            GUILayout.BeginVertical();

            GUILayout.Label(entry.BoxedValue.ToString(), GUILayout.ExpandWidth(true));

            GUILayout.BeginHorizontal(GUILayout.ExpandWidth(true));

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

                if (GUILayout.Button("Reload Skins", GUILayout.Width(180)))
                {
                    SkinManager.ReloadSkins(trainCarSelected);
                }
            }

            GUILayout.EndVertical();
        }
    }
}
