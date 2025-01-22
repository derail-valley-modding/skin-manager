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
                if (!renderer.sharedMaterial) continue;

                var data = new MaterialTextureData(renderer.sharedMaterial);
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

                if (data.DetailAlbedoName != null)
                {
                    RegisterTextureName(data.DetailAlbedoName, data.Material, TextureUtility.PropNames.DetailAlbedo);
                }
                if (data.DetailNormalName != null)
                {
                    RegisterTextureName(data.DetailNormalName, data.Material, TextureUtility.PropNames.DetailNormal);
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
            public readonly Material SubstitutedMaterial;

            public string DiffuseName;
            public string BumpMapName;
            public string SpecularOcclusionName;
            public string EmissionMapName;

            public string DetailAlbedoName;
            public string DetailNormalName;

            public MaterialTextureData(Material material)
            {
                Material = material;
                DiffuseName = GetTexName(material, TextureUtility.PropNames.Main);
                BumpMapName = GetTexName(material, TextureUtility.PropNames.BumpMap);
                SpecularOcclusionName = GetTexName(material, TextureUtility.PropNames.MetalGlossMap);
                EmissionMapName = GetTexName(material, TextureUtility.PropNames.EmissionMap);

                DetailAlbedoName = GetTexName(material, TextureUtility.PropNames.DetailAlbedo);
                DetailNormalName = GetTexName(material, TextureUtility.PropNames.DetailNormal);
            }

            private static string GetTexName(Material material, string property)
            {
                if (!material.HasProperty(property) || !(material.GetTexture(property) is Texture tex)) return null;
                return tex.name;
            }
        }
    }
}
