﻿using UnityModManagerNet;
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

namespace SkinManagerMod
{
    public static class Main
    {
        public static UnityModManager.ModEntry Instance { get; private set; }
        public static SkinManagerSettings Settings { get; private set; }
        public static TranslationInjector TranslationInjector { get; private set; }

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

            CarMaterialData.Initialize();
            if (!SkinProvider.Initialize())
            {
                Error("Failed to initialize skin manager");
                return false;
            }
            SkinManager.Initialize();

            UnloadWatcher.UnloadRequested += PaintFactory.DestroyInjectedShopData;

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

        private static string _guiMessage;

        static void OnGUI(UnityModManager.ModEntry modEntry)
        {
            GUILayout.BeginVertical();

            bool newAniso = GUILayout.Toggle(Settings.aniso5, Translations.Settings.IncreaseAniso);
            if (newAniso != Settings.aniso5)
            {
                Settings.aniso5 = newAniso;
            }
            Settings.parallelLoading = GUILayout.Toggle(Settings.parallelLoading, Translations.Settings.ParallelLoading);
            Settings.allowDE6SkinsForSlug = GUILayout.Toggle(Settings.allowDE6SkinsForSlug, Translations.Settings.AllowSlugDE6Skins);
            Settings.allowPaintingUnowned = GUILayout.Toggle(Settings.allowPaintingUnowned, Translations.Settings.AllowPaintingUnowned);
            Settings.verboseLogging = GUILayout.Toggle(Settings.verboseLogging, Translations.Settings.VerboseLogging);

            GUILayout.Label(Translations.Settings.DefaultSkinMode);

            string[] defaultSkinModeTexts = new string[]
            {
                Translations.DefaultSkinMode.PreferReskins,
                Translations.DefaultSkinMode.AllowForCustomCars,
                Translations.DefaultSkinMode.AllowForAllCars,
            };

            Settings.defaultSkinsMode = (DefaultSkinsMode)GUILayout.SelectionGrid((int)Settings.defaultSkinsMode, defaultSkinModeTexts, 1, "toggle");
            GUILayout.Space(5);

            GUILayout.Label(Translations.Settings.TextureTools);

            GUILayout.BeginHorizontal(GUILayout.Width(250));

            GUILayout.BeginVertical();

            string typeLabel = (trainCarSelected != null) ? LocalizationAPI.L(trainCarSelected.localizationKey) : Translations.Settings.SelectCarType;
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
                if (GUILayout.Button(Translations.Settings.ExportTextures, GUILayout.Width(300)))
                {
                    TextureUtility.DumpTextures(trainCarSelected);
                }

                if (GUILayout.Button(Translations.Settings.ReloadTextures, GUILayout.Width(300)))
                {
                    int reloadedCount = SkinProvider.ReloadSkinsForType(trainCarSelected);
                    _guiMessage = Translations.Settings.ReloadedCarType(reloadedCount, trainCarSelected.localizationKey);
                }
            }

            GUILayout.Space(5);
            if (GUILayout.Button(Translations.Settings.ExportAll, GUILayout.Width(300)))
            {
                foreach (var livery in Globals.G.Types.Liveries)
                {
                    TextureUtility.DumpTextures(livery);
                }
            }

            if (GUILayout.Button(Translations.Settings.ReloadAll, GUILayout.Width(300)))
            {
                int reloadedCount = SkinProvider.ReloadAllSkins(true);
                _guiMessage = Translations.Settings.ReloadedAll(reloadedCount);
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
        PreferReplacements,
        AllowForCustomCars,
        AllowForAllCars
    }

    public class SkinManagerSettings : UnityModManager.ModSettings
    {
        public bool aniso5 = true;
        public bool parallelLoading = true;
        public DefaultSkinsMode defaultSkinsMode = DefaultSkinsMode.AllowForCustomCars;
        public bool allowPaintingUnowned = true;
        public bool allowDE6SkinsForSlug = true;
        public bool verboseLogging = false;

        public override void Save(UnityModManager.ModEntry modEntry)
        {
            Save(this, modEntry);
        }
    }
}
