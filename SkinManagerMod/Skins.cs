using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace SkinManagerMod
{
    public class Skin
    {
        public string name;
        public List<SkinTexture> skinTextures = new List<SkinTexture>();

        public Skin(string name)
        {
            this.name = name;
        }

        public bool ContainsTexture(string name)
        {
            foreach(var tex in skinTextures)
            {
                if (tex.name == name)
                {
                    return true;
                }
            }

            return false;
        }

        public SkinTexture GetTexture(string name)
        {
            foreach (var tex in skinTextures)
            {
                if (tex.name == name)
                {
                    return tex;
                }
            }

            return null;
        }
    }

    public class SkinTexture
    {
        public string name;
        private Texture2D _textureData;
        private Task<Texture2D> task;
        public List<Skin> skins = new List<Skin>();
        public Texture2D TextureData
        {
            get
            {
                if (_textureData == null)
                {
                    _textureData = task.Result;
                    task = null;
                    Main.SetTextureOptions(_textureData);
                    _textureData.Apply(false, true);
                }
                return _textureData;
            }
        }

        public SkinTexture( string name, Task<Texture2D> task )
        {
            this.name = name;
            this.task = task;
        }
    }

    public class SkinGroup
    {
        TrainCarType trainCarType;

        public SkinGroup( TrainCarType trainCarType )
        {
            this.trainCarType = trainCarType;
        }

        public Skin GetSkin( string name )
        {
            foreach( var skin in skins )
            {
                if( skin.name == name )
                {
                    return skin;
                }
            }

            return null;
        }
    }
}