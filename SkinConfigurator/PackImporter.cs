using SMShared;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SkinConfigurator
{
    internal static class PackImporter
    {
        public static SkinPackModel ImportFromFolder(string path)
        {
            var model = new SkinPackModel();
            model.ModInfoModel.BindingName = GetCleanSkinName(path);

            foreach (string skinFolder in EnumerateSkinFolders(path))
            {
                model.AddSkinConfig(skinFolder, TryExtractCarType(skinFolder));
            }

            return model;
        }

        private static readonly Regex _nexusNameRegex = new(@"^([\w ]+)(?:[-\d]+)$");

        private static string GetCleanSkinName(string folderPath)
        {
            string folderName = Path.GetFileName(folderPath);

            var nexusNameMatch = _nexusNameRegex.Match(folderName);
            if (nexusNameMatch.Success)
            {
                folderName = nexusNameMatch.Groups[1].Value;
            }

            return folderName.Replace('_', ' ');
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

        public static SkinPackModel ImportFromArchive(string archivePath)
        {
            using var stream = File.OpenRead(archivePath);
            using var archive = new ZipArchive(stream, ZipArchiveMode.Read);

            string tempFolder = ExtractArchiveToTemp(archive, Path.GetFileNameWithoutExtension(archivePath));
            return ImportFromFolder(tempFolder);
        }

        private static string ExtractArchiveToTemp(ZipArchive archive, string archiveName)
        {
            string destFolder = Path.Combine(Environment.CurrentDirectory, "Temp", archiveName);

            foreach (var entry in archive.Entries.Where(e => !string.IsNullOrWhiteSpace(e.Name)))
            {
                string destFile = Path.Combine(destFolder, entry.FullName);
                Directory.CreateDirectory(Path.GetDirectoryName(destFile)!);
                entry.ExtractToFile(destFile, true);
            }

            return destFolder;
        }
    }
}
