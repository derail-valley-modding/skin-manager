using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using static UnityModManagerNet.UnityModManager;

namespace SkinManagerMod.Patches
{
    internal static class LocoMeshSplitterPatches
    {
        private const string LMS_ModID = "LocoMeshSplitter";

        public static void Initialize()
        {
            if ((FindMod(LMS_ModID) is ModEntry lms) && lms.Active)
            {
                DoPatching();
            }
            else
            {
                toggleModsListen += OnModToggle;
            }
        }

        public static void OnModToggle(ModEntry modEntry, bool enabled)
        {
            if (!enabled || (modEntry?.Info.Id != LMS_ModID)) return;

            DoPatching();
        }

        private static void DoPatching()
        {
            Type _paintSetupClass = AccessTools.TypeByName("LocoMeshSplitter.MeshLoaders.TrainCarPaintSetup");

            var methods = _paintSetupClass.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);

            foreach (var target in methods)
            {
                Main.Log($"Patch {_paintSetupClass.FullName}.{target.Name}");
                Main.Harmony.Patch(target, new HarmonyMethod(typeof(LocoMeshSplitterPatches), nameof(SkipMethod)));
            }
        }

        public static bool SkipMethod()
        {
            return false;
        }
    }
}
