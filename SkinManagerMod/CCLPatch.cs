using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Remoting.Contexts;
using CCL_GameScripts;
using DVCustomCarLoader;
using HarmonyLib;
using Newtonsoft.Json;
using UnityModManagerNet;

namespace SkinManagerMod
{
    internal interface ICustomCarLoader
    {
        IEnumerable<KeyValuePair<TrainCarType, string>> GetCustomCarList();
        bool IsCustomCar(TrainCarType type);
        string GetCarFolder(TrainCarType type);
    }

    internal class DummyCCL : ICustomCarLoader
    {
        public IEnumerable<KeyValuePair<TrainCarType, string>> GetCustomCarList()
        {
            return Enumerable.Empty<KeyValuePair<TrainCarType, string>>();
        }

        public bool IsCustomCar(TrainCarType type) => false;

        public string GetCarFolder(TrainCarType type) => null;
    }

    internal class RealCCL : ICustomCarLoader
    {
        private readonly Dictionary<TrainCarType, string> carDirectories = new Dictionary<TrainCarType, string>();

        public RealCCL(UnityModManager.ModEntry modEntry)
        {
            string carsFolder = Path.Combine(modEntry.Path, "Cars");
            if (Directory.Exists(carsFolder))
            {
                var carFolders = Directory.GetDirectories(carsFolder);
                foreach (var dir in carFolders)
                {
                    FetchCarFromFolder(dir);
                }
            }
        }

        private void FetchCarFromFolder(string carFolder)
        {
            string jsonFile = Path.Combine(carFolder, CarJSONKeys.JSON_FILENAME);
            if (File.Exists(jsonFile))
            {
                try
                {
                    JSONObject json = new JSONObject(File.ReadAllText(jsonFile));
                    string carId = json[CarJSONKeys.IDENTIFIER].str;

                    TrainCarType carType = CarTypeInjector.CarTypeById(carId);
                    carDirectories.Add(carType, carFolder);
                }
                catch 
                {
                    Main.ModEntry.Logger.Error($"Failed to connect custom car in folder {carFolder}");
                }
            }
        }

        public IEnumerable<KeyValuePair<TrainCarType, string>> GetCustomCarList() => CustomCarManager.GetCustomCarList();

        public bool IsCustomCar(TrainCarType type) => CarTypeInjector.IsInCustomRange(type);

        public string GetCarFolder(TrainCarType type)
        {
            if (carDirectories.TryGetValue(type, out var carFolder))
            {
                return carFolder;
            }
            return null;
        }
    }

    internal static class CCLPatch
    {
        public static bool Enabled { get; set; } = false;
        public static UnityModManager.ModEntry ModEntry;

        private static ICustomCarLoader carLoaderWrapper;

        public static void Initialize()
        {
            Enabled = false;

            ModEntry = UnityModManager.FindMod("DVCustomCarLoader");
            if (ModEntry == null)
            {
                carLoaderWrapper = new DummyCCL();

                Main.ModEntry.Logger.Log("Custom Car Loader not found, skipping integration");
                return;
            }

            try
            {
                carLoaderWrapper = new RealCCL(ModEntry);

                Enabled = true;
                Main.ModEntry.Logger.Log("Successfully connected to Custom Car Loader");
            }
            catch (Exception ex)
            {
                Main.ModEntry.Logger.Log($"Error while trying to connect with Custom Car Loader:\n{ex.Message}");
                return;
            }
        }

        public static IEnumerable<KeyValuePair<TrainCarType, string>> CarList => carLoaderWrapper.GetCustomCarList();

        public static bool IsCustomCarType(TrainCarType carType) => carLoaderWrapper.IsCustomCar(carType);

        public static string GetCarFolder(TrainCarType carType) => carLoaderWrapper.GetCarFolder(carType);
    }
}
