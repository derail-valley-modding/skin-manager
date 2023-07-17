using DV.ThingTypes;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace SkinManagerMod
{
    public static class TextureUtility
    {
        private static readonly string[] standardShaderUniqueTextures = new[] { "_MainTex", "_BumpMap", "_MetallicGlossMap", "_EmissionMap" };
        private static readonly string[] standardShaderAllTextures = new[] { "_MainTex", "_BumpMap", "_MetallicGlossMap", "_EmissionMap", "_OcclusionMap" };
        private const string METAL_GLOSS_TEXTURE = "_MetallicGlossMap";
        private const string OCCLUSION_TEXTURE = "_OcclusionMap";

        /// <summary>
        /// Export all textures associated with the given car type
        /// </summary>
        public static void DumpTextures(TrainCarLivery trainCarType)
        {
            var path = Path.Combine(Main.Instance.Path, "Exported", trainCarType.id);

            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            var renderers = GetAllCarRenderers(trainCarType);
            var textureList = new Dictionary<string, Texture>();

            foreach (var renderer in renderers)
            {
                if (!renderer.material)
                {
                    continue;
                }

                var diffuse = GetMaterialTexture(renderer, "_MainTex");
                var normal = GetMaterialTexture(renderer, "_BumpMap");
                var specular = GetMaterialTexture(renderer, "_MetallicGlossMap");
                var emission = GetMaterialTexture(renderer, "_EmissionMap");

                ExportTexture(path, diffuse, textureList);
                ExportTexture(path, normal, textureList, true);
                ExportTexture(path, specular, textureList);
                ExportTexture(path, emission, textureList);
            }
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
        public static Texture2D GetMaterialTexture(MeshRenderer cmp, string materialName)
        {
            if (cmp.material == null || !cmp.material.HasProperty(materialName))
            {
                return null;
            }

            return cmp.material.GetTexture(materialName) as Texture2D;
        }

        public static IEnumerable<Texture2D> EnumerateTextures(IEnumerable<MeshRenderer> renderers)
        {
            foreach (var renderer in renderers)
            {
                if (!renderer.material) continue;

                foreach (string textureName in standardShaderUniqueTextures)
                {
                    if (GetMaterialTexture(renderer, textureName) is Texture2D texture)
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

                foreach (string textureProperty in standardShaderUniqueTextures)
                {
                    if (GetMaterialTexture(renderer, textureProperty) is Texture2D texture)
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
        public static void ApplyTextures(MeshRenderer renderer, Skin skin, Skin defaultSkin)
        {
            foreach (string textureID in standardShaderAllTextures)
            {
                var currentTexture = GetMaterialTexture(renderer, textureID);

                if (currentTexture != null)
                {
                    if (skin.ContainsTexture(currentTexture.name))
                    {
                        var skinTexture = skin.GetTexture(currentTexture.name);
                        renderer.material.SetTexture(textureID, skinTexture.TextureData);

                        if (textureID == METAL_GLOSS_TEXTURE)
                        {
                            if (!GetMaterialTexture(renderer, OCCLUSION_TEXTURE))
                            {
                                renderer.material.SetTexture(OCCLUSION_TEXTURE, skinTexture.TextureData);
                            }
                        }
                    }
                    else if ((defaultSkin != null) && defaultSkin.ContainsTexture(currentTexture.name))
                    {
                        var skinTexture = defaultSkin.GetTexture(currentTexture.name);
                        renderer.material.SetTexture(textureID, skinTexture.TextureData);

                        if (textureID == METAL_GLOSS_TEXTURE)
                        {
                            if (!GetMaterialTexture(renderer, OCCLUSION_TEXTURE))
                            {
                                renderer.material.SetTexture(OCCLUSION_TEXTURE, skinTexture.TextureData);
                            }
                        }
                    }
                }
            }
        }
    }
}
