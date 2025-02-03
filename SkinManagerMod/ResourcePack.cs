using DV;
using DV.ThingTypes;
using Newtonsoft.Json;
using SMShared.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SkinManagerMod
{
    public class ResourcePack : ResourceConfigJson
    {
        [JsonIgnore]
        public TrainCarLivery Livery;

        [JsonIgnore]
        public string FolderPath;

        [JsonIgnore]
        public List<SkinTexture> Textures = new List<SkinTexture>();

        public ResourcePack(string name, string folderPath, TrainCarLivery livery)
        {
            Name = name;
            FolderPath = folderPath;
            Livery = livery;
        }

        public static ResourcePack? LoadFromFile(string filePath)
        {
            try
            {
                string contents = File.ReadAllText(filePath);
                var result = JsonConvert.DeserializeObject<ResourcePack>(contents)!;

                result.FolderPath = Path.GetDirectoryName(filePath);
                result.Livery = Globals.G.Types.Liveries
                    .FirstOrDefault(l => string.Equals(l.id, result.CarId, StringComparison.OrdinalIgnoreCase));

                if (result.Livery != null)
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
    }
}
