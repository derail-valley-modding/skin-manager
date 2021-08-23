using System;
using System.Runtime.InteropServices;

namespace SkinManagerMod
{
    public static class StbImage
    {
        private const string Lib = nameof(StbImage);

        [DllImport(Lib, EntryPoint = nameof(GetImageInfo))]
        private static extern int GetImageInfo(string filename, out uint x, out uint y, out uint comp);
        [DllImport(Lib, EntryPoint = nameof(ReadImageAsBCx))]
        private static extern int ReadImageAsBCx(string filename, int flipVertically, int useAlpha, IntPtr dest, int destSize);
        [DllImport(Lib, EntryPoint = nameof(ReadImageAsRGBA))]
        private static extern int ReadImageAsRGBA(string filename, int flipVertically, IntPtr dest, int destSize);

        public struct ImageInfo
        {
            public int height;
            public int width;
            public int componentCount;
        }

        public static ImageInfo GetImageInfo(string filename)
        {
            bool success = GetImageInfo(filename, out uint x, out uint y, out uint comp) != 0;
            if (!success)
                throw new Exception($"Unable to read {filename}");
            return new ImageInfo
            {
                height = (int)y,
                width = (int)x,
                componentCount = (int)comp,
            };
        }

        public static void ReadAndCompressImageWithMipmaps(string filename, bool flipVertically, bool useAlpha, IntPtr dest, int destSize)
        {
            bool success = ReadImageAsBCx(filename, flipVertically ? 1 : 0, useAlpha ? 1 : 0, dest, destSize) != 0;
            if (!success)
                throw new Exception($"Unable to read {filename}");
        }
    }
}