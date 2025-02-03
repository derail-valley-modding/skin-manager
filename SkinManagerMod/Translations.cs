using DV;
using static DV.Localization.LocalizationAPI;

namespace SkinManagerMod
{
    public static class Translations
    {
        public static string LoadingScreen => L("skinman/ui/loading");

        // Comms Radio
        public static string ReskinMode => L("skinman/radio/repaint_mode");
        public static string SelectCarPrompt => L("skinman/radio/select_car");
        public static string ReloadAction => L("skinman/radio/reload");
        public static string SelectAreasPrompt => L("skinman/radio/select_areas");
        public static string SelectPaintPrompt => L("skinman/radio/select_paint");

        public static string SelectAction => CommsRadioLocalization.SELECT;
        public static string ConfirmAction => CommsRadioLocalization.CONFIRM;
        public static string CancelAction => CommsRadioLocalization.CANCEL;

        // Items
        public static string PaintCanNameKey => "skinman/item/paint_can";

        // Settings
        public static class Settings
        {
            public static string AlwaysAllowRadioReskin => L("skinman/ui/allow_career_reskin");
            public static string AllowPaintingUnowned => L("skinman/ui/paint_unowned");
            public static string AllowSlugDE6Skins => L("skinman/ui/de6_for_slug");

            public static string IncreaseAniso => L("skinman/ui/increase_aniso");
            public static string ParallelLoading => L("skinman/ui/parallel_load");
            public static string VerboseLogging => L("skinman/ui/verbose_logging");
            public static string DefaultSkinMode => L("skinman/ui/default_skin_mode");
            public static string TextureTools => L("skinman/ui/texture_tools");
            
            // Per-car texture tools
            public static string SelectCarType => L("skinman/ui/select_car");
            public static string ExportTextures => L("skinman/ui/export_textures");
            public static string ReloadTextures => L("skinman/ui/reload_textures");
            public static string ReloadedCarType(int skinCount, string carTranslationKey) => L("skinman/ui/reloaded_cartype", skinCount.ToString(), L(carTranslationKey));

            // Global texture tools
            public static string ExportAll => L("skinman/ui/export_all");
            public static string ReloadAll => L("skinman/ui/reload_all");
            public static string ReloadedAll(int skinCount) => L("skinman/ui/reloaded_all", skinCount.ToString());
            public static string ExportedAll(int nDone, int nTotal) => L("skinman/ui/exported_all", nDone.ToString(), nTotal.ToString());
        }

        public static class DefaultSkinMode
        {
            public static string PreferReskins => L("skinman/skinmode/prefer_reskins");
            public static string AllowForCustomCars => L("skinman/skinmode/allow_custom");
            public static string AllowForAllCars => L("skinman/skinmode/allow_all");
            public static string PreferDefaults => L("skinman/skinmode/prefer_default");
        }
    }
}
