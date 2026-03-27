using DV.ThingTypes;
using SMShared;
using SMShared.Json;
using System;
using System.Linq;

namespace SkinManagerMod
{
    public class RandomSkinChooser : SkinChooser
    {
        internal override bool IsBuiltIn => true;

        private static bool DummyIsCCLCarType(TrainCarLivery _) => false;
        public static Func<TrainCarLivery, bool> IsCCLCarType { get; set; } = DummyIsCCLCarType;

        public override bool TryChooseSkin(TrainCar car, out string? skinName)
        {
            skinName = null;
            if (Main.Settings.defaultSkinsMode == DefaultSkinsMode.PreferDefaults) return false;

            var available = SkinProvider.GetSkinsForType(car.carLivery, false, false);

            if ((car.carLivery.id == Constants.SLUG_LIVERY_ID) && Main.Settings.allowDE6SkinsForSlug)// && SkinProvider.TryGetSkinGroup(Constants.DE6_LIVERY_ID, out group))
            {
                available.AddRange(
                    SkinProvider.GetSkinsForType(Constants.DE6_LIVERY_ID, false, false)
                    .Where(t => !available.Contains(t))
                );
            }

            bool allowDefaults =
                (Main.Settings.defaultSkinsMode == DefaultSkinsMode.AllowForAllCars) ||
                (IsCCLCarType(car.carLivery) && Main.Settings.defaultSkinsMode == DefaultSkinsMode.AllowForCustomCars);

            if (allowDefaults)
            {
                available.Add(SkinProvider.CustomDefaultTheme);

                if (Main.Settings.allowRandomDemonstrators)
                {
                    var demoTheme = SkinProvider.GetBaseTheme(BaseTheme.Demonstrator);
                    if (demoTheme.SupportsVehicle(car.carLivery)) available.Add(demoTheme);
                }
            }

            if (available.Count > 0)
            {
                int choice = UnityEngine.Random.Range(0, available.Count);
                skinName = available[choice].name;
                return true;
            }
            return false;
        }

        private static bool AllowRandomSpawning(Skin skin)
        {
            if (SkinProvider.TryGetThemeSettings(skin.Name, out var settings))
            {
                return !settings.PreventRandomSpawning;
            }
            return true;
        }
    }
}