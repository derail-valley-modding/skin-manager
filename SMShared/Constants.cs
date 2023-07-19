﻿using System.Linq;

namespace SMShared
{
    public static class Constants
    {
        public const string MOD_ID = "SkinManagerMod";
        public const string MOD_NAME = "Skin Manager";
        public const string MOD_VERSION = "3.1.0";

        public const string SKIN_FOLDER_NAME = "Skins";
        public const string EXPORT_FOLDER_NAME = "Exported";
        public const string CACHE_FOLDER_NAME = "Cache";
        
        public const string MOD_INFO_FILE = "Info.json";
        public const string SKIN_CONFIG_FILE = "skin.json";

        public static readonly string[] SupportedImageExtensions = { ".png", ".jpeg", ".jpg" };

        public static bool IsSupportedExtension(string extension)
        {
            var ext = extension.ToLowerInvariant();
            return SupportedImageExtensions.Contains(ext);
        }

        public const string CUSTOM_TYPE = "CUSTOM";

        public static readonly string[] LiveryNames =
        {
            CUSTOM_TYPE,
            "AutorackBlue",
            "AutorackGreen",
            "AutorackRed",
            "AutorackYellow",
            "BoxcarBrown",
            "BoxcarGreen",
            "BoxcarPink",
            "BoxcarRed",
            "BoxcarMilitary",
            "CabooseRed",
            "FlatbedEmpty",
            "FlatbedMilitary",
            "FlatbedStakes",
            "GondolaGray",
            "GondolaGreen",
            "GondolaRed",
            "HandCar",
            "HopperBrown",
            "HopperTeal",
            "HopperYellow",
            "LocoDE2",
            "LocoDE6",
            "LocoDH4",
            "LocoDM3",
            "LocoS282A",
            "LocoS282B",
            "NuclearFlask",
            "PassengerBlue",
            "PassengerGreen",
            "PassengerRed",
            "RefrigeratorWhite",
            "TankBlack",
            "TankBlue",
            "TankOrange",
            "TankChrome",
            "TankWhite",
            "TankYellow",
        };
    }
}