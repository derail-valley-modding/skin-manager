using System.Linq;

namespace SMShared
{
    public static class Constants
    {
        public const string MOD_ID = "SkinManagerMod";
        public const string MOD_NAME = "Skin Manager";
        public const string MOD_VERSION = "4.0.0";

        public const string SKIN_FOLDER_NAME = "Skins";
        public const string EXPORT_FOLDER_NAME = "Exported";
        public const string CACHE_FOLDER_NAME = "Cache";
        
        public const string MOD_INFO_FILE = "Info.json";
        public const string SKIN_CONFIG_FILE = "skin.json";
        public const string SKIN_RESOURCE_FILE = "skin_resource.json";

        public const string PAINT_CAN_LABEL_FILENAME = "can_label";
        public const string CUSTOM_THEME_SAVEDATA_KEY = "SM_Custom_Theme";

        public static readonly string[] SupportedImageExtensions = { ".png", ".jpeg", ".jpg" };

        public static bool IsSupportedExtension(string extension)
        {
            var ext = extension.ToLowerInvariant();
            return SupportedImageExtensions.Contains(ext);
        }

        public static bool IsSkinConfigFile(string filename)
        {
            return filename.EndsWith(MOD_INFO_FILE) ||
                filename.EndsWith(SKIN_CONFIG_FILE) ||
                filename.EndsWith(SKIN_RESOURCE_FILE);
        }

        public const string CUSTOM_TYPE = "CUSTOM";

        public static readonly string[] LiveryNames =
        {
            CUSTOM_TYPE,
            "LocoDE2",
            "LocoDE6",
            "LocoDE6Slug",
            "LocoDH4",
            "LocoDM1U",
            "LocoDM3",
            "LocoMicroshunter",
            "LocoS282A",
            "LocoS282B",
            "LocoS060",

            "HandCar",
            "CabooseRed",
            "NuclearFlask",

            "AutorackBlue",
            "AutorackGreen",
            "AutorackRed",
            "AutorackYellow",

            "BoxcarBrown",
            "BoxcarGreen",
            "BoxcarPink",
            "BoxcarRed",
            "BoxcarMilitary",
            "RefrigeratorWhite",

            "FlatbedEmpty",
            "FlatbedMilitary",
            "FlatbedStakes",
            "FlatbedShort",

            "GondolaGray",
            "GondolaGreen",
            "GondolaRed",

            "HopperBrown",
            "HopperTeal",
            "HopperYellow",

            "PassengerBlue",
            "PassengerGreen",
            "PassengerRed",

            "StockBrown",
            "StockGreen",
            "StockRed",

            "TankBlack",
            "TankBlue",
            "TankOrange",
            "TankShortMilk",
            "TankChrome",
            "TankWhite",
            "TankYellow",
        };

        public const string DE6_LIVERY_ID = "LocoDE6";
        public const string SLUG_LIVERY_ID = "LocoDE6Slug";
    }
}
