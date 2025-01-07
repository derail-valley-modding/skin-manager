#if PACKAGER
    #nullable disable
#else
    using DV.ThingTypes;
#endif

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace SMShared
{
    public static class Remaps
    {
        private static readonly Dictionary<string, string> _newToOldCarIdMap;

        public static bool TryGetOldTrainCarId(string newId, out string oldId)
        {
            return _newToOldCarIdMap.TryGetValue(newId, out oldId);
        }

        private static readonly Dictionary<string, string> _oldToNewCarIdMap = new Dictionary<string, string>()
        {
            { "loco_621",                   "LocoDE2" },
            { "LocoDiesel",                 "LocoDE6" },
            { "loco_steam_H",               "LocoS282A" },
            { "loco_steam_tender",          "LocoS282B" },

            { "car_flatbed_empty",          "FlatbedEmpty" },
            { "car_flatbed_stakes",         "FlatbedStakes" },
            { "car_flatbed_military_empty", "FlatbedMilitary" },

            { "CarAutorack_Red",            "AutorackRed" },
            { "CarAutorack_Blue",           "AutorackBlue" },
            { "CarAutorack_Green",          "AutorackGreen" },
            { "CarAutorack_Yellow",         "AutorackYellow" },

            { "CarTank_Orange",             "TankOrange" },
            { "CarTank_White",              "TankWhite" },
            { "CarTank_Yellow",             "TankYellow" },
            { "CarTank_Blue",               "TankBlue" },
            { "CarTank_Chrome",             "TankChrome" },
            { "CarTank_Black",              "TankBlack" },

            { "CarBoxcar_Brown",            "BoxcarBrown" },
            { "CarBoxcar_Green",            "BoxcarGreen" },
            { "CarBoxcar_Pink",             "BoxcarPink" },
            { "CarBoxcar_Red",              "BoxcarRed" },
            { "CarBoxcarMilitary",          "BoxcarMilitary" },
            { "CarRefrigerator_White",      "RefrigeratorWhite" },

            { "CarHopper_Brown",            "HopperBrown" },
            { "CarHopper_Teal",             "HopperTeal" },
            { "CarHopper_Yellow",           "HopperYellow" },

            { "CarGondola_Red",             "GondolaRed" },
            { "CarGondola_Green",           "GondolaGreen" },
            { "CarGondola_Grey",            "GondolaGray" },

            { "CarPassenger_Red",           "PassengerRed" },
            { "CarPassenger_Green",         "PassengerGreen" },
            { "CarPassenger_Blue",          "PassengerBlue" },
              
            { "handcar",                    "HandCar" },
            { "CarCaboose_Red",             "CabooseRed" },
            { "CarNuclearFlask",            "NuclearFlask" },
        };

        public static bool TryGetUpdatedCarId(string oldId, out string newId)
        {
            return _oldToNewCarIdMap.TryGetValue(oldId, out newId);
        }

        private class TextureMapping : IEnumerable<KeyValuePair<string, string>>
        {
            private readonly Dictionary<string, string> _map = 
                new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            public static readonly char[] DE = { 'd', 'e' };
            public static readonly char[] DS = { 'd', 's' };
            public static readonly char[] SN = { 's', 'n' };
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
        private static readonly Dictionary<string, TextureMapping> _legacyTextureNameMap = 
            new Dictionary<string, TextureMapping>()
            {
                {
                    "LocoDE2",
                    new TextureMapping
                    {
                        { "exterior_", "LocoDE2_Body_01", TextureMapping.DNS },
                    }
                },

                // 282 & tender new UVs -> no mappings :(

                {
                    "LocoDH4",
                    new TextureMapping
                    {
                        { "LocoDH4_ExteriorBody_01", "LocoDH4_Body_01", TextureMapping.DNS },
                    }
                },

                {
                    "LocoDE6",
                    new TextureMapping
                    {
                        { "LocoDiesel_bogies_", "LocoDE6_Bogie_01", TextureMapping.DNS },
                        { "LocoDiesel_cab_", "LocoDE6_Interior_01", TextureMapping.DNS },
                        { "LocoDiesel_engine_", "LocoDE6_Engine_01", TextureMapping.DNS },
                        { "LocoDiesel_exterior_", "LocoDE6_Body_01", TextureMapping.DNS },
                        { "LocoDiesel_gauges_01", "LocoDE6_Gauges_01", TextureMapping.DE },
                    }
                },

                {
                    "CabooseRed",
                    new TextureMapping
                    {
                        { "CabooseExterior_", "CarCabooseRed_Body_01", TextureMapping.DNS },
                        { "CabooseInterior_", "CarCabooseRed_Interior_01", TextureMapping.DNS },
                    }
                },

                // Boxcars
                {
                    "BoxcarMilitary",
                    new TextureMapping
                    {
                        { "CarMilitaryBoxcar_", "CarBoxcarMilitary_01", TextureMapping.DS },
                        { "CarMilitaryBoxcar_n", "CarBoxcar_Paint_01n" },
                    }
                },
                {
                    "RefrigeratorWhite",
                    new TextureMapping
                    {
                        { "Refrigerated_boxcar_", "CarRefrigerator_", TextureMapping.DNS },
                    }
                },

                {
                    "NuclearFlask",
                    new TextureMapping
                    {
                        { "CarFlaskCarrier_", "CarNuclearFlask_", TextureMapping.DNS },
                    }
                },

                // Passenger Cars
                {
                    "PassengerBlue",
                    new TextureMapping
                    {
                        { "CarPassengerBlue_d", "CarPassengerBlue_01d" },
                        { "CarPassengerClassII_s", "CarPassengerClassII_01s" },
                        { "CarPassenger_n", "CarPassenger_01n" }
                    }
                },
                {
                    "PassengerGreen",
                    new TextureMapping
                    {
                        { "CarPassengerGreen_d", "CarPassengerGreen_01d" },
                        { "CarPassengerClassI_s", "CarPassengerClassI_01s" },
                        { "CarPassenger_n", "CarPassenger_01n" }
                    }
                },
                {
                    "PassengerRed",
                    new TextureMapping
                    {
                        { "CarPassengerRed_d", "CarPassengerRed_01d" },
                        { "CarPassengerClassII_s", "CarPassengerClassII_01s" },
                        { "CarPassenger_n", "CarPassenger_01n" }
                    }
                },
            };

        static Remaps()
        {
            _newToOldCarIdMap = _oldToNewCarIdMap.ToDictionary(kvp => kvp.Value, kvp => kvp.Key);

            // Autorack Colors
            var autoTypes = new[] { "Blue", "Green", "Red", "Yellow" };

            foreach (string color in autoTypes)
            {
                _legacyTextureNameMap.Add($"Autorack{color}", new TextureMapping
                {
                    { $"CarAutorack{color}_d", $"CarAutorack{color}_01d" },
                    { $"CarAutorack{color}_", "CarAutorack_01", TextureMapping.SN },
                });
            }

            // Tanker Colors
            var tankerTypes = new[]
            {
                ("Black", "CarTankNoPaint_01n"),
                ("Blue", "CarTankNoPaint_01n"),
                ("Chrome", "CarTankNoPaint_01n"),
                ("White", "CarTankNoPaint_01n"),
                ("Yellow", "CarTankPaint_01n"),
            };

            foreach (var color in tankerTypes)
            {
                _legacyTextureNameMap.Add($"Tank{color.Item1}",
                    new TextureMapping
                    {
                        { $"CarTank_{color.Item1}_01", $"CarTank{color.Item1}_01", TextureMapping.DS },
                        { $"CarTank_{color.Item1}_01n", color.Item2 }
                    }
                );
            }

            // orange just *has* to be special...
            _legacyTextureNameMap.Add("TankOrange", new TextureMapping
            {
                { "CarTank_Orange_01", "CarTankOrange_01" },
                { "CarTank_Orange_01s", "CarTankOrange_01s" },
                { "CarTank_Paint_01n", "CarTankPaint_01n" },
            });
        }

        public static bool TryGetUpdatedTextureName(string liveryId, string oldName, out string newName)
        {
            if (_legacyTextureNameMap.TryGetValue(liveryId, out TextureMapping textureMapping))
            {
                return textureMapping.TryGetUpdatedName(oldName, out newName);
            }

            newName = null;
            return false;
        }
    }
}

#if PACKAGER
    #nullable restore
#endif