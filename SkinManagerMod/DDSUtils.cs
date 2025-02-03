using System;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace SkinManagerMod
{
    internal static class DDSUtils
    {
        private static int Mipmap0SizeInBytes(int width, int height, TextureFormat textureFormat)
        {
            var blockWidth = (width + 3) / 4;
            var blockHeight = (height + 3) / 4;
            var bytesPerBlock = textureFormat switch
            {
                TextureFormat.DXT1 => 8,
                TextureFormat.DXT5 or TextureFormat.BC5 => 16,
                _ => throw new ArgumentException($"Unsupported TextureFormat {textureFormat}", "textureFormat"),
            };
            return blockWidth * blockHeight * bytesPerBlock;
        }

        private const int DDS_HEADER_SIZE = 128;
        private const int DDS_HEADER_DXT10_SIZE = 20;
        private static byte[] DDSHeader(int width, int height, TextureFormat textureFormat, int numMipmaps)
        {
            var needsDXGIHeader = textureFormat != TextureFormat.DXT1 && textureFormat != TextureFormat.DXT5;
            var headerSize = needsDXGIHeader ? DDS_HEADER_SIZE + DDS_HEADER_DXT10_SIZE : DDS_HEADER_SIZE;
            var header = new byte[headerSize];
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
                // dwCaps2, dwCaps3, dwCaps4, dwReserved2
                for (int i = 0; i < 4; i++)
                    stream.Write(BitConverter.GetBytes(0), 0, 4);

                if (needsDXGIHeader)
                    stream.Write(DDSHeaderDXT10(textureFormat), 0, DDS_HEADER_DXT10_SIZE);
            }
            return header;
        }

        private static byte[] PixelFormat(TextureFormat textureFormat)
        {
            string fourCC = textureFormat switch
            {
                TextureFormat.DXT1 => "DXT1",
                TextureFormat.DXT5 => "DXT5",
                _ => "DX10",
            };

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
            return textureFormat switch
            {
                TextureFormat.BC5 => 83,
                _ => throw new ArgumentException("textureFormat", $"Unsupported TextureFormat {textureFormat}"),
            };
        }

        private static byte[] DDSHeaderDXT10(TextureFormat textureFormat)
        {
            var headerDXT10 = new byte[DDS_HEADER_DXT10_SIZE];
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

            using var fileStream = fileInfo.OpenWrite();
            using var outfile = new GZipStream(fileStream, CompressionLevel.Optimal);

            var header = DDSHeader(texture.width, texture.height, texture.format, texture.mipmapCount);
            outfile.Write(header, 0, header.Length);

            var data = texture.GetRawTextureData<byte>().ToArray();
            outfile.Write(data, 0, data.Length);
        }

        private static Texture2D ReadDDSHeader(Stream infile, bool linear)
        {
            var buf = new byte[4096];
            var bytesRead = infile.Read(buf, 0, DDS_HEADER_SIZE);
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
                    bytesRead = infile.Read(buf, 0, DDS_HEADER_DXT10_SIZE);
                    if (bytesRead != DDS_HEADER_DXT10_SIZE)
                        throw new DDSReadException("Could not read DXT10 header from DDS file");
                    int dxgiFormat = BitConverter.ToInt32(buf, 0);
                    textureFormat = dxgiFormat switch
                    {
                        83 => TextureFormat.BC5,
                        _ => throw new DDSReadException($"Unsupported DXGI_FORMAT {dxgiFormat}"),
                    };
                    break;
                default:
                    throw new DDSReadException($"Unknown FourCC: {fourCC}");
            }

            var texture = new Texture2D(width, height, textureFormat, true, linear);
            return texture;
        }

        public static Task<Texture2D?> ReadDDSGz(FileInfo fileInfo, bool isNormalMap)
        {
            FileStream? fileStream = null;
            GZipStream? zipStream = null;
            try
            {
                fileStream = fileInfo.OpenRead();
                zipStream = new GZipStream(fileStream, CompressionMode.Decompress);

                var texture = ReadDDSHeader(zipStream, isNormalMap);
                if (isNormalMap && texture.format != TextureFormat.BC5)
                {
                    Main.LogVerbose($"Cached normal map texture {fileInfo.FullName} has old format {texture.format}");
                    zipStream.Close();
                    fileStream.Close();
                    File.Delete(fileInfo.FullName);
                    return Task.FromResult<Texture2D?>(null);
                }

                Main.LogVerbose($"Reading cached {texture.format} texture from {fileInfo.FullName}");

                var loader = new CacheReader(fileInfo.FullName, texture, fileStream, zipStream);
                return loader.Dispatch();
            }
            catch (Exception ex)
            {
                zipStream?.Close();
                fileStream?.Close();
                throw ex;
            }
        }

        internal class CacheReader : IDisposable
        {
            public readonly string FileName;
            private readonly Texture2D _texture;
            private readonly FileStream _fileStream;
            private readonly GZipStream _zipstream;

            public CacheReader(string fileName, Texture2D target, FileStream fileStream, GZipStream zipStream)
            {
                FileName = fileName;
                _texture = target;
                _fileStream = fileStream;
                _zipstream = zipStream;
            }

            public Task<Texture2D?> Dispatch()
            {
                return Task.Run(DoWork);
            }

            public void Dispose()
            {
                _zipstream?.Dispose();
                _fileStream?.Dispose();
            }

            private Texture2D? DoWork()
            {
                try
                {
                    var nativeArray = _texture.GetRawTextureData<byte>();
                    var buf = new byte[nativeArray.Length];
                    var bytesRead = _zipstream.Read(buf, 0, nativeArray.Length);
                    if (bytesRead < nativeArray.Length)
                    {
                        Main.Error($"{FileName}: Expected {nativeArray.Length} bytes, but file contained {bytesRead}");
                        return null;
                    }
                    nativeArray.CopyFrom(buf);
                    return _texture;
                }
                catch (Exception ex)
                {
                    Main.Error($"Error while reading cache file {FileName}: {ex.Message}");
                    return null;
                }
                finally
                {
                    Dispose();
                }
            }
        }
    }

    internal class DDSReadException : Exception
    {
        public DDSReadException(string message) : base(message) { }
    }
}