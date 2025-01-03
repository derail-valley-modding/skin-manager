using DV.Customization.Paint;
using DV.JObjectExtstensions;
using DV.ThingTypes;
using Newtonsoft.Json.Linq;
using SMShared;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SkinManagerMod
{
    public static class SkinManager
    {
        private static readonly Dictionary<string, string> carGuidToAppliedSkinMap = new Dictionary<string, string>();

        public static void Initialize()
        {
            SkinProvider.SkinUpdated += ReapplySkinToUsers;
        }

        private static void ReapplySkinToUsers(SkinConfig skinConfig)
        {
            if (!CarSpawner.Instance) return;

            foreach (var car in CarSpawner.Instance.AllCars.Where(tc => tc.carLivery.id == skinConfig.CarId))
            {
                var toApply = GetCurrentCarSkin(car, false);

                if ((toApply != null) && (toApply == skinConfig.Name))
                {
                    ApplySkin(car, toApply);
                }
            }
        }

        /// <summary>Get the currently assigned skin for given car, or a new one if none is assigned</summary>
        public static string GetCurrentCarSkin(TrainCar car, bool returnNewSkin = true)
        {
            if (carGuidToAppliedSkinMap.TryGetValue(car.CarGUID, out var skinName))
            {
                if (string.IsNullOrWhiteSpace(skinName)) return null;

                if (SkinProvider.FindSkinByName(car.carLivery, skinName) is Skin result)
                {
                    return result.Name;
                }
            }

            return returnNewSkin ? SkinProvider.GetNewSkin(car.carLivery) : null;
        }

        /// <summary>Save the specified skin to the given car</summary>
        public static void SetAppliedCarSkin(TrainCar car, string skinName)
        {
            Main.LogVerbose($"Setting saved skin for car {car.ID} to \"{skinName}\"");
            carGuidToAppliedSkinMap[car.CarGUID] = skinName;

            // TODO: support for CCL steam locos (this method only checks if == locosteamheavy)
            if (CarTypes.IsMUSteamLocomotive(car.carType))
            {
                SkinProvider.LastSteamerSkin = skinName;
            }
            else
            {
                SkinProvider.LastSteamerSkin = null;
            }

            SkinProvider.LastDE6Skin = (car.carLivery.id == Constants.DE6_LIVERY_ID) ? skinName : null;
        }


        //====================================================================================================
        #region Skin Manipulation

        public static void ApplySkin(TrainCar trainCar, string skinName, PaintArea area = PaintArea.All)
        {
            if (PaintTheme.TryLoad(skinName, out PaintTheme newTheme))
            {
                if (area.HasFlag(PaintArea.Interior) && trainCar.PaintInterior)
                {
                    if (trainCar.PaintInterior.IsSupported(newTheme))
                    {
                        trainCar.PaintInterior.CurrentTheme = newTheme;
                    }
                }

                if (area.HasFlag(PaintArea.Exterior) && trainCar.PaintExterior)
                {
                    if (trainCar.PaintExterior.IsSupported(newTheme))
                    {
                        trainCar.PaintExterior.CurrentTheme = newTheme;
                    }
                }
            }
            else
            {
                Main.Log($"Couldn't find paint theme {skinName} for car {trainCar.ID}");
            }
        }

        #endregion

        //====================================================================================================
        #region Save/Load Methods

        public static JObject GetCarsSaveData()
        {
            JObject carsSaveData = new JObject();

            JObject[] array = new JObject[carGuidToAppliedSkinMap.Count];

            int i = 0;

            foreach (var kvp in carGuidToAppliedSkinMap)
            {
                JObject dataObject = new JObject();

                dataObject.SetString("guid", kvp.Key);
                dataObject.SetString("name", kvp.Value);

                array[i] = dataObject;

                i++;
            }

            carsSaveData.SetJObjectArray("carsData", array);

            return carsSaveData;
        }

        public static void LoadCarsSaveData(JObject carsSaveData)
        {
            JObject[] jobjectArray = carsSaveData.GetJObjectArray("carsData");

            if (jobjectArray != null)
            {
                foreach (JObject jobject in jobjectArray)
                {
                    var guid = jobject.GetString("guid");
                    var name = jobject.GetString("name");

                    if (!carGuidToAppliedSkinMap.ContainsKey(guid))
                    {
                        carGuidToAppliedSkinMap.Add(guid, name);
                    }
                }
            }
        }

        #endregion
    }

    [Flags]
    public enum PaintArea
    {
        Exterior = 1,
        Interior = 2,
        All = Exterior | Interior,
    }
}
