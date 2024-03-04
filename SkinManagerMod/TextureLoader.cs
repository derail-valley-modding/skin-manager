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
                try
                {
                    cached.Delete();
                }
                catch { }
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

        public static Task<Texture2D> LoadAsync(ResourceConfigJson skin, string texturePath, bool isNormalMap)
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
            var nativeArray = texture.GetRawTextureData<byte>();
            return Task.Run(() =>
            {
                PopulateTexture(texturePath, format, nativeArray);
                string cachePath = GetCachePath(skin, texturePath);
                Directory.CreateDirectory(Path.GetDirectoryName(cachePath));
                DDSUtils.WriteDDSGz(new FileInfo(cachePath), texture);
                return texture;
            });
        }

        public static Texture2D LoadSync(ResourceConfigJson skin, string texturePath, bool isNormalMap)
        {
            var texture = new Texture2D(0, 0, textureFormat: TextureFormat.RGBA32, mipChain: true, linear: isNormalMap);
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
            StbImage.TextureFormat format;
            switch (textureFormat)
            {
                case TextureFormat.DXT1:
                    format = StbImage.TextureFormat.BC1;
                    break;
                case TextureFormat.DXT5:
                    format = StbImage.TextureFormat.BC3;
                    break;
                case TextureFormat.BC5:
                    format = StbImage.TextureFormat.BC5;
                    break;
                default:
                    throw new ArgumentException("textureFormat", $"Unsupported TextureFormat {textureFormat}");
            }

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
    }

    internal class DDSReadException : Exception
    {
        public DDSReadException(string message) : base(message) { }
    }

    internal static class DDSUtils
    {
        private static int Mipmap0SizeInBytes(int width, int height, TextureFormat textureFormat)
        {
            var blockWidth = (width + 3) / 4;
            var blockHeight = (height + 3) / 4;
            int bytesPerBlock;
            switch (textureFormat)
            {
                case TextureFormat.DXT1:
                    bytesPerBlock = 8;
                    break;
                case TextureFormat.DXT5:
                case TextureFormat.BC5:
                    bytesPerBlock = 16;
                    break;
                default:
                    throw new ArgumentException("textureFormat", $"Unsupported TextureFormat {textureFormat}");
            }

            return blockWidth * blockHeight * bytesPerBlock;
        }

        private static byte[] DDSHeader(int width, int height, TextureFormat textureFormat, int numMipmaps)
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
                stream.Write(BitConverter.GetBytes(Mipmap0SizeInBytes(width, height, textureFormat)), 0, 4); // dwPitchOrLinearSize
                stream.Write(BitConverter.GetBytes(0), 0, 4); // dwDepth
                stream.Write(BitConverter.GetBytes(numMipmaps), 0, 4); // dwMipMapCount
                for (int i = 0; i < 11; i++)
                    stream.Write(BitConverter.GetBytes(0), 0, 4); // dwReserved1
                var pixelFormat = PixelFormat(textureFormat);
                stream.Write(pixelFormat, 0, pixelFormat.Length);
                // dwCaps = COMPLEX | MIPMAP | TEXTURE
                stream.Write(BitConverter.GetBytes(0x401008), 0, 4);
            }
            return header;
        }

        private static byte[] PixelFormat(TextureFormat textureFormat)
        {
            string fourCC;
            switch (textureFormat)
            {
                case TextureFormat.DXT1:
                    fourCC = "DXT1";
                    break;
                case TextureFormat.DXT5:
                    fourCC = "DXT5";
                    break;
                default:
                    fourCC = "DX10";
                    break;
            }

            var pixelFormat = new byte[32];
            using (var stream = new MemoryStream(pixelFormat))
            {
                stream.Write(BitConverter.GetBytes(32), 0, 4); // dwSize
                stream.Write(BitConverter.GetBytes(0x4), 0, 4); // dwFlags = FOURCC
                stream.Write(Encoding.ASCII.GetBytes(fourCC), 0, 4); // dwFourCC
            }
            return pixelFormat;
        }

        private static int DXGIFormat(TextureFormat textureFormat)
        {
            switch (textureFormat)
            {
                case TextureFormat.BC5: return 83;
                default:
                    throw new ArgumentException("textureFormat", $"Unsupported TextureFormat {textureFormat}");
            }
        }

        private static byte[] DDSHeaderDXT10(TextureFormat textureFormat)
        {
            var headerDXT10 = new byte[20];
            using (var stream = new MemoryStream(headerDXT10))
            {
                stream.Write(BitConverter.GetBytes(DXGIFormat(textureFormat)), 0, 4); // dxgiFormat
                stream.Write(BitConverter.GetBytes(3), 0, 4); // resourceDimension = 3 = DDS_DIMENSION_TEXTURE2D
                stream.Write(BitConverter.GetBytes(0), 0, 4); // miscFlag
                stream.Write(BitConverter.GetBytes(1), 0, 4); // arraySize = 1
                stream.Write(BitConverter.GetBytes(0), 0, 4); // miscFlags2 = 0 = DDS_ALPHA_MODE_UNKNOWN
            }
            return headerDXT10;
        }

        public static void WriteDDSGz(FileInfo fileInfo, Texture2D texture)
        {
            Main.Log($"Writing to {fileInfo.FullName}");
            using (var fileStream = fileInfo.OpenWrite())
            using (var outfile = new GZipStream(fileStream, CompressionLevel.Optimal))
            {
                var header = DDSHeader(texture.width, texture.height, texture.format, texture.mipmapCount);
                outfile.Write(header, 0, header.Length);
                if (texture.format != TextureFormat.DXT1 && texture.format != TextureFormat.DXT5)
                {
                    // compressed formats other than DXT1-5 require DX10
                    var headerDXT10 = DDSHeaderDXT10(texture.format);
                    outfile.Write(headerDXT10, 0, headerDXT10.Length);
                }
                var data = texture.GetRawTextureData<byte>().ToArray();
                outfile.Write(data, 0, data.Length);
            }
        }

        private static Texture2D ReadDDSHeader(Stream infile, bool linear)
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
            TextureFormat textureFormat;
            switch (fourCC)
            {
                case "DXT1":
                    textureFormat = TextureFormat.DXT1;
                    break;
                case "DXT5":
                    textureFormat = TextureFormat.DXT5;
                    break;
                case "DX10":
                    // read DDS_HEADER_DXT10 header extension
                    bytesRead = infile.Read(buf, 0, 20);
                    if (bytesRead != 20)
                        throw new DDSReadException("Could not read DXT10 header from DDS file");
                    int dxgiFormat = BitConverter.ToInt32(buf, 0);
                    switch (dxgiFormat)
                    {
                        case 83:
                            textureFormat = TextureFormat.BC5;
                            break;
                        default:
                            throw new DDSReadException($"Unsupported DXGI_FORMAT {dxgiFormat}");
                    }
                    break;
                default:
                    throw new DDSReadException($"Unknown FourCC: {fourCC}");
            }

            var texture = new Texture2D(width, height, textureFormat, true, linear);
            return texture;
        }

        public static Task<Texture2D> ReadDDSGz(FileInfo fileInfo, bool linear)
        {
            FileStream fileStream = null;
            GZipStream infile = null;
            try
            {
                Main.LogVerbose($"Reading from {fileInfo.FullName}");
                fileStream = fileInfo.OpenRead();
                infile = new GZipStream(fileStream, CompressionMode.Decompress);

                var texture = ReadDDSHeader(infile, linear);
                var nativeArray = texture.GetRawTextureData<byte>();
                return Task.Run(() =>
                {
                    try
                    {
                        var buf = new byte[nativeArray.Length];
                        var bytesRead = infile.Read(buf, 0, nativeArray.Length);
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
                infile?.Close();
                fileStream?.Close();
                throw ex;
            }
        }
    }
}