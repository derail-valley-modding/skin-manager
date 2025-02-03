using SMShared.Json;
using System;
using System.IO;
using UnityEngine;

namespace SkinManagerMod
{
    public class ThemeSettings
    {
        public readonly string ThemeName;
        public Version HighestVersion;
        public bool HideFromStores;
        public bool PreventRandomSpawning;
        public float? CanPrice;

        public SkinTexture? CanLabel;
        public Color? LabelBaseColor;
        public Color? LabelAccentColorA;
        public Color? LabelAccentColorB;

        public ThemeSettings(string themeName, Version version)
        {
            ThemeName = themeName;
            HighestVersion = version;
        }

        public void Merge(ThemeSettings other)
        {
            bool otherIsNewer = other.HighestVersion > HighestVersion;

            ReplaceIfNewer(ref HideFromStores, other.HideFromStores, otherIsNewer);
            ReplaceIfNewer(ref PreventRandomSpawning, other.PreventRandomSpawning, otherIsNewer);
            ReplaceIfNewer(ref CanPrice, other.CanPrice, otherIsNewer);
            ReplaceIfNewer(ref CanLabel, other.CanLabel, otherIsNewer);

            ReplaceIfNewer(ref LabelBaseColor, other.LabelBaseColor, otherIsNewer);
            ReplaceIfNewer(ref LabelAccentColorA, other.LabelAccentColorA, otherIsNewer);
            ReplaceIfNewer(ref LabelAccentColorB, other.LabelAccentColorB, otherIsNewer);
        }

        private static void ReplaceIfNewer<T>(ref T original, T other, bool otherIsNewer)
        {
            if (other == null) return;

            if ((original == null) || otherIsNewer)
            {
                original = other;
            }
        }

        public static ThemeSettings Create(string basePath, ThemeConfigItem data, Version version)
        {
            var result = new ThemeSettings(data.Name!, version)
            {
                HideFromStores = data.HideFromStores,
                PreventRandomSpawning = data.PreventRandomSpawning,
                CanPrice = data.CanPrice,
            };

            TryParseColor(data.LabelBaseColor, basePath, ref result.LabelBaseColor);
            TryParseColor(data.LabelAccentColorA, basePath, ref result.LabelAccentColorA);
            TryParseColor(data.LabelAccentColorB, basePath, ref result.LabelAccentColorB);

            if (!string.IsNullOrEmpty(data.LabelTextureFile))
            {
                string texturePath = Path.Combine(basePath, data.LabelTextureFile);

                if (File.Exists(texturePath))
                {
                    string texName = Path.GetFileNameWithoutExtension(texturePath);

                    try
                    {
                        var texture = new Texture2D(0, 0, TextureFormat.RGBA32, mipChain: true, linear: false);
                        texture.LoadImage(File.ReadAllBytes(texturePath));

                        result.CanLabel = new SkinTexture(texName, texture, File.GetLastWriteTime(texturePath));
                    }
                    catch (Exception ex)
                    {
                        Main.Error($"Failed to load can label texture {texturePath}: {ex.Message}");
                    }
                }
                else
                {
                    Main.Error($"Couldn't find can label texture file {texturePath} from theme config in {basePath}");
                }
            }

            return result;
        }

        private static void TryParseColor(string? value, string configPath, ref Color? result)
        {
            if (string.IsNullOrEmpty(value)) return;

            if (ColorUtility.TryParseHtmlString(value, out var color))
            {
                result = color;
                return;
            }

            Main.Warning($"Invalid color string in theme config in {configPath}");
            result = null;
        }
    }
}
