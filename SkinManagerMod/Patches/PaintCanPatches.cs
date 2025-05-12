using DV.Customization.Paint;
using DV.Interaction;
using DV.ThingTypes;
using HarmonyLib;
using UnityEngine;

namespace SkinManagerMod.Patches
{
    [HarmonyPatch(typeof(PaintCan))]
    internal static class PaintCanPatches
    {
        [HarmonyPatch(nameof(PaintCan.CheckPaintApplicationValidity))]
        [HarmonyPrefix]
        public static void CheckPaintValidity(ref bool isCareerMode)
        {
            if (isCareerMode && Main.Settings.allowPaintingUnowned)
            {
                isCareerMode = false;
            }
        }

        [HarmonyPatch(nameof(PaintCan.CheckPaintApplicationValidity))]
        [HarmonyPostfix]
        public static void FixAlreadyPaintedValidity(PaintCan __instance, ref PaintCan.Validity __result, PaintTheme themeFrom, TrainCar target)
        {
            if (__instance.theme.isStrippedSurface && themeFrom.IsStrippedSurface)
            {
                __result = PaintCan.Validity.AlreadyPainted;
                return;
            }

            if (__instance.theme is CustomPaintTheme customTheme)
            {
                if (!customTheme.SupportsVehicle(target.carLivery))
                {
                    __result = PaintCan.Validity.Incompatible;
                    return;
                }
            }

            if ((__result == PaintCan.Validity.Incompatible) && themeFrom)
            {
                if (CarTypes.IsRegularCar(target.carLivery) || themeFrom.IsStrippedSurface)
                {
                    __result = PaintCan.Validity.Ok;
                }
                else
                {
                    __result = PaintCan.Validity.AlreadyPainted;
                }
            }
        }
    }
}
