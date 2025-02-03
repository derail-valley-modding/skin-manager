using Microsoft.SqlServer.Server;
using SMShared.Json;
using System;
using System.IO;
using System.Threading.Tasks;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;

namespace SkinManagerMod
{
    public static class TextureLoader
    {
        public static void BustCache(ResourceConfigJson skin, string texturePath)
        {
            var cached = new FileInfo(GetCachePath(skin, texturePath));
            if (cached.Exists)
            {
                try
                {
                    cached.Delete();
                }
                catch { }
            }
        }

        private static Task<Texture2D?> TryLoadFromCache(ResourceConfigJson skin, string texturePath, bool isNormalMap)
        {
            var texFile = new FileInfo(texturePath);
            var cached = new FileInfo(GetCachePath(skin, texturePath));

            if (!cached.Exists)
            {
                return Task.FromResult<Texture2D?>(null);
            }

            if (cached.LastWriteTimeUtc < texFile.LastWriteTimeUtc)
            {
                Main.LogVerbose($"Cached texture {cached.FullName} is out of date");
                BustCache(skin, texturePath);
                return Task.FromResult<Texture2D?>(null);
            }

            try
            {
                return DDSUtils.ReadDDSGz(cached, isNormalMap);
            }
            catch (DDSReadException e)
            {
                Main.Warning($"Error loading cached texture {cached.FullName}: {e.Message}");
                BustCache(skin, texturePath);
                return Task.FromResult<Texture2D?>(null);
            }
        }

        public static Task<Texture2D?> LoadAsync(ResourceConfigJson skin, string texturePath, bool isNormalMap)
        {
            var cached = TryLoadFromCache(skin, texturePath, isNormalMap);
            if (!cached.IsCompleted || cached.Result != null)
            {
                return cached;
            }

            var info = StbImage.GetImageInfo(texturePath);
            var format = isNormalMap ? TextureFormat.BC5 :
                info.componentCount > 3 ? TextureFormat.DXT5 :
                TextureFormat.DXT1;

            var texture = new Texture2D(info.width, info.height, format,
                mipChain: true, linear: isNormalMap);

            var loader = new UncachedReader(skin, texturePath, format, texture);
            return loader.Dispatch();
        }

        public static Texture2D LoadSync(ResourceConfigJson _, string texturePath, bool isNormalMap)
        {
            var textureFormat = TextureFormat.RGBA32;
            var texture = new Texture2D(0, 0, textureFormat, mipChain: true, linear: isNormalMap);
            Main.LogVerbose($"Loading texture {texturePath} as {textureFormat} with LoadImage");
            texture.LoadImage(File.ReadAllBytes(texturePath));

            return texture;
        }

        private static string GetCachePath(ResourceConfigJson skin, string texturePath)
        {
            // SkinManagerMod/Cache/<carId>/<skinName>/<textureName>.dds.gz
            string cacheFileName = Path.GetFileNameWithoutExtension(texturePath) + ".dds.gz";
            return Path.Combine(Main.CacheFolderPath, skin.CarId, skin.Name, cacheFileName);
        }

        private static void PopulateTexture(string path, TextureFormat textureFormat, NativeArray<byte> dest)
        {
            var format = textureFormat switch
            {
                TextureFormat.DXT1 => StbImage.TextureFormat.BC1,
                TextureFormat.DXT5 => StbImage.TextureFormat.BC3,
                TextureFormat.BC5 => StbImage.TextureFormat.BC5,
                _ => throw new ArgumentException("textureFormat", $"Unsupported TextureFormat {textureFormat}"),
            };
            unsafe
            {
                StbImage.ReadAndCompressImageWithMipmaps(
                    path,
                    flipVertically: true,
                    format,
                    (IntPtr)dest.GetUnsafePtr(),
                    dest.Length);
            }
        }

        internal class UncachedReader
        {
            private ResourceConfigJson _package;
            private string _texturePath;
            private TextureFormat _format;
            private Texture2D _texture;

            public UncachedReader(ResourceConfigJson package, string texturePath, TextureFormat format, Texture2D texture)
            {
                _package = package;
                _texturePath = texturePath;
                _format = format;
                _texture = texture;
            }

            public Task<Texture2D?> Dispatch()
            {
                return Task.Run(DoLoadAsync);
            }

            private Texture2D? DoLoadAsync()
            {
                Main.LogVerbose($"Loading texture {_texturePath} as {_format} with StbImage");

                var nativeArray = _texture.GetRawTextureData<byte>();
                PopulateTexture(_texturePath, _format, nativeArray);
                string cachePath = GetCachePath(_package, _texturePath);
                Directory.CreateDirectory(Path.GetDirectoryName(cachePath));
                DDSUtils.WriteDDSGz(new FileInfo(cachePath), _texture);
                return _texture;
            }
        }
    }
}