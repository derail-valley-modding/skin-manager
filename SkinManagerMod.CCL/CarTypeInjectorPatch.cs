using System.Collections.Generic;
using System.Linq;
using CCL.Importer;
using CCL.Importer.Types;
using DV.Customization.Paint;
using DV.ThingTypes;
using HarmonyLib;

namespace SkinManagerMod.CCL
{
    [HarmonyPatch(typeof(CarTypeInjector))]
    public static class CarTypeInjectorPatch
    {
        [HarmonyPatch(nameof(CarTypeInjector.RegisterCarType))]
        [HarmonyPostfix]
        public static void AfterRegisterCarType(CCL_CarType carType, bool __result)
        {
            if (__result)
            {
                foreach (var livery in carType.Variants)
                {
                    CheckCCLSubstitutionSupport(livery);
                }
            }
        }

        private static void CheckCCLSubstitutionSupport(TrainCarLivery cclLivery)
        {
            // build ccl material matching
            Main.LogVerbose($"Check CCL livery {cclLivery.id}");

            IEnumerable<TrainCarPaint.MaterialSet> sets = cclLivery.prefab.GetComponentsInChildren<TrainCarPaint>(true).SelectMany(tcp => tcp.sets);

            foreach (var customTheme in SkinProvider.AllThemes)
            {
                foreach (var materialSet in sets)
                {
                    if (!materialSet.OriginalMaterial) continue;

                    if (customTheme.TryGetCCLSubstitution(materialSet.OriginalMaterial, out var substitute))
                    {
                        customTheme.SupportedCCLIds.Add(cclLivery.id);
                    }
                }
            }
        }
    }
}