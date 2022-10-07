using HarmonyLib;
using System.Reflection;
using UnityEngine;
using UnityModManagerNet;

namespace SkinManagerMod
{
    public static class Main
    {
        public static UnityModManager.ModEntry ModEntry { get; private set; }
        public static SkinManagerSettings Settings { get; private set; }

        static bool Load(UnityModManager.ModEntry modEntry)
        {
            ModEntry = modEntry;

            // Load the settings
            Settings = SkinManagerSettings.Load<SkinManagerSettings>(modEntry);

            CCLPatch.Initialize();
            if (!SkinManager.Initialize())
            {
                modEntry.Logger.Error("Failed to initialize skin manager");
                return false;
            }

            var harmony = new Harmony(modEntry.Info.Id);
            harmony.PatchAll(Assembly.GetExecutingAssembly());

            modEntry.OnGUI = OnGUI;
            modEntry.OnSaveGUI = OnSaveGUI;

            QualitySettings.anisotropicFiltering = AnisotropicFiltering.ForceEnable;

            return true;
        }

        static Vector2 scrollViewVector = Vector2.zero;
        static TrainCarType trainCarSelected = TrainCarType.NotSet;
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
            Settings.defaultSkinsMode = (SkinManagerSettings.DefaultSkinsMode)GUILayout.SelectionGrid((int)Settings.defaultSkinsMode, defaultSkinModeTexts, 1, "toggle");
            GUILayout.Space(2);

            GUILayout.Label("Texture Utility");

            GUILayout.BeginHorizontal(GUILayout.Width(250));

            GUILayout.BeginVertical();

            if (GUILayout.Button(trainCarSelected == TrainCarType.NotSet ? "Select Train Car" : SkinManager.EnabledCarTypes[trainCarSelected], GUILayout.Width(220)))
            {
                showDropdown = !showDropdown;
            }

            if (showDropdown)
            {
                scrollViewVector = GUILayout.BeginScrollView(scrollViewVector, GUILayout.Height(350));

                foreach (var entry in SkinManager.EnabledCarTypes)
                {
                    if (GUILayout.Button(entry.Value, GUILayout.Width(220)))
                    {
                        showDropdown = false;
                        trainCarSelected = entry.Key;
                    }
                }

                GUILayout.EndScrollView();
            }

            GUILayout.EndVertical();

            GUILayout.EndHorizontal();

            if (trainCarSelected != TrainCarType.NotSet)
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

        static void OnSaveGUI(UnityModManager.ModEntry modEntry)
        {
            Settings.Save(modEntry);
        }
    }


    // Mod settings
    public class SkinManagerSettings : UnityModManager.ModSettings
    {
        public enum DefaultSkinsMode
        {
            PreferReplacements,
            AllowForCustomCars,
            AllowForAllCars
        }

        public bool aniso5 = false;
        public bool parallelLoading = true;
        public DefaultSkinsMode defaultSkinsMode = DefaultSkinsMode.AllowForCustomCars;

        public override void Save(UnityModManager.ModEntry modEntry)
        {
            Save(this, modEntry);
        }
    }
}
