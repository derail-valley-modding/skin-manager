using DV.Customization.Paint;
using DV.ThingTypes;
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
        public readonly string Path;
        public readonly bool IsDefault;
        public readonly List<SkinTexture> SkinTextures = new List<SkinTexture>();
        public readonly string[] ResourcePaths;

        public event Action<Skin> LoadingFinished;

        public void StartLoadFinishedListener()
        {
            var toAwait = SkinTextures.Select(t => t.LoadingTask)
                .Where(t => !(t is null) && !t.IsCompleted)
                .ToArray();

            Task.WhenAll(toAwait).ContinueWith(OnLoadFinished);
        }

        private void OnLoadFinished(Task _ = null)
        {
            ThreadHelper.Instance.EnqueueAction(() => LoadingFinished?.Invoke(this));
        }

        public Skin(string liveryId, string name, string directory = null, bool isDefault = false, string[] resourcePaths = null)
        {
            LiveryId = liveryId;
            Name = name;
            Path = directory;
            IsDefault = isDefault;
            ResourcePaths = resourcePaths;
        }

        public bool ContainsTexture(string name)
        {
            return GetTexture(name) != null;
        }

        public SkinTexture GetTexture(string name)
        {
            return SkinTextures.Find(tex => tex.Name == name);
        }

        public FileInfo GetResource(string filename)
        {
            string absPath = System.IO.Path.Combine(Path, filename);
            if (File.Exists(absPath))
            {
                return new FileInfo(absPath);
            }

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

        public PaintTheme.Substitution[] CreateSubstitutions()
        {
            var carData = CarMaterialData.GetDataForCar(LiveryId);

            // map default material to new material
            var subMap = new Dictionary<Material, Material>();

            foreach (var texture in SkinTextures)
            {
                var exteriorUses = carData.Exterior.GetTextureAssignments(texture.Name);
                MapTextureUsesToNewMaterial(texture, subMap, exteriorUses);
            }

            var subs = subMap
                .Select(kvp => new PaintTheme.Substitution { original = kvp.Key, substitute = kvp.Value })
                .ToArray();

            return subs;
        }

        private static void MapTextureUsesToNewMaterial(SkinTexture texture, Dictionary<Material, Material> substitutions, IEnumerable<MaterialTexTypePair> uses)
        {
            foreach (var use in uses)
            {
                if (!substitutions.TryGetValue(use.Material, out Material newMaterial))
                {
                    newMaterial = new Material(use.Material);
                    substitutions.Add(use.Material, newMaterial);
                }

                texture.RunOnLoadingComplete(t => newMaterial.SetTexture(use.PropertyName, t.TextureData));
            }
        }
    }

    public class SkinTexture
    {
        public readonly string Name;
        private Task<Texture2D> task;
        public Task LoadingTask => task;

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