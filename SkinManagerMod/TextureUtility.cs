using DV.Customization.Paint;
using DV.ThingTypes;
using SMShared.Json;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace SkinManagerMod
{
    public static class TextureUtility
    {
        public static class PropNames
        {
            public static readonly string Main = "_MainTex";
            public static readonly string BumpMap = "_BumpMap";
            public static readonly string MetalGlossMap = "_MetallicGlossMap";
            public static readonly string EmissionMap = "_EmissionMap";
            public static readonly string OcclusionMap = "_OcclusionMap";

            public static readonly string DetailAlbedo = "_DetailAlbedoMap";
            public static readonly string DetailNormal = "_DetailNormalMap";

            public static readonly string[] UniqueTextures =
            {
                Main, BumpMap, MetalGlossMap, EmissionMap, DetailAlbedo, DetailNormal
            };

            public static readonly string[] AllTextures =
            {
                Main, BumpMap, MetalGlossMap, EmissionMap, OcclusionMap, DetailAlbedo, DetailNormal
            };

            public static readonly string[] DetailTextures =
            {
                DetailAlbedo, DetailNormal,
            };
        }

        /// <summary>
        /// Export all textures associated with the given car type
        /// </summary>
        public static void DumpTextures(TrainCarLivery trainCarType)
        {
            var path = Main.GetExportFolderForCar(trainCarType.id);

            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            var renderers = GetAllCarRenderers(trainCarType);
            var alreadyExported = new Dictionary<string, Texture>();
            var materialNames = new HashSet<string>();

            foreach (var renderer in renderers)
            {
                if (!renderer.sharedMaterial)
                {
                    continue;
                }

                ExportTexturesForMaterial(path, renderer.sharedMaterial, alreadyExported);

                string cleanName = renderer.sharedMaterial.name.Replace(" (Instance)", string.Empty);
                materialNames.Add(cleanName);
                Main.LogVerbose($"{renderer.name}: {cleanName}");
            }

            foreach (var themeName in SkinProvider.BuiltInThemeNames)
            {
                if (themeName == SkinProvider.DefaultThemeName) continue;

                string subPath = Path.Combine(path, themeName);
                if (!Directory.Exists(subPath))
                {
                    Directory.CreateDirectory(subPath);
                }

                var theme = SkinProvider.GetTheme(themeName);
                foreach (var substitution in theme.substitutions.Where(s => s.substitute && materialNames.Contains(s.original.name)))
                {
                    ExportTexturesForMaterial(subPath, substitution.substitute, alreadyExported);
                }
            }
        }

        private static void ExportTexturesForMaterial(string path, Material material, Dictionary<string, Texture> alreadyExported)
        {
            var diffuse = GetMaterialTexture(material, PropNames.Main);
            var normal = GetMaterialTexture(material, PropNames.BumpMap);
            var specular = GetMaterialTexture(material, PropNames.MetalGlossMap);
            var emission = GetMaterialTexture(material, PropNames.OcclusionMap);

            ExportTexture(path, diffuse, alreadyExported);
            ExportTexture(path, normal, alreadyExported, true);
            ExportTexture(path, specular, alreadyExported);
            ExportTexture(path, emission, alreadyExported);

            var detailAlbedo = GetMaterialTexture(material, PropNames.DetailAlbedo);
            var detailBump = GetMaterialTexture(material, PropNames.DetailNormal);

            ExportTexture(path, detailAlbedo, alreadyExported);
            ExportTexture(path, detailBump, alreadyExported, true);
        }

        private static void ExportTexture(string path, Texture2D texture, Dictionary<string, Texture> alreadyExported, bool isNormal = false)
        {
            if (texture != null && !alreadyExported.ContainsKey(texture.name))
            {
                alreadyExported.Add(texture.name, texture);

                var tex = DuplicateTexture(texture, isNormal);

                if (isNormal)
                {
                    tex = DTXnm2RGBA(tex);
                }

                SaveTextureAsPNG(tex, Path.Combine(path, $"{texture.name}.png"));
            }
        }

        /// <summary>
        /// Create a readable copy of a texture for export
        /// </summary>
        private static Texture2D DuplicateTexture(Texture2D source, bool isNormal)
        {
            RenderTexture renderTex;
            Texture2D readableText;

            if (isNormal)
            {
                readableText = new Texture2D(source.width, source.height, TextureFormat.ARGB32, true, true);
                renderTex = RenderTexture.GetTemporary(
                            source.width,
                            source.height,
                            0,
                            RenderTextureFormat.ARGB32,
                            RenderTextureReadWrite.Linear);
            }
            else
            {
                readableText = new Texture2D(source.width, source.height);
                renderTex = RenderTexture.GetTemporary(
                            source.width,
                            source.height,
                            0,
                            RenderTextureFormat.Default);
            }

            Graphics.Blit(source, renderTex);
            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = renderTex;
            readableText.ReadPixels(new Rect(0, 0, renderTex.width, renderTex.height), 0, 0);
            readableText.Apply();
            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(renderTex);
            return readableText;
        }

        public static void SaveTextureAsPNG(Texture2D _texture, string _fullPath)
        {
            byte[] _bytes = _texture.EncodeToPNG();
            File.WriteAllBytes(_fullPath, _bytes);
            Debug.Log(_bytes.Length / 1024 + "Kb was saved as: " + _fullPath);
        }

        /// <summary>
        /// Convert a DXT compressed texture to uncompressed RGBA format
        /// </summary>
        public static Texture2D DTXnm2RGBA(Texture2D tex)
        {
            Color[] colors = tex.GetPixels();
            for (int i = 0; i < colors.Length; i++)
            {
                Color c = colors[i];
                c.r = c.a * 2 - 1;  //red<-alpha (x<-w)
                c.g = c.g * 2 - 1; //green is always the same (y)
                Vector2 xy = new Vector2(c.r, c.g); //this is the xy vector
                c.b = Mathf.Sqrt(1 - Mathf.Clamp01(Vector2.Dot(xy, xy))); //recalculate the blue channel (z)
                colors[i] = new Color(c.r * 0.5f + 0.5f, c.g * 0.5f + 0.5f, c.b * 0.5f + 0.5f); //back to 0-1 range
            }
            tex.SetPixels(colors); //apply pixels to the texture
            tex.Apply();
            return tex;
        }

        
        /// <summary>
        /// Get the named 2D texture property from a material
        /// </summary>
        public static Texture2D GetMaterialTexture(Material material, string propName)
        {
            if (!material || !material.HasProperty(propName))
            {
                return null;
            }

            return material.GetTexture(propName) as Texture2D;
        }

        public static IEnumerable<Texture2D> EnumerateTextures(IEnumerable<MeshRenderer> renderers)
        {
            foreach (var renderer in renderers)
            {
                if (!renderer.material) continue;

                foreach (string textureName in PropNames.UniqueTextures)
                {
                    if (GetMaterialTexture(renderer.sharedMaterial, textureName) is Texture2D texture)
                    {
                        yield return texture;
                    }
                }
            }
        }

        public static IEnumerable<Texture2D> EnumerateTextures(IEnumerable<Material> materials)
        {
            foreach (var material in materials)
            {
                foreach (string textureName in PropNames.UniqueTextures)
                {
                    if (GetMaterialTexture(material, textureName) is Texture2D texture)
                    {
                        yield return texture;
                    }
                }
            }
        }

        public static IEnumerable<Texture2D> EnumerateTextures(TrainCarLivery livery)
        {
            var renderers = GetAllCarRenderers(livery);
            return EnumerateTextures(renderers);
        }

        /// <summary>
        /// Aggregate all renderers assigned to a car variant's prefab, interior, and interactables
        /// </summary>
        public static IEnumerable<MeshRenderer> GetAllCarRenderers(TrainCarLivery carType)
        {
            IEnumerable<MeshRenderer> cmps = carType.prefab.gameObject.GetComponentsInChildren<MeshRenderer>();

            if (carType.interiorPrefab != null)
            {
                var interiorCmps = carType.interiorPrefab.GetComponentsInChildren<MeshRenderer>();
                cmps = cmps.Concat(interiorCmps);
            }
            if (carType.externalInteractablesPrefab != null)
            {
                var interactCmps = carType.externalInteractablesPrefab.GetComponentsInChildren<MeshRenderer>();
                cmps = cmps.Concat(interactCmps);
            }

            return cmps;
        }

        /// <summary>
        /// Get all textures assigned to each renderers, and which material property they are assigned to
        /// </summary>
        /// <returns>Dictionary of texture name to material property id</returns>
        public static Dictionary<string, string> GetRendererTextureNames(IEnumerable<MeshRenderer> renderers)
        {
            var dict = new Dictionary<string, string>();

            foreach (var renderer in renderers)
            {
                if (!renderer.material) continue;

                foreach (string textureProperty in PropNames.UniqueTextures)
                {
                    if (GetMaterialTexture(renderer.sharedMaterial, textureProperty) is Texture2D texture)
                    {
                        dict[texture.name] = textureProperty;
                    }
                }
            }

            return dict;
        }

        /// <summary>
        /// Set anisoLevel of imported textures to 5 for better visuals. (3 gives barely an improvement, anything over 9 is pointless)
        /// </summary>
        public static void SetTextureOptions(Texture2D tex)
        {
            if (!Main.Settings.aniso5) return;
            tex.anisoLevel = 5;
        }

        /// <summary>
        /// Actually assign applicable skin textures to a renderer, using default skin to supply fallbacks
        /// </summary>
        public static void ApplyTextures(MeshRenderer renderer, Skin skin, CarMaterialData defaultSkin)
        {
            var defaultData = defaultSkin.GetDataForMaterial(renderer.material);
            if (defaultData is null) return;

            var defaultMaterial = defaultData.GetMaterialForBaseTheme(skin.BaseTheme);

            foreach (var defaultTexture in defaultData.AllTextures)
            {
                if (skin.ContainsTexture(defaultTexture.TextureName))
                {
                    var skinTexture = skin.GetTexture(defaultTexture.TextureName);
                    renderer.material.SetTexture(defaultTexture.PropertyName, skinTexture.TextureData);

                    if (defaultTexture.PropertyName == PropNames.MetalGlossMap)
                    {
                        if (!GetMaterialTexture(renderer.material, PropNames.OcclusionMap))
                        {
                            renderer.material.SetTexture(PropNames.OcclusionMap, skinTexture.TextureData);
                        }
                    }
                }
                else
                {
                    var skinTexture = defaultMaterial.GetTexture(defaultTexture.PropertyName);
                    renderer.material.SetTexture(defaultTexture.PropertyName, skinTexture);

                    if (!skinTexture)
                    {
                        // demo bogies et al. don't have textures...
                        renderer.material.color = defaultMaterial.color;
                    }

                    if (defaultTexture.PropertyName == PropNames.MetalGlossMap)
                    {
                        if (!GetMaterialTexture(renderer.material, PropNames.OcclusionMap))
                        {
                            renderer.material.SetTexture(PropNames.OcclusionMap, skinTexture);
                        }
                    }
                }
            }
        }
    }
}
