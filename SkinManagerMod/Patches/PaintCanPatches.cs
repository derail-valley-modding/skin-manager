using DV.Customization.Paint;
using DV.Interaction;
using DV.ThingTypes;
using HarmonyLib;

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
        public static void FixAlreadyPaintedValidity(ref PaintCan.Validity __result, PaintTheme themeFrom, TrainCar target)
        {
            if ((__result == PaintCan.Validity.Incompatible) && themeFrom)
            {
                if (CarTypes.IsRegularCar(target.carLivery))
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
