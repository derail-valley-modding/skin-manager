using DV.Customization.Paint;
using DV.ThingTypes;
using HarmonyLib;

namespace SkinManagerMod.Patches
{
    [HarmonyPatch(typeof(TrainCarPaint))]
    internal static class TrainCarPaintPatches
    {
        [HarmonyPatch(nameof(TrainCarPaint.IsSupported))]
        [HarmonyPrefix]
        private static bool IsSupportedOverride(TrainCarPaint __instance, PaintTheme theme, ref bool __result)
        {
            var train = TrainCar.Resolve(__instance.gameObject);

            if (theme is not CustomPaintTheme customTheme)
            {
                // default theme
                return (theme.name == SkinProvider.DefaultThemeName) || (theme.name == SkinProvider.DefaultNewThemeName) || (theme.name == SkinProvider.PrimerThemeName);
            }

            __result = customTheme.SupportsVehicle(train.carLivery);
            
            return false;
        }

        [HarmonyPatch(nameof(TrainCarPaint.UpdateTheme))]
        [HarmonyPrefix]
        private static bool UpdateThemeOverride(TrainCarPaint __instance)
        {
            if (!__instance.currentTheme) return false;

            __instance.hasChangedWhileDisabled = false;

            var train = TrainCar.Resolve(__instance.gameObject);
            PaintArea area = (__instance.targetArea == TrainCarPaint.Target.Interior) ? PaintArea.Interior : PaintArea.Exterior;

            if (__instance.currentTheme is not CustomPaintTheme theme)
            {
                // default theme
                string themeName = __instance.currentTheme.name;

                if ((themeName == SkinProvider.DefaultNewThemeName) && !SkinProvider.IsThemeable(train.carLivery))
                {
                    themeName = SkinProvider.DefaultThemeName;
                }
                theme = SkinProvider.GetTheme(themeName);
            }

            if (__instance.TargetArea == TrainCarPaint.Target.Interior)
            {
                if (train.loadedInterior)
                {
                    theme.Apply(train.loadedInterior, train);
                }
            }
            else
            {
                theme.Apply(__instance.gameObject, train);
            }
            SkinManager.SetAppliedCarSkin(train, theme.name, area);
            //Main.LogVerbose($"Applied {theme.name} to {train.ID} {area}");

            return false;
        }
    }
}
