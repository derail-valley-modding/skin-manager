using DV.Customization.Paint;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace SkinManagerMod.Patches
{
    [HarmonyPatch]
    internal static class CarSpawnerPatches
    {
        static IEnumerable<MethodBase> TargetMethods()
        {
            yield return AccessTools.Method(typeof(CarSpawner), nameof(CarSpawner.SpawnCar));
            yield return AccessTools.Method(typeof(CarSpawner), nameof(CarSpawner.SpawnLoadedCar));
        }

        [HarmonyPostfix]
        private static void BaseSpawn(TrainCar __result, bool uniqueCar)
        {
            if (!__result.PaintExterior)
            {
                var paintExt = __result.gameObject.AddComponent<TrainCarPaint>();
                paintExt.sets = Array.Empty<TrainCarPaint.MaterialSet>();
                paintExt.targetArea = TrainCarPaint.Target.Exterior;
                paintExt.currentTheme = SkinProvider.CustomDefaultTheme;
                paintExt.OriginallyAssignedTheme = SkinProvider.CustomDefaultTheme;
                __result.PaintExterior = paintExt;

                if (__result.carLivery.interiorPrefab)
                {
                    var paintInt = __result.gameObject.AddComponent<TrainCarPaint>();
                    paintInt.sets = Array.Empty<TrainCarPaint.MaterialSet>();
                    paintInt.targetArea = TrainCarPaint.Target.Interior;
                    paintInt.currentTheme = SkinProvider.CustomDefaultTheme;
                    paintInt.OriginallyAssignedTheme = SkinProvider.CustomDefaultTheme;
                    __result.PaintInterior = paintInt;
                }
            }

            if (!uniqueCar)
            {
                (string? exterior, string? interior) = SkinManager.GetCurrentCarSkin(__result);

                if (__result.PaintInterior && interior is not null && SkinProvider.TryGetTheme(interior, out var interiorTheme))
                {
                    __result.PaintInterior.CurrentTheme = interiorTheme;
                }

                if (__result.PaintExterior && exterior is not null && SkinProvider.TryGetTheme(exterior, out var exteriorTheme))
                {
                    __result.PaintExterior.CurrentTheme = exteriorTheme;
                }
            }
        }
    }

    [HarmonyPatch(typeof(TrainCar))]
    internal static class TrainCarPatches
    {
        [HarmonyPatch(nameof(TrainCar.InitializeObjectPaint))]
        [HarmonyPrefix]
        public static void BeforeInitializePaint(GameObject obj)
        {
            if (obj.GetComponent<TrainCar>()) return;

            foreach (var paint in obj.GetComponents<TrainCarPaint>())
            {
                UnityEngine.Object.Destroy(paint);
            }
        }

        [HarmonyPatch(nameof(TrainCar.InitializeObjectPaint))]
        [HarmonyPostfix]
        public static void AfterInitializePaint(TrainCar __instance)
        {
            TrainCarPaintPatches.ReapplyThemes(__instance);
        }
    }
}
