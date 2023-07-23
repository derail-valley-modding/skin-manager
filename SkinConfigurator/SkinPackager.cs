using SMShared;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace SkinConfigurator
{
    internal class SkinPackager : IDisposable
    {
        private readonly SkinPackModel _model;
        private readonly FileStream _stream;
        private readonly ZipArchive _archive;

        private static readonly JsonSerializerOptions _serializeOptions = new()
        {
            WriteIndented = true,
            IncludeFields = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        };

        /// <exception cref="SkinPackageException"></exception>
        public static void SaveToArchive(string path, SkinPackModel model)
        {
            try
            {
                model.Trim();
                using var packager = new SkinPackager(path, model);
                packager.Package();
            }
            catch (Exception ex)
            {
                throw new SkinPackageException($"Error packaging skin mod {model.ModInfoModel.Id}: {ex.Message}", ex);
            }
        }

        private SkinPackager(string archivePath, SkinPackModel model)
        {
            if (File.Exists(archivePath))
            {
                File.Delete(archivePath);
            }

            _model = model;
            _stream = File.OpenWrite(archivePath);
            _archive = new ZipArchive(_stream, ZipArchiveMode.Create);
            _model = model;
        }

        public void Dispose()
        {
            _archive.Dispose();
            _stream.Dispose();
        }

        private void Package()
        {
            WriteModInfo(_model.ModInfoModel);

            foreach (var skin in _model.SkinConfigModels)
            {
                WriteSkin(skin);
            }
        }

        private void WriteModInfo(SkinModInfoModel model)
        {
            var entry = _archive.CreateEntry(Constants.MOD_INFO_FILE);
            using var stream = entry.Open();
            JsonSerializer.Serialize(stream, model, _serializeOptions);
            stream.Close();
        }

        private void WriteSkin(SkinConfigModel skin)
        {
            // skin.json
            string folderName = GetSkinFolderName(skin.Name, skin.CarId);
            var jsonEntry = _archive.CreateEntry(Path.Combine(folderName, Constants.SKIN_CONFIG_FILE));
            using var jsonStream = jsonEntry.Open();
            JsonSerializer.Serialize(jsonStream, skin, _serializeOptions);
            jsonStream.Close();

            // textures & whatever else
            foreach (var sourceFile in Directory.EnumerateFiles(skin.FolderPath))
            {
                string relativePath = GetTargetFileName(folderName, sourceFile, skin.CarId);
                _archive.CreateEntryFromFile(sourceFile, relativePath);
            }
        }

        private static string GetTargetFileName(string folderName, string sourcePath, string liveryId)
        {
            if (Constants.IsSupportedExtension(Path.GetExtension(sourcePath)))
            {
                string filename = Path.GetFileNameWithoutExtension(sourcePath);
                string extension = Path.GetExtension(sourcePath);

                if (Remaps.TryGetUpdatedTextureName(liveryId, filename, out string newName))
                {
                    return Path.Combine(folderName, string.Concat(newName, extension));
                }
            }

            return Path.Combine(folderName, Path.GetFileName(sourcePath));
        }

        private string GetSkinFolderName(string skinName, string liveryId)
        {
            if (_model.SkinConfigModels.Any(s => (s.Name == skinName) && (s.CarId != liveryId)))
            {
                // exporting same skin for another car type, prefix name
                return $"{skinName}_{liveryId}";
            }
            return skinName;
        }
    }

    internal class SkinPackageException : Exception
    {
        public SkinPackageException(string message) : base(message) { }

        public SkinPackageException(string message, Exception innerException) : base(message, innerException) { }
    }
}
