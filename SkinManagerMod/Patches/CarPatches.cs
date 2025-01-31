using DV.Customization.Paint;
using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;

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
                __result.gameObject.SetActive(false);

                var paintExt = __result.gameObject.AddComponent<TrainCarPaint>();
                paintExt.targetArea = TrainCarPaint.Target.Exterior;
                paintExt.currentTheme = SkinProvider.CustomDefaultTheme;
                __result.PaintExterior = paintExt;

                if (__result.carLivery.interiorPrefab)
                {
                    var paintInt = __result.gameObject.AddComponent<TrainCarPaint>();
                    paintInt.targetArea = TrainCarPaint.Target.Interior;
                    paintInt.currentTheme = SkinProvider.CustomDefaultTheme;
                    __result.PaintInterior = paintInt;
                }

                __result.gameObject.SetActive(true);
            }

            if (uniqueCar)
            {
                if (__result.PaintExterior)
                {
                    SkinManager.SetAppliedCarSkin(__result, __result.PaintExterior.CurrentTheme.name, PaintArea.Exterior);
                }
                if (__result.PaintInterior)
                {
                    SkinManager.SetAppliedCarSkin(__result, __result.PaintInterior.CurrentTheme.name, PaintArea.Interior);
                }
            }
            else
            {
                (string? exterior, string? interior) = SkinManager.GetCurrentCarSkin(__result);

                if (__result.PaintExterior && exterior is not null && SkinProvider.TryGetTheme(exterior, out var exteriorTheme))
                {
                    __result.PaintExterior.CurrentTheme = exteriorTheme;
                    SkinManager.SetAppliedCarSkin(__result, exterior, PaintArea.Exterior);
                }

                if (__result.PaintInterior && interior is not null && SkinProvider.TryGetTheme(interior, out var interiorTheme))
                {
                    __result.PaintInterior.CurrentTheme = interiorTheme;
                    SkinManager.SetAppliedCarSkin(__result, interior, PaintArea.Interior);
                }
            }
        }
    }
    /*
    [HarmonyPatch]
    class TrainCar_LoadInterior_Patch
    {
        static IEnumerable<MethodBase> TargetMethods()
        {
            yield return AccessTools.Method(typeof(TrainCar), nameof(TrainCar.LoadInterior));
            yield return AccessTools.Method(typeof(TrainCar), nameof(TrainCar.LoadExternalInteractables));
            yield return AccessTools.Method(typeof(TrainCar), nameof(TrainCar.LoadDummyExternalInteractables));
        }

        static void Postfix(TrainCar __instance)
        {
            if (SkinProvider.IsThemeable(__instance.carLivery)) return;

            var skinName = SkinManager.GetCurrentCarSkin(__instance);
            if (!string.IsNullOrEmpty(skinName))
            {
                SkinManager.ApplyNonThemeSkinToInterior(__instance, skinName);
            }
        }
    }

    [HarmonyPatch(typeof(TrainCarPaint))]
    internal static class TrainCarPaintPatch
    {
        [HarmonyPatch(nameof(TrainCarPaint.CurrentTheme), MethodType.Setter)]
        [HarmonyPostfix]
        public static void AfterCurrentThemeSet(TrainCarPaint __instance, PaintTheme ___currentTheme)
        {
            var trainCar = TrainCar.Resolve(__instance.gameObject);
            string themeName = ___currentTheme ? ___currentTheme.name : null;

            Main.Log($"Applying skin {themeName} to car {trainCar.ID} {__instance.TargetArea}");

            if (__instance.TargetArea != TrainCarPaint.Target.Exterior) return;

            PaintArea area = __instance.TargetArea.ToPaintArea();
            SkinManager.SetAppliedCarSkin(trainCar, themeName, area);
        }
    }
    */
}
