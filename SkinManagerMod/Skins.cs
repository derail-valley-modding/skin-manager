using DV.ThingTypes;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace SkinManagerMod
{
    public class Skin
    {
        public readonly string LiveryId;
        public readonly string Name;
        public readonly string Path;
        public readonly bool IsDefault;
        public readonly List<SkinTexture> SkinTextures = new List<SkinTexture>();

        public Skin(string liveryId, string name, string directory = null, bool isDefault = false)
        {
            LiveryId = liveryId;
            Name = name;
            Path = directory;
            IsDefault = isDefault;
        }

        public bool ContainsTexture(string name)
        {
            return GetTexture(name) != null;
        }

        public SkinTexture GetTexture(string name)
        {
            return SkinTextures.Find(tex => tex.Name == name);
        }
    }

    public class SkinTexture
    {
        public readonly string Name;
        private Task<Texture2D> task;
        private Texture2D _textureData;

        public Texture2D TextureData
        {
            get
            {
                if (_textureData == null)
                {
                    _textureData = task.Result;
                    task = null;

                    // need to set name for reskinning to work
                    _textureData.name = Name;
                    TextureUtility.SetTextureOptions(_textureData);

                    _textureData.Apply(false, true);
                }
                return _textureData;
            }
        }

        public SkinTexture(string name, Texture2D textureData)
        {
            Name = name;

            // make sure that texture properties are assigned properly
            textureData.name = name;
            TextureUtility.SetTextureOptions(textureData);

            _textureData = textureData;
        }

        public SkinTexture(string name, Task<Texture2D> task)
        {
            Name = name;
            this.task = task;
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