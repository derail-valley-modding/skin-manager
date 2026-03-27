using DV.Customization.Paint;
using DV.JObjectExtstensions;
using DV.ThingTypes;
using HarmonyLib;
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

        public delegate void ThemeAppliedDelegate(TrainCar car, TrainCarPaint paint, CustomPaintTheme newTheme);
        public static event ThemeAppliedDelegate? ThemeAppliedToRegion;

        internal static void RaiseThemeAppliedToRegion(TrainCar car, TrainCarPaint paint, CustomPaintTheme newTheme) =>
            ThemeAppliedToRegion?.Invoke(car, paint, newTheme);

        public delegate void ThemesReappliedDelegate(TrainCar car);
        public static event ThemesReappliedDelegate? ThemesReapplied;

        internal static void RaiseThemesReapplied(TrainCar car) =>
            ThemesReapplied?.Invoke(car);

        public delegate void SkinAppliedDelegate(TrainCar car, CustomPaintTheme theme, PaintArea area);
        public static event SkinAppliedDelegate? SkinApplied;

        public delegate void SkinChangingDelegate(TrainCar car, PaintTheme? oldTheme, CustomPaintTheme newTheme, PaintArea area);
        public static event SkinChangingDelegate? SkinChanging;

        public static void Initialize()
        {
            SkinProvider.SkinUpdated += ReapplySkinToUsers;
        }

        private static void ReapplySkinToUsers(SkinConfig skinConfig)
        {
            if (!CarSpawner.Instance) return;

            var theme = SkinProvider.GetTheme(skinConfig.Name);

            foreach (var car in CarSpawner.Instance.AllCars.Where(tc => tc.carLivery.id == skinConfig.CarId))
            {
                (string? exterior, string? interior) = GetCurrentCarSkin(car, false);

                PaintArea matchingArea = PaintArea.None;
                if (exterior == skinConfig.Name) matchingArea |= PaintArea.Exterior;
                if (interior == skinConfig.Name) matchingArea |= PaintArea.Interior;

                if (matchingArea != PaintArea.None)
                {
                    ApplySkin(car, theme, matchingArea);
                }
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
                    exterior = SkinProvider.GetNewSkin(car);
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
            if (car.logicCar is null) return;

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
                    SkinChanging?.Invoke(trainCar, trainCar.PaintInterior.CurrentTheme, newTheme, PaintArea.Interior);
                    trainCar.PaintInterior.CurrentTheme = newTheme;
                }

                if (area.HasFlag(PaintArea.Exterior) && trainCar.PaintExterior)
                {
                    SkinChanging?.Invoke(trainCar, trainCar.PaintExterior.CurrentTheme, newTheme, PaintArea.Exterior);
                    trainCar.PaintExterior.CurrentTheme = newTheme;
                }

                SkinApplied?.Invoke(trainCar, newTheme, area);
            }
        }

        #endregion

        //====================================================================================================
        #region Save/Load Methods

        public static JObject GetCarsSaveData()
        {
            JObject carsSaveData = new JObject();

            var exteriorSaveData = new JObject[carGuidToAppliedSkinMap.Count];
            int i = 0;
            foreach (var kvp in carGuidToAppliedSkinMap)
            {
                var dataObject = new JObject();

                dataObject.SetString("guid", kvp.Key);
                dataObject.SetString("name", kvp.Value);

                exteriorSaveData[i] = dataObject;
                i++;
            }
            carsSaveData.SetJObjectArray("carsData", exteriorSaveData);

            var interiorSaveData = new JObject[interiorSkinMap.Count];
            i = 0;

            foreach (var kvp in interiorSkinMap)
            {
                JObject dataObject = new JObject();

                dataObject.SetString("guid", kvp.Key);
                dataObject.SetString("name", kvp.Value);

                interiorSaveData[i] = dataObject;
                i++;
            }
            carsSaveData.SetJObjectArray("interiorData", interiorSaveData);

            return carsSaveData;
        }

        public static void LoadCarsSaveData(JObject carsSaveData)
        {
            JObject[] exteriorSaveData = carsSaveData.GetJObjectArray("carsData");

            if (exteriorSaveData != null)
            {
                foreach (JObject entry in exteriorSaveData)
                {
                    var guid = entry.GetString("guid");
                    var name = entry.GetString("name");

                    if (!carGuidToAppliedSkinMap.ContainsKey(guid))
                    {
                        carGuidToAppliedSkinMap.Add(guid, name);
                    }
                }
            }

            var interiorSaveData = carsSaveData.GetJObjectArray("interiorData");
            if (interiorSaveData != null)
            {
                foreach (JObject entry in interiorSaveData)
                {
                    var guid = entry.GetString("guid");
                    var name = entry.GetString("name");

                    if (!interiorSkinMap.ContainsKey(guid))
                    {
                        interiorSkinMap.Add(guid, name);
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
        None = 0,
        Exterior = 1,
        Interior = 2,
        All = Exterior | Interior,
    }
}
