using DV;
using DV.ThingTypes;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityModManagerNet;

namespace SkinManagerMod
{
    [Serializable]
    public class SkinConfig : IEquatable<SkinConfig>
    {
        public string Name;
        public string CarId;

        [JsonIgnore]
        public TrainCarLivery livery;

        [JsonIgnore]
        public string folderPath;

        [JsonIgnore]
        public Skin skin;

        public static SkinConfig LoadFromFile(string filePath)
        {
            try
            {
                string contents = File.ReadAllText(filePath);
                var result = JsonConvert.DeserializeObject<SkinConfig>(contents);

                result.folderPath = Path.GetDirectoryName(filePath);
                result.livery = Globals.G.Types.Liveries
                    .FirstOrDefault(l => string.Equals(l.id, result.CarId, StringComparison.OrdinalIgnoreCase));

                if (result.livery != null)
                {
                    return result;
                }
                else
                {
                    Main.Error($"Unknown livery id: {result.CarId}");
                }
            }
            catch
            {
                Main.Error($"Failed to parse skin config, please check the syntax: {filePath}");
            }

            return null;
        }

        public bool Equals(SkinConfig other)
        {
            return Name == other?.Name;
        }
    }

    public class ModSkinCollection : IEnumerable<SkinConfig>, IEquatable<ModSkinCollection>
    {
        public UnityModManager.ModEntry modEntry;

        public bool IsEnabled => modEntry.Active;

        public readonly List<SkinConfig> Configs = new List<SkinConfig>();

        public ModSkinCollection(UnityModManager.ModEntry modEntry)
        {
            this.modEntry = modEntry;
        }

        public IEnumerator<SkinConfig> GetEnumerator() => Configs.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => Configs.GetEnumerator();

        public bool Equals(ModSkinCollection other)
        {
            return modEntry.Info == other?.modEntry.Info;
        }
    }
}
