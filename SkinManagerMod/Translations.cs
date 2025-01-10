using DV;
using static DV.Localization.LocalizationAPI;

namespace SkinManagerMod
{
    public static class Translations
    {
        // Comms Radio
        public static string SelectCarPrompt => L("skinman/radio/select_car");
        public static string ReloadAction => L("skinman/radio/reload");
        public static string SelectAreasPrompt => L("skinman/radio/select_areas");
        public static string SelectPaintPrompt => L("skinman/radio/select_paint");

        public static string SelectAction => CommsRadioLocalization.SELECT;
        public static string ConfirmAction => CommsRadioLocalization.CONFIRM;
        public static string CancelAction => CommsRadioLocalization.CANCEL;

        // Items
        public static string PaintCanNameKey => "skinman/item/paint_can";
    }
}
