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
            CommsRadioSkinSwitcher.RadioSkinApplied += TrainsetPropagator.OnRadioSkinApplied;
            SkinManager.IsCCLCarType = IsCCLCarType;

            Main.Harmony.PatchAll(typeof(Bootstrap).Assembly);
        }

        private static bool IsCCLCarType(TrainCarLivery livery) =>
            CarTypeInjector.IdToLiveryMap.ContainsKey(livery.id);

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