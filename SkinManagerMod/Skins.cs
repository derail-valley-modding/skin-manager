using DV.Customization.Paint;
using DV.ThingTypes;
using SMShared.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

namespace SkinManagerMod
{
    public class Skin
    {
        public readonly string LiveryId;
        public readonly string Name;
        public readonly string? Path;
        public readonly bool IsDefault;
        public readonly List<SkinTexture> SkinTextures = new();
        public readonly string[]? ResourcePaths;
        public readonly BaseTheme BaseTheme;

        private Skin(string liveryId, string name, string? directory, bool isDefault, string[]? resourcePaths, BaseTheme baseTheme)
        {
            LiveryId = liveryId;
            Name = name;
            Path = directory;
            IsDefault = isDefault;
            ResourcePaths = resourcePaths;
            BaseTheme = baseTheme;
        }

        public static Skin Custom(string liveryId, string name, string directory, BaseTheme baseTheme, string[]? resourcePaths = null)
        {
            return new Skin(liveryId, name, directory, false, resourcePaths, baseTheme);
        }

        public static Skin Custom(SkinConfig config)
        {
            return new Skin(config.CarId, config.Name, config.FolderPath, false, config.ResourcePaths, config.BaseTheme);
        }

        public static Skin Default(string liveryId, BaseTheme baseTheme)
        {
            return new Skin(liveryId, GetDefaultSkinName(liveryId), null, true, null, baseTheme);
        }

        public bool ContainsTexture(string name)
        {
            return GetTexture(name) != null;
        }

        public SkinTexture? GetTexture(string name)
        {
            return SkinTextures.Find(tex => tex.Name == name);
        }

        public FileInfo? GetResource(string filename)
        {
            string absPath = System.IO.Path.Combine(Path, filename);
            if (File.Exists(absPath))
            {
                return new FileInfo(absPath);
            }

            if (ResourcePaths is null) return null;

            foreach (string resourceFolder in ResourcePaths)
            {
                absPath = System.IO.Path.Combine(resourceFolder, filename);
                if (File.Exists(absPath))
                {
                    return new FileInfo(absPath);
                }
            }

            return null;
        }

        public static string GetDefaultSkinName(string liveryId) => $"Default_{liveryId}";
    }

    public class SkinTexture
    {
        public readonly string Name;
        public readonly DateTime LastModified;

        private Task<Texture2D?>? task;
        public Task? LoadingTask => task;

        private Texture2D? _textureData;

        public Texture2D TextureData
        {
            get
            {
                if (_textureData is null)
                {
                    _textureData = task!.Result!;
                    task = null;

                    // need to set name for reskinning to work
                    _textureData.name = Name;
                    TextureUtility.SetTextureOptions(_textureData);

                    _textureData.Apply(false, true);
                }
                return _textureData;
            }
        }

        public void RunOnLoadingComplete(Action<SkinTexture> toRun)
        {
            if (task is null || task.IsCompleted)
            {
                toRun(this);
            }
            else
            {
                task.ContinueWith(_ => toRun(this));
            }
        }

        public SkinTexture(string name, Texture2D textureData, DateTime? lastModified = null)
        {
            Name = name;

            // make sure that texture properties are assigned properly
            textureData.name = name;
            TextureUtility.SetTextureOptions(textureData);

            _textureData = textureData;
            LastModified = lastModified ?? DateTime.MinValue;
        }

        public SkinTexture(string name, Task<Texture2D?> task, DateTime lastModified)
        {
            Name = name;
            this.task = task;
            LastModified = lastModified;
        }
    }

    public class SkinGroup
    {
        public readonly TrainCarLivery TrainCarType;
        public readonly List<Skin> Skins = new List<Skin>();

        public SkinGroup(TrainCarLivery trainCarType)
        {
            TrainCarType = trainCarType;
        }

        public Skin GetSkin(string name)
        {
            return Skins.Find(s => s.Name == name);
        }
    }
}