using System;
using DV.ThingTypes;
using SMShared;

namespace SkinManagerMod
{
    /// <summary>
    /// If the current car is a tender or slug, returns the skin of the attached S282 or DE6
    /// </summary>
    public class TenderSkinChooser : SkinChooser
    {
        internal override bool IsBuiltIn => true;

        public override bool TryChooseSkin(TrainCar car, out string? skinName)
        {
            if (CarTypes.IsTender(car.carLivery))
            {
                if (CarTypes.IsMUSteamLocomotive(SkinProvider.LastChosenSkin.CarType.v1))
                {
                    skinName = SkinProvider.LastChosenSkin.SkinName;
                    return true;
                }
            }
            else if (CarTypes.IsSlug(car.carLivery))
            {
                if (SkinProvider.LastChosenSkin.CarType.id == Constants.DE6_LIVERY_ID)
                {
                    skinName = SkinProvider.LastChosenSkin.SkinName;
                    return true;
                }
            }

            skinName = null;
            return false;
        }
    }
}