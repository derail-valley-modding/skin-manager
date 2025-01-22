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
        public readonly string Path;
        public readonly bool IsDefault;
        public readonly List<SkinTexture> SkinTextures = new List<SkinTexture>();
        public readonly string[] ResourcePaths;
        public readonly BaseTheme BaseTheme;

        public readonly bool IsThemeable;

        public event Action<Skin> LoadingFinished;

        private PaintTheme.Substitution[] _cachedSubstitutions = null;

        public void StartLoadFinishedListener()
        {
            var toAwait = SkinTextures
                .Select(t => t.LoadingTask)
                .Where(t => !(t is null) && !t.IsCompleted);

            var taskArr = toAwait.ToArray();
            if ((taskArr.Length == 0) || taskArr.All(t => t.IsCompleted))
            {
                LoadingFinished?.Invoke(this);
            }
            else
            {
                Task.WhenAll(taskArr).ContinueWith(OnLoadFinished);
            }
        }

        private void OnLoadFinished(Task _ = null)
        {
            ThreadHelper.Instance.EnqueueAction(() => LoadingFinished?.Invoke(this));
        }

        private Skin(string liveryId, string name, string directory, bool isDefault, string[] resourcePaths, BaseTheme baseTheme)
        {
            LiveryId = liveryId;
            Name = name;
            Path = directory;
            IsDefault = isDefault;
            ResourcePaths = resourcePaths;
            BaseTheme = baseTheme;

            IsThemeable = SkinProvider.IsThemeable(liveryId);
        }

        public static Skin Custom(string liveryId, string name, string directory, BaseTheme baseTheme, string[] resourcePaths = null)
        {
            return new Skin(liveryId, name, directory, false, resourcePaths, baseTheme);
        }

        public static Skin Custom(SkinConfig config)
        {
            return new Skin(config.CarId, config.Name, config.FolderPath, false, config.ResourcePaths, config.BaseTheme);
        }

        public static Skin Default(string liveryId)
        {
            return new Skin(liveryId, GetDefaultSkinName(liveryId), null, true, null, BaseTheme.DVRT);
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

        public PaintTheme.Substitution[] GetSubstitutions()
        {
            if (!(_cachedSubstitutions is null)) return _cachedSubstitutions;

            var carData = CarMaterialData.GetDataForCar(LiveryId);

            // map default material to new material
            var subMap = new Dictionary<Material, Material>();

            foreach (var texture in SkinTextures)
            {
                var exteriorUses = carData.Exterior.GetTextureAssignments(texture.Name);
                MapTextureUsesToNewMaterial(texture, subMap, exteriorUses);

                var interiorUses = carData.Interior.GetTextureAssignments(texture.Name);
                MapTextureUsesToNewMaterial(texture, subMap, interiorUses);
            }

            var subs = subMap
                .Select(kvp => new PaintTheme.Substitution { original = kvp.Key, substitute = kvp.Value })
                .ToArray();

            _cachedSubstitutions = subs;
            return subs;
        }

        private static Material GetBaseMaterial(Material defaultMaterial, BaseTheme themeType)
        {
            var result = SkinProvider.GetBuiltinTheme(themeType);

            if (result && result.TryGetSubstitute(defaultMaterial, out var substitution))
            {
                return substitution.substitute;
            }
            return defaultMaterial;
        }

        private void MapTextureUsesToNewMaterial(SkinTexture texture, Dictionary<Material, Material> substitutions, IEnumerable<MaterialTexTypePair> uses)
        {
            foreach (var use in uses)
            {
                if (!substitutions.TryGetValue(use.Material, out Material newMaterial))
                {
                    var baseMaterial = GetBaseMaterial(use.Material, BaseTheme);
                    newMaterial = new Material(use.Material);

                    if (BaseTheme.HasFlag(BaseTheme.DVRT_NoDetails))
                    {
                        foreach (string propName in TextureUtility.PropNames.DetailTextures)
                        {
                            newMaterial.SetTexture(propName, null);
                        }
                    }

                    substitutions.Add(use.Material, newMaterial);
                }

                texture.RunOnLoadingComplete(t => newMaterial.SetTexture(use.PropertyName, t.TextureData));
            }
        }

        public static string GetDefaultSkinName(string liveryId) => $"Default_{liveryId}";
    }

    public class SkinTexture
    {
        public readonly string Name;
        public readonly DateTime LastModified;

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

        public SkinTexture(string name, Texture2D textureData, DateTime? lastModified = null)
        {
            Name = name;

            // make sure that texture properties are assigned properly
            textureData.name = name;
            TextureUtility.SetTextureOptions(textureData);

            _textureData = textureData;
            LastModified = lastModified ?? DateTime.MinValue;
        }

        public SkinTexture(string name, Task<Texture2D> task, DateTime lastModified)
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