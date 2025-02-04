using System;

namespace SMShared.Json
{
    [Serializable]
    public class SkinConfigJson : ResourceConfigJson
    {
        public string[]? ResourceNames;
        public BaseTheme BaseTheme = BaseTheme.DVRT;
    }

    [Flags]
    public enum BaseTheme
    {
        DVRT = 0,
        Pristine = 1,
        Demonstrator = 2,
        Relic = 4,
        Primer = 8,

        DVRT_NoDetails = 32,
        Pristine_NoDetails = Pristine | DVRT_NoDetails,
        Demonstrator_NoDetails = Demonstrator | DVRT_NoDetails,
        Relic_NoDetails = Relic | DVRT_NoDetails,
    }
}
