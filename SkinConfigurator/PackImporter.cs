using SMShared;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SkinConfigurator
{
    internal static class PackImporter
    {
        public static SkinPackModel ImportFromFolder(string path)
        {
            var model = new SkinPackModel();
            model.ModInfoModel.BindingName = Path.GetFileName(path);

            foreach (string skinFolder in EnumerateSkinFolders(path))
            {
                var skin = new SkinConfigModel(skinFolder)
                {
                    BindingCarId = TryExtractCarType(skinFolder)
                };

                model.SkinConfigModels.Add(skin);
            }

            return model;
        }

        private static IEnumerable<string> EnumerateSkinFolders(string path)
        {
            if (Directory.EnumerateFiles(path).Any(f => Constants.IsSupportedExtension(Path.GetExtension(f))))
            {
                yield return path;
            }

            foreach (string subDir in Directory.EnumerateDirectories(path))
            {
                foreach (string skinFolder in EnumerateSkinFolders(subDir))
                {
                    yield return skinFolder;
                }
            }
        }

        private static string? TryExtractCarType(string skinFolder)
        {
            string? parentName = Path.GetFileName(Path.GetDirectoryName(skinFolder));
            if (parentName == null) return null;

            if (Remaps.TryGetUpdatedCarId(parentName, out var carId))
            {
                return carId;
            }

            if (Constants.LiveryNames.Contains(parentName))
            {
                return parentName;
            }

            return null;
        }
    }
}
