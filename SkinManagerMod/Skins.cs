using System.Collections.Generic;
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
        public Texture2D textureData;

        public SkinTexture( string name, Texture2D textureData )
        {
            this.name = name;
            this.textureData = textureData;
        }
    }

    public class SkinGroup
    {
        TrainCarType trainCarType;
        public List<Skin> skins = new List<Skin>();

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