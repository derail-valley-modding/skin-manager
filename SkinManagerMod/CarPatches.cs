using DV.Customization.Paint;
using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;

namespace SkinManagerMod
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
        private static void BaseSpawn(TrainCar __result)
        {
            var skinName = SkinManager.GetCurrentCarSkin(__result);
            if (!string.IsNullOrEmpty(skinName))
            {
                // only need to replace textures if not staying with default skin
                SkinManager.ApplySkin(__result, skinName);
            }
        }
    }

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

            SkinManager.SetAppliedCarSkin(trainCar, themeName);
        }
    }
}
