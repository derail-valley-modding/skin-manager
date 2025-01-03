using DV;
using DV.ThingTypes;
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
        private static readonly Dictionary<string, CarMaterialData> _liveryToMaterialsMap = new Dictionary<string, CarMaterialData>();

        public static void Initialize()
        {
            foreach (var livery in Globals.G.Types.Liveries)
            {
                var carData = new CarMaterialData(livery);
                _liveryToMaterialsMap.Add(livery.id, carData);
            }
        }

        public static CarMaterialData GetDataForCar(string liveryId) => _liveryToMaterialsMap[liveryId];


        public readonly string LiveryId;
        public readonly CarAreaMaterialData Exterior;
        public readonly CarAreaMaterialData Interior;

        public CarMaterialData(TrainCarLivery livery)
        {
            LiveryId = livery.id;
            Exterior = new CarAreaMaterialData(livery.prefab);
            Interior = new CarAreaMaterialData(livery.interiorPrefab);
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

    public class CarAreaMaterialData
    {
        private readonly List<MaterialTextureData> _materialData;
        private readonly Dictionary<string, List<MaterialTexTypePair>> _texToMaterialMap;

        public IEnumerable<MaterialTexTypePair> GetTextureAssignments(string textureName)
        {
            if (_texToMaterialMap.TryGetValue(textureName, out var list))
            {
                return list;
            }
            return Enumerable.Empty<MaterialTexTypePair>();
        }

        public CarAreaMaterialData(GameObject areaRoot)
        {
            _materialData = new List<MaterialTextureData>();
            _texToMaterialMap = new Dictionary<string, List<MaterialTexTypePair>>();

            if (!areaRoot) return;

            foreach (var renderer in areaRoot.GetComponentsInChildren<MeshRenderer>(true))
            {
                if (MaterialTextureData.TryCreate(renderer, out var data))
                {
                    _materialData.Add(data);

                    RegisterTextureName(data.DiffuseName, data.Material, TextureUtility.PropNames.Main);
                    if (data.BumpMapName != null)
                    {
                        RegisterTextureName(data.BumpMapName, data.Material, TextureUtility.PropNames.BumpMap);
                    }
                    if (data.SpecularOcclusionName != null)
                    {
                        RegisterTextureName(data.SpecularOcclusionName, data.Material, TextureUtility.PropNames.MetalGlossMap);
                    }
                    if (data.EmissionMapName != null)
                    {
                        RegisterTextureName(data.EmissionMapName, data.Material, TextureUtility.PropNames.EmissionMap);
                    }
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
            public string DiffuseName;
            public string BumpMapName;
            public string SpecularOcclusionName;
            public string EmissionMapName;

            public MaterialTextureData(Material material)
            {
                Material = material;
                DiffuseName = GetTexName(material, TextureUtility.PropNames.Main);
                BumpMapName = GetTexName(material, TextureUtility.PropNames.BumpMap);
                SpecularOcclusionName = GetTexName(material, TextureUtility.PropNames.MetalGlossMap);
                EmissionMapName = GetTexName(material, TextureUtility.PropNames.EmissionMap);
            }

            private static string GetTexName(Material material, string property)
            {
                if (!material.HasProperty(property) || !(material.GetTexture(property) is Texture tex)) return null;
                return tex.name;
            }

            public static bool TryCreate(MeshRenderer renderer, out MaterialTextureData data)
            {
                data = renderer.sharedMaterial ? new MaterialTextureData(renderer.sharedMaterial) : null;

                return data != null;
            }
        }
    }
}
