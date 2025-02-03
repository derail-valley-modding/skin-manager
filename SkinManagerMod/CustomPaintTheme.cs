using DV;
using DV.Customization.Paint;
using DV.ThingTypes;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SkinManagerMod
{
    public class CustomPaintTheme : PaintTheme
    {
        private Dictionary<string, Skin> _skins = new();

        public void AddSkin(Skin skin) => _skins.Add(skin.LiveryId, skin);

        public void RemoveSkin(Skin skin) => _skins.Remove(skin.LiveryId);
        public void RemoveSkin(string liveryId) => _skins.Remove(liveryId);

        public bool SupportsVehicle(string liveryId) => _skins.ContainsKey(liveryId);
        public bool SupportsVehicle(TrainCarLivery livery) => _skins.ContainsKey(livery.id);

        public IEnumerable<TrainCarLivery> SupportedCarTypes => Globals.G.Types.Liveries.Where(type => _skins.ContainsKey(type.id));

        public void Apply(GameObject target, TrainCar train)
        {
            if (_skins.TryGetValue(train.carLivery.id, out var skin))
            {
                var defaultSkin = CarMaterialData.GetDataForCar(train.carLivery.id);

                ApplyToTransform(target, skin, defaultSkin);
            }
        }

        private static void ApplyToTransform(GameObject objectRoot, Skin skin, CarMaterialData defaults)
        {
            foreach (var renderer in objectRoot.GetComponentsInChildren<MeshRenderer>(true))
            {
                if (!renderer.material)
                {
                    continue;
                }

                TextureUtility.ApplyTextures(renderer, skin, defaults);
            }
        }

        public Texture2D? GetBodyTexture()
        {
            return _skins.Values.Select(GetBodyTexture)
                .Where(t => t)
                .OrderBy(t => t!.name)
                .FirstOrDefault();
        }

        private static Texture2D? GetBodyTexture(Skin skin)
        {
            var materialData = CarMaterialData.GetDataForCar(skin.LiveryId);
            if ((materialData.GetBodyMaterial() is CarMaterialData.MaterialTextureData bodyMaterial) &&
                (bodyMaterial.GetTexture(TextureUtility.PropNames.Main) is TexturePropNamePair mainTex))
            {
                if (skin.GetTexture(mainTex.TextureName) is SkinTexture substitute)
                {
                    return substitute.TextureData;
                }

                return (Texture2D)bodyMaterial.Material.GetTexture(TextureUtility.PropNames.Main);
            }

            return null;
        }
    }
}
