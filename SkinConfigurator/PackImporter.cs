﻿using SkinConfigurator.ViewModels;
using SMShared;
using SMShared.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SkinConfigurator
{
    internal static class PackImporter
    {
        public static SkinPackModel ImportFromFolder(string path)
        {
            if (Directory.EnumerateFiles(path, "Info.json", SearchOption.AllDirectories).Any())
            {
                return ImportSimulatorProject(path);
            }
            else
            {
                return ImportGenericFolder(path);
            }
        }

        #region Simulator Project

        private static SkinPackModel ImportSimulatorProject(string path)
        {
            var model = new SkinPackModel();
            
            string modInfoPath = Directory.EnumerateFiles(path, Constants.MOD_INFO_FILE, SearchOption.AllDirectories).First();
            using FileStream modInfoStream = File.OpenRead(modInfoPath);
            var modInfo = JsonSerializer.Deserialize<ModInfoJson>(modInfoStream, SkinPackager.JsonSettings);
            model.ModInfoModel = new SkinModInfoModel(modInfo!);

            // Parse resource configs
            var resources = new List<PackComponentModel>();
            foreach (string resourcePath in Directory.EnumerateFiles(path, Constants.SKIN_RESOURCE_FILE, SearchOption.AllDirectories))
            {
                using FileStream resourceStream = File.OpenRead(resourcePath);
                var resourceJson = JsonSerializer.Deserialize<ResourceConfigJson>(resourceStream, SkinPackager.JsonSettings)!;

                var resource = new PackComponentModel(PackComponentType.Resource, resourceJson);
                resource.AddItemsFromFolder(Path.GetDirectoryName(resourcePath)!);

                resources.Add(resource);
            }

            // Parse skin configs
            var skins = new List<PackComponentModel>();
            foreach (string skinPath in Directory.EnumerateFiles(path, Constants.SKIN_CONFIG_FILE, SearchOption.AllDirectories))
            {
                using FileStream skinStream = File.OpenRead(skinPath);
                var skinJson = JsonSerializer.Deserialize<SkinConfigJson>(skinStream, SkinPackager.JsonSettings)!;

                var skin = new PackComponentModel(PackComponentType.Skin, skinJson)
                {
                    Resources = resources.Where(r => skinJson.ResourceNames?.Contains(r.Name) ?? false).ToList()
                };
                skin.AddItemsFromFolder(Path.GetDirectoryName(skinPath)!);

                skins.Add(skin);
            }

            foreach (var resource in resources) model.AddSkinConfig(resource);
            foreach (var skin in skins) model.AddSkinConfig(skin);

            return model;
        }

        #endregion

        #region Legacy Folder

        private static SkinPackModel ImportGenericFolder(string path)
        {
            var model = new SkinPackModel();
            model.ModInfoModel.DisplayName = GetCleanSkinName(path);

            foreach (string skinFolder in EnumerateSkinFolders(path))
            {
                var skin = model.CreateSkinConfig(TryExtractCarType(skinFolder));
                skin.Name = Path.GetFileName(skinFolder).Replace('_', ' ');
                skin.AddItemsFromFolder(skinFolder);
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

        #endregion

        public static SkinPackModel ImportFromArchive(string archivePath)
        {
            using var stream = File.OpenRead(archivePath);
            using var archive = new ZipArchive(stream, ZipArchiveMode.Read);

            string tempFolder = ExtractArchiveToTemp(archive, Path.GetFileNameWithoutExtension(archivePath));
            var pack = ImportFromFolder(tempFolder);
            Directory.Delete(tempFolder, true);
            return pack;
        }

        private static string ExtractArchiveToTemp(ZipArchive archive, string archiveName)
        {
            string destFolder = Path.Combine(Environment.CurrentDirectory, "Temp", "Extract", archiveName);
            
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
