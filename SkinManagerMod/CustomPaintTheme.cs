using DV;
using DV.Customization.Paint;
using DV.ThingTypes;
using SMShared;
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

        public bool SupportsVehicle(string liveryId)
        {
            if (_skins.ContainsKey(liveryId)) return true;
            return Main.Settings.allowDE6SkinsForSlug && (liveryId == Constants.SLUG_LIVERY_ID) && _skins.ContainsKey(Constants.DE6_LIVERY_ID);
        }
        public bool SupportsVehicle(TrainCarLivery livery) => SupportsVehicle(livery.id);

        public IEnumerable<TrainCarLivery> SupportedCarTypes => Globals.G.Types.Liveries.Where(type => _skins.ContainsKey(type.id));

        public void Apply(GameObject target, TrainCar train)
        {
            if (_skins.TryGetValue(train.carLivery.id, out var skin))
            {
                var defaultSkin = CarMaterialData.GetDataForCar(train.carLivery.id);

                ApplyToTransform(target, skin, defaultSkin, GetExludes(train));
            }
            else if ((train.carLivery.id == Constants.SLUG_LIVERY_ID) &&
                Main.Settings.allowDE6SkinsForSlug &&
                _skins.TryGetValue(Constants.DE6_LIVERY_ID, out skin))
            {
                var defaultSkin = CarMaterialData.GetDataForCar(Constants.SLUG_LIVERY_ID);
                ApplyToTransform(target, skin, defaultSkin);
            }
        }

        public void ApplyPaxInterior(GameObject target, TrainCar train)
        {
            if (_skins.TryGetValue(train.carLivery.id, out var skin))
            {
                var defaultSkin = CarMaterialData.GetDataForCar(train.carLivery.id);

                ApplyToTransform(target, skin, defaultSkin, GetExludes(train), true);
            }
        }

        // The optional array 'exclude' makes the loop skip renderers with names included in it.
        // By setting 'invert' to true, it will instead only apply to the renderers in 'exclude'.
        private static void ApplyToTransform(GameObject objectRoot, Skin skin, CarMaterialData defaults, string[]? exclude = null, bool invert = false)
        {
            foreach (var renderer in objectRoot.GetComponentsInChildren<MeshRenderer>(true))
            {                
                if (!renderer.material || SkipExludes(renderer))
                {
                    continue;
                }

                TextureUtility.ApplyTextures(renderer, skin, defaults);
            }

            bool SkipExludes(MeshRenderer renderer)
            {
                return exclude != null && (invert ?
                    exclude.Any(x => x == renderer.name) :
                    !exclude.Any(x => x == renderer.name));
            }
        }

        private static string[]? GetExludes(TrainCar car)
        {
            // Not inverting this if in case more are added in the future.
            if (car.IsVanillaPassenger())
            {
                return new[]
                {
                    "CarPassengerInterior_LOD0",
                    "CarPassengerInterior_LOD1",
                    "CarPassengerInterior_LOD2",
                };
            }

            return null;
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
