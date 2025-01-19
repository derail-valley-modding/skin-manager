using DV.Interaction;
using HarmonyLib;

namespace SkinManagerMod.Items
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
    }
}
