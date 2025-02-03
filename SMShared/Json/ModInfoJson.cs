using System;

namespace SMShared.Json
{
    [Serializable]
    public class ModInfoJson
    {
        public string? Id;
        public string? DisplayName;
        public string Version = "1.0.0";
        public string? Author;
        public readonly string ManagerVersion = "0.27.3";
        public readonly string[] Requirements = { "SkinManagerMod" };
    }
}
