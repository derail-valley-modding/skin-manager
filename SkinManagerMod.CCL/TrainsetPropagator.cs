using System.Linq;
using CCL.Importer;
using CCL.Importer.Types;

namespace SkinManagerMod.CCL
{
    public static class TrainsetPropagator
    {
        public static void OnRadioSkinApplied(TrainCar car, CustomPaintTheme theme, PaintArea area)
        {
            if (!CarTypeInjector.IdToLiveryMap.ContainsKey(car.carLivery.id)) return;

            // trainsets that start with this car
            var trainsets = Bootstrap.GetContainingTrainsets(car.carLivery)
                .Where(ts => (ts.Length > 1) && (ts[0] == car.carLivery));

            foreach (var trainset in trainsets)
            {
                bool walkBackward = true;
                bool partialMatch = false;
                TrainCar? coupledCar = GetNextCoupledCar(car, ref walkBackward);

                int tsIndex = 1;
                while (tsIndex < trainset.Length && coupledCar && (coupledCar!.carLivery == trainset[tsIndex]))
                {
                    partialMatch = true;

                    SkinManager.ApplySkin(coupledCar, theme, area);

                    coupledCar = GetNextCoupledCar(coupledCar, ref walkBackward);
                    tsIndex += 1;
                }

                if (partialMatch) break;
            }
        }
        
        private static TrainCar? GetNextCoupledCar(TrainCar current, ref bool walkBackward)
        {
            Coupler outCoupler = walkBackward ? current.rearCoupler : current.frontCoupler;
            Coupler? mated = outCoupler.coupledTo;
            TrainCar? nextCar = mated?.train;
            walkBackward = (mated == nextCar?.frontCoupler);
            return nextCar;
        }
    }
}