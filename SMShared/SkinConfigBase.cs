#if PACKAGER
    #nullable disable
#endif

using System;

namespace SMShared
{
    [Serializable]
    public class SkinConfigBase
    {
        public string Name;
        public string CarId;

        public SkinConfigBase() { }
    }
}

#if PACKAGER
    #nullable restore
#endif