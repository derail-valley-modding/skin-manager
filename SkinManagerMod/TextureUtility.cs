using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace SkinManagerMod
{
    public static class TextureUtility
    {
        /// <summary>
        /// Export all textures associated with the given car type
        /// </summary>
        public static void DumpTextures(TrainCarType trainCarType)
        {
            MeshRenderer[] cmps;
            Dictionary<string, Texture> textureList;

            var obj = CarTypes.GetCarPrefab(trainCarType);

            var path = Path.Combine(Main.ModEntry.Path, "Exported", obj.name);

            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            cmps = obj.GetComponentsInChildren<MeshRenderer>();
            textureList = new Dictionary<string, Texture>();

            foreach (var cmp in cmps)
            {
                if (!cmp.material)
                {
                    continue;
                }

                var diffuse = GetMaterialTexture(cmp, "_MainTex");
                var normal = GetMaterialTexture(cmp, "_BumpMap");
                var specular = GetMaterialTexture(cmp, "_MetallicGlossMap");
                var emission = GetMaterialTexture(cmp, "_EmissionMap");

                ExportTexture(path, diffuse, textureList);
                ExportTexture(path, normal, textureList, true);
                ExportTexture(path, specular, textureList);
                ExportTexture(path, emission, textureList);
            }

            var trainCar = obj.GetComponent<TrainCar>();

            if (trainCar.interiorPrefab != null)
            {
                cmps = trainCar.interiorPrefab.GetComponentsInChildren<MeshRenderer>();
                textureList = new Dictionary<string, Texture>();

                foreach (var cmp in cmps)
                {
                    if (!cmp.material)
                    {
                        continue;
                    }

                    var diffuse = GetMaterialTexture(cmp, "_MainTex");
                    var normal = GetMaterialTexture(cmp, "_BumpMap");
                    var specular = GetMaterialTexture(cmp, "_MetallicGlossMap");
                    var emission = GetMaterialTexture(cmp, "_EmissionMap");

                    ExportTexture(path, diffuse, textureList);
                    ExportTexture(path, normal, textureList, true);
                    ExportTexture(path, specular, textureList);
                    ExportTexture(path, emission, textureList);
                }
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

        /// <summary>
        /// Set anisoLevel of imported textures to 5 for better visuals. (3 gives barely an improvement, anything over 9 is pointless)
        /// </summary>
        public static void SetTextureOptions(Texture2D tex)
        {
            if (!Main.Settings.aniso5) return;
            tex.anisoLevel = 5;
        }
    }
}
