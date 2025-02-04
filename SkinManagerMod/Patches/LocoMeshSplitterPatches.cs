using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace SkinManagerMod.Patches
{
    [HarmonyPatch]
    internal static class LocoMeshSplitterPatches
    {
        private static Type _paintSetupClass = AccessTools.TypeByName("LocoMeshSplitter.MeshLoaders.TrainCarPaintSetup");

        public static bool Prepare()
        {
            return (_paintSetupClass is not null);
        }

        public static IEnumerable<MethodBase> TargetMethods()
        {
            if (_paintSetupClass is not null)
            {
                var methods = AccessTools.GetDeclaredMethods(_paintSetupClass);
                foreach (var method in methods)
                {
                    yield return method;
                }
            }
        }

        [HarmonyPrefix]
        public static bool SkipMethod()
        {
            return false;
        }
    }
}
