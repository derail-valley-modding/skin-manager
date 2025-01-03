using DV.Customization.Paint;
using HarmonyLib;

namespace SkinManagerMod
{
    [HarmonyPatch(typeof(CarSpawner))]
    internal static class CarSpawnerPatches
    {
        [HarmonyPatch(nameof(CarSpawner.BaseSpawn))]
        [HarmonyPostfix]
        private static void BaseSpawn(bool uniqueCar, TrainCar __result)
        {
            if (uniqueCar)
            {
                Main.LogVerbose($"Spawn unique {__result.ID}");
                return;
            }

            var skinName = SkinManager.GetCurrentCarSkin(__result);
            if (!string.IsNullOrEmpty(skinName))
            {
                // only need to replace textures if not staying with default skin
                SkinManager.ApplySkin(__result, skinName);
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
