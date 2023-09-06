using SMShared.Json;
using System;
using System.IO;
using System.IO.Compression;
using System.Text;
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
                cached.Delete();
            }
        }

        private static Task<Texture2D> TryLoadFromCache(ResourceConfigJson skin, string texturePath, bool linear)
        {
            var texFile = new FileInfo(texturePath);
            var cached = new FileInfo(GetCachePath(skin, texturePath));

            if (!cached.Exists)
            {
                return Task.FromResult<Texture2D>(null);
            }

            if (cached.LastWriteTimeUtc < texFile.LastWriteTimeUtc)
            {
                cached.Delete();
                return Task.FromResult<Texture2D>(null);
            }

            try
            {
                return DDSUtils.ReadDDSGz(cached, linear);
            }
            catch (DDSReadException e)
            {
                Main.Warning($"Error loading cached skin {skin.Name}: {e.Message}");
                BustCache(skin, texturePath);
                return Task.FromResult<Texture2D>(null);
            }
        }

        public static Task<Texture2D> LoadAsync(ResourceConfigJson skin, string texturePath, bool linear)
        {
            var cached = TryLoadFromCache(skin, texturePath, linear);
            if (!cached.IsCompleted || cached.Result != null)
            {
                return cached;
            }

            var info = StbImage.GetImageInfo(texturePath);
            var texture = new Texture2D(info.width, info.height,
                info.componentCount > 3 ? TextureFormat.DXT5 : TextureFormat.DXT1,
                mipChain: true, linear);
            var nativeArray = texture.GetRawTextureData<byte>();
            return Task.Run(() =>
            {
                PopulateTexture(texturePath, info.componentCount > 3, nativeArray);
                string cachePath = GetCachePath(skin, texturePath);
                Directory.CreateDirectory(Path.GetDirectoryName(cachePath));
                DDSUtils.WriteDDSGz(new FileInfo(cachePath), texture);
                return texture;
            });
        }

        public static Texture2D LoadSync(ResourceConfigJson skin, string texturePath, bool linear)
        {
            var texture = new Texture2D(0, 0, textureFormat: TextureFormat.RGBA32, mipChain: true, linear: linear);
            texture.LoadImage(File.ReadAllBytes(texturePath));

            return texture;
        }

        private static string GetCachePath(ResourceConfigJson skin, string texturePath)
        {
            // SkinManagerMod/Cache/<carId>/<skinName>/<textureName>.dds.gz
            string cacheFileName = Path.GetFileNameWithoutExtension(texturePath) + ".dds.gz";
            return Path.Combine(Main.CacheFolderPath, skin.CarId, skin.Name, cacheFileName);
        }

        private static void PopulateTexture(string path, bool hasAlpha, NativeArray<byte> dest)
        {
            unsafe
            {
                StbImage.ReadAndCompressImageWithMipmaps(
                    path,
                    flipVertically: true,
                    useAlpha: hasAlpha,
                    (IntPtr)dest.GetUnsafePtr(),
                    dest.Length);
            }
        }
    }

    internal class DDSReadException : Exception
    {
        public DDSReadException(string message) : base(message) { }
    }

    internal static class DDSUtils
    {
        private static int Mipmap0SizeInBytes(int width, int height, bool hasAlpha)
        {
            var blockWidth = (width + 3) / 4;
            var blockHeight = (height + 3) / 4;
            return blockWidth * blockHeight * (hasAlpha ? 16 : 8);
        }

        private static byte[] DDSHeader(int width, int height, bool hasAlpha, int numMipmaps)
        {
            var header = new byte[128];
            using (var stream = new MemoryStream(header))
            {
                stream.Write(Encoding.ASCII.GetBytes("DDS "), 0, 4);
                stream.Write(BitConverter.GetBytes(124), 0, 4); // dwSize
                                                                // dwFlags = CAPS | HEIGHT | WIDTH | PIXELFORMAT | MIPMAPCOUNT | LINEARSIZE
                stream.Write(BitConverter.GetBytes(0x1 | 0x2 | 0x4 | 0x1000 | 0x20000 | 0x80000), 0, 4);
                stream.Write(BitConverter.GetBytes(height), 0, 4);
                stream.Write(BitConverter.GetBytes(width), 0, 4);
                stream.Write(BitConverter.GetBytes(Mipmap0SizeInBytes(width, height, hasAlpha)), 0, 4); // dwPitchOrLinearSize
                stream.Write(BitConverter.GetBytes(0), 0, 4); // dwDepth
                stream.Write(BitConverter.GetBytes(numMipmaps), 0, 4); // dwMipMapCount
                for (int i = 0; i < 11; i++)
                    stream.Write(BitConverter.GetBytes(0), 0, 4); // dwReserved1
                var pixelFormat = PixelFormat(hasAlpha);
                stream.Write(pixelFormat, 0, pixelFormat.Length);
                // dwCaps = COMPLEX | MIPMAP | TEXTURE
                stream.Write(BitConverter.GetBytes(0x401008), 0, 4);
            }
            return header;
        }

        private static byte[] PixelFormat(bool hasAlpha)
        {
            var pixelFormat = new byte[32];
            var stream = new MemoryStream(pixelFormat);
            stream.Write(BitConverter.GetBytes(32), 0, 4); // dwSize
            stream.Write(BitConverter.GetBytes(0x4), 0, 4); // dwFlags = FOURCC
            stream.Write(Encoding.ASCII.GetBytes(hasAlpha ? "DXT5" : "DXT1"), 0, 4); // dwFourCC
            stream.Close();
            return pixelFormat;
        }

        public static void WriteDDSGz(FileInfo fileInfo, Texture2D texture)
        {
            using (var fileStream = fileInfo.OpenWrite())
            using (var outfile = new GZipStream(fileStream, CompressionLevel.Optimal))
            {
                outfile.Write(DDSHeader(texture.width, texture.height, texture.format == TextureFormat.DXT5, texture.mipmapCount), 0, 128);
                var data = texture.GetRawTextureData<byte>().ToArray();
                Main.Log($"Writing to {fileInfo.FullName}");
                outfile.Write(data, 0, data.Length);
            }
        }

        public static Task<Texture2D> ReadDDSGz(FileInfo fileInfo, bool linear)
        {
            var fileStream = fileInfo.OpenRead();
            var infile = new GZipStream(fileStream, CompressionMode.Decompress);

            try
            {
                var buf = new byte[4096];
                var bytesRead = infile.Read(buf, 0, 128);
                if (bytesRead != 128 || Encoding.ASCII.GetString(buf, 0, 4) != "DDS ")
                    throw new DDSReadException("File is not a DDS file");

                int height = BitConverter.ToInt32(buf, 12);
                int width = BitConverter.ToInt32(buf, 16);

                int pixelFormatFlags = BitConverter.ToInt32(buf, 80);
                if ((pixelFormatFlags & 0x4) == 0)
                    throw new DDSReadException("DDS header does not have a FourCC");
                string fourCC = Encoding.ASCII.GetString(buf, 84, 4);
                TextureFormat pixelFormat;
                switch (fourCC)
                {
                    case "DXT1": pixelFormat = TextureFormat.DXT1; break;
                    case "DXT5": pixelFormat = TextureFormat.DXT5; break;
                    default: throw new DDSReadException($"Unknown FourCC: {fourCC}");
                }

                var texture = new Texture2D(width, height, pixelFormat, true, linear);
                var nativeArray = texture.GetRawTextureData<byte>();
                return Task.Run(() =>
                {
                    try
                    {
                        buf = new byte[nativeArray.Length];
                        bytesRead = infile.Read(buf, 0, nativeArray.Length);
                        if (bytesRead < nativeArray.Length)
                            throw new DDSReadException($"{fileInfo.FullName}: Expected {nativeArray.Length} bytes, but file contained {bytesRead}");
                        nativeArray.CopyFrom(buf);
                        return texture;
                    }
                    finally
                    {
                        infile.Close();
                        fileStream.Close();
                    }
                });
            }
            catch (Exception ex)
            {
                infile.Close();
                fileStream.Close();
                throw ex;
            }
        }
    }
}