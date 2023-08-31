#if PACKAGER
    #nullable disable
#endif

using System;

namespace SMShared
{
    [Serializable]
    public class SkinJsonFileBase
    {
        public string Name;
        public string CarId;

        public SkinJsonFileBase() { }
    }

    public class SkinConfigBase : SkinJsonFileBase
    {
        public string[] ResourceNames;

        public SkinConfigBase() { }
    }
}

#if PACKAGER
    #nullable restore
#endif