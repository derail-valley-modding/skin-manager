using System;
using System.Collections.Generic;
using System.Linq;
using CCL.Importer;
using DV.Customization.Paint;
using DV.ThingTypes;

namespace SkinManagerMod.CCL
{
    public static class Bootstrap
    {
        public static void Initialize()
        {
            SkinProvider.NewLiveryDetected += CheckCCLSubstitutionSupport;
            CommsRadioSkinSwitcher.RadioSkinApplied += TrainsetPropagator.OnRadioSkinApplied;
            RandomSkinChooser.IsCCLCarType = IsCCLCarType;

            Main.Harmony.PatchAll(typeof(Bootstrap).Assembly);
        }

        private static bool IsCCLCarType(TrainCarLivery livery) =>
            CarTypeInjector.IdToLiveryMap.ContainsKey(livery.id);

        private static void CheckCCLSubstitutionSupport(TrainCarLivery cclLivery)
        {
            if (!CarTypeInjector.IdToLiveryMap.ContainsKey(cclLivery.id)) return;

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


        private static Dictionary<TrainCarLivery, List<TrainCarLivery[]>> _fullTrainsetMap = new();

        internal static IEnumerable<TrainCarLivery[]> GetContainingTrainsets(TrainCarLivery livery)
        {
            if (_fullTrainsetMap.TryGetValue(livery, out var result))
            {
                return result;
            }

            result = new List<TrainCarLivery[]>();
            foreach (var trainset in CarManager.Trainsets.Values)
            {
                if (trainset.Contains(livery))
                {
                    if (!result.Any(t => t.SequenceEqual(trainset)))
                    {
                        result.Add(trainset);
                    }
                }
            }
            _fullTrainsetMap[livery] = result;
            return result;
        }
    }
}