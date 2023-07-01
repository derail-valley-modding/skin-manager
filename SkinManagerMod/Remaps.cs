using DV.ThingTypes;
using System;
using System.Collections;
using System.Collections.Generic;

namespace SkinManagerMod
{
    internal static class Remaps
    {
        public static readonly Dictionary<TrainCarType, string> OldCarTypeIDs = new Dictionary<TrainCarType, string>()
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

        private class TextureMapping : IEnumerable<KeyValuePair<string, string>>
        {
            private readonly Dictionary<string, string> _map = 
                new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            public static readonly char[] DE = { 'd', 'e' };
            public static readonly char[] DNS = { 'd', 'n', 's' };

            public void Add(string oldName, string newName)
            {
                _map.Add(oldName, newName);
            }

            public void Add(string oldBase, string newBase, char[] suffixes)
            {
                foreach (char suffix in suffixes)
                {
                    _map.Add($"{oldBase}{suffix}", $"{newBase}{suffix}");
                }
            }

            public IEnumerator<KeyValuePair<string, string>> GetEnumerator()
            {
                return ((IEnumerable<KeyValuePair<string, string>>)_map).GetEnumerator();
            }

            public bool TryGetUpdatedName(string oldName, out string newName)
            {
                return _map.TryGetValue(oldName, out newName);
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return ((IEnumerable)_map).GetEnumerator();
            }
        }

        /// <summary>Old texture name -> new texture name</summary>
        private static readonly Dictionary<TrainCarType, TextureMapping> _legacyTextureNameMap = 
            new Dictionary<TrainCarType, TextureMapping>()
            {
                {
                    TrainCarType.LocoShunter,
                    new TextureMapping
                    {
                        { "exterior_", "LocoDE2_Body_01", TextureMapping.DNS },
                    }
                },

                // 282 & tender new UVs -> no mappings :(

                {
                    TrainCarType.LocoDiesel,
                    new TextureMapping
                    {
                        { "LocoDiesel_bogies_", "LocoDE6_Body_01", TextureMapping.DNS },
                        { "LocoDiesel_cab_", "LocoDE6_Interior_01", TextureMapping.DNS },
                        { "LocoDiesel_engine_", "LocoDE6_Engine_01", TextureMapping.DNS },
                        { "LocoDiesel_exterior_", "LocoDE6_Body_01", TextureMapping.DNS },
                        { "LocoDiesel_gauges_01", "LocoDE6_Gauges_01", TextureMapping.DE },
                    }
                },

                {
                    TrainCarType.CabooseRed,
                    new TextureMapping
                    {
                        { "CabooseExterior_", "CarCabooseRed_Body_01", TextureMapping.DNS },
                        { "CabooseInterior_", "CarCabooseRed_Interior_01", TextureMapping.DNS },
                    }
                },
            };

        public static bool TryGetUpdatedTextureName(TrainCarType carType, string oldName, out string newName)
        {
            if (_legacyTextureNameMap.TryGetValue(carType, out TextureMapping textureMapping))
            {
                return textureMapping.TryGetUpdatedName(oldName, out newName);
            }

            newName = null;
            return false;
        }
    }
}
