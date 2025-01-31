using DV.Customization.Paint;
using DV.JObjectExtstensions;
using DV.ThingTypes;
using Newtonsoft.Json.Linq;
using SMShared;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SkinManagerMod
{
    public static class SkinManager
    {
        private static readonly Dictionary<string, string> carGuidToAppliedSkinMap = new();
        private static readonly Dictionary<string, string> interiorSkinMap = new();

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

                //if ((toApply != null) && (toApply == skinConfig.Name))
                //{
                //    ApplySkin(car, toApply);
                //}
            }
        }

        /// <summary>Get the currently assigned skin for given car, or a new one if none is assigned</summary>
        public static (string? exterior, string? interior) GetCurrentCarSkin(TrainCar car, bool returnNewSkin = true)
        {
            if (!carGuidToAppliedSkinMap.TryGetValue(car.CarGUID, out string? exterior) ||
                string.IsNullOrWhiteSpace(exterior) ||
                !SkinProvider.TryGetTheme(exterior, out _))
            {
                if (returnNewSkin)
                {
                    exterior = SkinProvider.GetNewSkin(car.carLivery);
                }
                else
                {
                    exterior = null;
                }
            }

            if (!interiorSkinMap.TryGetValue(car.CarGUID, out string? interior) ||
                string.IsNullOrWhiteSpace(interior) ||
                !SkinProvider.TryGetTheme(interior, out _))
            {
                interior = exterior;

            }

            return (exterior, interior);
        }

        /// <summary>Save the specified skin to the given car</summary>
        public static void SetAppliedCarSkin(TrainCar car, string skinName, PaintArea area)
        {
            Main.LogVerbose($"Setting saved skin for car {car.ID} {area} to \"{skinName}\"");

            if (area.HasFlag(PaintArea.Exterior))
            {
                carGuidToAppliedSkinMap[car.CarGUID] = skinName;
            }
            if (area.HasFlag(PaintArea.Interior))
            {
                interiorSkinMap[car.CarGUID] = skinName;
            }

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
            if (SkinProvider.TryGetTheme(skinName, out CustomPaintTheme newTheme))
            {
                ApplySkin(trainCar, newTheme, area);
            }
            else
            {
                Main.Log($"Couldn't find paint theme {skinName} for car {trainCar.ID}");
            }
        }

        public static void ApplySkin(TrainCar trainCar, CustomPaintTheme newTheme, PaintArea area = PaintArea.All)
        {
            if (newTheme.SupportsVehicle(trainCar.carLivery))
            {
                if (area.HasFlag(PaintArea.Interior) && trainCar.PaintInterior)
                {
                    trainCar.PaintInterior.CurrentTheme = newTheme;
                }

                if (area.HasFlag(PaintArea.Exterior) && trainCar.PaintExterior)
                {
                    trainCar.PaintExterior.CurrentTheme = newTheme;
                }

                //SetAppliedCarSkin(trainCar, newTheme.name, area);
            }
        }

        //public static void ApplyNonThemeSkinToInterior(TrainCar trainCar, string skinName)
        //{
        //    var skin = SkinProvider.FindSkinByName(trainCar.carLivery.id, skinName);
        //    if (skin == null) return;

        //    var defaultSkin = CarMaterialData.GetDataForCar(trainCar.carLivery.id);

        //    if (trainCar.loadedInterior)
        //    {
        //        ApplyNonThemeSkinToTransform(trainCar.loadedInterior, skin, defaultSkin);
        //    }
        //    if (trainCar.loadedExternalInteractables)
        //    {
        //        ApplyNonThemeSkinToTransform(trainCar.loadedExternalInteractables, skin, defaultSkin);
        //    }
        //    if (trainCar.loadedDummyExternalInteractables)
        //    {
        //        ApplyNonThemeSkinToTransform(trainCar.loadedDummyExternalInteractables, skin, defaultSkin);
        //    }
        //}

        //public static void ApplySkin(TrainCar trainCar, Skin skin, PaintArea area)
        //{
        //    if (skin == null) return;

        //    var defaultSkin = CarMaterialData.GetDataForCar(trainCar.carLivery.id);

        //    if (area.HasFlag(PaintArea.Exterior))
        //    {
        //        ApplyNonThemeSkinToTransform(trainCar.gameObject, skin, defaultSkin);
        //    }

        //    if (area.HasFlag(PaintArea.Interior) && trainCar.loadedInterior)
        //    {
        //        ApplyNonThemeSkinToTransform(trainCar.loadedInterior, skin, defaultSkin);
        //    }

        //    SetAppliedCarSkin(trainCar, skin.Name, area);
        //}

        //private static void ApplyNonThemeSkinToTransform(GameObject objectRoot, Skin skin, CarMaterialData defaults)
        //{
        //    foreach (var renderer in objectRoot.GetComponentsInChildren<MeshRenderer>(true))
        //    {
        //        if (!renderer.material)
        //        {
        //            continue;
        //        }

        //        TextureUtility.ApplyTextures(renderer, skin, defaults);
        //    }
        //}

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

        public static PaintArea ToPaintArea(this TrainCarPaint.Target target)
        {
            return (target == TrainCarPaint.Target.Interior) ? PaintArea.Interior : PaintArea.Exterior;
        }
    }

    [Flags]
    public enum PaintArea
    {
        Exterior = 1,
        Interior = 2,
        All = Exterior | Interior,
    }
}
