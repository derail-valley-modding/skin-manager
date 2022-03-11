using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using HarmonyLib;

namespace SkinManagerMod
{
    using GetCarListDelegate = Func<IEnumerable<KeyValuePair<TrainCarType, string>>>;

    internal static class CCLPatch
    {
        public static bool Enabled { get; set; } = false;

        private static GetCarListDelegate GetCustomCarList = null;

        public static void Initialize()
        {
            Enabled = false;

            try
            {
                var carManagerType = AccessTools.TypeByName("DVCustomCarLoader.CustomCarManager");
                if (carManagerType != null)
                {
                    var getCarListMethod = AccessTools.Method(carManagerType, "GetCustomCarList");
                    if (getCarListMethod != null)
                    {
                        GetCustomCarList = AccessTools.MethodDelegate<GetCarListDelegate>(getCarListMethod);
                        if (GetCustomCarList != null)
                        {
                            Enabled = true;
                            Main.ModEntry.Logger.Log("Successfully connected to Custom Car Loader");
                            return;
                        }
                    }
                }

            }
            catch (Exception ex)
            {
                Main.ModEntry.Logger.Log($"Error while trying to connect with Custom Car Loader:\n{ex.Message}");
                return;
            }

            Main.ModEntry.Logger.Log("Custom Car Loader not found, skipping integration");
        }

        public static IEnumerable<KeyValuePair<TrainCarType, string>> CarList
        {
            get
            {
                if (!Enabled)
                {
                    return Enumerable.Empty<KeyValuePair<TrainCarType, string>>();
                }
                else
                {
                    return GetCustomCarList();
                }
            }
        }
    }
}
