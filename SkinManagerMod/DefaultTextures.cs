using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SkinManagerMod
{
    public static class DefaultTextures
    {
        private static readonly Dictionary<string, string> _bodyTextures = new()
        {
            { "AutorackBlue", "CarAutorackBlue_01d" },
            { "AutorackGreen", "CarAutorackGreen_01d" },
            { "AutorackRed", "CarAutorackRed_01d" },
            { "AutorackYellow", "CarAutorackYellow_01d" },

            { "BoxcarBrown", "CarBoxcar_Brown_01d" },
            { "BoxcarGreen", "CarBoxcar_Green_01d" },
            { "BoxcarPink", "CarBoxcar_Pink_01d" },
            { "BoxcarRed", "CarBoxcar_Red_01d" },
            { "BoxcarMilitary", "CarBoxcarMilitary_01d" },
            { "RefrigeratorWhite", "CarRefrigerator_d" },
            { "StockBrown", "CarStockcar_Brown_d" },
            { "StockGreen", "CarStockcar_Green_d" },
            { "StockRed", "CarStockcar_Red_d" },

            { "FlatbedEmpty", "CarFlatcarCBBulkheadStakes_Brown_d" },
            { "FlatbedStakes", "CarFlatcarCBBulkheadStakes_Brown_d" },
            { "FlatbedShort", "CarFlatcarShort_01d" },
            { "FlatbedMilitary", "CarFlatcarCBBulkheadStakes_Military_d" },

            { "GondolaGray", "CarGondolaGrey_d" },
            { "GondolaGreen", "CarGondolaGreen_d" },
            { "GondolaRed", "CarGondolaRed_d" },

            { "HopperBrown", "CarHopperBrown_d" },
            { "HopperTeal", "CarHopperTeal_d" },
            { "HopperYellow", "CarHopperYellow_d" },
            { "HopperCoveredBrown", "CarHopperCovered_01d" },

            { "TankBlack", "CarTankBlack_01d" },
            { "TankBlue", "CarTankBlue_01d" },
            { "TankChrome", "CarTankChrome_01d" },
            { "TankOrange", "CarTankOrange_01" },
            { "TankWhite", "CarTankWhite_01d" },
            { "TankYellow", "CarTankYellow_01d" },
            { "TankShortMilk", "CarTanker_WhiteMilk_d" },

            { "LocoDE2", "LocoDE2_Body_01d" },
            { "LocoDE6", "LocoDE6_Body_01d" },
            { "LocoDE6Slug", "LocoDE6_Body_01d" },
            { "LocoDH4", "LocoDH4_Body_01d" },
            { "LocoDM1U", "LocoDM1U-150_Body_01d" },
            { "LocoDM3", "LocoDM3_Body_01d" },
            { "LocoMicroshunter", "LocoMicroshunter_Body_01d" },
            { "LocoS060", "LocoS060_Body_01d" },
            { "LocoS282A", "LocoS282A_Body_01d" },
            { "LocoS282B", "LocoS282B_Body_01d" },

            { "CabooseRed", "CarCabooseRed_Body_01d" },
            { "HandCar", "LocoHandcar_01d" },
            { "NuclearFlask", "CarNuclearFlask_d" },

            { "PassengerBlue", "CarPassengerBlue_01d" },
            { "PassengerGreen", "CarPassengerGreen_01d" },
            { "PassengerRed", "CarPassengerRed_01d" },
        };

        public static string GetBodyTextureName(string liveryId)
        {
            return _bodyTextures[liveryId];
        }
    }
}
