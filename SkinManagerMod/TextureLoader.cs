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
        public static Task<Texture2D> Add(FileInfo fileInfo, bool linear)
        {
            var result = TryLoadFromCache(fileInfo, linear);
            if (!result.IsCompleted || result.Result != null)
                return result;
            return Load(fileInfo, linear);
        }

        private static Task<Texture2D> TryLoadFromCache(FileInfo fileInfo, bool linear)
        {
            var cached = new FileInfo(GetCachePath(fileInfo.FullName));
            if (!cached.Exists)
                return Task.FromResult<Texture2D>(null);
            if (cached.LastWriteTimeUtc < fileInfo.LastWriteTimeUtc)
            {
                cached.Delete();
                return Task.FromResult<Texture2D>(null);
            }

            return DDSUtils.ReadDDSGz(cached, linear);
        }

        private static Task<Texture2D> Load(FileInfo fileInfo, bool linear)
        {
            var info = StbImage.GetImageInfo(fileInfo.FullName);
            var texture = new Texture2D(info.width, info.height,
                info.componentCount > 3 ? TextureFormat.DXT5 : TextureFormat.DXT1,
                mipChain: true, linear);
            var nativeArray = texture.GetRawTextureData<byte>();
            return Task.Run(() =>
            {
                PopulateTexture(fileInfo, info.componentCount > 3, nativeArray);
                var cachePath = GetCachePath(fileInfo.FullName);
                Directory.CreateDirectory(Path.GetDirectoryName(cachePath));
                DDSUtils.WriteDDSGz(new FileInfo(cachePath), texture);
                return texture;
            });
        }

        private static string GetCachePath(string path)
        {
            var sep = Path.DirectorySeparatorChar;
            var cacheDirName = Path.GetDirectoryName(path.Replace(sep + "Skins" + sep, sep + "Cache" + sep));
            var cacheFileName = Path.GetFileNameWithoutExtension(path) + ".dds.gz";
            return Path.Combine(cacheDirName, cacheFileName);
        }

        private static void PopulateTexture(FileInfo path, bool hasAlpha, NativeArray<byte> dest)
        {
            unsafe
            {
                StbImage.ReadAndCompressImageWithMipmaps(
                    path.FullName,
                    flipVertically: true,
                    useAlpha: hasAlpha,
                    (IntPtr)dest.GetUnsafePtr(),
                    dest.Length);
            }
        }
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
            var stream = new MemoryStream(header);
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
            stream.Close();
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
            var outfile = new GZipStream(fileInfo.OpenWrite(), CompressionLevel.Optimal);
            outfile.Write(DDSHeader(texture.width, texture.height, texture.format == TextureFormat.DXT5, texture.mipmapCount), 0, 128);
            var data = texture.GetRawTextureData<byte>().ToArray();
            Debug.Log($"Writing to {fileInfo.FullName}");
            outfile.Write(data, 0, data.Length);
            outfile.Close();
        }

        public static Task<Texture2D> ReadDDSGz(FileInfo fileInfo, bool linear)
        {
            var infile = new GZipStream(fileInfo.OpenRead(), CompressionMode.Decompress);
            var buf = new byte[4096];
            var bytesRead = infile.Read(buf, 0, 128);
            if (bytesRead != 128 || Encoding.ASCII.GetString(buf, 0, 4) != "DDS ")
                throw new Exception("File is not a DDS file");

            int height = BitConverter.ToInt32(buf, 12);
            int width = BitConverter.ToInt32(buf, 16);

            int pixelFormatFlags = BitConverter.ToInt32(buf, 80);
            if ((pixelFormatFlags & 0x4) == 0)
                throw new Exception("DDS header does not have a FourCC");
            string fourCC = Encoding.ASCII.GetString(buf, 84, 4);
            TextureFormat pixelFormat;
            switch (fourCC)
            {
                case "DXT1": pixelFormat = TextureFormat.DXT1; break;
                case "DXT5": pixelFormat = TextureFormat.DXT5; break;
                default    :  throw new Exception($"Unknown FourCC: {fourCC}");
            }

            var texture = new Texture2D(width, height, pixelFormat, true, linear);
            var nativeArray = texture.GetRawTextureData<byte>();
            return Task.Run(() =>
            {
                buf = new byte[nativeArray.Length];
                bytesRead = infile.Read(buf, 0, nativeArray.Length);
                if (bytesRead < nativeArray.Length)
                    throw new Exception($"{fileInfo.FullName}: Expected {nativeArray.Length} bytes, but file contained {bytesRead}");
                nativeArray.CopyFrom(buf);
                return texture;
            });
        }
    }
}