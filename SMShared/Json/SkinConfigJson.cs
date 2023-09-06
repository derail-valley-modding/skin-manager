using System;

#if PACKAGER
#nullable disable
#endif

namespace SMShared.Json
{
    [Serializable]
    public class SkinConfigJson : ResourceConfigJson
    {
        public string[] ResourceNames;
    }
}
