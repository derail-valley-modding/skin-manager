using DV;
using DV.Customization.Paint;
using DV.ThingTypes;
using SMShared.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace SkinManagerMod
{
    public class CarMaterialData
    {
        private static readonly Dictionary<string, CarMaterialData> _liveryToMaterialsMap = new();
        private static readonly Dictionary<string, List<ThemeAlternative>> _materialSubstitutes = new();

        public static void Initialize()
        {
            // built map of all substituted textures
            foreach (var defaultTheme in SkinProvider.BuiltInThemes)
            {
                if (defaultTheme.name == SkinProvider.DefaultThemeName) continue;

                BaseTheme themeType = SkinProvider.GetThemeTypeByName(defaultTheme.name);

                foreach (var substitute in defaultTheme.substitutions)
                {
                    if (!substitute.substitute) continue;

                    string originalName = GetCleanName(substitute.original);

                    if (!_materialSubstitutes.TryGetValue(originalName, out var alternatives))
                    {
                        alternatives = new List<ThemeAlternative>();
                        _materialSubstitutes.Add(originalName, alternatives);
                    }
                    alternatives.Add(new ThemeAlternative(themeType, substitute.substitute));
                }
            }

            foreach (var livery in Globals.G.Types.Liveries)
            {
                var carData = new CarMaterialData(livery);
                _liveryToMaterialsMap.Add(livery.id, carData);
            }
        }

        public static CarMaterialData GetDataForCar(string liveryId) => _liveryToMaterialsMap[liveryId];

        private static string GetCleanName(Material material) => GetCleanName(material.name);
        private static string GetCleanName(string materialName) => materialName.Replace(" (Instance)", string.Empty);

        public readonly string LiveryId;

        private readonly Dictionary<string, MaterialTextureData> _materialData;
        private readonly Dictionary<string, List<MaterialTexTypePair>> _texToMaterialMap;

        public IEnumerable<MaterialTextureData> MaterialData => _materialData.Values;

        public IEnumerable<Material> GetAllMaterials() => MaterialData.Select(m => m.Material);

        public MaterialTextureData? GetBodyMaterial()
        {
            var body = _materialData.Values.FirstOrDefault(m => m.CleanMaterialName.Contains("Body"));
            if (body is not null) return body;

            return _materialData.Values.FirstOrDefault(m => m.CleanMaterialName.Contains("Car"));
        }

        public IEnumerable<MaterialTexTypePair> GetTextureAssignments(string textureName)
        {
            if (_texToMaterialMap.TryGetValue(textureName, out var list))
            {
                return list;
            }
            return Enumerable.Empty<MaterialTexTypePair>();
        }

        public MaterialTextureData? GetDataForMaterial(Material material)
        {
            if (material && _materialData.TryGetValue(GetCleanName(material), out var data))
            {
                return data;
            }
            return null;
        }

        public bool GetDataForMaterial(string materialName, out MaterialTextureData? data)
        {
            return _materialData.TryGetValue(GetCleanName(materialName), out data);
        }

        public CarMaterialData(TrainCarLivery livery)
        {
            LiveryId = livery.id;

            _materialData = new Dictionary<string, MaterialTextureData>();
            _texToMaterialMap = new Dictionary<string, List<MaterialTexTypePair>>();

            IEnumerable<MeshRenderer> renderers = livery.prefab.GetComponentsInChildren<MeshRenderer>(true);

            if (livery.interiorPrefab)
            {
                renderers = renderers.Concat(livery.interiorPrefab.GetComponentsInChildren<MeshRenderer>(true));
            }

            foreach (var renderer in renderers)
            {
                if (!renderer.sharedMaterial) continue;

                string cleanName = GetCleanName(renderer.sharedMaterial);
                if (_materialData.ContainsKey(cleanName)) continue;

                IEnumerable<ThemeAlternative> alternatives;
                if (_materialSubstitutes.TryGetValue(cleanName, out var altList))
                {
                    alternatives = altList;
                }
                else
                {
                    alternatives = Enumerable.Empty<ThemeAlternative>();
                }

                var data = new MaterialTextureData(renderer.sharedMaterial, alternatives);
                _materialData.Add(cleanName, data);

                foreach (var texture in data.AllTextures)
                {
                    RegisterTextureName(texture.TextureName, data.Material, texture.PropertyName);
                }
            }
        }

        private void RegisterTextureName(string texName, Material material, string texType)
        {
            if (string.IsNullOrEmpty(texName)) return;

            if (!_texToMaterialMap.TryGetValue(texName, out var matDataList))
            {
                matDataList = new List<MaterialTexTypePair>();
                _texToMaterialMap[texName] = matDataList;
            }
            
            matDataList.Add(new MaterialTexTypePair(material, texType));
        }
        
        public class MaterialTextureData
        {
            public readonly Material Material;

            private readonly string _cleanName;
            public string CleanMaterialName => _cleanName;

            private readonly List<TexturePropNamePair> _textures = new(TextureUtility.PropNames.AllTextures.Length);

            public List<TexturePropNamePair> AllTextures => _textures;

            private readonly List<ThemeAlternative> _alternatives;

            public TexturePropNamePair? GetTexture(string propName)
            {
                foreach (var tex in _textures)
                {
                    if (tex.PropertyName == propName) return tex;
                }
                return null;
            }

            public MaterialTextureData(Material material, IEnumerable<ThemeAlternative> alternatives)
            {
                Material = material;
                _cleanName = GetCleanName(material);

                foreach (string property in TextureUtility.PropNames.AllTextures)
                {
                    if (material.HasProperty(property) && (material.GetTexture(property) is Texture tex))
                    {
                        _textures.Add(new TexturePropNamePair(tex.name, property));
                    }
                }

                _alternatives = alternatives.ToList();
            }

            public Material GetMaterialForBaseTheme(BaseTheme themeType)
            {
                int altIndex = _alternatives.FindIndex(a => a.Theme == themeType);
                if (altIndex >= 0)
                {
                    return _alternatives[altIndex].Material;
                }
                return Material;
            }
        }

        public readonly struct ThemeAlternative
        {
            public readonly BaseTheme Theme;
            public readonly Material Material;

            public ThemeAlternative(BaseTheme theme, Material substitute)
            {
                Theme = theme;
                Material = substitute;
            }
        }
    }

    public readonly struct MaterialTexTypePair
    {
        public readonly Material Material;
        public readonly string PropertyName;

        public MaterialTexTypePair(Material material, string texType)
        {
            Material = material;
            PropertyName = texType;
        }
    }

    public readonly struct TexturePropNamePair
    {
        public readonly string TextureName;
        public readonly string PropertyName;

        public TexturePropNamePair(string texName, string propName)
        {
            TextureName = texName;
            PropertyName = propName;
        }
    }
}
