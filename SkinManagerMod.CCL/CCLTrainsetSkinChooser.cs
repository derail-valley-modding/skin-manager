using System;
using System.Collections.Generic;
using System.Linq;
using DV.ThingTypes;

namespace SkinManagerMod.CCL
{
    public class CCLTrainsetSkinChooser : SkinChooser
    {
        public override string[] PrioritizeOver => new[] { nameof(TenderSkinChooser) };

        public override bool TryChooseSkin(TrainCar car, out string? skinName)
        {
            var trainsets = Bootstrap.GetContainingTrainsets(car.carLivery);
            foreach (var trainset in trainsets)
            {
                if (trainset.Length < 2) continue;

                int indexInSet = Array.IndexOf(trainset, car.carLivery);
                if (indexInSet < 1) continue; // this is the first car in the trainset

                Main.LogVerbose($"Checking possible CCL trainset for {car.ID}: {GetTrainsetString(trainset)}");

                if (SkinProvider.LastChosenSkin.CarType == trainset[indexInSet - 1])
                {
                    skinName = SkinProvider.LastChosenSkin.SkinName;
                    return true;
                }
            }

            skinName = null;
            return false;
        }

        private static string GetTrainsetString(IEnumerable<TrainCarLivery> trainset)
        {
            return "[" + string.Join(", ", trainset.Select(c => c.id)) + "]";
        }
    }
}