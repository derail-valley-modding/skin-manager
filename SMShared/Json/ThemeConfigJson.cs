using System;
using System.Collections.Generic;
using System.Text;

#if PACKAGER
#nullable disable
#endif

namespace SMShared.Json
{
    [Serializable]
    public class ThemeConfigJson
    {
        public string Version = "1.0.0";
        public ThemeConfigItem[] Themes;
    }

    [Serializable]
    public class ThemeConfigItem
    {
        public string Name;
        public bool HideFromStores;

        public string LabelTextureFile;
        public string LabelBaseColor;
        public string LabelAccentColorA;
        public string LabelAccentColorB;
    }
}
