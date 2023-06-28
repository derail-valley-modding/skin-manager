using DV.ThingTypes;
using System;
using System.Collections.Generic;

namespace SkinManagerMod
{
    internal static class Remaps
    {
        public static Dictionary<TrainCarType, string> OldCarTypeIDs = new Dictionary<TrainCarType, string>()
        {
            { TrainCarType.LocoShunter,     "loco_621" },
            { TrainCarType.LocoSteamHeavy,  "loco_steam_H" },
            { TrainCarType.Tender,          "loco_steam_tender" },
            //{ TrainCarType.LocoRailbus,     "" },
            //{ TrainCarType.LocoDiesel,      "" },
            //{ TrainCarType.LocoDH2,         "" },
            //{ TrainCarType.LocoDM1,         "" },

            { TrainCarType.FlatbedEmpty,    "car_flatbed_empty" },
            { TrainCarType.FlatbedStakes,   "car_flatbed_stakes" },
            { TrainCarType.FlatbedMilitary, "car_flatbed_military_empty" },

            { TrainCarType.AutorackRed,     "CarAutorack_Red" },
            { TrainCarType.AutorackBlue,    "CarAutorack_Blue" },
            { TrainCarType.AutorackGreen,   "CarAutorack_Green" },
            { TrainCarType.AutorackYellow,  "CarAutorack_Yellow" },

            { TrainCarType.TankOrange,      "CarTank_Orange" },
            { TrainCarType.TankWhite,       "CarTank_White" },
            { TrainCarType.TankYellow,      "CarTank_Yellow" },
            { TrainCarType.TankBlue,        "CarTank_Blue" },
            { TrainCarType.TankChrome,      "CarTank_Chrome" },
            { TrainCarType.TankBlack,       "CarTank_Black" },

            { TrainCarType.BoxcarBrown,     "CarBoxcar_Brown" },
            { TrainCarType.BoxcarGreen,     "CarBoxcar_Green" },
            { TrainCarType.BoxcarPink,      "CarBoxcar_Pink" },
            { TrainCarType.BoxcarRed,       "CarBoxcar_Red" },
            { TrainCarType.BoxcarMilitary,  "CarBoxcarMilitary" },
            { TrainCarType.RefrigeratorWhite, "CarRefrigerator_White" },

            { TrainCarType.HopperBrown,     "CarHopper_Brown" },
            { TrainCarType.HopperTeal,      "CarHopper_Teal" },
            { TrainCarType.HopperYellow,    "CarHopper_Yellow" },

            { TrainCarType.GondolaRed,      "CarGondola_Red" },
            { TrainCarType.GondolaGreen,    "CarGondola_Green" },
            { TrainCarType.GondolaGray,     "CarGondola_Grey" },

            { TrainCarType.PassengerRed,    "CarPassenger_Red" },
            { TrainCarType.PassengerGreen,  "CarPassenger_Green" },
            { TrainCarType.PassengerBlue,   "CarPassenger_Blue" },

            { TrainCarType.HandCar,         "handcar" },
            { TrainCarType.CabooseRed,      "CarCaboose_Red" },
            { TrainCarType.NuclearFlask,    "CarNuclearFlask" },
        };
    }
}
