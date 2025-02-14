using SMShared;

namespace SkinManagerMod
{
    internal static class Extensions
    {
        public static bool IsVanillaPassenger(this TrainCar trainCar)
        {
            return trainCar.carLivery.parentType.id == Constants.PASSENGER_TYPE_ID;
        }
    }
}
