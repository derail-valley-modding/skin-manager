using DV.JObjectExtstensions;
using DV.ThingTypes;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SkinManagerMod
{
    public static class SkinManager
    {
        //private static Dictionary<TrainCarType, string> CustomCarTypes;

        private static readonly Dictionary<string, string> carGuidToAppliedSkinMap = new Dictionary<string, string>();

        public static void Initialize()
        {
            SkinProvider.SkinUpdated += ReapplySkinToUsers;
        }

        private static void ReapplySkinToUsers(SkinConfig skinConfig)
        {
            var carsInScene = Object.FindObjectsOfType<TrainCar>().Where(tc => tc.carLivery == skinConfig.Livery);
            foreach (var car in carsInScene)
            {
                var toApply = GetCurrentCarSkin(car);

                if (toApply != null)
                {
                    ApplySkin(car, toApply);
                }
            }
        }

        /// <summary>Get the currently assigned skin for given car, or a new one if none is assigned</summary>
        public static Skin GetCurrentCarSkin(TrainCar car)
        {
            if (carGuidToAppliedSkinMap.TryGetValue(car.CarGUID, out var skinName))
            {
                if (SkinProvider.TryGetDefaultSkin(car.carLivery.id, out Skin defaultSkin) && (skinName == defaultSkin.Name))
                {
                    return defaultSkin;
                }

                if (SkinProvider.FindSkinByName(car.carLivery, skinName) is Skin result)
                {
                    return result;
                }
            }

            return SkinProvider.GetNewSkin(car.carLivery);
        }

        /// <summary>Save the specified skin to the given car</summary>
        private static void SetAppliedCarSkin(TrainCar car, Skin skin)
        {
            carGuidToAppliedSkinMap[car.CarGUID] = skin.Name;

            // TODO: support for CCL steam locos (this method only checks if == locosteamheavy)
            if (CarTypes.IsSteamLocomotive(car.carType))
            {
                SkinProvider.LastSteamerSkin = skin;
            }
            else
            {
                SkinProvider.LastSteamerSkin = null;
            }
        }


        //====================================================================================================
        #region Skin Manipulation

        public static void ApplySkin(TrainCar trainCar, Skin skin)
        {
            if (skin == null) return;

            //Main.ModEntry.Logger.Log($"Applying skin {skin.Name} to car {trainCar.ID}");

            ApplySkin(trainCar.gameObject.transform, skin, SkinProvider.GetDefaultSkin(trainCar.carLivery.id));
            if (trainCar.IsInteriorLoaded || trainCar.AreExternalInteractablesLoaded)
            {
                ApplySkinToInterior(trainCar, skin);
            }

            SetAppliedCarSkin(trainCar, skin);
        }

        public static void ApplySkinToInterior(TrainCar trainCar, Skin skin)
        {
            if (skin == null) return;

            ApplySkin(trainCar.interior, skin, SkinProvider.GetDefaultSkin(trainCar.carLivery.id));
        }

        private static void ApplySkin(Transform objectRoot, Skin skin, Skin defaultSkin)
        {
            foreach (var renderer in objectRoot.GetComponentsInChildren<MeshRenderer>())
            {
                if (!renderer.material)
                {
                    continue;
                }

                TextureUtility.ApplyTextures(renderer, skin, defaultSkin);
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

            foreach ((string guid, string skinName) in carGuidToAppliedSkinMap)
            {
                JObject dataObject = new JObject();

                dataObject.SetString("guid", guid);
                dataObject.SetString("name", skinName);

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
}
