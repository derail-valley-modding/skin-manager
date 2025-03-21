﻿using DV;
using DV.ThingTypes;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using SMShared.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityModManagerNet;

namespace SkinManagerMod
{
    public class SkinConfig : SkinConfigJson
    {
        [JsonIgnore]
        public TrainCarLivery Livery;

        [JsonIgnore]
        public string FolderPath;

        [JsonIgnore]
        public Skin? Skin;

        [JsonIgnore]
        public List<ResourcePack> Resources = new();

        [JsonIgnore]
        public string[] ResourcePaths => Resources.Select(r => r.FolderPath).ToArray();

#pragma warning disable CS8618
        public SkinConfig() { }
#pragma warning restore CS8618

        public SkinConfig(string name, string folderPath, TrainCarLivery livery)
        {
            Name = name;
            FolderPath = folderPath;
            Livery = livery;
            CarId = livery.id;
        }

        private static readonly JsonSerializerSettings _jsonSettings;

        static SkinConfig()
        {
            _jsonSettings = new JsonSerializerSettings();
            _jsonSettings.Converters.Add(new StringEnumConverter());
        }

        public static SkinConfig? LoadFromFile(string filePath)
        {
            try
            {
                string contents = File.ReadAllText(filePath);
                var result = JsonConvert.DeserializeObject<SkinConfig>(contents, _jsonSettings)!;

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

    public class ModSkinCollection : IEnumerable<SkinConfig>
    {
        public UnityModManager.ModEntry modEntry;

        public bool IsEnabled => modEntry.Active;

        public readonly List<SkinConfig> Configs = new List<SkinConfig>();
        public readonly List<ResourcePack> ResourcePacks = new List<ResourcePack>();

        public ModSkinCollection(UnityModManager.ModEntry modEntry)
        {
            this.modEntry = modEntry;
        }

        public IEnumerator<SkinConfig> GetEnumerator() => Configs.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => Configs.GetEnumerator();
    }
}
