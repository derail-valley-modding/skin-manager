using DV;
using DV.CabControls;
using DV.Customization.Paint;
using DV.Interaction;
using DV.Localization;
using DV.Shops;
using DV.ThingTypes;
using OokiiTsuki.Palette;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEngine;

namespace SkinManagerMod.Items
{
    public static class PaintFactory
    {
        public const string DEFAULT_CAN_PREFAB_NAME = "PaintCan";

        public static string GetDummyPrefabName(string themeName) => $"SM_ItemSpec_{themeName}";

        private static GameObject _defaultCanPrefab = null;
        public static GameObject DefaultCanPrefab
        {
            get
            {
                if (_defaultCanPrefab is null)
                {
                    _defaultCanPrefab = Resources.Load("PaintCan") as GameObject;
                }
                return _defaultCanPrefab;
            }
        }

        private static Material _defaultCanLabelMaterial = null;
        public static Material DefaultCanLabelMaterial
        {
            get
            {
                if (_defaultCanLabelMaterial is null)
                {
                    var renderer = DefaultCanPrefab.GetComponentInChildren<MeshRenderer>();
                    _defaultCanLabelMaterial = renderer.sharedMaterials[1];
                }
                return _defaultCanLabelMaterial;
            }
        }

        private static ShopItemData _defaultCanShopData = null;
        private static ShopItemData DefaultCanShopData
        {
            get
            {
                if (_defaultCanShopData is null)
                {
                    _defaultCanShopData = GlobalShopController.Instance.GetShopItemData(DEFAULT_CAN_PREFAB_NAME);
                }
                return _defaultCanShopData;
            }
        }

        private static readonly Dictionary<string, Material> _labelMaterials = new Dictionary<string, Material>();

        private static GameObject _labelTextGizmo;
        private static TextMeshProUGUI _themeNameTextMesh;
        private static TextMeshProUGUI _carTypesTextMesh;
        private static TMP_FontAsset _labelFont;
        private static Camera _textCamera;

        private static readonly Texture2D _labelBackgroundTexture;
        private static readonly Texture2D _labelAccentBottom;
        private static readonly Texture2D _labelAccentCenter;
        private static readonly Texture2D _labelAccentTop;

        private static readonly Sprite _inventoryIcon;
        private static readonly Sprite _droppedIcon;

        private static Material _blittingMaterial;

        private static string TexPath(string filename) => Path.Combine(Main.Instance.Path, "Resources", filename);

        static PaintFactory()
        {
            _labelBackgroundTexture = new Texture2D(0, 0, TextureFormat.RGBA32, mipChain: true, linear: false);
            _labelBackgroundTexture.LoadImage(File.ReadAllBytes(TexPath("label_background.png")));

            _labelAccentBottom = new Texture2D(0, 0, TextureFormat.RGBA32, mipChain: true, linear: false);
            _labelAccentBottom.LoadImage(File.ReadAllBytes(TexPath("label_accent_bottom.png")));

            _labelAccentCenter = new Texture2D(0, 0, TextureFormat.RGBA32, mipChain: true, linear: false);
            _labelAccentCenter.LoadImage(File.ReadAllBytes(TexPath("label_accent_center.png")));

            _labelAccentTop = new Texture2D(0, 0, TextureFormat.RGBA32, mipChain: true, linear: false);
            _labelAccentTop.LoadImage(File.ReadAllBytes(TexPath("label_accent_top.png")));


            // Inventory Icons
            var iconTex = new Texture2D(0, 0, TextureFormat.RGBA32, false, false);
            iconTex.LoadImage(File.ReadAllBytes(TexPath("can_icon.png")));

            var spriteSize = new Rect(0, 0, iconTex.width, iconTex.height);
            var spritePivot = new Vector2(0.5f, 0.5f);
            _inventoryIcon = Sprite.Create(iconTex, spriteSize, spritePivot);

            var droppedTex = new Texture2D(0, 0, TextureFormat.RGBA32, false, false);
            droppedTex.LoadImage(File.ReadAllBytes(TexPath("can_dropped.png")));

            spriteSize = new Rect(0, 0, droppedTex.width, droppedTex.height);
            _droppedIcon = Sprite.Create(droppedTex, spriteSize, spritePivot);


            _blittingMaterial = new Material(Shader.Find("UI/Unlit/Transparent"))
            {
                renderQueue = (int)UnityEngine.Rendering.RenderQueue.AlphaTest
            };
        }

        public static GameObject InstantiateCustomCan(PaintTheme theme, Vector3 position, Quaternion rotation)
        {
            var newCan = UnityEngine.Object.Instantiate(DefaultCanPrefab, position, rotation);

            var itemSpec = newCan.GetComponent<InventoryItemSpec>();
            itemSpec.localizationKeyName = Translations.PaintCanNameKey;
            itemSpec.ItemIconSprite = _inventoryIcon;
            itemSpec.ItemIconSpriteDropped = _droppedIcon;

            var item = newCan.GetComponent<ItemBase>();
            item.InventorySpecs = itemSpec;

            var labelMaterial = GetCanLabelMaterial(theme.name);
            ApplyLabelMaterial(newCan, labelMaterial);

            var nameProvider = newCan.AddComponent<PaintCanThemeNameProvider>();
            nameProvider.theme = theme;

            var restocker = newCan.GetComponent<ShopRestocker>();
            restocker.itemPrefabName = GetDummyPrefabName(theme.name);

            var paintSpec = newCan.GetComponent<PaintCan>();
            paintSpec.theme = theme;

            return newCan;
        }

        private static Material GetCanLabelMaterial(string themeName)
        {
            if (!_labelMaterials.TryGetValue(themeName, out Material material))
            {
                material = new Material(DefaultCanLabelMaterial)
                {
                    name = $"PaintCan_Label_SM_{themeName}",
                };

                if (SkinProvider.TryGetThemeSettings(themeName, out ThemeSettings settings))
                {
                    if (settings.CanLabel != null)
                    {
                        material.mainTexture = settings.CanLabel.TextureData;
                    }
                    else
                    {
                        GenerateDefaultLabelMaterial(material, themeName, settings);
                    }
                }
                else
                {
                    GenerateDefaultLabelMaterial(material, themeName);
                }

                _labelMaterials.Add(themeName, material);
            }
            return material;
        }

        public static void ApplyLabelMaterial(GameObject canPrefab, Material labelMaterial)
        {
            var renderers = canPrefab.GetComponentsInChildren<MeshRenderer>(true);
            foreach (var renderer in renderers)
            {
                var materials = renderer.materials;
                materials[1] = labelMaterial;
                renderer.materials = materials;
            }
        }

        public static void ApplyLabelMaterialToShelfItem(GameObject shelfItem, string themeName)
        {
            var labelMaterial = GetCanLabelMaterial(themeName);
            var renderers = shelfItem.GetComponentsInChildren<MeshRenderer>(true);
            
            foreach (var renderer in renderers)
            {
                if (renderer.name != "PaintCan_LOD0") continue;

                var materials = renderer.materials;
                materials[1] = labelMaterial;
                renderer.materials = materials;
            }
        }

        private static void GenerateDefaultLabelMaterial(Material labelMaterial, string themeName, ThemeSettings themeSettings = null)
        {
            SkinProvider.TryGetTheme(themeName, out var theme);

            Color baseColor, accentA, accentB;

            if (themeSettings != null)
            {
                // use user supplied colors
                baseColor = themeSettings.LabelBaseColor ?? Color.white;
                accentA = themeSettings.LabelAccentColorA ?? Color.white;
                accentB = themeSettings.LabelAccentColorB ?? Color.white;
            }
            else
            {
                // calculate label colors from texture palette
                var bodySubstitution = theme.substitutions.OrderBy(s => s.original.name)
                    .FirstOrDefault(s => s.original.name.Contains("Body"));

                if (!bodySubstitution.original)
                {
                    Main.Error($"No body sub for theme {themeName}");
                    labelMaterial.mainTexture = Texture2D.whiteTexture;
                    return;
                }

                var palette = Palette.Generate((Texture2D)bodySubstitution.substitute.mainTexture, 12);

                //WritePaletteBmp(palette, themeName);

                var byPopulation = palette.mSwatches.OrderByDescending(s => s.Population).ToList();
                baseColor = byPopulation.First().ToColor();

                accentA = GetAccentColor(byPopulation, baseColor);
                accentB = GetSaturatedColor(byPopulation, baseColor, accentA);
            }

            int texWidth = _labelBackgroundTexture.width;
            int texHeight = _labelBackgroundTexture.height;

            RenderTexture previous = RenderTexture.active;
            var blitTarget = RenderTexture.GetTemporary(texWidth, texHeight, 0, RenderTextureFormat.ARGB32);
            RenderTexture.active = blitTarget;
            GL.Clear(true, true, Color.clear);

            BlitLabelSection(_labelBackgroundTexture, blitTarget, Color.white);
            BlitLabelSection(_labelAccentBottom, blitTarget, baseColor);
            BlitLabelSection(_labelAccentCenter, blitTarget, accentA);
            BlitLabelSection(_labelAccentTop, blitTarget, accentB);

            if (!_labelTextGizmo) CreateTextGizmo();
            _labelTextGizmo.SetActive(true);

            _themeNameTextMesh.color = GetOverlayTextColor(accentB);
            _themeNameTextMesh.text = theme.LocalizedName;

            _carTypesTextMesh.text = GetCarTypesText(themeName);

            _textCamera.targetTexture = blitTarget;
            _textCamera.Render();

            _labelTextGizmo.SetActive(false);

            var targetTex = new Texture2D(texWidth, texHeight, TextureFormat.ARGB32, false);
            targetTex.ReadPixels(new Rect(0, 0, texWidth, texHeight), 0, 0, false);
            targetTex.Apply();

            RenderTexture.ReleaseTemporary(blitTarget);
            RenderTexture.active = previous;

            labelMaterial.mainTexture = targetTex;
        }

        private static Color GetAccentColor(List<Palette.Swatch> swatches, Color contrastA)
        {
            // swatches ordered descending population
            Color accentColor = Color.white;
            float bestChoiceFactor = 0;

            for (int i = 1; i < swatches.Count; i++)
            {
                float popScale = 1f - ((float)i / swatches.Count);
                Color currentColor = swatches[i].ToColor();
                float choiceFactorA = currentColor.CalculateContrast(contrastA);
                float choiceFactor = popScale * choiceFactorA;

                if (choiceFactor > bestChoiceFactor)
                {
                    accentColor = currentColor;
                    bestChoiceFactor = choiceFactor;
                }
            }

            return accentColor;
        }

        private static Color GetSaturatedColor(IEnumerable<Palette.Swatch> swatches, Color contrastA, Color contrastB)
        {
            Color best = Color.white;
            float bestChoiceFactor = 0;

            foreach (var swatch in swatches)
            {
                Color current = swatch.ToColor();
                float choiceFactor = swatch.Hsl[1] * current.CalculateContrast(contrastA) * current.CalculateContrast(contrastB);

                if (choiceFactor > bestChoiceFactor)
                {
                    bestChoiceFactor = choiceFactor;
                    best = current;
                }
            }

            return best;
        }

        private const float _whiteBlackTextThreshold = 150 / 255f;
        private static Color _whiteTextColor = new Color(0.84f, 0.84f, 0.84f);

        private static Color GetOverlayTextColor(Color background)
        {
            float intensity = (background.r * 0.299f) + (background.g * 0.587f) + (background.b * 0.114f);
            return (intensity > _whiteBlackTextThreshold) ? Color.black : _whiteTextColor;
        }

        private static string GetCarTypesText(string themeName)
        {
            var carNames = SkinProvider.ThemeableSkinGroups
                .Where(g => g.Skins.Any(s => s.Name == themeName))
                .Select(g => LocalizationAPI.L(g.TrainCarType.localizationKey));

            return string.Join("\n", carNames);
        }

        private static void WritePaletteBmp(Palette palette, string themeName)
        {
            var tex = new Texture2D(palette.mSwatches.Count, 2, TextureFormat.ARGB32, false);

            var byPopulation = palette.mSwatches
                .OrderByDescending(s => s.Population);

            var mostUsedColor = byPopulation.First().ToColor();

            var pixels = palette.mSwatches
                .OrderByDescending(s => s.Population * s.ToColor().CalculateContrast(mostUsedColor))
                .Select(s => s.ToColor());

            pixels = byPopulation
                .Select(s => s.ToColor())
                .Concat(pixels);

            tex.SetPixels(pixels.ToArray());
            tex.Apply();

            TextureUtility.SaveTextureAsPNG(tex, Path.Combine(Main.ExportFolderPath, "_Palette", $"{themeName}_palette.png"));
        }

        public static Palette GenPalette(string themeName)
        {
            SkinProvider.TryGetTheme(themeName, out var theme);

            var bodySubstitution = theme.substitutions.OrderBy(s => s.original.name)
                .FirstOrDefault(s => s.original.name.Contains("Body"));

            return Palette.Generate((Texture2D)bodySubstitution.substitute.mainTexture, 8);
        }

        private static void BlitLabelSection(Texture2D section, RenderTexture target, Color tint)
        {
            _blittingMaterial.color = tint;
            Graphics.Blit(section, target, _blittingMaterial);
        }

        private static void CreateTextGizmo()
        {
            _labelTextGizmo = new GameObject("[SM] Label Gizmo");

            var cameraHolder = new GameObject("[SM] Label Camera");
            cameraHolder.transform.SetParent(_labelTextGizmo.transform, false);
            _textCamera = cameraHolder.AddComponent<Camera>();
            _textCamera.enabled = false;
            _textCamera.clearFlags = CameraClearFlags.Nothing;
            _textCamera.orthographic = true;

            var canvasHolder = new GameObject("[SM] Label Canvas");
            canvasHolder.transform.SetParent(_labelTextGizmo.transform, false);

            var rect = canvasHolder.AddComponent<RectTransform>();
            var canvas = canvasHolder.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = _textCamera;

            // Theme Name
            var textHolder = new GameObject("[SM] Label TextMesh");
            textHolder.transform.SetParent(canvasHolder.transform, false);

            var halfVector = new Vector2(0.5f, 0.5f);
            if (!_labelFont)
            {
                _labelFont = Resources.FindObjectsOfTypeAll<TMP_FontAsset>().First(f => f.name == "NotoSans-Regular__MAIN__LFS");
            }

            rect = textHolder.AddComponent<RectTransform>();
            rect.anchorMin = halfVector;
            rect.anchorMax = halfVector;
            rect.pivot = halfVector;
            rect.localPosition = new Vector3(178, -13, 0);
            rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 600);
            rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 98);

            textHolder.AddComponent<CanvasRenderer>();
            _themeNameTextMesh = textHolder.AddComponent<TextMeshProUGUI>();
            _themeNameTextMesh.font = _labelFont;
            _themeNameTextMesh.fontStyle = FontStyles.UpperCase;
            _themeNameTextMesh.enableAutoSizing = true;
            _themeNameTextMesh.alignment = TextAlignmentOptions.Center;
            _themeNameTextMesh.text = "Test Paint Don't Eat";

            // Car Types
            textHolder = new GameObject("[SM] Label CarTypes");
            textHolder.transform.SetParent(canvasHolder.transform, false);

            rect = textHolder.AddComponent<RectTransform>();
            rect.anchorMin = halfVector;
            rect.anchorMax = halfVector;
            rect.pivot = halfVector;
            rect.localPosition = new Vector3(-266, 120, 0);
            rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 200);
            rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 250);

            textHolder.AddComponent<CanvasRenderer>();
            _carTypesTextMesh = textHolder.AddComponent<TextMeshProUGUI>();
            _carTypesTextMesh.font = _labelFont;
            _carTypesTextMesh.fontStyle = FontStyles.UpperCase;
            _carTypesTextMesh.fontSizeMin = 20;
            _carTypesTextMesh.fontSizeMax = 38;
            _carTypesTextMesh.enableAutoSizing = true;
            _carTypesTextMesh.textWrappingMode = TextWrappingModes.Normal;
            _carTypesTextMesh.alignment = TextAlignmentOptions.TopLeft;
            _carTypesTextMesh.color = Color.black;
        }

        public static bool ShopDataInjected { get; private set; } = false;
        private static GameObject _dummyItemSpecHolder;
        private static readonly Dictionary<string, CustomPaintInventorySpec> _dummyItemSpecs = new Dictionary<string, CustomPaintInventorySpec>();

        public static CustomPaintInventorySpec GetDummyItemSpec(string themeName) => _dummyItemSpecs[themeName];

        public static void InjectShopData()
        {
            if (ShopDataInjected) return;
            Main.Log("Injecting global shop data");

            _dummyItemSpecHolder = new GameObject("[SM] Dummy Item Holder");

            foreach (var theme in SkinProvider.PaintThemes.Where(SkinProvider.IsThemeAllowedInStore))
            {
                var subHolder = new GameObject($"[SM] Dummy Item Spec {theme.name}");
                subHolder.transform.SetParent(_dummyItemSpecHolder.transform, false);

                // dummy item spec
                var canFab = Resources.Load<GameObject>(DEFAULT_CAN_PREFAB_NAME);
                var originalItemSpec = canFab.GetComponent<InventoryItemSpec>();
                var newItemSpec = CustomPaintInventorySpec.Create(originalItemSpec, subHolder, theme);

                _dummyItemSpecs.Add(theme.name, newItemSpec);

                // global shop data
                Main.LogVerbose($"Injecting shop data for theme {theme.name}");

                var newShopData = new ShopItemData()
                {
                    item = newItemSpec,
                    shelfItem = DefaultCanShopData.shelfItem,

                    amount = 20,
                    basePrice = DefaultCanShopData.basePrice,

                    isGlobal = true,
                    careerOnly = false,
                };

                GlobalShopController.Instance.shopItemsData.Add(newShopData);
            }

            ShopDataInjected = true;
        }

        public static void DestroyInjectedShopData()
        {
            if (!ShopDataInjected) return;
            Main.Log("Destroying global shop data");

            _dummyItemSpecs.Clear();
            UnityEngine.Object.Destroy(_dummyItemSpecHolder);

            ShopDataInjected = false;
        }
    }
}
