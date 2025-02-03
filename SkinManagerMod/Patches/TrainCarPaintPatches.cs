using DV.Customization.Paint;
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

            ReapplyThemes(train);

            var theme = GetCustomTheme(__instance, train);
            SkinManager.SetAppliedCarSkin(train, theme.name, area);

            return false;
        }

        public static void ReapplyThemes(TrainCar train)
        {
            var extTheme = GetCustomTheme(train.PaintExterior, train);
            if (!extTheme) return;

            var intTheme = train.PaintInterior ? GetCustomTheme(train.PaintInterior, train) : null;
            
            extTheme.Apply(train.gameObject, train);

            if (intTheme is not null)
            {
                if (train.loadedInterior)
                {
                    intTheme.Apply(train.loadedInterior, train);
                }
                if (train.interiorLOD)
                {
                    intTheme.Apply(train.interiorLOD.gameObject, train);
                }
            }

            if (train.loadedExternalInteractables)
            {
                extTheme.Apply(train.loadedExternalInteractables, train);
            }
            if (train.loadedDummyExternalInteractables)
            {
                extTheme.Apply(train.loadedDummyExternalInteractables, train);
            }
        }

        private static CustomPaintTheme GetCustomTheme(TrainCarPaint paint, TrainCar train)
        {
            if (!paint || !paint.currentTheme) return null!;

            if (paint.currentTheme is not CustomPaintTheme theme)
            {
                // default theme
                string themeName = paint.currentTheme.name;

                if ((themeName == SkinProvider.DefaultNewThemeName) && !SkinProvider.IsThemeable(train.carLivery))
                {
                    themeName = SkinProvider.DefaultThemeName;
                }
                theme = SkinProvider.GetTheme(themeName);
            }

            return theme;
        }
    }
}
