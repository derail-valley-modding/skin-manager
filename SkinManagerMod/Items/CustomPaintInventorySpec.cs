using DV.Customization.Paint;
using System.Collections;
using UnityEngine;

namespace SkinManagerMod.Items
{
    public class CustomPaintInventorySpec : InventoryItemSpec
    {
        public PaintTheme Theme;

        public static CustomPaintInventorySpec Create(InventoryItemSpec original, GameObject holder, PaintTheme theme)
        {
            var subHolder = new GameObject($"PaintInventorySM_{theme.name}");
            subHolder.transform.parent = holder.transform;

            var spec = subHolder.AddComponent<CustomPaintInventorySpec>();
            spec.Theme = theme;
            spec.localizationKeyName = Translations.PaintCanNameKey;
            spec.localizationKeyDescription = original.localizationKeyDescription;
            spec.iconRenderPositionOffset = original.iconRenderPositionOffset;
            spec.iconRenderAngleOffset = original.iconRenderAngleOffset;

            spec.immuneToDumpster = original.immuneToDumpster;
            spec.isEssential = original.isEssential;

            spec.itemPrefabName = PaintFactory.GetDummyPrefabName(theme.name);
            spec.itemIconSprite = original.itemIconSprite;
            spec.itemIconSpriteStandard = original.itemIconSpriteStandard;
            spec.itemIconSpriteDropped = original.itemIconSpriteDropped;

            spec.previewPrefab = original.previewPrefab;
            spec.previewBounds = original.previewBounds;
            spec.previewRotation = original.previewRotation;

            var nameProvider = subHolder.AddComponent<PaintCanThemeNameProvider>();
            nameProvider.theme = theme;

            return spec;
        }
    }
}